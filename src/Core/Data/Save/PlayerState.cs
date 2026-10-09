using System.Collections.Generic;
using DarkSpace.Core.Data;

namespace DarkSpace.Core.Data.Save;

/// <summary>
/// Состояние игрока: здоровье, деньги, репутация, инвентарь, квесты.
/// </summary>
public sealed class PlayerState
{
    /// <summary>Текущее здоровье (0–100).</summary>
    public double Health { get; set; } = 100.0;

    /// <summary>Баланс кредитов.</summary>
    public double Credits { get; set; } = 1000.0;

    /// <summary>Баланс реликвий.</summary>
    public int Relics { get; set; }

    /// <summary>Баланс тёмных кристаллов.</summary>
    public int DarkCrystals { get; set; }

    /// <summary>Репутация по фракциям (от -1000 до +1000).</summary>
    public Dictionary<FactionType, int> Reputation { get; set; } = new();

    /// <summary>Инвентарь: имя предмета → количество.</summary>
    public Dictionary<string, int> Inventory { get; set; } = new();

    /// <summary>Идентификаторы активных квестов.</summary>
    public List<string> ActiveQuests { get; set; } = new();

    /// <summary>Идентификаторы завершённых квестов.</summary>
    public List<string> CompletedQuests { get; set; } = new();
}
