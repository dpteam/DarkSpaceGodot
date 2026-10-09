using DarkSpace.Core.Data;
using DarkSpace.Core.Generation;
using Xunit;

namespace DarkSpace.Tests.Unit;

/// <summary>Тесты процедурной генерации галактик.</summary>
public sealed class GalaxyGeneratorTests
{
    [Fact]
    public void GenerateGalaxy_IsDeterministic()
    {
        GalaxyData a = GalaxyGenerator.GenerateGalaxy(0xDEADBEEF, 7);
        GalaxyData b = GalaxyGenerator.GenerateGalaxy(0xDEADBEEF, 7);
        Assert.Equal(a, b);
    }

    [Fact]
    public void GenerateGalaxy_DifferentIndices_Differ()
    {
        GalaxyData a = GalaxyGenerator.GenerateGalaxy(1234, 1);
        GalaxyData b = GalaxyGenerator.GenerateGalaxy(1234, 2);
        Assert.NotEqual(a.Seed, b.Seed);
    }

    [Fact]
    public void GenerateGalaxy_StarCountInValidRange()
    {
        for (int i = 0; i < GalaxyGenerator.MaxGalaxyCount; i++)
        {
            GalaxyData galaxy = GalaxyGenerator.GenerateGalaxy(999, i);
            Assert.InRange(galaxy.StarCount, 40, 400);
            Assert.InRange(galaxy.Density, 0.2, 1.0);
            Assert.False(string.IsNullOrWhiteSpace(galaxy.Name));
        }
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(256)]
    [InlineData(1000)]
    public void GenerateGalaxy_IndexOutOfRange_Throws(int index)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => GalaxyGenerator.GenerateGalaxy(1, index));
    }

    [Fact]
    public void GenerateUniverse_Returns256Galaxies()
    {
        GalaxyData[] universe = GalaxyGenerator.GenerateUniverse(2026);
        Assert.Equal(GalaxyGenerator.MaxGalaxyCount, universe.Length);
        for (int i = 0; i < universe.Length; i++)
        {
            Assert.Equal(i, universe[i].Index);
        }
    }

    [Fact]
    public void GenerateUniverse_IsDeterministic()
    {
        GalaxyData[] a = GalaxyGenerator.GenerateUniverse(777);
        GalaxyData[] b = GalaxyGenerator.GenerateUniverse(777);
        Assert.Equal(a, b);
    }

    [Fact]
    public void IsOdyalutaiIndex_Detects256()
    {
        Assert.True(GalaxyGenerator.IsOdyalutaiIndex(256));
        Assert.False(GalaxyGenerator.IsOdyalutaiIndex(255));
    }

    [Fact]
    public void GenerateUniverse_ProducesVariedTypes()
    {
        GalaxyData[] universe = GalaxyGenerator.GenerateUniverse(123);
        var distinctTypes = universe.Select(g => g.Type).Distinct().Count();
        Assert.True(distinctTypes >= 4, $"Ожидалось разнообразие типов, получено {distinctTypes}.");
    }
}
