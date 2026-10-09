using DarkSpace.Core.Utils;
using Xunit;

namespace DarkSpace.Tests.Unit;

/// <summary>Тесты детерминированного генератора случайных чисел.</summary>
public sealed class DeterministicRandomTests
{
    [Fact]
    public void SameSeed_ProducesSameSequence()
    {
        var a = new DeterministicRandom(12345);
        var b = new DeterministicRandom(12345);

        for (int i = 0; i < 100; i++)
        {
            Assert.Equal(a.NextUInt64(), b.NextUInt64());
        }
    }

    [Fact]
    public void DifferentSeeds_ProduceDifferentSequences()
    {
        var a = new DeterministicRandom(1);
        var b = new DeterministicRandom(2);

        Assert.NotEqual(a.NextUInt64(), b.NextUInt64());
    }

    [Fact]
    public void NextInt_RespectsBounds()
    {
        var random = new DeterministicRandom(42);

        for (int i = 0; i < 1000; i++)
        {
            int value = random.NextInt(10, 20);
            Assert.InRange(value, 10, 19);
        }
    }

    [Fact]
    public void NextInt_InvalidRange_Throws()
    {
        var random = new DeterministicRandom(42);
        Assert.Throws<ArgumentException>(() => random.NextInt(5, 5));
    }

    [Fact]
    public void NextDouble_IsWithinUnitInterval()
    {
        var random = new DeterministicRandom(7);
        for (int i = 0; i < 1000; i++)
        {
            double value = random.NextDouble();
            Assert.InRange(value, 0.0, 1.0);
        }
    }

    [Fact]
    public void NextGaussian_IsRoughlyCentered()
    {
        var random = new DeterministicRandom(99);
        double sum = 0;
        const int count = 100_000;
        for (int i = 0; i < count; i++)
        {
            sum += random.NextGaussian();
        }

        double mean = sum / count;
        Assert.InRange(mean, -0.05, 0.05);
    }

    [Fact]
    public void Shuffle_IsDeterministic()
    {
        var a = new DeterministicRandom(555);
        var b = new DeterministicRandom(555);
        var listA = new List<int> { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
        var listB = new List<int> { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };

        a.Shuffle(listA);
        b.Shuffle(listB);

        Assert.Equal(listA, listB);
        Assert.Equal(new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 }, listA.OrderBy(x => x));
    }

    [Fact]
    public void DerivedConstructor_DiffersBySalt()
    {
        var a = new DeterministicRandom(100, 1);
        var b = new DeterministicRandom(100, 2);
        Assert.NotEqual(a.NextUInt64(), b.NextUInt64());
    }
}
