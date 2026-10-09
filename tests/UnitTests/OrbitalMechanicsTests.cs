using DarkSpace.Core.Data;
using DarkSpace.Core.Generation;
using DarkSpace.Core.Systems.Orbital;
using Xunit;

namespace DarkSpace.Tests.Unit;

/// <summary>Тесты генерации орбит и кеплеровой механики.</summary>
public sealed class OrbitalMechanicsTests
{
    private static StarSystemData SampleSystem()
    {
        GalaxyData galaxy = GalaxyGenerator.GenerateGalaxy(0x0B170FFUL, 0);
        return StarSystemGenerator.GenerateSystem(galaxy, 0);
    }

    [Fact]
    public void GenerateOrbits_IsDeterministic()
    {
        StarSystemData system = SampleSystem();
        var a = OrbitGenerator.GenerateOrbits(system);
        var b = OrbitGenerator.GenerateOrbits(system);
        Assert.Equal(a, b);
    }

    [Fact]
    public void GenerateOrbits_OnePerPlanet_SemiMajorAxisIncreasing()
    {
        StarSystemData system = SampleSystem();
        var orbits = OrbitGenerator.GenerateOrbits(system);

        Assert.Equal(system.PlanetCount, orbits.Count);
        for (int i = 1; i < orbits.Count; i++)
        {
            Assert.True(orbits[i].SemiMajorAxis > orbits[i - 1].SemiMajorAxis,
                "Большая полуось должна расти с индексом планеты.");
        }
    }

    [Fact]
    public void GenerateOrbits_EccentricityWithinRange()
    {
        StarSystemData system = SampleSystem();
        foreach (OrbitData orbit in OrbitGenerator.GenerateOrbits(system))
        {
            Assert.InRange(orbit.Eccentricity, 0.0, 0.95);
            Assert.True(orbit.PeriodSeconds > 0);
        }
    }

    [Fact]
    public void GenerateOrbit_OutOfRange_Throws()
    {
        StarSystemData system = SampleSystem();
        Assert.Throws<ArgumentOutOfRangeException>(() => OrbitGenerator.GenerateOrbit(system, system.PlanetCount));
        Assert.Throws<ArgumentOutOfRangeException>(() => OrbitGenerator.GenerateOrbit(system, -1));
    }

    [Fact]
    public void KeplersThirdLaw_PeriodSquaredOverCubeIsConstant()
    {
        // T² / a³ должно быть одинаковым для всех орбит (закон Кеплера).
        double reference = 0;
        for (double a = 6.0; a <= 40.0; a += 2.0)
        {
            double period = OrbitGenerator.ComputePeriod(a);
            double ratio = period * period / (a * a * a);
            if (reference == 0)
            {
                reference = ratio;
            }

            Assert.Equal(reference, ratio, precision: 6);
        }
    }

    [Fact]
    public void ComputePeriod_InvalidAxis_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => OrbitGenerator.ComputePeriod(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => OrbitGenerator.ComputePeriod(-1));
    }

    [Fact]
    public void PositionAt_ReturnsToStartAfterOnePeriod()
    {
        StarSystemData system = SampleSystem();
        OrbitData orbit = OrbitGenerator.GenerateOrbit(system, 0);

        Vector3D start = OrbitalMechanics.PositionAt(orbit, 0.0);
        Vector3D afterPeriod = OrbitalMechanics.PositionAt(orbit, orbit.PeriodSeconds);

        Assert.Equal(start.X, afterPeriod.X, precision: 6);
        Assert.Equal(start.Y, afterPeriod.Y, precision: 6);
        Assert.Equal(start.Z, afterPeriod.Z, precision: 6);
    }

    [Fact]
    public void RadiusAt_StaysWithinPeriapsisAndApoapsis()
    {
        StarSystemData system = SampleSystem();
        foreach (OrbitData orbit in OrbitGenerator.GenerateOrbits(system))
        {
            double periapsis = orbit.SemiMajorAxis * (1.0 - orbit.Eccentricity);
            double apoapsis = orbit.SemiMajorAxis * (1.0 + orbit.Eccentricity);

            for (double t = 0.0; t < orbit.PeriodSeconds; t += orbit.PeriodSeconds / 20.0)
            {
                double r = OrbitalMechanics.RadiusAt(orbit, t);
                Assert.InRange(r, periapsis - 1e-3, apoapsis + 1e-3);
            }
        }
    }

    [Fact]
    public void PositionAt_IsDeterministic()
    {
        StarSystemData system = SampleSystem();
        OrbitData orbit = OrbitGenerator.GenerateOrbit(system, 2);
        Assert.Equal(OrbitalMechanics.PositionAt(orbit, 123.456), OrbitalMechanics.PositionAt(orbit, 123.456));
    }

    [Fact]
    public void NullOrbit_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => OrbitalMechanics.PositionAt(null!, 0));
        Assert.Throws<ArgumentNullException>(() => OrbitalMechanics.RadiusAt(null!, 0));
    }
}
