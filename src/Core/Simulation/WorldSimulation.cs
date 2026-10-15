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
/// Полёт — 6-степенная модель в духе Frontier/Elite: у корабля есть положение, <b>ориентация</b>
/// (кватернион) и угловая скорость. Тяга идёт вдоль <b>носа корабля</b> в его локальной системе
/// (<see cref="ShipAxes"/>), а не в мировых осях. Вращение — угловая динамика: управление задаёт
/// угловое ускорение, ориентация интегрируется по угловой скорости. При <see cref="AngularAssistDamping"/> = 0
/// угловой момент сохраняется (чистый Ньютон), при &gt;0 работают стабилизаторы (как в игре).
/// </para>
/// <para>
/// Поступательная динамика — второй закон Ньютона (a = F/m, масса растёт с топливом), сглаженная
/// гравитация звезды с единым гравитационным параметром μ, и ракетное уравнение Циолковского.
/// Интегратор — полушаговый Эйлера (симплектический).
/// </para>
/// </remarks>
public sealed class WorldSimulation
{
    /// <summary>Длина сглаживания гравитации (делает притяжение конечным вблизи ядра звезды).</summary>
    public const double GravitySoftening = SystemScale.GravitySoftening;

    /// <summary>Стартовый баланс кредитов.</summary>
    public const double StartingCredits = 1000.0;

    /// <summary>Доля топлива, расходуемая на один межгалактический прыжок.</summary>
    public const double JumpFuelFraction = 0.25;

    /// <summary>Максимальное число сообщений чата, хранимых в снапшоте.</summary>
    public const int MaxChatLog = 50;

    /// <summary>Точка появления корабля — над плоскостью системы, вдали от звезды.</summary>
    public static readonly Vector3D ShipSpawn = new(0.0, 0.0, SystemScale.InnerOrbit * 1.3);

    private readonly Queue<IGameCommand> _pending = new();
    private readonly Dictionary<int, int> _cargo = new();
    private readonly Dictionary<int, int> _tradeCargo = new();
    private readonly List<string> _chatLog = new();
    private readonly Dictionary<FactionType, int> _reputation = new();
    private readonly ShipPhysicsProfile _profile;

    private Vector3D _position = Vector3D.Zero;
    private Vector3D _velocity = Vector3D.Zero;
    private Quaternion _orientation = Quaternion.Identity;
    private Vector3D _angularVelocity = Vector3D.Zero;
    private double _speed; // скалярная скорость для элитовской модели

    // Управление (сохраняется между тиками, как «положение РУД/стика»).
    private Vector3D _thrustCommand = Vector3D.Zero;   // (forward, strafe, lift)
    private Vector3D _rotationCommand = Vector3D.Zero; // (pitch, yaw, roll)

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

    /// <summary>Текущая ориентация корабля.</summary>
    public Quaternion Orientation => _orientation;

    /// <summary>Текущая угловая скорость корабля.</summary>
    public Vector3D AngularVelocity => _angularVelocity;

    /// <summary>
    /// Коэффициент линейного «ассистента полёта» (в секунду). По умолчанию 0 — чистый вакуум.
    /// </summary>
    public double FlightAssistDrag { get; set; }

    /// <summary>
    /// Коэффициент углового демпфирования (стабилизаторы). По умолчанию — из профиля; 0 — чистый Ньютон
    /// (угловой момент сохраняется, корабль «кувыркается»).
    /// </summary>
    public double AngularAssistDamping { get; set; }

    /// <summary>
    /// Модель полёта. По умолчанию — <see cref="FlightMode.EliteAligned"/> (элитовская: скорость вдоль носа,
    /// макс. скорость, тангаж/крен). <see cref="FlightMode.Newtonian"/> включает полную инерцию и гравитацию.
    /// </summary>
    public FlightMode FlightMode { get; set; } = FlightMode.EliteAligned;

    /// <summary>Текущая скалярная скорость корабля (для элитовской модели).</summary>
    public double Speed => _speed;

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

    /// <summary>Состыкован ли корабль со станцией (открывает доступ к рынку).</summary>
    public bool DockedStation { get; private set; }

    /// <summary>Название станции текущей системы (или пустая строка).</summary>
    public string StationName => SystemModel.Station?.Name ?? string.Empty;

    /// <summary>Торговый трюм: id товара → количество (только для чтения, для UI).</summary>
    public IReadOnlyDictionary<int, int> TradeCargo => _tradeCargo;

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
        AngularAssistDamping = _profile.AngularDamping;
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
        DockedStation = false;
        _position = ShipSpawn;
        _velocity = Vector3D.Zero;
        _speed = 0.0;
        _orientation = Quaternion.Identity;
        _angularVelocity = Vector3D.Zero;
        _thrustCommand = Vector3D.Zero;
        _rotationCommand = Vector3D.Zero;
    }

    /// <summary>
    /// Выполняет один тик: применяет накопленные команды, затем шаг физики на <paramref name="deltaSeconds"/>.
    /// </summary>
    /// <param name="deltaSeconds">Конечная положительная длительность тика в секундах, одинаковая у всех участников.</param>
    /// <exception cref="ArgumentOutOfRangeException">Если шаг не положителен или не является конечным числом.</exception>
    public void Advance(double deltaSeconds)
    {
        if (!double.IsFinite(deltaSeconds) || deltaSeconds <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(deltaSeconds), deltaSeconds, "Длительность тика должна быть конечной и положительной.");
        }

        while (_pending.Count > 0)
        {
            Apply(_pending.Dequeue());
        }

        if (!Destroyed && DockedBodyIndex < 0 && !DockedStation)
        {
            IntegratePhysics(deltaSeconds);
        }

        SimTime += deltaSeconds;
        Tick++;
    }

    private void IntegratePhysics(double deltaSeconds)
    {
        IntegrateRotation(deltaSeconds);

        if (FlightMode == FlightMode.Newtonian)
        {
            IntegrateTranslationNewtonian(deltaSeconds);
        }
        else
        {
            IntegrateTranslationElite(deltaSeconds);
        }
    }

    private void IntegrateRotation(double deltaSeconds)
    {
        // Локальное угловое ускорение: pitch вокруг «вправо», yaw вокруг «вверх», roll вокруг носа.
        Vector3D localAngularAcceleration =
            (ShipAxes.Right * _rotationCommand.X) +
            (ShipAxes.Up * _rotationCommand.Y) +
            (ShipAxes.Forward * _rotationCommand.Z);

        if (localAngularAcceleration.Length > 1.0)
        {
            localAngularAcceleration = localAngularAcceleration.Normalized();
        }

        Vector3D worldAngularAcceleration = _orientation.Rotate(localAngularAcceleration) * _profile.AngularAcceleration;
        _angularVelocity += worldAngularAcceleration * deltaSeconds;

        // Стабилизаторы (угловое демпфирование). 0 — чисто ньютоновское вращение.
        double damping = Math.Max(0.0, 1.0 - (AngularAssistDamping * deltaSeconds));
        _angularVelocity *= damping;

        // Ограничение скорости вращения.
        if (_angularVelocity.Length > _profile.MaxAngularRate)
        {
            _angularVelocity = _angularVelocity.Normalized() * _profile.MaxAngularRate;
        }

        _orientation = _orientation.Integrate(_angularVelocity, deltaSeconds);
    }

    /// <summary>
    /// Элитовская модель: скорость всегда направлена вдоль носа, скаляр скорости меняется тягой
    /// (тангаж/крен — для разворота). Корабль «летит туда, куда смотрит», как в Elite.
    /// </summary>
    private void IntegrateTranslationElite(double deltaSeconds)
    {
        // Органы управления: forward (throttle) увеличивает/уменьшает скорость, strafe/lift — нет.
        double throttle = Math.Clamp(_thrustCommand.X, -1.0, 1.0);
        if (throttle != 0.0)
        {
            _speed += throttle * _profile.SpeedAcceleration * deltaSeconds;
            _speed = Math.Clamp(_speed, 0.0, _profile.MaxSpeed);

            // Расход топлива пропорционален модулю тяги.
            double burn = _profile.MassFlowRate(Math.Abs(throttle) * _profile.Thrust) * deltaSeconds;
            Fuel = Math.Max(0.0, Fuel - burn);
        }

        // В элитовской модели поворот разворачивает вектор движения вместе с носом:
        // скорость всегда направлена вдоль носа корабля.
        _velocity = _orientation.Rotate(ShipAxes.Forward) * _speed;
        _position += _velocity * deltaSeconds;
    }

    /// <summary>
    /// Ньютоновская модель: скорость интегрируется независимо от ориентации, действует гравитация.
    /// </summary>
    private void IntegrateTranslationNewtonian(double deltaSeconds)
    {
        double forward = _thrustCommand.X;
        double strafe = _thrustCommand.Y * _profile.LateralThrustFactor;
        double lift = _thrustCommand.Z * _profile.LateralThrustFactor;
        if (forward < 0)
        {
            forward *= _profile.ReverseThrustFactor;
        }

        Vector3D localThrust = (ShipAxes.Forward * forward) + (ShipAxes.Right * strafe) + (ShipAxes.Up * lift);
        if (localThrust.Length > 1.0)
        {
            localThrust = localThrust.Normalized();
        }

        double thrustMagnitude = localThrust.Length * _profile.Thrust;
        Vector3D thrustAcceleration = _orientation.Rotate(localThrust) * _profile.Acceleration(_profile.Thrust, Fuel);
        Vector3D gravity = Astro.SoftenedGravityAcceleration(SystemModel.Mu, _position, GravitySoftening);
        Vector3D acceleration = thrustAcceleration + gravity;

        _velocity = (_velocity + (acceleration * deltaSeconds)) * Math.Max(0.0, 1.0 - (FlightAssistDrag * deltaSeconds));
        _position += _velocity * deltaSeconds;
        _speed = _velocity.Length;

        if (thrustMagnitude > 1e-6)
        {
            double burn = _profile.MassFlowRate(thrustMagnitude) * deltaSeconds;
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
            _speed,
            _orientation,
            _angularVelocity,
            _thrustCommand,
            _rotationCommand,
            TargetBodyIndex,
            DockedBodyIndex,
            DockedStation,
            StationName,
            new Dictionary<int, int>(_tradeCargo),
            CaptureMarket(),
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

    /// <summary>Строит котировки рынка станции для снимка состояния.</summary>
    private IReadOnlyList<CommodityQuote> CaptureMarket()
    {
        var quotes = new List<CommodityQuote>(CommodityCatalog.Count);
        StationMarket market = SystemModel.Market;
        for (int id = 0; id < CommodityCatalog.Count; id++)
        {
            Commodity c = CommodityCatalog.GetById(id);
            quotes.Add(new CommodityQuote(id, c.Name, c.Unit, market.BuyPrice(id), market.SellPrice(id), market.Stock(id)));
        }

        return quotes;
    }

    /// <summary>Устанавливает репутацию игрока у фракции (с ограничением диапазона).</summary>
    public void SetReputation(FactionType faction, int value) => _reputation[faction] = ReputationSystem.Clamp(value);

    /// <summary>
    /// Устанавливает состояние корабля (положение и скорость). Используется загрузкой сохранений,
    /// точками спавна и тестами физических законов. Ориентация — единичная, вращение/тяга сброшены.
    /// </summary>
    /// <param name="position">Позиция корабля в системе.</param>
    /// <param name="velocity">Скорость корабля.</param>
    public void InitializeShip(Vector3D position, Vector3D velocity)
        => InitializeShip(position, velocity, Quaternion.Identity, Vector3D.Zero);

    /// <summary>Устанавливает полное состояние корабля (положение, скорость, ориентацию, угловую скорость).</summary>
    /// <param name="position">Позиция корабля в системе.</param>
    /// <param name="velocity">Скорость корабля.</param>
    /// <param name="orientation">Ориентация корабля.</param>
    /// <param name="angularVelocity">Угловая скорость корабля.</param>
    public void InitializeShip(Vector3D position, Vector3D velocity, Quaternion orientation, Vector3D angularVelocity)
    {
        _position = position;
        _velocity = velocity;
        _speed = velocity.Length;
        _orientation = orientation.Normalized();
        _angularVelocity = angularVelocity;
        _thrustCommand = Vector3D.Zero;
        _rotationCommand = Vector3D.Zero;
        DockedBodyIndex = -1;
    }

    private void Apply(IGameCommand command)
    {
        switch (command)
        {
            case MoveCommand move:
                ApplyMove(move);
                break;

            case RotateCommand rotate:
                _rotationCommand = new Vector3D(rotate.Pitch, rotate.Yaw, rotate.Roll);
                break;

            case SetTargetCommand target:
                ApplySetTarget(target);
                break;

            case DockCommand dock:
                ApplyDock(dock);
                break;

            case DockStationCommand:
                ApplyDockStation();
                break;

            case BuyCommodityCommand buyCommodity:
                ApplyBuyCommodity(buyCommodity);
                break;

            case SellCommodityCommand sellCommodity:
                ApplySellCommodity(sellCommodity);
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
        var thrust = new Vector3D(move.Forward, move.Strafe, move.Lift);
        if (thrust.Length > 1e-6 && DockedBodyIndex >= 0)
        {
            // Любая тяга снимает корабль со стыковки.
            DockedBodyIndex = -1;
        }

        if (thrust.Length > 1e-6 && Math.Abs(move.Forward) > 1e-6 && DockedStation)
        {
            // Отход от станции по тяге вперёд.
            DockedStation = false;
        }

        _thrustCommand = thrust.Length > 1.0 ? thrust.Normalized() : thrust;
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
        if (distance <= SystemModel.DockRadius(TargetBodyIndex))
        {
            DockedBodyIndex = TargetBodyIndex;
            _position = SystemModel.BodyPosition(TargetBodyIndex, SimTime);
            _velocity = Vector3D.Zero;
            _thrustCommand = Vector3D.Zero;
            _rotationCommand = Vector3D.Zero;
            _angularVelocity = Vector3D.Zero;
        }
    }

    private void ApplyDockStation()
    {
        if (Destroyed || DockedStation || !SystemModel.HasStation)
        {
            return;
        }

        double distance = SystemModel.DistanceToStation(_position, SimTime);
        if (distance <= SystemModel.StationDockRadius)
        {
            DockedStation = true;
            DockedBodyIndex = -1;
            _position = SystemModel.StationPosition(SimTime);
            _velocity = Vector3D.Zero;
            _thrustCommand = Vector3D.Zero;
            _rotationCommand = Vector3D.Zero;
            _angularVelocity = Vector3D.Zero;
        }
    }

    private void ApplyBuyCommodity(BuyCommodityCommand buy)
    {
        if (!DockedStation || buy.Quantity <= 0 || buy.CommodityId < 0 || buy.CommodityId >= CommodityCatalog.Count)
        {
            return;
        }

        StationMarket market = SystemModel.Market;
        int available = market.Stock(buy.CommodityId);
        int quantity = Math.Min(buy.Quantity, available);
        if (quantity <= 0)
        {
            return;
        }

        int unitPrice = market.BuyPrice(buy.CommodityId);
        long total = (long)unitPrice * quantity;
        if (Credits < total)
        {
            quantity = (int)Math.Min(quantity, Credits / unitPrice);
            if (quantity <= 0)
            {
                return;
            }

            total = (long)unitPrice * quantity;
        }

        Credits -= total;
        _tradeCargo[buy.CommodityId] = _tradeCargo.GetValueOrDefault(buy.CommodityId) + quantity;
        market.RemoveStock(buy.CommodityId, quantity);
    }

    private void ApplySellCommodity(SellCommodityCommand sell)
    {
        if (!DockedStation || sell.Quantity <= 0 || sell.CommodityId < 0 || sell.CommodityId >= CommodityCatalog.Count)
        {
            return;
        }

        int owned = _tradeCargo.GetValueOrDefault(sell.CommodityId);
        int quantity = Math.Min(sell.Quantity, owned);
        if (quantity <= 0)
        {
            return;
        }

        StationMarket market = SystemModel.Market;
        int unitPrice = market.SellPrice(sell.CommodityId);
        Credits += (double)unitPrice * quantity;

        _tradeCargo[sell.CommodityId] = owned - quantity;
        if (_tradeCargo[sell.CommodityId] <= 0)
        {
            _tradeCargo.Remove(sell.CommodityId);
        }

        market.AddStock(sell.CommodityId, quantity);
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
            _thrustCommand = Vector3D.Zero;
            _rotationCommand = Vector3D.Zero;
            _velocity = Vector3D.Zero;
            _angularVelocity = Vector3D.Zero;
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
