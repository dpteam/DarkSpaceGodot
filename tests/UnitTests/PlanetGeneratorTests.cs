using DarkSpace.Core.Data;
using DarkSpace.Core.Generation;
using Xunit;

namespace DarkSpace.Tests.Unit;

/// <summary>Тесты генерации планет.</summary>
public sealed class PlanetGeneratorTests
{
    private static StarSystemData SampleSystem
    {
        get
        {
            GalaxyData galaxy = GalaxyGenerator.GenerateGalaxy(0x5151, 1);
            return StarSystemGenerator.GenerateSystem(galaxy, 0);
        }
    }

    [Fact]
    public void GeneratePlanet_IsDeterministic()
    {
        StarSystemData system = SampleSystem;
        PlanetData a = PlanetGenerator.GeneratePlanet(system, 0);
        PlanetData b = PlanetGenerator.GeneratePlanet(system, 0);
        // Рекорды содержат коллекции, поэтому сравниваем проект со строковой свёрткой списка ресурсов.
        Assert.Equal(Project(a), Project(b));
    }

    private static object Project(PlanetData p) => new
    {
        p.Name,
        p.Type,
        p.Gravity,
        p.Atmosphere,
        p.TemperatureCelsius,
        p.HasLife,
        p.HasAggressiveFauna,
        p.HasFlora,
        Resources = string.Join('|', p.Resources),
        p.Description,
        p.Seed,
    };

    [Fact]
    public void GeneratePlanet_HasDescriptionAndResources()
    {
        StarSystemData system = SampleSystem;
        for (int i = 0; i < system.PlanetCount; i++)
        {
            PlanetData planet = PlanetGenerator.GeneratePlanet(system, i);
            Assert.False(string.IsNullOrWhiteSpace(planet.Name));
            Assert.False(string.IsNullOrWhiteSpace(planet.Description));
            Assert.NotEmpty(planet.Resources);
            Assert.True(planet.Gravity > 0);
        }
    }

    [Fact]
    public void GeneratePlanet_PlanetNameContainsSystemName()
    {
        StarSystemData system = SampleSystem;
        PlanetData planet = PlanetGenerator.GeneratePlanet(system, 0);
        Assert.StartsWith(system.Name, planet.Name);
    }

    [Fact]
    public void GeneratePlanet_IndexOutOfRange_Throws()
    {
        StarSystemData system = SampleSystem;
        Assert.Throws<ArgumentOutOfRangeException>(
            () => PlanetGenerator.GeneratePlanet(system, system.PlanetCount));
    }

    [Fact]
    public void GeneratePlanet_NullSystem_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => PlanetGenerator.GeneratePlanet(null!, 0));
    }
}
