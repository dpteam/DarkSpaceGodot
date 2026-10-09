using DarkSpace.Core.Data;
using DarkSpace.Core.Data.Save;
using Xunit;

namespace DarkSpace.Tests.Unit;

/// <summary>Тесты сериализации и миграций сохранений.</summary>
public sealed class SaveSerializerTests
{
    [Fact]
    public void RoundTrip_PreservesData()
    {
        SaveGame original = SaveGame.CreateNew("Тестовый Пилот", 0x1234);
        original.Metadata.GalaxyIndex = 5;
        original.Metadata.SystemIndex = 2;
        original.Player.Credits = 4321.5;
        original.Player.Reputation[FactionType.VoidSyndicate] = -300;
        original.Player.Inventory["Радиоактивная Вода"] = 3;
        original.Ship.Name = "«Утренняя Звезда»";
        original.World.DiscoveredPlanets.Add("5:2:0");

        string json = SaveSerializer.ToJson(original);
        SaveGame? restored = SaveSerializer.FromJson(json);

        Assert.NotNull(restored);
        Assert.Equal(original.Version, restored!.Version);
        Assert.Equal("Тестовый Пилот", restored.Metadata.PlayerName);
        Assert.Equal(5, restored.Metadata.GalaxyIndex);
        Assert.Equal(4321.5, restored.Player.Credits);
        Assert.Equal(-300, restored.Player.Reputation[FactionType.VoidSyndicate]);
        Assert.Equal(3, restored.Player.Inventory["Радиоактивная Вода"]);
        Assert.Equal("«Утренняя Звезда»", restored.Ship.Name);
        Assert.Contains("5:2:0", restored.World.DiscoveredPlanets);
    }

    [Fact]
    public void ToJson_WritesMandatoryVersion()
    {
        SaveGame save = SaveGame.CreateNew("A", 1);
        save.Version = 0; // портим вручную
        string json = SaveSerializer.ToJson(save);
        Assert.Contains("\"Version\"", json);
        Assert.Contains(SaveGame.CurrentVersion.ToString(), json);
    }

    [Fact]
    public void FromJson_EmptyOrWhitespace_ReturnsNull()
    {
        Assert.Null(SaveSerializer.FromJson(string.Empty));
        Assert.Null(SaveSerializer.FromJson("   "));
    }

    [Fact]
    public void FromJson_Corrupted_ThrowsSaveCorruptedException()
    {
        Assert.Throws<SaveCorruptedException>(() => SaveSerializer.FromJson("{ это не json"));
    }

    [Fact]
    public void Migrate_UpdatesVersionToCurrent()
    {
        var save = new SaveGame { Version = 0 };
        SaveGame migrated = SaveMigrations.Migrate(save);
        Assert.Equal(SaveGame.CurrentVersion, migrated.Version);
    }

    [Fact]
    public void Migrate_FutureVersion_Throws()
    {
        var save = new SaveGame { Version = SaveGame.CurrentVersion + 1 };
        Assert.Throws<SaveCorruptedException>(() => SaveMigrations.Migrate(save));
    }
}
