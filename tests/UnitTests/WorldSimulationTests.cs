using DarkSpace.Core.Data;
using DarkSpace.Core.Network;
using DarkSpace.Core.Simulation;
using DarkSpace.Core.Systems.Items;
using Xunit;

namespace DarkSpace.Tests.Unit;

/// <summary>Тесты детерминированной тик-симуляции мира.</summary>
public sealed class WorldSimulationTests
{
    private const double Dt = 1.0 / 30.0;

    [Fact]
    public void Constructor_SetsInitialSystem()
    {
        var sim = new WorldSimulation(42);
        Assert.Equal(0, sim.GalaxyIndex);
        Assert.Equal(0, sim.SystemIndex);
        Assert.Equal(WorldSimulation.StartingCredits, sim.Credits);
        Assert.Equal(WorldSimulation.MaxFuel, sim.Fuel);
        Assert.False(sim.Destroyed);
    }

    [Fact]
    public void Advance_IncrementsTick()
    {
        var sim = new WorldSimulation(1);
        sim.Advance(Dt);
        sim.Advance(Dt);
        Assert.Equal(2, sim.Tick);
    }

    [Fact]
    public void Advance_InvalidDt_Throws()
    {
        var sim = new WorldSimulation(1);
        Assert.Throws<ArgumentOutOfRangeException>(() => sim.Advance(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => sim.Advance(-1));
    }

    [Fact]
    public void Move_ChangesVelocityAndPosition()
    {
        var sim = new WorldSimulation(1);
        sim.Enqueue(new MoveCommand(1, 0, 1.0, 0.0));
        for (int i = 0; i < 30; i++)
        {
            sim.Advance(Dt);
        }

        SimulationSnapshot snap = sim.CaptureSnapshot();
        Assert.True(snap.Velocity.X > 0);
        Assert.True(snap.Position.X > 0);
    }

    [Fact]
    public void Thrust_IsClampedToUnitLength()
    {
        var sim = new WorldSimulation(1);
        sim.Enqueue(new MoveCommand(1, 0, 100.0, 0.0));
        sim.Advance(Dt);
        SimulationSnapshot snap = sim.CaptureSnapshot();
        Assert.Equal(1.0, snap.Thrust.Length, precision: 6);
    }

    [Fact]
    public void Buy_ThenSell_DecreasesCreditsAndMovesCargo()
    {
        var sim = new WorldSimulation(123);
        double startCredits = sim.Credits;

        sim.Enqueue(new BuyItemCommand(1, 0, 12, 2)); // «Не умирай» (дешёвый расходник)
        sim.Advance(Dt);

        SimulationSnapshot afterBuy = sim.CaptureSnapshot();
        Assert.True(afterBuy.Credits < startCredits);
        Assert.Equal(2, afterBuy.Cargo[12]);

        sim.Enqueue(new SellItemCommand(1, 1, 12, 2));
        sim.Advance(Dt);

        SimulationSnapshot afterSell = sim.CaptureSnapshot();
        Assert.False(afterSell.Cargo.ContainsKey(12));
    }

    [Fact]
    public void Buy_InsufficientCredits_DoesNothing()
    {
        var sim = new WorldSimulation(1);
        sim.Enqueue(new BuyItemCommand(1, 0, 23, 1000)); // «Слеза пустоты» — очень дорого
        sim.Advance(Dt);

        SimulationSnapshot snap = sim.CaptureSnapshot();
        Assert.Equal(WorldSimulation.StartingCredits, snap.Credits);
        Assert.Empty(snap.Cargo);
    }

    [Fact]
    public void Sell_NotOwned_DoesNothing()
    {
        var sim = new WorldSimulation(1);
        double start = sim.Credits;
        sim.Enqueue(new SellItemCommand(1, 0, 12, 5));
        sim.Advance(Dt);
        Assert.Equal(start, sim.Credits);
    }

    [Fact]
    public void Chat_IsAppendedToLog()
    {
        var sim = new WorldSimulation(1);
        sim.Enqueue(new ChatCommand(3, 0, "За Архитектора!"));
        sim.Advance(Dt);
        Assert.Contains("[3] За Архитектора!", sim.CaptureSnapshot().ChatLog);
    }

    [Fact]
    public void Jump_ConsumesFuelAndChangesGalaxyOrDestroys()
    {
        var sim = new WorldSimulation(0xABCDEF);
        sim.Enqueue(new JumpCommand(1, 0, 5));
        sim.Advance(Dt);

        SimulationSnapshot snap = sim.CaptureSnapshot();
        Assert.True(snap.Fuel < WorldSimulation.MaxFuel);
        if (!snap.Destroyed)
        {
            Assert.Equal(5, snap.GalaxyIndex);
        }
    }

    [Fact]
    public void Determinism_SameCommandsProduceSameState()
    {
        SimulationSnapshot a = RunScripted(out _);
        SimulationSnapshot b = RunScripted(out _);

        // Сравниваем через сериализацию: порядок вставки в словари одинаков для одинаковых прогонов.
        Assert.Equal(MessageCodec.EncodePayload(a), MessageCodec.EncodePayload(b));
    }

    [Fact]
    public void Null_Enqueue_Throws()
    {
        var sim = new WorldSimulation(1);
        Assert.Throws<ArgumentNullException>(() => sim.Enqueue(null!));
    }

    private static SimulationSnapshot RunScripted(out WorldSimulation sim)
    {
        sim = new WorldSimulation(0x5151);
        sim.Enqueue(new MoveCommand(1, 0, 0.5, 0.5));
        sim.Enqueue(new BuyItemCommand(1, 1, 12, 3));
        sim.Enqueue(new ChatCommand(1, 2, "Полёт нормальный."));
        for (int i = 0; i < 60; i++)
        {
            sim.Advance(Dt);
            if (i == 30)
            {
                sim.Enqueue(new MoveCommand(1, 3, -0.3, 0.1));
            }
        }

        sim.Enqueue(new SellItemCommand(1, 4, 12, 1));
        sim.Advance(Dt);
        return sim.CaptureSnapshot();
    }
}
