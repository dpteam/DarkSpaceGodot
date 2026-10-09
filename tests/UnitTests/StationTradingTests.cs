using DarkSpace.Core.Data;
using DarkSpace.Core.Generation;
using DarkSpace.Core.Simulation;
using DarkSpace.Core.Systems.Economy;
using DarkSpace.Core.Systems.Orbital;
using Xunit;

namespace DarkSpace.Tests.Unit;

/// <summary>Тесты станции и торговли: стыковка со станцией открывает рынок.</summary>
public sealed class StationTradingTests
{
    private const double Dt = 1.0 / 60.0;

    /// <summary>Находит систему с космической станцией.</summary>
    private static StarSystemModel SystemWithStation()
    {
        GalaxyData galaxy = GalaxyGenerator.GenerateGalaxy(0x571A7104UL, 0);
        for (int s = 0; s < galaxy.StarCount; s++)
        {
            var model = StarSystemModel.Create(galaxy, s);
            if (model.HasStation)
            {
                return model;
            }
        }

        throw new Xunit.Sdk.XunitException("Не найдено системы со станцией для теста.");
    }

    [Fact]
    public void Station_OrbitsHostPlanet()
    {
        StarSystemModel model = SystemWithStation();
        Assert.NotNull(model.Station);

        Vector3D atStart = model.StationPosition(0.0);
        Vector3D later = model.StationPosition(model.Station!.PeriodSeconds * 0.5);
        Assert.True((atStart - later).Length > model.Station.OrbitRadius, "Станция должна обращаться вокруг планеты.");
    }

    [Fact]
    public void DockStation_WhenAtStationPosition_Docks()
    {
        StarSystemModel model = SystemWithStation();
        var sim = new WorldSimulation(0x571A7104UL);
        sim.SetCurrentSystem(sim.GalaxyIndex, FindStationSystemIndex());

        Vector3D stationPos = sim.SystemModel.StationPosition(0.0);
        sim.InitializeShip(stationPos, Vector3D.Zero);
        sim.Enqueue(new DockStationCommand(1, 0));
        sim.Advance(Dt);

        Assert.True(sim.CaptureSnapshot().DockedStation, "Корабль у станции должен состыковаться.");
    }

    [Fact]
    public void DockStation_WhenFar_DoesNothing()
    {
        var sim = new WorldSimulation(0x571A7104UL);
        sim.SetCurrentSystem(sim.GalaxyIndex, FindStationSystemIndex());
        sim.InitializeShip(WorldSimulation.ShipSpawn, Vector3D.Zero);
        sim.Enqueue(new DockStationCommand(1, 0));
        sim.Advance(Dt);
        Assert.False(sim.CaptureSnapshot().DockedStation);
    }

    [Fact]
    public void Trade_WithoutDocking_IsRejected()
    {
        var sim = new WorldSimulation(0x571A7104UL);
        double start = sim.Credits;
        sim.Enqueue(new BuyCommodityCommand(1, 0, 0, 1));
        sim.Advance(Dt);
        Assert.Equal(start, sim.Credits);
        Assert.Empty(sim.CaptureSnapshot().TradeCargo);
    }

    [Fact]
    public void Trade_AfterDocking_BuysAndSells()
    {
        var sim = new WorldSimulation(0x571A7104UL);
        sim.SetCurrentSystem(sim.GalaxyIndex, FindStationSystemIndex());

        Vector3D stationPos = sim.SystemModel.StationPosition(0.0);
        sim.InitializeShip(stationPos, Vector3D.Zero);
        sim.Enqueue(new DockStationCommand(1, 0));
        sim.Advance(Dt);
        Assert.True(sim.CaptureSnapshot().DockedStation);

        double creditsBefore = sim.Credits;
        int price = sim.SystemModel.Market.BuyPrice(9); // «Сплавы» — дешёвые
        sim.Enqueue(new BuyCommodityCommand(1, 1, 9, 2));
        sim.Advance(Dt);

        SimulationSnapshot afterBuy = sim.CaptureSnapshot();
        Assert.Equal(2, afterBuy.TradeCargo[9]);
        Assert.Equal(creditsBefore - (price * 2), afterBuy.Credits, precision: 6);

        // Продаём обратно — креitы растут, трюм пустеет.
        sim.Enqueue(new SellCommodityCommand(1, 2, 9, 2));
        sim.Advance(Dt);
        SimulationSnapshot afterSell = sim.CaptureSnapshot();
        Assert.False(afterSell.TradeCargo.ContainsKey(9));
        Assert.True(afterSell.Credits > afterBuy.Credits - (price * 2));
    }

    [Fact]
    public void Trade_CannotBuyMoreThanStock()
    {
        var sim = new WorldSimulation(0x571A7104UL);
        sim.SetCurrentSystem(sim.GalaxyIndex, FindStationSystemIndex());
        sim.InitializeShip(sim.SystemModel.StationPosition(0.0), Vector3D.Zero);
        sim.Enqueue(new DockStationCommand(1, 0));
        sim.Advance(Dt);

        int stock = sim.SystemModel.Market.Stock(0);
        sim.Enqueue(new BuyCommodityCommand(1, 1, 0, stock + 1000));
        sim.Advance(Dt);

        // Купить больше запаса нельзя.
        Assert.Equal(stock, sim.CaptureSnapshot().TradeCargo.GetValueOrDefault(0));
    }

    [Fact]
    public void Thrust_WhileDockedAtStation_Undocks()
    {
        var sim = new WorldSimulation(0x571A7104UL);
        sim.SetCurrentSystem(sim.GalaxyIndex, FindStationSystemIndex());
        sim.InitializeShip(sim.SystemModel.StationPosition(0.0), Vector3D.Zero);
        sim.Enqueue(new DockStationCommand(1, 0));
        sim.Advance(Dt);
        Assert.True(sim.CaptureSnapshot().DockedStation);

        sim.Enqueue(new MoveCommand(1, 1, 1.0, 0.0, 0.0));
        sim.Advance(Dt);
        Assert.False(sim.CaptureSnapshot().DockedStation);
    }

    private static int FindStationSystemIndex()
    {
        GalaxyData galaxy = GalaxyGenerator.GenerateGalaxy(0x571A7104UL, 0);
        for (int s = 0; s < galaxy.StarCount; s++)
        {
            if (StarSystemModel.Create(galaxy, s).HasStation)
            {
                return s;
            }
        }

        return 0;
    }
}
