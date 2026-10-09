using System.Collections.Generic;
using System.Linq;

namespace DarkSpace.Core.Systems.Economy;

/// <summary>
/// Каталог торговых товаров станции (в духе Elite: 17 базовых коммодити).
/// </summary>
/// <remarks>
/// Значения базовой цены, экономического фактора и базового запаса портированы из таблицы
/// рынка Elite (QQ23) и адаптированы под проект. Порядок соответствует возрастанию <see cref="Commodity.Id"/>.
/// </remarks>
public static class CommodityCatalog
{
    private static readonly Commodity[] Entries =
    {
        new(0, "Продовольствие", "т", 19, -2, 6),
        new(1, "Текстиль", "т", 20, -1, 10),
        new(2, "Радиоактивы", "т", 65, -3, 2),
        new(3, "Рабо-дроны", "т", 40, -5, 226),
        new(4, "Напитки", "т", 83, -5, 251),
        new(5, "Предметы роскоши", "т", 196, 8, 54),
        new(6, "Редкие виды", "т", 235, 29, 8),
        new(7, "Компьютеры", "т", 154, 14, 56),
        new(8, "Механизмы", "т", 117, 6, 40),
        new(9, "Сплавы", "т", 78, 1, 17),
        new(10, "Оружие", "т", 124, 13, 29),
        new(11, "Меха", "т", 176, -9, 220),
        new(12, "Минералы", "т", 32, -1, 53),
        new(13, "Золото", "кг", 97, -1, 66),
        new(14, "Платина", "кг", 171, -2, 55),
        new(15, "Самоцветы", "г", 45, -1, 250),
        new(16, "Ксено-артефакты", "т", 53, 15, 192),
    };

    /// <summary>Количество товаров в каталоге.</summary>
    public static int Count => Entries.Length;

    /// <summary>Возвращает товар по идентификатору.</summary>
    /// <exception cref="System.ArgumentOutOfRangeException">Если идентификатор вне диапазона.</exception>
    public static Commodity GetById(int id)
    {
        if (id < 0 || id >= Entries.Length)
        {
            throw new System.ArgumentOutOfRangeException(nameof(id), id, $"Нет товара с id={id}.");
        }

        return Entries[id];
    }

    /// <summary>Полный список товаров (только для чтения).</summary>
    public static IReadOnlyList<Commodity> All => Entries;

    /// <summary>Возвращает все товары (для перебора в UI).</summary>
    public static IEnumerable<Commodity> Enumerate() => Entries.ToList();
}
