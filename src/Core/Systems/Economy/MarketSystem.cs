using System;
using System.Collections.Generic;
using DarkSpace.Core.Data;
using DarkSpace.Core.Systems.Factions;

namespace DarkSpace.Core.Systems.Economy;

/// <summary>
/// Торговая система: расчёт цен покупки и продажи с учётом экономики, спроса и репутации.
/// </summary>
/// <remarks>
/// Цена зависит от трёх факторов:
/// <list type="bullet">
///   <item>тип экономики системы (что здесь производят дёшево, а что завозят дорого);</item>
///   <item>коэффициент спроса/предложения (<c>demandFactor</c>), динамический;</item>
///   <item>репутация игрока у контролирующей фракции.</item>
/// </list>
/// </remarks>
public static class MarketSystem
{
    /// <summary>Коэффициент "наценки" — доля цены, которую игрок получает при продаже.</summary>
    public const double SellMargin = 0.65;

    /// <summary>
    /// Возвращает множитель категории предмета для экономики системы.
    /// Меньше 1 — экономика производит и продаёт дёшево; больше 1 — дефицит/импорт.
    /// </summary>
    /// <param name="economy">Тип экономики.</param>
    /// <param name="category">Категория предмета.</param>
    public static double CategoryMultiplier(EconomyType economy, ItemCategory category)
    {
        // Ключи: категории, для которых экономика задаёт особую цену.
        var table = EconomyTable(economy);
        return table.TryGetValue(category, out double value) ? value : 1.0;
    }

    /// <summary>
    /// Рассчитывает цену покупки предмета игроком.
    /// </summary>
    /// <param name="item">Предмет.</param>
    /// <param name="economy">Экономика системы.</param>
    /// <param name="demandFactor">Коэффициент спроса (>1 — дефицит, дороже; &lt;1 — избыток, дешевле).</param>
    /// <param name="reputation">Репутация игрока у контролирующей фракции.</param>
    /// <returns>Цена в кредитах, округлённая до целого.</returns>
    /// <exception cref="ArgumentNullException">Если <paramref name="item"/> равен <see langword="null"/>.</exception>
    public static long GetBuyPrice(Item item, EconomyType economy, double demandFactor, int reputation)
    {
        ArgumentNullException.ThrowIfNull(item);

        double price = item.BaseValue
                       * CategoryMultiplier(economy, item.Category)
                       * ClampDemand(demandFactor)
                       * ReputationSystem.BuyPriceModifier(reputation);

        return Math.Max(1, (long)Math.Round(price));
    }

    /// <summary>
    /// Рассчитывает цену, которую игрок получает при продаже предмета.
    /// </summary>
    /// <param name="item">Предмет.</param>
    /// <param name="economy">Экономика системы.</param>
    /// <param name="demandFactor">Коэффициент спроса.</param>
    /// <param name="reputation">Репутация игрока.</param>
    /// <returns>Цена выкупа в кредитах, округлённая до целого.</returns>
    /// <exception cref="ArgumentNullException">Если <paramref name="item"/> равен <see langword="null"/>.</exception>
    public static long GetSellPrice(Item item, EconomyType economy, double demandFactor, int reputation)
    {
        ArgumentNullException.ThrowIfNull(item);

        double price = item.BaseValue
                       * CategoryMultiplier(economy, item.Category)
                       * ClampDemand(demandFactor)
                       * SellMargin
                       * ReputationSystem.SellPriceModifier(reputation);

        return Math.Max(1, (long)Math.Round(price));
    }

    private static double ClampDemand(double demandFactor)
    {
        if (double.IsNaN(demandFactor) || demandFactor <= 0)
        {
            return 1.0;
        }

        return Math.Clamp(demandFactor, 0.25, 4.0);
    }

    private static Dictionary<ItemCategory, double> EconomyTable(EconomyType economy) => economy switch
    {
        EconomyType.Agricultural => new()
        {
            [ItemCategory.Consumable] = 0.75,
            [ItemCategory.ShipModule] = 1.25,
            [ItemCategory.Weapon] = 1.15,
            [ItemCategory.Armor] = 1.10,
            [ItemCategory.Relic] = 1.10,
        },
        EconomyType.Industrial => new()
        {
            [ItemCategory.ShipModule] = 0.80,
            [ItemCategory.Armor] = 0.85,
            [ItemCategory.Weapon] = 0.90,
            [ItemCategory.Consumable] = 1.20,
        },
        EconomyType.Technocratic => new()
        {
            [ItemCategory.ShipModule] = 0.80,
            [ItemCategory.QuestItem] = 0.90,
            [ItemCategory.Consumable] = 1.10,
            [ItemCategory.Weapon] = 1.05,
        },
        EconomyType.Religious => new()
        {
            [ItemCategory.Relic] = 0.70,
            [ItemCategory.QuestItem] = 0.90,
            [ItemCategory.Weapon] = 1.15,
            [ItemCategory.Consumable] = 1.05,
        },
        EconomyType.Military => new()
        {
            [ItemCategory.Weapon] = 0.80,
            [ItemCategory.Armor] = 0.85,
            [ItemCategory.Consumable] = 1.10,
            [ItemCategory.Relic] = 1.30,
        },
        EconomyType.Pirate => new()
        {
            [ItemCategory.Weapon] = 0.85,
            [ItemCategory.Armor] = 0.90,
            [ItemCategory.Relic] = 1.20,
            [ItemCategory.Consumable] = 1.15,
        },
        _ => new(),
    };
}
