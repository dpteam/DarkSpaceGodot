using DarkSpace.Core.Utils;
using Xunit;

namespace DarkSpace.Tests.Unit;

/// <summary>Тесты смешивания сидов.</summary>
public sealed class SeedHelperTests
{
    [Fact]
    public void Combine_IsDeterministic()
    {
        ulong a = SeedHelper.Combine(1, 2);
        ulong b = SeedHelper.Combine(1, 2);
        Assert.Equal(a, b);
    }

    [Fact]
    public void Combine_OrderMatters()
    {
        Assert.NotEqual(SeedHelper.Combine(1, 2), SeedHelper.Combine(2, 1));
    }

    [Fact]
    public void Combine_WithExtra_IsDeterministic()
    {
        ulong a = SeedHelper.Combine(1, 2, 3, 4, 5);
        ulong b = SeedHelper.Combine(1, 2, 3, 4, 5);
        Assert.Equal(a, b);
    }

    [Fact]
    public void FromString_IsStable_ForKnownValue()
    {
        // FNV-1a 64 для пустой строки равен offset basis.
        Assert.Equal(0xCBF29CE484222325UL, SeedHelper.FromString(string.Empty));
        Assert.Equal(SeedHelper.FromString("Гидро"), SeedHelper.FromString("Гидро"));
        Assert.NotEqual(SeedHelper.FromString("Гидро"), SeedHelper.FromString("Урано"));
    }

    [Fact]
    public void FromString_Null_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => SeedHelper.FromString(null!));
    }

    [Fact]
    public void Derive_DiffersByTag()
    {
        Assert.NotEqual(SeedHelper.Derive(42, "alpha"), SeedHelper.Derive(42, "beta"));
    }
}
