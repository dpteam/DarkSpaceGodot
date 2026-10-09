using System;
using System.Collections.Generic;
using DarkSpace.Core.Data;
using DarkSpace.Core.Generation;
using DarkSpace.Core.Systems.Economy;
using DarkSpace.Core.Systems.Factions;
using DarkSpace.Core.Systems.Items;
using DarkSpace.Core.Systems.Orbital;
using DarkSpace.Core.Systems.Physics;
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
/// Физика строится на реальных законах (см. <see cref="Astro"/> и <see cref="ShipPhysicsProfile"/>):
/// второй закон Ньютона (a = F/m, масса растёт с топливом), сглаженная гравитация звезды с тем же
/// гравитационным параметром μ, что и у орбит планет, и ракетное уравнение Циолковского для топлива.
/// Интегратор — полушаговый Эйлера (симплектический), устойчивый к дрейфу энергии орбиты.
/// </para>
/// <para>
/// Детерминизм обязателен: одинаковый мировой сид + одинаковая последовательность команд дают
/// одинаковый результат. Здесь не используется <see cref="System.Random"/> и нет зависимости от Godot.
/// </para>
/// </remarks>
public sealed class WorldSimulation
{
    /// <summary>Длина сглаживания гравитации (делает притяжение конечным вблизи ядра звезды).</summary>
    public const double GravitySoftening = 8.0;

    /// <summary>Радиус стыковки с планетой (условные единицы).</summary>
    public const double DockRange = 4.0;

    /// <summary>Стартовый баланс кредитов.</summary>
    public const double StartingCredits = 1000.0;

    /// <summary>Доля топлива, расходуемая на один межгалактический прыжок.</summary>
    public const double JumpFuelFraction = 0.25;

    /// <summary>Максимальное число сообщений чата, хранимых в снапшоте.</summary>
    public const int MaxChatLog = 50;

    /// <summary>Точка появления корабля — над плоскостью системы, вдали от звезды и планет.</summary>
    public static readonly Vector3D ShipSpawn = new(0.0, 0.0, 12.0);

    private readonly Queue<IGameCommand> _pending = new();
    private readonly Dictionary<int, int> _cargo = new();
    private readonly List<string> _chatLog = new();
    private readonly Dictionary<FactionType, int> _reputation = new();
    private readonly ShipPhysicsProfile _profile;

    private Vector3D _position = Vector3D.Zero;
    private Vector3D _velocity = Vector3D.Zero;
    private Vector3D _thrust = Vector3D.Zero;
    private ShipData? _jumpShip;

    /// <summary>Мировой сид вселенной.</summary>
    public ulong WorldSeed { get; }

    /// <summary>Текущий номер тика.</summary>
    public long Tick { get; private set; }

    /// <summary>Модельное время симуляции в секундах.</summary>
    public double SimTime { get; private set; }

    /// <summary>Баланс кредитов.</summary>
    public double Credits { get; private set; } = StartingCredits;

    /// <summary>Остаток топлива (по массе).</summary>
    public double Fuel { get; private set; }

    /// <summary>Максимальный запас топлива (по массе).</summary>
    public double MaxFuel => _profile.FuelMass;

    /// <summary>Текущая полная масса корабля (сухая масса + топливо).</summary>
    public double ShipMass => _profile.DryMass + Fuel;

    /// <summary>Остаток характеристической скорости (бюджет Δv, по ракетному уравнению).</summary>
    public double RemainingDeltaV => _profile.RemainingDeltaV(Fuel);

    /// <summary>
    /// Коэффициент сопротивления «ассистента полёта» (в секунду). По умолчанию 0 — чистый вакуум,
    /// как в реальном космосе (энергия и угловой момент сохраняются).
    /// </summary>
    public double FlightAssistDrag { get; set; }

    /// <summary>Индекс текущей галактики.</summary>
    public int GalaxyIndex { get; private set; }

    /// <summary>Индекс текущей звёздной системы.</summary>
    public int SystemIndex { get; private set; }

    /// <summary>Модель текущей системы (звезда и планеты с орбитами).</summary>
    public StarSystemModel SystemModel { get; private set; } = null!;

    /// <summary>Название текущей системы.</summary>
    public string SystemName { get; private set; } = "—";

    /// <summary>Экономика текущей системы.</summary>
    public EconomyType Economy { get; private set; } = EconomyType.Industrial;

    /// <summary>Контролирующая фракция текущей системы.</summary>
    public FactionType Faction { get; private set; } = FactionType.EmpireOfDawn;

    /// <summary>Индекс выбранной планеты-цели, либо -1.</summary>
    public int TargetBodyIndex { get; private set; } = -1;

    /// <summary>Индекс планеты, с которой корабль состыкован, либо -1.</summary>
    public int DockedBodyIndex { get; private set; } = -1;

    /// <summary>Уничтожен ли корабль (после неудачного прыжка).</summary>
    public bool Destroyed { get; private set; }

    /// <summary>Сколько команд находится в очереди на обработку.</summary>
    public int PendingCommandCount => _pending.Count;

    /// <summary>
    /// Создаёт новую симуляцию.
    /// </summary>
    /// <param name="worldSeed">Мировой сид вселенной.</param>
    /// <param name="profile">Физический профиль корабля игрока (по умолчанию — <see cref="ShipPhysicsProfile.Default"/>).</param>
    public WorldSimulation(ulong worldSeed, ShipPhysicsProfile? profile = null)
    {
        WorldSeed = worldSeed;
        _profile = profile ?? ShipPhysicsProfile.Default;
        Fuel = _profile.FuelMass;
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
    /// Перемещает симуляцию в заданную систему вселенной, обновляя экономику, фракцию и модель системы.
    /// </summary>
    /// <param name="galaxyIndex">Индекс галактики.</param>
    /// <param name="systemIndex">Индекс системы.</param>
    /// <exception cref="ArgumentOutOfRangeException">Если индексы вне допустимого диапазона.</exception>
    public void SetCurrentSystem(int galaxyIndex, int systemIndex)
    {
        GalaxyData galaxy = GalaxyGenerator.GenerateGalaxy(WorldSeed, galaxyIndex);
        StarSystemModel model = StarSystemModel.Create(galaxy, systemIndex);

        GalaxyIndex = galaxyIndex;
        SystemIndex = systemIndex;
        SystemModel = model;
        SystemName = model.System.Name;
        Economy = model.System.Economy;
        Faction = model.System.ControllingFaction;
        TargetBodyIndex = -1;
        DockedBodyIndex = -1;
        _position = ShipSpawn;
        _velocity = Vector3D.Zero;
        _thrust = Vector3D.Zero;
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

        if (!Destroyed && DockedBodyIndex < 0)
        {
            IntegratePhysics(deltaSeconds);
        }

        SimTime += deltaSeconds;
        Tick++;
    }

    private void IntegratePhysics(double deltaSeconds)
    {
        double throttle = _thrust.Length;
        Vector3D thrustAcceleration = _thrust * _profile.Acceleration(throttle > 1.0 ? 1.0 : throttle, Fuel);
        Vector3D gravity = Astro.SoftenedGravityAcceleration(SystemModel.Mu, _position, GravitySoftening);
        Vector3D acceleration = thrustAcceleration + gravity;

        // Полушаговый (симплектический) Эйлера: сначала скорость, затем позиция.
        _velocity = (_velocity + (acceleration * deltaSeconds)) * Math.Max(0.0, 1.0 - (FlightAssistDrag * deltaSeconds));
        _position += _velocity * deltaSeconds;

        if (throttle > 1e-6)
        {
            double burn = _profile.MassFlowRate(throttle) * deltaSeconds;
            Fuel = Math.Max(0.0, Fuel - burn);
        }
    }

    /// <summary>Возвращает неизменяемый снимок текущего состояния.</summary>
    public SimulationSnapshot CaptureSnapshot()
    {
        return new SimulationSnapshot(
            WorldSeed,
            Tick,
            SimTime,
            _position,
            _velocity,
            _thrust,
            TargetBodyIndex,
            DockedBodyIndex,
            Credits,
            Fuel,
            ShipMass,
            RemainingDeltaV,
            GalaxyIndex,
            SystemIndex,
            SystemName,
            Economy,
            Faction,
            Destroyed,
            new Dictionary<int, int>(_cargo),
            new List<string>(_chatLog));
    }

    /// <summary>Возвращает репутацию игрока у фракции (0, если не задана).</summary>
    public int ReputationFor(FactionType faction) => _reputation.GetValueOrDefault(faction);

    /// <summary>Устанавливает репутацию игрока у фракции (с ограничением диапазона).</summary>
    public void SetReputation(FactionType faction, int value) => _reputation[faction] = ReputationSystem.Clamp(value);

    /// <summary>
    /// Устанавливает состояние корабля (позиция и скорость). Используется загрузкой сохранений,
    /// точками спавна и тестами физических законов. Тяга и стыковка сбрасываются.
    /// </summary>
    /// <param name="position">Позиция корабля в системе.</param>
    /// <param name="velocity">Скорость корабля.</param>
    public void InitializeShip(Vector3D position, Vector3D velocity)
    {
        _position = position;
        _velocity = velocity;
        _thrust = Vector3D.Zero;
        DockedBodyIndex = -1;
    }

    private void Apply(IGameCommand command)
    {
        switch (command)
        {
            case MoveCommand move:
                ApplyMove(move);
                break;

            case SetTargetCommand target:
                ApplySetTarget(target);
                break;

            case DockCommand dock:
                ApplyDock(dock);
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

    private void ApplyMove(MoveCommand move)
    {
        var thrust = new Vector3D(move.ThrustX, move.ThrustY, move.ThrustZ);
        if (thrust.Length > 1e-6 && DockedBodyIndex >= 0)
        {
            // Любая тяга снимает корабль со стыковки.
            DockedBodyIndex = -1;
        }

        _thrust = thrust.Length > 1.0 ? thrust.Normalized() : thrust;
    }

    private void ApplySetTarget(SetTargetCommand target)
    {
        TargetBodyIndex = target.BodyIndex >= 0 && target.BodyIndex < SystemModel.BodyCount
            ? target.BodyIndex
            : -1;
    }

    private void ApplyDock(DockCommand dock)
    {
        if (Destroyed || TargetBodyIndex < 0 || TargetBodyIndex >= SystemModel.BodyCount)
        {
            return;
        }

        double distance = SystemModel.DistanceToBody(TargetBodyIndex, _position, SimTime);
        if (distance <= DockRange)
        {
            DockedBodyIndex = TargetBodyIndex;
            _position = SystemModel.BodyPosition(TargetBodyIndex, SimTime);
            _velocity = Vector3D.Zero;
            _thrust = Vector3D.Zero;
        }
    }

    private void ApplyJump(JumpCommand jump)
    {
        if (Destroyed || DockedBodyIndex >= 0 || Fuel <= 0.0)
        {
            return;
        }

        _jumpShip ??= ShipGenerator.GenerateShip(WorldSeed, ShipType.Cruiser);
        ulong seed = SeedHelper.Combine(WorldSeed, "jump", (ulong)Tick ^ (ulong)jump.Sequence);
        JumpResult result = BlackHoleTravel.ComputeJump(seed, _jumpShip, jump.TargetGalaxyIndex);

        Fuel = Math.Max(0.0, Fuel - (MaxFuel * JumpFuelFraction));

        if (result.Outcome == JumpOutcome.Destroyed)
        {
            Destroyed = true;
            _thrust = Vector3D.Zero;
            _velocity = Vector3D.Zero;
            DockedBodyIndex = -1;
            return;
        }

        SetCurrentSystem(Math.Clamp(jump.TargetGalaxyIndex, 0, GalaxyGenerator.MaxGalaxyCount - 1), 0);
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
}
