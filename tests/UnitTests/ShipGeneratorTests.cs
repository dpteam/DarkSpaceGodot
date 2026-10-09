using DarkSpace.Core.Data;
using DarkSpace.Core.Generation;
using Xunit;

namespace DarkSpace.Tests.Unit;

/// <summary>Тесты генерации кораблей.</summary>
public sealed class ShipGeneratorTests
{
    [Fact]
    public void GenerateShip_IsDeterministic()
    {
        ShipData a = ShipGenerator.GenerateShip(12345, ShipType.Frigate);
        ShipData b = ShipGenerator.GenerateShip(12345, ShipType.Frigate);
        // Рекорды содержат коллекции, поэтому сравниваем проект со строковой свёрткой модулей.
        Assert.Equal(Project(a), Project(b));
    }

    private static object Project(ShipData s) => new
    {
        s.Name,
        s.Type,
        s.CargoCapacity,
        s.MaxSpeed,
        s.Maneuverability,
        s.Armor,
        s.Shields,
        s.PowerDraw,
        s.JumpRange,
        Modules = string.Join('|', s.Modules),
        s.Description,
        s.Seed,
    };

    [Theory]
    [InlineData(ShipType.Scout)]
    [InlineData(ShipType.Trader)]
    [InlineData(ShipType.Frigate)]
    [InlineData(ShipType.Cruiser)]
    [InlineData(ShipType.Battleship)]
    [InlineData(ShipType.Miner)]
    [InlineData(ShipType.Courier)]
    [InlineData(ShipType.Science)]
    public void GenerateShip_AllTypes_ProduceValidData(ShipType type)
    {
        ShipData ship = ShipGenerator.GenerateShip(777, type);
        Assert.Equal(type, ship.Type);
        Assert.False(string.IsNullOrWhiteSpace(ship.Name));
        Assert.False(string.IsNullOrWhiteSpace(ship.Description));
        Assert.True(ship.CargoCapacity > 0);
        Assert.True(ship.MaxSpeed > 0);
        Assert.InRange(ship.Maneuverability, 0.0, 1.0);
        Assert.NotEmpty(ship.Modules);
    }

    [Fact]
    public void GenerateShip_Courier_FasterThanBattleship()
    {
        ShipData courier = ShipGenerator.GenerateShip(1, ShipType.Courier);
        ShipData battleship = ShipGenerator.GenerateShip(1, ShipType.Battleship);
        Assert.True(courier.MaxSpeed > battleship.MaxSpeed);
    }
}
