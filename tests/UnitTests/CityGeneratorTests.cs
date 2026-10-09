using DarkSpace.Core.Data;
using DarkSpace.Core.Generation;
using Xunit;

namespace DarkSpace.Tests.Unit;

/// <summary>Тесты процедурной генерации поселений.</summary>
public sealed class CityGeneratorTests
{
    private static (StarSystemData System, PlanetData Planet) Sample()
    {
        GalaxyData galaxy = GalaxyGenerator.GenerateGalaxy(0xC17E, 2);
        StarSystemData system = StarSystemGenerator.GenerateSystem(galaxy, 0);
        PlanetData planet = PlanetGenerator.GeneratePlanet(system, 0);
        return (system, planet);
    }

    [Fact]
    public void GenerateCities_IsDeterministic()
    {
        (StarSystemData system, PlanetData planet) = Sample();
        var a = CityGenerator.GenerateCities(system, planet);
        var b = CityGenerator.GenerateCities(system, planet);

        Assert.Equal(a.Count, b.Count);
        for (int i = 0; i < a.Count; i++)
        {
            Assert.Equal(Project(a[i]), Project(b[i]));
        }
    }

    [Fact]
    public void GenerateCities_AllFieldsValid()
    {
        (StarSystemData system, PlanetData planet) = Sample();
        var cities = CityGenerator.GenerateCities(system, planet);

        Assert.All(cities, c =>
        {
            Assert.False(string.IsNullOrWhiteSpace(c.Name));
            Assert.False(string.IsNullOrWhiteSpace(c.Description));
            Assert.True(c.Population > 0);
            Assert.NotEmpty(c.Infrastructure);
        });
    }

    [Fact]
    public void GenerateCity_NullArgs_Throw()
    {
        (StarSystemData system, PlanetData planet) = Sample();
        Assert.Throws<ArgumentNullException>(() => CityGenerator.GenerateCities(null!, planet));
        Assert.Throws<ArgumentNullException>(() => CityGenerator.GenerateCities(system, null!));
    }

    [Fact]
    public void GenerateCity_InfrastructureHasNoDuplicates()
    {
        (StarSystemData system, PlanetData planet) = Sample();
        CityData city = CityGenerator.GenerateCity(system, planet, 0);
        var distinct = city.Infrastructure.Distinct().Count();
        Assert.Equal(city.Infrastructure.Count, distinct);
    }

    [Fact]
    public void GenerateCities_SpaceportImpliesLargePopulation()
    {
        // Перебираем планеты, пока не найдём крупное поселение с космопортом.
        GalaxyData galaxy = GalaxyGenerator.GenerateGalaxy(0xBEE, 1);
        StarSystemData system = StarSystemGenerator.GenerateSystem(galaxy, 0);
        for (int p = 0; p < system.PlanetCount; p++)
        {
            PlanetData planet = PlanetGenerator.GeneratePlanet(system, p);
            foreach (CityData city in CityGenerator.GenerateCities(system, planet))
            {
                if (city.Infrastructure.Contains(InfrastructureType.Spaceport))
                {
                    Assert.True(city.Population > 10_000, "Космопорт подразумевает немалое население.");
                    return;
                }
            }
        }
    }

    private static object Project(CityData c) => new
    {
        c.Name,
        c.Type,
        c.Population,
        c.Faction,
        c.Economy,
        Infrastructure = string.Join('|', c.Infrastructure),
        c.Description,
        c.Seed,
    };
}
