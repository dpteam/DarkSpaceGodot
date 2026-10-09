using System.Collections.Generic;
using DarkSpace.Core.Data;
using DarkSpace.Core.Data.Save;
using DarkSpace.Core.Generation;
using DarkSpace.Core.Utils.Logging;

namespace DarkSpace.Gameplay;

/// <summary>
/// Одна игровая сессия: хранит мировой сид и предоставляет доступ к генерации и сохранениям.
/// </summary>
/// <remarks>
/// Это тонкая «обёртка» над чистым ядром: сама логика генерации живёт в
/// <c>DarkSpace.Core.Generation</c> и не зависит от Godot.
/// </remarks>
public sealed class GameSession
{
    /// <summary>Мировой сид текущей вселенной.</summary>
    public ulong WorldSeed { get; }

    /// <summary>Менеджер сохранений сессии.</summary>
    public SaveManager SaveManager { get; }

    /// <summary>
    /// Создаёт новую игровую сессию.
    /// </summary>
    /// <param name="worldSeed">Мировой сид вселенной.</param>
    /// <param name="saveDirectory">Каталог сохранений (в слое движка — путь к <c>user://saves</c>).</param>
    /// <param name="maxBackups">Максимальное число хранимых бэкапов.</param>
    public GameSession(
        ulong worldSeed,
        string saveDirectory,
        int maxBackups = SaveBackupManager.DefaultMaxBackups)
    {
        WorldSeed = worldSeed;
        SaveManager = new SaveManager(saveDirectory, maxBackups);
    }

    /// <summary>Генерирует галактику по её индексу.</summary>
    public GalaxyData GetGalaxy(int galaxyIndex)
        => GalaxyGenerator.GenerateGalaxy(WorldSeed, galaxyIndex);

    /// <summary>Генерирует набор разумных рас для галактики.</summary>
    /// <param name="galaxy">Галактика.</param>
    public IReadOnlyList<RaceData> GetGalaxyRaces(GalaxyData galaxy)
    {
        ArgumentNullException.ThrowIfNull(galaxy);
        return RaceGenerator.GenerateGalaxyRaces(galaxy.Seed);
    }

    /// <summary>Генерирует корабль заданного класса по сиду галактики.</summary>
    /// <param name="galaxy">Галактика.</param>
    /// <param name="type">Класс корабля.</param>
    public ShipData GetShip(GalaxyData galaxy, ShipType type)
    {
        ArgumentNullException.ThrowIfNull(galaxy);
        return ShipGenerator.GenerateShip(galaxy.Seed, type);
    }

    /// <summary>Генерирует галактику, логируя результат.</summary>
    public GalaxyData GetGalaxyLogged(int galaxyIndex)
    {
        GalaxyData galaxy = GetGalaxy(galaxyIndex);
        Log.Info("Universe", $"Галактика #{galaxy.Index} «{galaxy.Name}»: {galaxy.Type}, " +
                             $"{galaxy.StarCount} систем, плотность {galaxy.Density:0.00}.");
        return galaxy;
    }
}
