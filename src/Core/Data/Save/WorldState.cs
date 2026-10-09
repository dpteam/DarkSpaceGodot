using System.Collections.Generic;

namespace DarkSpace.Core.Data.Save;

/// <summary>
/// Состояние игрового мира: экономика, фракции, открытые локации.
/// </summary>
public sealed class WorldState
{
    /// <summary>Общий мировой сид вселенной.</summary>
    public ulong WorldSeed { get; set; }

    /// <summary>Открытые игроком планеты (ключ вида «galaxy:system:planet»).</summary>
    public HashSet<string> DiscoveredPlanets { get; set; } = new();

    /// <summary>Открытые игроком галактики.</summary>
    public HashSet<int> DiscoveredGalaxies { get; set; } = new();

    /// <summary>Зафиксированные события мира (например, «война в секторе 12»).</summary>
    public List<string> WorldEvents { get; set; } = new();

    /// <summary>Модификаторы экономики по системам (ключ «galaxy:system» → множитель цен).</summary>
    public Dictionary<string, double> EconomyModifiers { get; set; } = new();
}
