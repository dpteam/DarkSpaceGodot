using DarkSpace.Core.Systems.Alchemy;
using Xunit;

namespace DarkSpace.Tests.Unit;

/// <summary>Тесты алхимической системы ресурсов.</summary>
public sealed class AlchemySystemTests
{
    [Fact]
    public void Catalog_HasAtLeastTwelveElements()
    {
        Assert.InRange(ElementCatalog.Count, 12, 16);
    }

    [Fact]
    public void Combine_IsCommutative()
    {
        Element a = ElementCatalog.GetById(2);
        Element b = ElementCatalog.GetById(7);
        CombinationResult first = AlchemySystem.Combine(a, b);
        CombinationResult second = AlchemySystem.Combine(b, a);
        Assert.Equal(first, second);
    }

    [Fact]
    public void Combine_HydrogenAndUranium_ProducesRadioactiveWater()
    {
        Element hydrogen = ElementCatalog.FindByStem("Гидро")!;
        Element uranium = ElementCatalog.FindByStem("Урано")!;
        CombinationResult result = AlchemySystem.Combine(hydrogen, uranium);

        Assert.True(result.IsNovel);
        Assert.Equal("Радиоактивная Вода", result.Name);
        Assert.True(result.IsDangerous);
        Assert.False(string.IsNullOrWhiteSpace(result.Description));
    }

    [Fact]
    public void Combine_GenericPair_GeneratesCompoundName()
    {
        Element silicon = ElementCatalog.FindByStem("Сили")!;
        Element iron = ElementCatalog.FindByStem("Ферро")!;
        CombinationResult result = AlchemySystem.Combine(silicon, iron);

        Assert.True(result.IsNovel);
        Assert.Contains("-", result.Name);
        Assert.True(result.Value > 0);
    }

    [Fact]
    public void Combine_UnstablePair_IsNotNovel()
    {
        Element darkMatter = ElementCatalog.FindByStem("Нигро")!;
        Element philosopherStone = ElementCatalog.FindByStem("Панацея")!;
        CombinationResult result = AlchemySystem.Combine(darkMatter, philosopherStone);

        Assert.False(result.IsNovel);
    }

    [Fact]
    public void Combine_IsDeterministic()
    {
        Element a = ElementCatalog.GetById(3);
        Element b = ElementCatalog.GetById(9);
        Assert.Equal(AlchemySystem.Combine(a, b), AlchemySystem.Combine(a, b));
    }

    [Fact]
    public void EnumerateAllCombinations_ReturnsUniqueValuedResults()
    {
        var results = AlchemySystem.EnumerateAllCombinations();
        int expected = ElementCatalog.Count * (ElementCatalog.Count + 1) / 2;
        Assert.Equal(expected, results.Count);
        Assert.All(results, r => Assert.False(string.IsNullOrWhiteSpace(r.Name)));
    }

    [Fact]
    public void ElementCatalog_InvalidId_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => ElementCatalog.GetById(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => ElementCatalog.GetById(ElementCatalog.Count));
    }
}
