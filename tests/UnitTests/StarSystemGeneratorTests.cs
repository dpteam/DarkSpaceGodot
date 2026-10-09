using DarkSpace.Core.Data;
using DarkSpace.Core.Generation;
using Xunit;

namespace DarkSpace.Tests.Unit;

/// <summary>Тесты генерации звёздных систем.</summary>
public sealed class StarSystemGeneratorTests
{
    private static GalaxyData SampleGalaxy => GalaxyGenerator.GenerateGalaxy(0xABCDEF, 3);

    [Fact]
    public void GenerateSystem_IsDeterministic()
    {
        GalaxyData galaxy = SampleGalaxy;
        StarSystemData a = StarSystemGenerator.GenerateSystem(galaxy, 0);
        StarSystemData b = StarSystemGenerator.GenerateSystem(galaxy, 0);
        Assert.Equal(a, b);
    }

    [Fact]
    public void GenerateSystem_PlanetCountInRange()
    {
        GalaxyData galaxy = SampleGalaxy;
        for (int i = 0; i < galaxy.StarCount; i++)
        {
            StarSystemData system = StarSystemGenerator.GenerateSystem(galaxy, i);
            Assert.InRange(system.PlanetCount, StarSystemGenerator.MinPlanets, StarSystemGenerator.MaxPlanets);
            Assert.False(string.IsNullOrWhiteSpace(system.Name));
            Assert.Equal(galaxy.Index, system.GalaxyIndex);
            Assert.Equal(i, system.Index);
        }
    }

    [Fact]
    public void GenerateSystem_IndexOutOfRange_Throws()
    {
        GalaxyData galaxy = SampleGalaxy;
        Assert.Throws<ArgumentOutOfRangeException>(
            () => StarSystemGenerator.GenerateSystem(galaxy, galaxy.StarCount));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => StarSystemGenerator.GenerateSystem(galaxy, -1));
    }

    [Fact]
    public void GenerateSystem_NullGalaxy_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => StarSystemGenerator.GenerateSystem(null!, 0));
    }

    [Fact]
    public void GenerateSystem_ExoticStars_Appear()
    {
        GalaxyData galaxy = GalaxyGenerator.GenerateGalaxy(1, 0);
        bool hasExotic = false;
        for (int i = 0; i < galaxy.StarCount; i++)
        {
            StarSystemData system = StarSystemGenerator.GenerateSystem(galaxy, i);
            if (system.Star is SpectralClass.BlackHole or SpectralClass.NeutronStar or SpectralClass.Binary)
            {
                hasExotic = true;
                break;
            }
        }

        Assert.True(hasExotic, "За всю галактику не встретилось ни одного экзотического объекта.");
    }
}
