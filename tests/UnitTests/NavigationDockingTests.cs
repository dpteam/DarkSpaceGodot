using DarkSpace.Core.Data;
using DarkSpace.Core.Simulation;
using DarkSpace.Core.Systems.Physics;
using Xunit;

namespace DarkSpace.Tests.Unit;

/// <summary>Тесты навигации по системе: наведение, гравитация звезды и стыковка.</summary>
public sealed class NavigationDockingTests
{
    private const double Dt = 1.0 / 30.0;

    [Fact]
    public void SetTarget_StoresValidIndex()
    {
        var sim = new WorldSimulation(1234);
        sim.Enqueue(new SetTargetCommand(1, 0, 0));
        sim.Advance(Dt);
        Assert.Equal(0, sim.TargetBodyIndex);
    }

    [Fact]
    public void SetTarget_InvalidIndex_ClearsTarget()
    {
        var sim = new WorldSimulation(1234);
        sim.Enqueue(new SetTargetCommand(1, 0, 0));
        sim.Advance(Dt);
        sim.Enqueue(new SetTargetCommand(1, 1, 9999));
        sim.Advance(Dt);
        Assert.Equal(-1, sim.TargetBodyIndex);
    }

    [Fact]
    public void Dock_WithoutTarget_DoesNothing()
    {
        var sim = new WorldSimulation(1234);
        sim.Enqueue(new DockCommand(1, 0));
        sim.Advance(Dt);
        Assert.Equal(-1, sim.DockedBodyIndex);
    }

    [Fact]
    public void Dock_WhenFarFromTarget_DoesNothing()
    {
        var sim = new WorldSimulation(1234);
        sim.Enqueue(new SetTargetCommand(1, 0, 2));
        sim.Enqueue(new DockCommand(1, 1));
        sim.Advance(Dt); // корабль в начале координат, планета далеко
        Assert.Equal(-1, sim.DockedBodyIndex);
    }

    [Fact]
    public void Dock_WhenCloseToTarget_DocksAndZeroesVelocity()
    {
        var sim = new WorldSimulation(1234) { FlightMode = FlightMode.Newtonian };
        ApproachBody(sim, 0);

        sim.Enqueue(new SetTargetCommand(1, 1_000_000, 0));
        sim.Enqueue(new DockCommand(1, 1_000_001));
        sim.Advance(Dt);

        Assert.Equal(0, sim.DockedBodyIndex);
        Assert.Equal(0.0, sim.CaptureSnapshot().Velocity.Length, precision: 6);
    }

    [Fact]
    public void Thrust_WhileDocked_Undocks()
    {
        var sim = new WorldSimulation(1234) { FlightMode = FlightMode.Newtonian };
        ApproachBody(sim, 0);

        sim.Enqueue(new SetTargetCommand(1, 1_000_000, 0));
        sim.Enqueue(new DockCommand(1, 1_000_001));
        sim.Advance(Dt);
        Assert.Equal(0, sim.DockedBodyIndex);

        // Любая тяга должна снять со стыковки.
        sim.Enqueue(new MoveCommand(1, 1_000_002, 1.0, 0.0, 0.0));
        sim.Advance(Dt);
        Assert.Equal(-1, sim.DockedBodyIndex);
    }

    [Fact]
    public void Gravity_PullsShipTowardStar()
    {
        var sim = new WorldSimulation(777) { FlightMode = FlightMode.Newtonian };

        // Разгоняем корабль в сторону от звезды (радиально наружу = +Z в мире при единичной ориентации).
        for (int i = 0; i < 120; i++)
        {
            sim.Enqueue(new MoveCommand(1, i, 0.0, 0.0, 1.0));
            sim.Advance(Dt);
        }

        double speedBefore = sim.CaptureSnapshot().Velocity.Length;

        // Отпускаем тягу — гравитация звезды должна замедлять удаляющийся корабль.
        for (int i = 0; i < 120; i++)
        {
            sim.Enqueue(new MoveCommand(1, 1000 + i, 0.0, 0.0, 0.0));
            sim.Advance(Dt);
        }

        double speedAfter = sim.CaptureSnapshot().Velocity.Length;
        Assert.True(speedAfter < speedBefore, "Гравитация звезды должна замедлять удаляющийся корабль.");
    }

    [Fact]
    public void Determinism_WithNavigation_IsStable()
    {
        Assert.Equal(RunScript(), RunScript());
    }

    /// <summary>
    /// PD-контроллер с feed-forward скорости планеты: подводит корабль к планете и удерживает
    /// рядом с ней (иначе пропорциональное управление даёт стационарное отставание).
    /// </summary>
    private static void ApproachBody(WorldSimulation sim, int bodyIndex)
    {
        long seq = 0;
        for (int i = 0; i < 40000; i++)
        {
            SimulationSnapshot snap = sim.CaptureSnapshot();
            Vector3D targetPos = sim.SystemModel.BodyPosition(bodyIndex, sim.SimTime);
            Vector3D toTarget = targetPos - snap.Position;
            double distance = toTarget.Length;

            if (distance <= WorldSimulation.DockRange * 0.75)
            {
                break;
            }

            // Feed-forward: скорость планеты (численно).
            Vector3D prevPos = sim.SystemModel.BodyPosition(bodyIndex, sim.SimTime - Dt);
            Vector3D bodyVelocity = (targetPos - prevPos) * (1.0 / Dt);

            Vector3D desiredVelocity = bodyVelocity + (toTarget.Normalized() * Math.Min(30.0, distance * 3.0));
            Vector3D accelCommand = (desiredVelocity - snap.Velocity) * 0.5;
            Vector3D worldThrust = accelCommand.Length > 1.0 ? accelCommand.Normalized() : accelCommand;

            // Тяга теперь в локальной системе корабля — раскладываем мировой вектор по ориентации.
            (double forward, double strafe, double lift) = ShipAxes.WorldToLocal(snap.Orientation, worldThrust);
            sim.Enqueue(new MoveCommand(1, seq++, forward, strafe, lift));
            sim.Advance(Dt);
        }
    }

    private static string RunScript()
    {
        var sim = new WorldSimulation(0x4E4156UL);
        sim.Enqueue(new SetTargetCommand(1, 0, 1));
        sim.Enqueue(new MoveCommand(1, 1, 0.3, 0.2, -0.1));
        for (int i = 0; i < 90; i++)
        {
            sim.Advance(Dt);
        }

        sim.Enqueue(new DockCommand(1, 2));
        sim.Advance(Dt);

        SimulationSnapshot snap = sim.CaptureSnapshot();
        return $"{snap.Tick}|{snap.SimTime:0.000}|{snap.Position.X:0.0000},{snap.Position.Y:0.0000},{snap.Position.Z:0.0000}|" +
               $"{snap.Velocity.Length:0.0000}|{snap.TargetBodyIndex}|{snap.DockedBodyIndex}";
    }
}
