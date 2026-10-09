using DarkSpace.Core.Generation;
using DarkSpace.Core.Utils;
using Xunit;

namespace DarkSpace.Tests.Unit;

/// <summary>Тесты процедурного генератора имён.</summary>
public sealed class NameGeneratorTests
{
    [Fact]
    public void GalaxyName_IsDeterministic()
    {
        string a = NameGenerator.GalaxyName(new DeterministicRandom(10));
        string b = NameGenerator.GalaxyName(new DeterministicRandom(10));
        Assert.Equal(a, b);
    }

    [Fact]
    public void AllNameKinds_AreNonEmpty()
    {
        var random = new DeterministicRandom(2026);
        Assert.False(string.IsNullOrWhiteSpace(NameGenerator.GalaxyName(random)));
        Assert.False(string.IsNullOrWhiteSpace(NameGenerator.StarSystemName(random)));
        Assert.False(string.IsNullOrWhiteSpace(NameGenerator.PlanetName(random)));
        Assert.False(string.IsNullOrWhiteSpace(NameGenerator.ShipName(random)));
        Assert.False(string.IsNullOrWhiteSpace(NameGenerator.RaceName(random)));
        Assert.False(string.IsNullOrWhiteSpace(NameGenerator.CharacterName(random)));
    }

    [Fact]
    public void PlanetName_ContainsRomanNumeral()
    {
        string name = NameGenerator.PlanetName(new DeterministicRandom(5));
        Assert.Matches(@"\b(I|II|III|IV|V|VI|VII|VIII|IX|X|XI|XII|XIII|XIV|XV|XVI|XVII|XVIII|XIX|XX)$", name);
    }

    [Fact]
    public void ShipName_IsQuoted()
    {
        string name = NameGenerator.ShipName(new DeterministicRandom(3));
        Assert.StartsWith("«", name);
        Assert.EndsWith("»", name);
    }

    [Fact]
    public void GetDictionaries_ReturnsNonEmptyDictionaries()
    {
        var dictionaries = NameGenerator.GetDictionaries();
        Assert.NotEmpty(dictionaries);
        Assert.All(dictionaries.Values, list => Assert.NotEmpty(list));
    }
}
