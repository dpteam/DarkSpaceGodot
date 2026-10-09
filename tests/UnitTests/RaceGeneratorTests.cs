using DarkSpace.Core.Data;
using DarkSpace.Core.Generation;
using Xunit;

namespace DarkSpace.Tests.Unit;

/// <summary>Тесты процедурной генерации рас.</summary>
public sealed class RaceGeneratorTests
{
    [Fact]
    public void GenerateGalaxyRaces_IsDeterministic()
    {
        var a = RaceGenerator.GenerateGalaxyRaces(0xABCDEF);
        var b = RaceGenerator.GenerateGalaxyRaces(0xABCDEF);

        Assert.Equal(a.Count, b.Count);
        for (int i = 0; i < a.Count; i++)
        {
            // RaceData содержит список черт, поэтому сравниваем через проекцию.
            Assert.Equal(Project(a[i]), Project(b[i]));
        }
    }

    private static object Project(RaceData r) => new
    {
        r.Archetype,
        r.Name,
        r.Culture,
        Traits = string.Join('|', r.Traits),
        r.HomeworldPreference,
        r.Description,
        r.Seed,
    };

    [Fact]
    public void GenerateGalaxyRaces_CountInRange()
    {
        var races = RaceGenerator.GenerateGalaxyRaces(1234);
        Assert.InRange(races.Count, RaceGenerator.MinRacesPerGalaxy, RaceGenerator.MaxRacesPerGalaxy);
    }

    [Fact]
    public void GenerateGalaxyRaces_ArchetypesAreUnique()
    {
        var races = RaceGenerator.GenerateGalaxyRaces(777);
        var distinct = races.Select(r => r.Archetype).Distinct().Count();
        Assert.Equal(races.Count, distinct);
    }

    [Fact]
    public void GenerateGalaxyRaces_AllFieldsPopulated()
    {
        var races = RaceGenerator.GenerateGalaxyRaces(42);
        Assert.All(races, r =>
        {
            Assert.False(string.IsNullOrWhiteSpace(r.Name));
            Assert.False(string.IsNullOrWhiteSpace(r.Culture));
            Assert.False(string.IsNullOrWhiteSpace(r.Description));
            Assert.False(string.IsNullOrWhiteSpace(r.HomeworldPreference));
            Assert.NotEmpty(r.Traits);
        });
    }

    [Theory]
    [InlineData(RaceArchetype.Human)]
    [InlineData(RaceArchetype.Reptilian)]
    [InlineData(RaceArchetype.Insectoid)]
    [InlineData(RaceArchetype.EnergyBeing)]
    [InlineData(RaceArchetype.Cyborg)]
    [InlineData(RaceArchetype.Synthetic)]
    [InlineData(RaceArchetype.Amphibious)]
    [InlineData(RaceArchetype.Fungoid)]
    public void GenerateRace_AllArchetypes_ProduceValidData(RaceArchetype archetype)
    {
        RaceData race = RaceGenerator.GenerateRace(99, archetype, 0);
        Assert.Equal(archetype, race.Archetype);
        Assert.NotEmpty(race.Traits);
        Assert.False(string.IsNullOrWhiteSpace(race.Description));
    }
}
