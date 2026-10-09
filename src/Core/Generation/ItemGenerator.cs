using DarkSpace.Core.Data;
using DarkSpace.Core.Systems.Items;
using DarkSpace.Core.Utils;

namespace DarkSpace.Core.Generation;

/// <summary>
/// Процедурный генератор предметов: создаёт варианты на основе базового каталога.
/// </summary>
/// <remarks>
/// Вариант наследует базовый образец из <see cref="ItemCatalog"/> и может быть улучшен:
/// повышается редкость, изменяется стоимость/вес и добавляется префикс-модификатор.
/// Генерация детерминирована по переданному сиду.
/// </remarks>
public static class ItemGenerator
{
    private const double ValueStep = 1.35;

    private static readonly (ItemRarity Rarity, string Prefix, double Multiplier)[] UpgradePrefixes =
    {
        (ItemRarity.Uncommon, "Закалённый", 1.5),
        (ItemRarity.Rare, "Трофейный", 2.4),
        (ItemRarity.Epic, "Прототип «Омега»", 4.2),
        (ItemRarity.Legendary, "Реликвия Архитектора", 8.0),
    };

    /// <summary>
    /// Генерирует предмет по сиду. Базовый образец выбирается случайно.
    /// </summary>
    /// <param name="seed">Сид генерации.</param>
    public static Item GenerateItem(ulong seed)
    {
        var random = new DeterministicRandom(seed);
        Item template = random.Pick(ItemCatalog.All);
        return Roll(random, template);
    }

    /// <summary>
    /// Генерирует вариант конкретного базового предмета.
    /// </summary>
    /// <param name="seed">Сид генерации.</param>
    /// <param name="baseId">Идентификатор базового предмета из каталога.</param>
    /// <exception cref="ArgumentOutOfRangeException">Если <paramref name="baseId"/> вне диапазона каталога.</exception>
    public static Item GenerateVariant(ulong seed, int baseId)
    {
        Item template = ItemCatalog.GetById(baseId);
        var random = new DeterministicRandom(SeedHelper.Combine(seed, "item", (ulong)baseId));
        return Roll(random, template);
    }

    private static Item Roll(DeterministicRandom random, Item template)
    {
        // Чем реже базовый предмет, тем реже он улучшается — но улучшения весомее.
        double roll = random.NextDouble();
        if (roll < 0.62)
        {
            // Без улучшения: небольшой разброс по весу/стоимости.
            double jitter = random.NextDouble(0.9, 1.1);
            return template with
            {
                Weight = Round(template.Weight * jitter),
                BaseValue = Round(template.BaseValue * jitter),
                Rarity = DowngradeGuard(template.Rarity, template.Rarity),
            };
        }

        (ItemRarity rarity, string prefix, double multiplier) = UpgradePrefixes[random.NextInt(UpgradePrefixes.Length)];
        // Улучшение не может понизить редкость относительно базовой.
        ItemRarity finalRarity = (ItemRarity)Math.Max((int)template.Rarity, (int)rarity);

        double weightMultiplier = random.NextDouble(0.95, 1.3);
        return template with
        {
            Name = $"{prefix} {template.Name}",
            Rarity = finalRarity,
            Weight = Round(template.Weight * weightMultiplier),
            BaseValue = Round(template.BaseValue * multiplier),
            Effect = $"{template.Effect} Эффект усилен (×{multiplier:0.#}).",
        };
    }

    private static ItemRarity DowngradeGuard(ItemRarity baseRarity, ItemRarity candidate)
        => (ItemRarity)Math.Max((int)baseRarity, (int)candidate);

    private static double Round(double value) => Math.Round(value, 2);
}
