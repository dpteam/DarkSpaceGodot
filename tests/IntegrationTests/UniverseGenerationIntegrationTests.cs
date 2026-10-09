using System.Collections.Concurrent;
using DarkSpace.Core.Data;
using DarkSpace.Core.Generation;
using Xunit;

namespace DarkSpace.Tests.Integration;

/// <summary>
/// Интеграционные тесты процедурной генерации: полный прогон вселенной и проверка детерминизма,
/// в том числе при параллельной генерации.
/// </summary>
public sealed class UniverseGenerationIntegrationTests
{
    private const ulong WorldSeed = 0x0D41524B53504143UL; // "DARKSPAC"

    [Fact]
    public void FullUniverse_GeneratesWithoutErrors()
    {
        GalaxyData[] universe = GalaxyGenerator.GenerateUniverse(WorldSeed);
        Assert.Equal(GalaxyGenerator.MaxGalaxyCount, universe.Length);

        int totalSystems = 0;
        int totalPlanets = 0;

        foreach (GalaxyData galaxy in universe)
        {
            Assert.False(string.IsNullOrWhiteSpace(galaxy.Name));

            // Сэмплируем до 5 систем на галактику для скорости теста.
            int sample = Math.Min(galaxy.StarCount, 5);
            for (int s = 0; s < sample; s++)
            {
                StarSystemData system = StarSystemGenerator.GenerateSystem(galaxy, s);
                totalSystems++;

                for (int p = 0; p < system.PlanetCount; p++)
                {
                    PlanetData planet = PlanetGenerator.GeneratePlanet(system, p);
                    Assert.False(string.IsNullOrWhiteSpace(planet.Description));
                    totalPlanets++;
                }
            }
        }

        Assert.True(totalSystems > 0);
        Assert.True(totalPlanets > 0);
    }

    [Fact]
    public void ParallelGeneration_MatchesSequential()
    {
        GalaxyData[] sequential = GalaxyGenerator.GenerateUniverse(WorldSeed);

        var parallel = new GalaxyData[GalaxyGenerator.MaxGalaxyCount];
        Parallel.For(0, GalaxyGenerator.MaxGalaxyCount, i =>
        {
            parallel[i] = GalaxyGenerator.GenerateGalaxy(WorldSeed, i);
        });

        Assert.Equal(sequential, parallel);
    }

    [Fact]
    public void ConcurrentSystems_AreDeterministic()
    {
        GalaxyData galaxy = GalaxyGenerator.GenerateGalaxy(WorldSeed, 10);
        var sequential = new ConcurrentDictionary<int, StarSystemData>();
        for (int i = 0; i < galaxy.StarCount; i++)
        {
            sequential[i] = StarSystemGenerator.GenerateSystem(galaxy, i);
        }

        var parallel = new ConcurrentDictionary<int, StarSystemData>();
        Parallel.For(0, galaxy.StarCount, i =>
        {
            parallel[i] = StarSystemGenerator.GenerateSystem(galaxy, i);
        });

        Assert.Equal(sequential.Count, parallel.Count);
        foreach (int key in sequential.Keys)
        {
            Assert.Equal(sequential[key], parallel[key]);
        }
    }
}
