namespace DarkSpace.Core.Data;

/// <summary>
/// Описание предмета (снаряжения, модуля, расходника, реликвии).
/// </summary>
/// <param name="Id">Идентификатор базового образца из каталога.</param>
/// <param name="Name">Название (может включать префикс улучшения).</param>
/// <param name="Category">Категория предмета.</param>
/// <param name="Rarity">Редкость.</param>
/// <param name="Weight">Вес в килограммах.</param>
/// <param name="Volume">Объём в литрах.</param>
/// <param name="BaseValue">Базовая стоимость в кредитах.</param>
/// <param name="Effect">Игровой эффект.</param>
/// <param name="Description">Описание с элементами чёрного юмора.</param>
public sealed record Item(
    int Id,
    string Name,
    ItemCategory Category,
    ItemRarity Rarity,
    double Weight,
    double Volume,
    double BaseValue,
    string Effect,
    string Description);
