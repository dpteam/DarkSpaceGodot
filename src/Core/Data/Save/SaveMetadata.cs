using System;

namespace DarkSpace.Core.Data.Save;

/// <summary>
/// Метаданные сохранения (заголовок, читаемый без полной загрузки).
/// </summary>
public sealed class SaveMetadata
{
    /// <summary>Имя игрока.</summary>
    public string PlayerName { get; set; } = "Пилот без имени";

    /// <summary>Дата и время создания сохранения (UTC).</summary>
    public DateTimeOffset SavedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>Индекс текущей галактики (0–255).</summary>
    public int GalaxyIndex { get; set; }

    /// <summary>Индекс текущей звёздной системы.</summary>
    public int SystemIndex { get; set; }

    /// <summary>Индекс текущей планеты.</summary>
    public int PlanetIndex { get; set; }

    /// <summary>Игровое время в секундах (для статистики и событий).</summary>
    public double PlayTimeSeconds { get; set; }
}
