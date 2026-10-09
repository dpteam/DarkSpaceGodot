using DarkSpace.Core.Data;
using DarkSpace.Core.Simulation;
using DarkSpace.Core.Systems.Physics;
using Xunit;

namespace DarkSpace.Tests.Unit;

/// <summary>
/// Тесты модели полёта: кватернионы ориентации, угловая динамика и тяга вдоль носа.
/// </summary>
/// <remarks>
/// Проверяются математические свойства (норма кватерниона, сохранение длины при повороте) и
/// игровые принципы Frontier: тяга идёт вдоль носа, вращение обладает инерцией и без стабилизаторов
/// сохраняет угловой момент.
/// </remarks>
public sealed class FlightModelTests
{
    private const double Dt = 1.0 / 120.0;

    // ---------- Кватернионы ----------

    [Fact]
    public void Quaternion_Rotate_PreservesLength()
    {
        var q = new Quaternion(0.5, 0.5, 0.5, 0.5); // норма 1
        var v = new Vector3D(3.0, -2.0, 5.0);
        double before = v.Length;
        double after = q.Rotate(v).Length;
        Assert.Equal(before, after, precision: 9);
    }

    [Fact]
    public void Quaternion_Identity_DoesNothing()
    {
        var v = new Vector3D(1.0, 2.0, 3.0);
        Assert.Equal(v, Quaternion.Identity.Rotate(v));
    }

    [Fact]
    public void Quaternion_Rotation90AboutZ_MapsXToY()
    {
        var q = Quaternion.FromAxisAngle(new Vector3D(0.0, 0.0, 1.0), Math.PI / 2.0);
        Vector3D r = q.Rotate(new Vector3D(1.0, 0.0, 0.0));
        Assert.Equal(0.0, r.X, precision: 9);
        Assert.Equal(1.0, r.Y, precision: 9);
        Assert.Equal(0.0, r.Z, precision: 9);
    }

    [Fact]
    public void Quaternion_Integrate_KeepsUnitLength()
    {
        Quaternion q = Quaternion.Identity;
        var omega = new Vector3D(0.7, -0.3, 0.5);
        for (int i = 0; i < 1000; i++)
        {
            q = q.Integrate(omega, Dt);
        }

        Assert.Equal(1.0, q.Length, precision: 9);
    }

    // ---------- Тяга вдоль носа ----------

    [Fact]
    public void NoseThrust_WithRotatedOrientation_AcceleratesAlongNose()
    {
        var sim = new WorldSimulation(1);

        // Разворачиваем корабль: нос уходит из исходного направления в сторону.
        var q = Quaternion.FromAxisAngle(new Vector3D(0.0, 0.0, 1.0), -Math.PI / 2.0);
        sim.InitializeShip(WorldSimulation.ShipSpawn, Vector3D.Zero, q, Vector3D.Zero);

        sim.Enqueue(new MoveCommand(1, 0, 1.0, 0.0, 0.0));
        for (int i = 0; i < 60; i++)
        {
            sim.Advance(Dt);
        }

        SimulationSnapshot s = sim.CaptureSnapshot();
        Vector3D nose = s.Orientation.Rotate(ShipAxes.Forward);
        Assert.True(s.Velocity.Length > 0);
        Assert.True(Vector3D.Dot(s.Velocity, nose) > 0, "Скорость должна быть направлена вдоль носа корабля.");
    }

    // ---------- Угловая динамика ----------

    [Fact]
    public void Rotation_ChangesOrientation()
    {
        var sim = new WorldSimulation(1);
        sim.InitializeShip(WorldSimulation.ShipSpawn, Vector3D.Zero);
        Quaternion start = sim.Orientation;

        sim.Enqueue(new RotateCommand(1, 0, 0.0, 0.0, 1.0)); // крен
        for (int i = 0; i < 30; i++)
        {
            sim.Advance(Dt);
        }

        Assert.NotEqual(start, sim.Orientation);
        Assert.True(sim.AngularVelocity.Length > 0);
    }

    [Fact]
    public void AngularMomentum_IsConserved_WithoutStabilizers()
    {
        // При отключённых стабилизаторах свободное вращение сохраняет угловую скорость (угловой момент).
        var sim = new WorldSimulation(1) { AngularAssistDamping = 0.0 };
        sim.InitializeShip(WorldSimulation.ShipSpawn, Vector3D.Zero);

        sim.Enqueue(new RotateCommand(1, 0, 0.4, 0.0, 0.0));
        sim.Advance(Dt);
        sim.Enqueue(new RotateCommand(1, 1, 0.0, 0.0, 0.0)); // отпускаем управление
        sim.Advance(Dt);

        Vector3D h0 = sim.AngularVelocity;
        Assert.True(h0.Length > 0);

        for (int i = 0; i < 500; i++)
        {
            sim.Advance(Dt);
        }

        Vector3D h1 = sim.AngularVelocity;
        Assert.Equal(h0.X, h1.X, precision: 9);
        Assert.Equal(h0.Y, h1.Y, precision: 9);
        Assert.Equal(h0.Z, h1.Z, precision: 9);
    }

    [Fact]
    public void Stabilizers_BringRotationToRest()
    {
        var sim = new WorldSimulation(1) { AngularAssistDamping = ShipPhysicsProfile.Default.AngularDamping };
        sim.InitializeShip(WorldSimulation.ShipSpawn, Vector3D.Zero);

        sim.Enqueue(new RotateCommand(1, 0, 0.5, 0.0, 0.0));
        for (int i = 0; i < 20; i++)
        {
            sim.Advance(Dt);
        }

        sim.Enqueue(new RotateCommand(1, 1, 0.0, 0.0, 0.0));
        for (int i = 0; i < 600; i++)
        {
            sim.Advance(Dt);
        }

        Assert.True(sim.AngularVelocity.Length < 0.01, "Стабилизаторы должны гасить вращение.");
    }

    [Fact]
    public void Determinism_FlightModel_IsStable()
    {
        Assert.Equal(RunFlight(), RunFlight());
    }

    private static string RunFlight()
    {
        var sim = new WorldSimulation(0x51A7UL);
        sim.Enqueue(new RotateCommand(1, 0, -0.3, 0.2, 0.1));
        sim.Enqueue(new MoveCommand(1, 1, 0.8, 0.1, -0.2));
        for (int i = 0; i < 200; i++)
        {
            sim.Advance(Dt);
            if (i == 100)
            {
                sim.Enqueue(new RotateCommand(1, 2, 0.0, 0.0, 0.0));
            }
        }

        SimulationSnapshot s = sim.CaptureSnapshot();
        return $"{s.Tick}|{s.Position.X:0.0000},{s.Position.Y:0.0000},{s.Position.Z:0.0000}|" +
               $"{s.Orientation.X:0.0000},{s.Orientation.Y:0.0000},{s.Orientation.Z:0.0000},{s.Orientation.W:0.0000}|" +
               $"{s.Velocity.Length:0.0000}";
    }
}
