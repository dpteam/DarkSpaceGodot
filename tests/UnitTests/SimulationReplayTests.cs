using System.Globalization;
using DarkSpace.Core.Network;
using DarkSpace.Core.Simulation;
using DarkSpace.Core.Systems.Physics;
using Xunit;

namespace DarkSpace.Tests.Unit;

/// <summary>Регрессии фиксированных input replay и канонического digest снимков.</summary>
public sealed class SimulationReplayTests
{
    private const ulong Seed = 0x51A7UL;
    private const double Dt = 1.0 / 60.0;
    private const int Ticks = 480;

    // Reviewed fixture: accelerate, pitch+roll, release rotation, brake, coast.
    // Never update this automatically: changes require reviewing physics and snapshot schema.
    private const string EliteGolden = "2c912cecd702fb00e52844ea37a8428f66c66b5f3759b805c5a87ee5ad7bd798";

    [Theory]
    [InlineData(FlightMode.EliteAligned)]
    [InlineData(FlightMode.Newtonian)]
    public void Run_SameInputsMatchWireRoundTripAndManualSimulation(FlightMode mode)
    {
        ReplayCommand[] commands = FlightScript();
        ReplayResult first = SimulationReplay.Run(Seed, Dt, Ticks, commands, mode);
        ReplayResult second = SimulationReplay.Run(Seed, Dt, Ticks, commands, mode);
        var manual = new WorldSimulation(Seed) { FlightMode = mode };
        for (int tick = 0; tick < Ticks; tick++)
        {
            foreach (ReplayCommand entry in commands.Where(entry => entry.Tick == tick))
            {
                manual.Enqueue(CommandCodec.Decode(CommandCodec.Encode(entry.Command)));
            }

            manual.Advance(Dt);
        }

        Assert.Equal(first.SnapshotHash, second.SnapshotHash);
        Assert.Equal(first.SnapshotHash, SnapshotDigest.Compute(manual.CaptureSnapshot()));
        Assert.Equal(MessageCodec.EncodePayload(first.Snapshot), MessageCodec.EncodePayload(second.Snapshot));
        Assert.Equal(Ticks, first.Snapshot.Tick);
        Assert.Equal(Ticks * Dt, first.Snapshot.SimTime, precision: 10);
        Assert.Equal(1.0, first.Snapshot.Orientation.Length, precision: 12);
    }

    [Fact]
    public void EliteFlight_MatchesReviewedGolden()
    {
        ReplayResult result = SimulationReplay.Run(Seed, Dt, Ticks, FlightScript());
        Assert.True(result.SnapshotHash == EliteGolden,
            $"Observed golden: {result.SnapshotHash}; speed={result.Snapshot.Speed:R}; fuel={result.Snapshot.Fuel:R}");
        Assert.Equal(0.0, result.Snapshot.Speed, precision: 9);
        // 5 s at 80% + 2 s braking at 100%: burn = (4 + 2) * Thrust / ExhaustVelocity = 16.
        Assert.Equal(34.0, result.Snapshot.Fuel, precision: 9);
        Assert.NotEqual(WorldSimulation.ShipSpawn, result.Snapshot.Position);
    }

    [Fact]
    public void Run_ChangedInputOrSeedChangesDigest()
    {
        ReplayCommand[] commands = FlightScript();
        ReplayResult baseline = SimulationReplay.Run(Seed, Dt, Ticks, commands);
        ReplayCommand[] changed = FlightScript();
        changed[1] = new ReplayCommand(60, new RotateCommand(1, 1, -0.4, 0.0, 0.25));

        Assert.NotEqual(baseline.SnapshotHash, SimulationReplay.Run(Seed, Dt, Ticks, changed).SnapshotHash);
        Assert.NotEqual(baseline.SnapshotHash, SimulationReplay.Run(Seed + 1, Dt, Ticks, commands).SnapshotHash);
        Assert.NotEqual(baseline.SnapshotHash,
            SimulationReplay.Run(Seed, Dt, Ticks, commands, FlightMode.Newtonian).SnapshotHash);
        Assert.NotEqual(baseline.SnapshotHash,
            SimulationReplay.Run(Seed, Dt, Ticks, commands,
                profile: ShipPhysicsProfile.Default with { AngularAcceleration = 4.0 }).SnapshotHash);
    }

    [Fact]
    public void Run_SameTickPreservesEnqueueOrderNotSequenceOrder()
    {
        var accelerate = new ReplayCommand(0, new MoveCommand(1, 2, 1.0, 0.0));
        var brake = new ReplayCommand(0, new MoveCommand(1, 1, -1.0, 0.0));
        ReplayResult stopped = SimulationReplay.Run(Seed, Dt, 1, new[] { accelerate, brake });
        ReplayResult moving = SimulationReplay.Run(Seed, Dt, 1, new[] { brake, accelerate });

        Assert.Equal(0.0, stopped.Snapshot.Speed);
        Assert.True(moving.Snapshot.Speed > 0.0);
        Assert.NotEqual(stopped.SnapshotHash, moving.SnapshotHash);
    }

    [Fact]
    public void Run_AppliesCommandBeforeItsTickAndKeepsInputBetweenTicks()
    {
        var command = new ReplayCommand(1, new MoveCommand(1, 0, 1.0, 0.0));
        ReplayResult result = SimulationReplay.Run(Seed, Dt, 3, new[] { command });
        Assert.Equal(2 * ShipPhysicsProfile.Default.SpeedAcceleration * Dt, result.Snapshot.Speed, precision: 12);
    }

    [Fact]
    public void Run_ZeroTicksReturnsInitialState()
    {
        ReplayResult result = SimulationReplay.Run(Seed, Dt, 0, Array.Empty<ReplayCommand>());
        Assert.Equal(0, result.Snapshot.Tick);
        Assert.Equal(SnapshotDigest.Compute(new WorldSimulation(Seed).CaptureSnapshot()), result.SnapshotHash);
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(-1.0)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void Run_RejectsInvalidDt(double dt)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => SimulationReplay.Run(Seed, dt, 1, Array.Empty<ReplayCommand>()));
    }

    [Fact]
    public void Run_RejectsNegativeDurationAndUnknownMode()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => SimulationReplay.Run(Seed, Dt, -1, Array.Empty<ReplayCommand>()));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => SimulationReplay.Run(Seed, Dt, 1, Array.Empty<ReplayCommand>(), (FlightMode)99));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(2)]
    public void Run_RejectsCommandOutsideDuration(int tick)
    {
        var entry = new ReplayCommand(tick, new MoveCommand(1, 0, 1.0, 0.0));
        Assert.Throws<ArgumentOutOfRangeException>(() => SimulationReplay.Run(Seed, Dt, 2, new[] { entry }));
    }

    [Fact]
    public void Run_RejectsUnorderedScheduleAndNulls()
    {
        var command = new MoveCommand(1, 0, 1.0, 0.0);
        Assert.Throws<ArgumentException>(() => SimulationReplay.Run(Seed, Dt, 2,
            new[] { new ReplayCommand(1, command), new ReplayCommand(0, command) }));
        Assert.Throws<ArgumentNullException>(() => SimulationReplay.Run(Seed, Dt, 2, null!));
        Assert.Throws<ArgumentNullException>(() => SimulationReplay.Run(Seed, Dt, 2, new ReplayCommand[] { null! }));
        Assert.Throws<ArgumentNullException>(() => SimulationReplay.Run(Seed, Dt, 2,
            new[] { new ReplayCommand(0, null!) }));
    }

    [Fact]
    public void Digest_IsIndependentOfDictionaryInsertionOrderAndCulture()
    {
        SimulationSnapshot original = new WorldSimulation(Seed).CaptureSnapshot();
        SimulationSnapshot first = original with
        {
            Cargo = new Dictionary<int, int> { [2] = 7, [10] = 3 },
            TradeCargo = new Dictionary<int, int> { [1] = 4, [0] = 2 },
        };
        SimulationSnapshot second = original with
        {
            Cargo = new Dictionary<int, int> { [10] = 3, [2] = 7 },
            TradeCargo = new Dictionary<int, int> { [0] = 2, [1] = 4 },
        };
        CultureInfo previous = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("ru-RU");
            string russian = SnapshotDigest.Compute(first);
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
            Assert.Equal(russian, SnapshotDigest.Compute(second));
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
        }
    }

    [Fact]
    public void Digest_PreservesArrayOrderAndExactNumbers()
    {
        SimulationSnapshot snapshot = new WorldSimulation(Seed).CaptureSnapshot();
        string baseline = SnapshotDigest.Compute(snapshot);
        Assert.NotEqual(baseline, SnapshotDigest.Compute(snapshot with { Fuel = Math.BitIncrement(snapshot.Fuel) }));
        Assert.NotEqual(SnapshotDigest.Compute(snapshot with { ChatLog = new[] { "a", "b" } }),
            SnapshotDigest.Compute(snapshot with { ChatLog = new[] { "b", "a" } }));
        Assert.Matches("^[0-9a-f]{64}$", baseline);
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void Digest_RejectsNonFiniteState(double value)
    {
        SimulationSnapshot snapshot = new WorldSimulation(Seed).CaptureSnapshot();
        Assert.Throws<ArgumentException>(() => SnapshotDigest.Compute(snapshot with { SimTime = value }));
    }

    [Fact]
    public void Digest_RejectsNullSnapshot()
    {
        Assert.Throws<ArgumentNullException>(() => SnapshotDigest.Compute(null!));
    }

    private static ReplayCommand[] FlightScript() => new[]
    {
        new ReplayCommand(0, new MoveCommand(1, 0, 0.8, 0.0)),
        new ReplayCommand(60, new RotateCommand(1, 1, 0.4, 0.0, 0.25)),
        new ReplayCommand(180, new RotateCommand(1, 2, 0.0, 0.0, 0.0)),
        new ReplayCommand(300, new MoveCommand(1, 3, -1.0, 0.0)),
        new ReplayCommand(420, new MoveCommand(1, 4, 0.0, 0.0)),
    };
}
