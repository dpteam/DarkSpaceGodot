using DarkSpace.Core.Data;
using DarkSpace.Core.Generation;
using DarkSpace.Core.Systems.Orbital;
using Xunit;

namespace DarkSpace.Tests.Integration;

/// <summary>
/// Интеграционные тесты орбитальной подсистемы: орбиты для всех планет выборки систем галактики
/// детерминированы и физически корректны.
/// </summary>
public sealed class OrbitalPipelineIntegrationTests
{
    private const ulong WorldSeed = 0x0B17A1UL; // "ORBITA"

    [Fact]
    public void AllSampledSystems_ProduceValidDeterministicOrbits()
    {
        GalaxyData galaxy = GalaxyGenerator.GenerateGalaxy(WorldSeed, 0);
        int sample = Math.Min(galaxy.StarCount, 12);

        for (int s = 0; s < sample; s++)
        {
            StarSystemData system = StarSystemGenerator.GenerateSystem(galaxy, s);
            var first = OrbitGenerator.GenerateOrbits(system);
            var second = OrbitGenerator.GenerateOrbits(system);

            Assert.Equal(first, second);
            Assert.Equal(system.PlanetCount, first.Count);

            foreach (OrbitData orbit in first)
            {
                double periapsis = orbit.SemiMajorAxis * (1.0 - orbit.Eccentricity);
                double apoapsis = orbit.SemiMajorAxis * (1.0 + orbit.Eccentricity);

                // Точка орбиты всегда лежит в границах перицентра/апоцентра.
                Vector3D position = OrbitalMechanics.PositionAt(orbit, 0.0);
                double r = position.Length;
                Assert.InRange(r, periapsis - 1e-3, apoapsis + 1e-3);
            }
        }
    }
}
