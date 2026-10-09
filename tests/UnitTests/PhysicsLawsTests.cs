using DarkSpace.Core.Data;
using DarkSpace.Core.Simulation;
using DarkSpace.Core.Systems.Orbital;
using DarkSpace.Core.Systems.Physics;
using Xunit;

namespace DarkSpace.Tests.Unit;

/// <summary>
/// Тесты физических законов: игра проверяется не «по картинке», а по математическим инвариантам.
/// </summary>
/// <remarks>
/// Проверяются: второй закон Ньютона, уравнение vis-viva, скорость убегания, третий закон Кеплера,
/// сохранение удельной энергии и углового момента в центральном поле, устойчивость круговой орбиты
/// и ракетное уравнение Циолковского (численно). Если модель повторяет законы нашей вселенной,
/// числа обязаны сходиться.
/// </remarks>
public sealed class PhysicsLawsTests
{
    // ---------- Формулы (Astro) ----------

    [Fact]
    public void CircularSpeed_MatchesFormula()
    {
        double mu = 400.0;
        double r = 25.0;
        Assert.Equal(Math.Sqrt(mu / r), Astro.CircularSpeed(mu, r), precision: 9);
    }

    [Fact]
    public void EscapeSpeed_IsSqrt2TimesCircular()
    {
        double mu = 500.0;
        double r = 18.0;
        Assert.Equal(Math.Sqrt(2.0) * Astro.CircularSpeed(mu, r), Astro.EscapeSpeed(mu, r), precision: 9);
    }

    [Fact]
    public void VisViva_AtCircularRadius_EqualsCircularSpeed()
    {
        double mu = 300.0;
        double a = 20.0;
        // На круговой орбите r = a => vis-viva сводится к круговой скорости.
        Assert.Equal(Astro.CircularSpeed(mu, a), Astro.VisVivaSpeed(mu, a, a), precision: 9);
    }

    [Fact]
    public void VisViva_PeriapsisFasterThanApoapsis()
    {
        double mu = 300.0;
        const double a = 20.0;
        const double e = 0.3;
        double rPeri = a * (1.0 - e);
        double rApo = a * (1.0 + e);

        double vPeri = Astro.VisVivaSpeed(mu, rPeri, a);
        double vApo = Astro.VisVivaSpeed(mu, rApo, a);

        Assert.True(vPeri > vApo, "В перицентре тело движется быстрее, чем в апоцентре.");
    }

    [Fact]
    public void OrbitalPeriod_MatchesKeplerThirdLaw()
    {
        // T = 2π·√(a³/μ), причём T²/a³ постоянно для любой орбиты вокруг одного тела.
        double mu = 250.0;
        double ratio = 0;
        for (double a = 5.0; a <= 60.0; a += 5.0)
        {
            double t = Astro.OrbitalPeriod(mu, a);
            double current = t * t / (a * a * a);
            if (ratio == 0)
            {
                ratio = current;
            }

            Assert.Equal(ratio, current, precision: 6);
        }
    }

    [Fact]
    public void RocketDeltaV_MatchesClosedForm()
    {
        // Δv = v_e·ln(m0/mf). Для m0=2, mf=1: Δv = v_e·ln2.
        double ve = 1000.0;
        double expected = ve * Math.Log(2.0);
        Assert.Equal(expected, Astro.RocketDeltaV(ve, 2.0, 1.0), precision: 6);
    }

    [Fact]
    public void MassFlowRate_MatchesFormula()
    {
        // mdot = F / v_e.
        Assert.Equal(4.0, Astro.MassFlowRate(4000.0, 1000.0), precision: 9);
    }

    // ---------- Динамика (численный интегратор) ----------

    [Fact]
    public void RocketEquation_NumericIntegration_MatchesClosedForm()
    {
        // Численно интегрируем разгон в вакууме и сравниваем достигнутую Δv с формулой Циолковского.
        ShipPhysicsProfile profile = ShipPhysicsProfile.Default;
        const double dt = 1e-3;

        double velocity = 0.0;
        double fuel = profile.FuelMass;
        while (fuel > 0.0)
        {
            // Полная тяга → ускорение a = F/m; расход mdot = F/v_e.
            velocity += profile.Acceleration(profile.Thrust, fuel) * dt;
            fuel = Math.Max(0.0, fuel - (profile.MassFlowRate(profile.Thrust) * dt));
        }

        double expected = Astro.RocketDeltaV(profile.ExhaustVelocity, profile.TotalMass, profile.DryMass);
        Assert.Equal(expected, velocity, expected * 1e-3);
    }

    [Fact]
    public void RemainingDeltaV_AtFullFuel_EqualsBudget()
    {
        ShipPhysicsProfile profile = ShipPhysicsProfile.Default;
        Assert.Equal(profile.DeltaVBudget, profile.RemainingDeltaV(profile.FuelMass), precision: 9);
    }

    [Fact]
    public void FreeFall_ConservesSpecificEnergy()
    {
        var sim = new WorldSimulation(0x0FFE1CEUL) { FlightMode = FlightMode.Newtonian };
        double mu = sim.SystemModel.Mu;
        const double r0 = 30.0;

        // Круговая орбита в сглаженном поле — минимальный радиальный дрейф.
        var position = new Vector3D(0.0, 0.0, r0);
        double v = Astro.SoftenedCircularSpeed(mu, r0, WorldSimulation.GravitySoftening);
        sim.InitializeShip(position, new Vector3D(v, 0.0, 0.0));

        double e0 = Energy(sim, mu);
        double worst = 0.0;

        const double dt = 1.0 / 240.0;
        for (int i = 0; i < 24000; i++) // 100 секунд (несколько периодов)
        {
            sim.Advance(dt);
            SimulationSnapshot s = sim.CaptureSnapshot();
            double e = Energy(s, mu);
            worst = Math.Max(worst, Math.Abs((e - e0) / e0));
        }

        Assert.True(worst < 0.01, $"Удельная энергия орбиты дрейфует на {worst:P2}, ожидалось < 1%.");
    }

    [Fact]
    public void FreeFall_ConservesAngularMomentum()
    {
        var sim = new WorldSimulation(0x0FFE1CEUL) { FlightMode = FlightMode.Newtonian };
        double mu = sim.SystemModel.Mu;
        const double r0 = 30.0;
        double v = Astro.SoftenedCircularSpeed(mu, r0, WorldSimulation.GravitySoftening);
        sim.InitializeShip(new Vector3D(0.0, 0.0, r0), new Vector3D(v, 0.0, 0.0));

        SimulationSnapshot start = sim.CaptureSnapshot();
        double h0 = Astro.SpecificAngularMomentum(start.Position, start.Velocity);

        const double dt = 1.0 / 240.0;
        for (int i = 0; i < 24000; i++)
        {
            sim.Advance(dt);
        }

        SimulationSnapshot end = sim.CaptureSnapshot();
        double h = Astro.SpecificAngularMomentum(end.Position, end.Velocity);

        Assert.True(Math.Abs((h - h0) / h0) < 1e-3, "Удельный угловой момент в центральном поле должен сохраняться.");
    }

    [Fact]
    public void CircularOrbit_RadiusStaysStable()
    {
        var sim = new WorldSimulation(0x0FFE1CEUL) { FlightMode = FlightMode.Newtonian };
        double mu = sim.SystemModel.Mu;
        const double r0 = 30.0;
        double v = Astro.SoftenedCircularSpeed(mu, r0, WorldSimulation.GravitySoftening);
        sim.InitializeShip(new Vector3D(0.0, 0.0, r0), new Vector3D(v, 0.0, 0.0));

        double min = double.MaxValue;
        double max = 0.0;
        const double dt = 1.0 / 240.0;
        for (int i = 0; i < 24000; i++)
        {
            sim.Advance(dt);
            double r = sim.CaptureSnapshot().Position.Length;
            min = Math.Min(min, r);
            max = Math.Max(max, r);
        }

        Assert.True(max < r0 * 1.02 && min > r0 * 0.98,
            $"Радиус круговой орбиты должен держаться в пределах 2% (min={min:0.00}, max={max:0.00}, r0={r0}).");
    }

    [Fact]
    public void BoundOrbit_HasNegativeEnergy()
    {
        var sim = new WorldSimulation(0x0FFE1CEUL) { FlightMode = FlightMode.Newtonian };
        double mu = sim.SystemModel.Mu;
        const double r0 = 30.0;
        // Скорость ниже круговой — тело связано гравитацией.
        double v = 0.5 * Astro.SoftenedCircularSpeed(mu, r0, WorldSimulation.GravitySoftening);
        sim.InitializeShip(new Vector3D(0.0, 0.0, r0), new Vector3D(v, 0.0, 0.0));

        Assert.True(Energy(sim, mu) < 0, "Связанная орбита должна иметь отрицательную удельную энергию.");
    }

    [Fact]
    public void EscapeOrbit_HasNonNegativeEnergy_AndEscapes()
    {
        var sim = new WorldSimulation(0x0FFE1CEUL) { FlightMode = FlightMode.Newtonian };
        double mu = sim.SystemModel.Mu;
        const double r0 = 30.0;
        // Скорость выше скорости убегания — тело покидает систему.
        double v = 2.0 * Astro.EscapeSpeed(mu, r0);
        sim.InitializeShip(new Vector3D(0.0, 0.0, r0), new Vector3D(v, 0.0, 0.0));

        Assert.True(Energy(sim, mu) >= 0, "Орбита убегания должна иметь неотрицательную энергию.");

        const double dt = 1.0 / 240.0;
        for (int i = 0; i < 6000; i++)
        {
            sim.Advance(dt);
        }

        double r = sim.CaptureSnapshot().Position.Length;
        Assert.True(r > r0 * 2.0, "Тело с энергией ≥ 0 должно удаляться от звезды.");
    }

    [Fact]
    public void GravityAcceleration_FollowsInverseSquareOutsideSoftening()
    {
        // Далеко от ядра (r >> ε) ускорение ≈ μ/r².
        double mu = 400.0;
        const double softening = 1.0;
        const double r = 100.0;
        Vector3D a = Astro.SoftenedGravityAcceleration(mu, new Vector3D(0.0, 0.0, r), softening);
        double expected = mu / (r * r);
        Assert.Equal(expected, a.Length, precision: 4);
    }

    private static double Energy(WorldSimulation sim, double mu)
    {
        SimulationSnapshot s = sim.CaptureSnapshot();
        return Astro.SoftenedSpecificEnergy(mu, WorldSimulation.GravitySoftening, s.Position, s.Velocity);
    }

    private static double Energy(SimulationSnapshot s, double mu)
        => Astro.SoftenedSpecificEnergy(mu, WorldSimulation.GravitySoftening, s.Position, s.Velocity);
}
