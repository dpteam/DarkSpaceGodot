using System.Collections.Generic;

namespace DarkSpace.Core.Data.Save;

/// <summary>
/// Состояние корабля игрока.
/// </summary>
public sealed class ShipState
{
    /// <summary>Имя корабля.</summary>
    public string Name { get; set; } = "Безымянный";

    /// <summary>Текущее топливо.</summary>
    public double Fuel { get; set; } = 100.0;

    /// <summary>Максимальный запас топлива.</summary>
    public double MaxFuel { get; set; } = 100.0;

    /// <summary>Текущая прочность корпуса (0–100).</summary>
    public double HullIntegrity { get; set; } = 100.0;

    /// <summary>Текущая прочность щитов (0–100).</summary>
    public double ShieldIntegrity { get; set; } = 100.0;

    /// <summary>Боеприпасы: тип → количество.</summary>
    public Dictionary<string, int> Ammunition { get; set; } = new();

    /// <summary>Установленные модули.</summary>
    public List<string> Modules { get; set; } = new();

    /// <summary>Груз: имя ресурса/предмета → количество.</summary>
    public Dictionary<string, int> Cargo { get; set; } = new();
}
