using DarkSpace.Core.Data;
using DarkSpace.Core.Generation;
using DarkSpace.Core.Systems.Travel;
using Xunit;

namespace DarkSpace.Tests.Unit;

/// <summary>Тесты механики прыжка через чёрную дыру.</summary>
public sealed class BlackHoleTravelTests
{
    [Fact]
    public void ComputeProtection_StaysWithinUnitRange()
    {
        foreach (ShipType type in Enum.GetValues<ShipType>())
        {
            ShipData ship = ShipGenerator.GenerateShip(42, type);
            double protection = BlackHoleTravel.ComputeProtection(ship);
            Assert.InRange(protection, 0.0, 1.0);
        }
    }

    [Fact]
    public void ComputeJump_IsDeterministic()
    {
        ShipData ship = ShipGenerator.GenerateShip(7, ShipType.Cruiser);
        JumpResult a = BlackHoleTravel.ComputeJump(123, ship, 5);
        JumpResult b = BlackHoleTravel.ComputeJump(123, ship, 5);
        Assert.Equal(a, b);
    }

    [Fact]
    public void ComputeJump_TargetGalaxyIsPreserved()
    {
        ShipData ship = ShipGenerator.GenerateShip(1, ShipType.Scout);
        JumpResult result = BlackHoleTravel.ComputeJump(9, ship, 256);
        Assert.Equal(256, result.TargetGalaxyIndex);
        Assert.False(string.IsNullOrWhiteSpace(result.Narrative));
    }

    [Fact]
    public void StrongShip_SurvivesMoreOftenThanWeakShip()
    {
        ShipData strong = ShipGenerator.GenerateShip(1, ShipType.Battleship);
        ShipData weak = ShipGenerator.GenerateShip(1, ShipType.Courier);

        int strongDeaths = CountDeaths(strong);
        int weakDeaths = CountDeaths(weak);

        Assert.True(strongDeaths <= weakDeaths,
            $"Сильный корабль должен гибнуть не чаще слабого (strong={strongDeaths}, weak={weakDeaths}).");
    }

    [Fact]
    public void ComputeJump_NullShip_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => BlackHoleTravel.ComputeJump(1, null!, 0));
    }

    private static int CountDeaths(ShipData ship)
    {
        int deaths = 0;
        for (ulong seed = 0; seed < 500; seed++)
        {
            if (BlackHoleTravel.ComputeJump(seed, ship, 1).Outcome == JumpOutcome.Destroyed)
            {
                deaths++;
            }
        }

        return deaths;
    }
}
