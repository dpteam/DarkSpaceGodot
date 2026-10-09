using System;
using System.Collections.Generic;
using DarkSpace.Core.Data;
using DarkSpace.Core.Generation;
using DarkSpace.Core.Systems.Economy;
using DarkSpace.Core.Systems.Factions;
using DarkSpace.Core.Systems.Items;
using DarkSpace.Core.Systems.Travel;
using DarkSpace.Core.Utils;

namespace DarkSpace.Core.Simulation;

/// <summary>
/// Детерминированная тик-симуляция мира: единый источник истины для геймплея и мультиплеера.
/// </summary>
/// <remarks>
/// <para>
/// Симуляция работает «команда → тик»: клиенты/UI отправляют <see cref="IGameCommand"/>,
/// а <see cref="Advance"/> применяет накопленные команды в порядке поступления, затем выполняет
/// шаг физики на фиксированный интервал времени.
/// </para>
/// <para>
/// Детерминизм обязателен: одинаковый мировой сид + одинаковая последовательность команд
/// дают одинаковый результат. Это позволяет серверу быть авторитарным, а клиентам —
/// воспроизводить состояние (будущее предсказание). Здесь не используется <see cref="System.Random"/>
/// и нет зависимости от Godot.
/// </para>
/// </remarks>
public sealed class WorldSimulation
{
    /// <summary>Ускорение, придаваемое единичной тягой (в условных единицах в секунду²).</summary>
    public const double ThrustAcceleration = 24.0;

    /// <summary>Коэффициент линейного сопротивления среды (в секунду).</summary>
    public const double LinearDrag = 0.2;

    /// <summary>Стартовый баланс кредитов.</summary>
    public const double StartingCredits = 1000.0;

    /// <summary>Максимальный запас топлива.</summary>
    public const double MaxFuel = 100.0;

    /// <summary>Максимальное число сообщений чата, хранимых в снапшоте.</summary>
    public const int MaxChatLog = 50;

    private readonly Queue<IGameCommand> _pending = new();
    private readonly Dictionary<int, int> _cargo = new();
    private readonly List<string> _chatLog = new();
    private readonly Dictionary<FactionType, int> _reputation = new();

    private Vector2D _position = Vector2D.Zero;
    private Vector2D _velocity = Vector2D.Zero;
    private Vector2D _thrust = Vector2D.Zero;
    private ShipData? _ship;

    /// <summary>Мировой сид вселенной.</summary>
    public ulong WorldSeed { get; }

    /// <summary>Текущий номер тика.</summary>
    public long Tick { get; private set; }

    /// <summary>Баланс кредитов.</summary>
    public double Credits { get; private set; } = StartingCredits;

    /// <summary>Текущее топливо.</summary>
    public double Fuel { get; private set; } = MaxFuel;

    /// <summary>Индекс текущей галактики.</summary>
    public int GalaxyIndex { get; private set; }

    /// <summary>Индекс текущей звёздной системы.</summary>
    public int SystemIndex { get; private set; }

    /// <summary>Название текущей системы.</summary>
    public string SystemName { get; private set; } = "—";

    /// <summary>Экономика текущей системы.</summary>
    public EconomyType Economy { get; private set; } = EconomyType.Industrial;

    /// <summary>Контролирующая фракция текущей системы.</summary>
    public FactionType Faction { get; private set; } = FactionType.EmpireOfDawn;

    /// <summary>Уничтожен ли корабль (после неудачного прыжка).</summary>
    public bool Destroyed { get; private set; }

    /// <summary>Сколько команд находится в очереди на обработку.</summary>
    public int PendingCommandCount => _pending.Count;

    /// <summary>
    /// Создаёт новую симуляцию.
    /// </summary>
    /// <param name="worldSeed">Мировой сид вселенной.</param>
    public WorldSimulation(ulong worldSeed)
    {
        WorldSeed = worldSeed;
        SetCurrentSystem(0, 0);
    }

    /// <summary>
    /// Ставит команду в очередь. Команды применяются в порядке вызова при следующем <see cref="Advance"/>.
    /// </summary>
    /// <param name="command">Команда.</param>
    /// <exception cref="ArgumentNullException">Если <paramref name="command"/> равен <see langword="null"/>.</exception>
    public void Enqueue(IGameCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        _pending.Enqueue(command);
    }

    /// <summary>
    /// Перемещает симуляцию в заданную систему вселенной, обновляя экономику и фракцию.
    /// </summary>
    /// <param name="galaxyIndex">Индекс галактики.</param>
    /// <param name="systemIndex">Индекс системы.</param>
    /// <exception cref="ArgumentOutOfRangeException">Если индексы вне допустимого диапазона.</exception>
    public void SetCurrentSystem(int galaxyIndex, int systemIndex)
    {
        GalaxyData galaxy = GalaxyGenerator.GenerateGalaxy(WorldSeed, galaxyIndex);
        StarSystemData system = StarSystemGenerator.GenerateSystem(galaxy, systemIndex);

        GalaxyIndex = galaxyIndex;
        SystemIndex = systemIndex;
        SystemName = system.Name;
        Economy = system.Economy;
        Faction = system.ControllingFaction;
    }

    /// <summary>
    /// Выполняет один тик: применяет накопленные команды, затем шаг физики на <paramref name="deltaSeconds"/>.
    /// </summary>
    /// <param name="deltaSeconds">Длительность тика в секундах. Должна быть положительной и одинаковой у всех участников.</param>
    /// <exception cref="ArgumentOutOfRangeException">Если <paramref name="deltaSeconds"/> не положительна.</exception>
    public void Advance(double deltaSeconds)
    {
        if (deltaSeconds <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(deltaSeconds), deltaSeconds, "Длительность тика должна быть положительной.");
        }

        while (_pending.Count > 0)
        {
            Apply(_pending.Dequeue());
        }

        if (!Destroyed)
        {
            _velocity = (_velocity + (_thrust * ThrustAcceleration * deltaSeconds)) * Math.Max(0.0, 1.0 - (LinearDrag * deltaSeconds));
            _position += _velocity * deltaSeconds;
        }

        Tick++;
    }

    /// <summary>Возвращает неизменяемый снимок текущего состояния.</summary>
    public SimulationSnapshot CaptureSnapshot()
    {
        return new SimulationSnapshot(
            WorldSeed,
            Tick,
            _position,
            _velocity,
            _thrust.X,
            _thrust.Y,
            Credits,
            Fuel,
            GalaxyIndex,
            SystemIndex,
            SystemName,
            Economy,
            Faction,
            Destroyed,
            new Dictionary<int, int>(_cargo),
            new List<string>(_chatLog));
    }

    private void Apply(IGameCommand command)
    {
        switch (command)
        {
            case MoveCommand move:
                _thrust = new Vector2D(move.ThrustX, move.ThrustY).ClampLength(1.0);
                break;

            case JumpCommand jump:
                ApplyJump(jump);
                break;

            case BuyItemCommand buy:
                ApplyBuy(buy);
                break;

            case SellItemCommand sell:
                ApplySell(sell);
                break;

            case ChatCommand chat:
                _chatLog.Add($"[{chat.PlayerId}] {chat.Text}");
                if (_chatLog.Count > MaxChatLog)
                {
                    _chatLog.RemoveAt(0);
                }

                break;
        }
    }

    private void ApplyJump(JumpCommand jump)
    {
        if (Destroyed || Fuel <= 0.0)
        {
            return;
        }

        _ship ??= ShipGenerator.GenerateShip(WorldSeed, ShipType.Cruiser);
        ulong seed = SeedHelper.Combine(WorldSeed, "jump", (ulong)Tick ^ (ulong)jump.Sequence);
        JumpResult result = BlackHoleTravel.ComputeJump(seed, _ship, jump.TargetGalaxyIndex);

        Fuel = Math.Max(0.0, Fuel - 25.0);

        if (result.Outcome == JumpOutcome.Destroyed)
        {
            Destroyed = true;
            _thrust = Vector2D.Zero;
            _velocity = Vector2D.Zero;
            return;
        }

        SetCurrentSystem(Math.Clamp(jump.TargetGalaxyIndex, 0, GalaxyGenerator.MaxGalaxyCount - 1), 0);
        _position = Vector2D.Zero;
        _velocity = Vector2D.Zero;
        _thrust = Vector2D.Zero;
    }

    private void ApplyBuy(BuyItemCommand buy)
    {
        if (buy.Quantity <= 0 || buy.ItemId < 0 || buy.ItemId >= ItemCatalog.Count)
        {
            return;
        }

        Item item = ItemCatalog.GetById(buy.ItemId);
        long unitPrice = MarketSystem.GetBuyPrice(item, Economy, 1.0, ReputationFor(Faction));
        double total = (double)unitPrice * buy.Quantity;

        if (Credits < total)
        {
            return;
        }

        Credits -= total;
        _cargo[buy.ItemId] = _cargo.GetValueOrDefault(buy.ItemId) + buy.Quantity;
    }

    private void ApplySell(SellItemCommand sell)
    {
        if (sell.Quantity <= 0 || sell.ItemId < 0 || sell.ItemId >= ItemCatalog.Count)
        {
            return;
        }

        int owned = _cargo.GetValueOrDefault(sell.ItemId);
        if (owned < sell.Quantity)
        {
            return;
        }

        Item item = ItemCatalog.GetById(sell.ItemId);
        long unitPrice = MarketSystem.GetSellPrice(item, Economy, 1.0, ReputationFor(Faction));

        _cargo[sell.ItemId] = owned - sell.Quantity;
        if (_cargo[sell.ItemId] <= 0)
        {
            _cargo.Remove(sell.ItemId);
        }

        Credits += (double)unitPrice * sell.Quantity;
    }

    /// <summary>Возвращает репутацию игрока у фракции (0, если не задана).</summary>
    public int ReputationFor(FactionType faction) => _reputation.GetValueOrDefault(faction);

    /// <summary>Устанавливает репутацию игрока у фракции (с ограничением диапазона).</summary>
    public void SetReputation(FactionType faction, int value) => _reputation[faction] = ReputationSystem.Clamp(value);
}
