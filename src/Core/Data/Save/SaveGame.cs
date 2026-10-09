namespace DarkSpace.Core.Data.Save;

/// <summary>
/// Корневой объект сохранения игры.
/// </summary>
/// <remarks>
/// Поле <see cref="Version"/> обязательно и используется для миграции старых сохранений.
/// При изменении схемы увеличивайте <see cref="CurrentVersion"/> и добавляйте миграцию в
/// <see cref="SaveMigrations"/>.
/// </remarks>
public sealed class SaveGame
{
    /// <summary>Текущая версия схемы сохранения.</summary>
    public const int CurrentVersion = 1;

    /// <summary>Версия схемы сохранения. Обязательное поле.</summary>
    public int Version { get; set; } = CurrentVersion;

    /// <summary>Метаданные сохранения.</summary>
    public SaveMetadata Metadata { get; set; } = new();

    /// <summary>Состояние игрока.</summary>
    public PlayerState Player { get; set; } = new();

    /// <summary>Состояние корабля.</summary>
    public ShipState Ship { get; set; } = new();

    /// <summary>Состояние мира.</summary>
    public WorldState World { get; set; } = new();

    /// <summary>Создаёт новое сохранение с настройками по умолчанию.</summary>
    public static SaveGame CreateNew(string playerName, ulong worldSeed) => new()
    {
        Version = CurrentVersion,
        Metadata = new SaveMetadata { PlayerName = playerName },
        World = new WorldState { WorldSeed = worldSeed },
    };
}
