using DarkSpace.Core.Data;
using DarkSpace.Core.Systems.Factions;
using Xunit;

namespace DarkSpace.Tests.Unit;

/// <summary>Тесты системы репутации.</summary>
public sealed class ReputationSystemTests
{
    [Theory]
    [InlineData(-5000, ReputationSystem.Min)]
    [InlineData(5000, ReputationSystem.Max)]
    [InlineData(123, 123)]
    public void Clamp_RespectsBounds(int input, int expected)
    {
        Assert.Equal(expected, ReputationSystem.Clamp(input));
    }

    [Theory]
    [InlineData(-1000, ReputationLevel.Hated)]
    [InlineData(-600, ReputationLevel.Hated)]
    [InlineData(-599, ReputationLevel.Hostile)]
    [InlineData(-1, ReputationLevel.Unfriendly)]
    [InlineData(0, ReputationLevel.Neutral)]
    [InlineData(199, ReputationLevel.Neutral)]
    [InlineData(200, ReputationLevel.Friendly)]
    [InlineData(600, ReputationLevel.Honored)]
    [InlineData(1000, ReputationLevel.Honored)]
    public void Level_MapsThresholds(int reputation, ReputationLevel expected)
    {
        Assert.Equal(expected, ReputationSystem.Level(reputation));
    }

    [Fact]
    public void BuyPriceModifier_IsMonotonicAndBounded()
    {
        Assert.Equal(1.0, ReputationSystem.BuyPriceModifier(0));
        Assert.True(ReputationSystem.BuyPriceModifier(1000) < 1.0);
        Assert.True(ReputationSystem.BuyPriceModifier(-1000) > 1.0);
        Assert.Equal(0.80, ReputationSystem.BuyPriceModifier(1000), precision: 6);
        Assert.Equal(1.20, ReputationSystem.BuyPriceModifier(-1000), precision: 6);
    }

    [Fact]
    public void SellPriceModifier_IsInverseToBuyTrend()
    {
        Assert.True(ReputationSystem.SellPriceModifier(1000) > 1.0);
        Assert.True(ReputationSystem.SellPriceModifier(-1000) < 1.0);
    }

    [Fact]
    public void Add_Clamps()
    {
        Assert.Equal(ReputationSystem.Max, ReputationSystem.Add(950, 200));
        Assert.Equal(ReputationSystem.Min, ReputationSystem.Add(-950, -200));
    }

    [Fact]
    public void IsQuestAvailable_ChecksThreshold()
    {
        Assert.True(ReputationSystem.IsQuestAvailable(300, 200));
        Assert.False(ReputationSystem.IsQuestAvailable(100, 200));
        Assert.True(ReputationSystem.IsQuestAvailable(0, -100));
    }

    [Fact]
    public void GetGreeting_IsDeterministicAndNonEmpty()
    {
        foreach (FactionType faction in Enum.GetValues<FactionType>())
        {
            foreach (int rep in new[] { -1000, -300, 0, 300, 1000 })
            {
                string first = ReputationSystem.GetGreeting(faction, rep);
                string second = ReputationSystem.GetGreeting(faction, rep);
                Assert.Equal(first, second);
                Assert.False(string.IsNullOrWhiteSpace(first));
            }
        }
    }

    [Fact]
    public void GetGreeting_HatedFreeClans_MentionsDemocracy()
    {
        // Фракционные реплики для враждебных уровней должны отличаться от общих.
        string clans = ReputationSystem.GetGreeting(FactionType.FreeClans, -1000);
        string empire = ReputationSystem.GetGreeting(FactionType.EmpireOfDawn, -1000);
        Assert.NotEqual(clans, empire);
    }
}
