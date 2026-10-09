using System.Collections.Generic;
using DarkSpace.Core.Utils;

namespace DarkSpace.Core.Systems.Alchemy;

/// <summary>
/// Алхимическая система: комбинирует два базовых элемента в новый ресурс.
/// </summary>
/// <remarks>
/// Комбинация <b>коммутативна</b>: порядок аргументов не влияет на результат — элементы
/// нормализуются по возрастанию <see cref="Element.Id"/>. Результат полностью детерминирован
/// по паре элементов (без дополнительного сида), поэтому воспроизводим и на клиенте, и на сервере.
/// </remarks>
public static class AlchemySystem
{
    private static readonly Dictionary<(int Min, int Max), (string Name, string Description, double Value, bool Dangerous)>
        SpecialRecipes = new()
        {
            [(0, 11)] = ("Радиоактивная Вода",
                "Отлично утоляет жажду и гарантирует лучевую болезнь. Пить строго один раз в жизни.",
                420, true),
            [(0, 4)] = ("Гремучий Газ",
                "Взрывается при малейшем поводе. Идеален для сварки и для сведения счётов.",
                180, true),
            [(0, 2)] = ("Метан",
                "Пахнет как совесть крупной корпорации. Горит красиво, если не жалко коллег.",
                95, false),
            [(4, 7)] = ("Ржавчина Вечности",
                "Разъедает металл, надежды и пункты договора. Универсальный растворитель.",
                210, false),
            [(10, 15)] = ("Философское Золото",
                "Обычное золото, но с глубоким внутренним содержанием и завышенной ценой.",
                1500, false),
            [(13, 14)] = ("Призрачная Дыра",
                "Поглощает свет, деньги и смысл. Продаётся исключительно лицам без воображения.",
                2100, true),
        };

    private static readonly string[] GenericFlavor =
    {
        "Побочный эффект не изучен, но уже вызывает вопросы.",
        "Сертификация пройдена. Сертификатор потом исчез.",
        "Пахнет инновациями и лёгким нарушением техники безопасности.",
        "Жидкость. Или порошок. Лаборатория обещала уточнить.",
        "Стабильно ровно до тех пор, пока никто на него не смотрит.",
        "Патент оспаривают три фракции и одна очень настойчивая религия.",
    };

    // Пересечение идентификаторов, при которых комбинация не даёт результата (несовместимые пары).
    private static readonly HashSet<(int Min, int Max)> UnstablePairs = new()
    {
        // Философский Камень не вступает в реакции с экзотическими сущностями.
        (13, 15),
        (14, 15),
    };

    /// <summary>
    /// Комбинирует два элемента и возвращает новый ресурс.
    /// </summary>
    /// <param name="a">Первый элемент.</param>
    /// <param name="b">Второй элемент.</param>
    /// <returns>Результат комбинации. Если пара несовместима, <see cref="CombinationResult.IsNovel"/> равен <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException">Если один из элементов равен <see langword="null"/>.</exception>
    public static CombinationResult Combine(Element a, Element b)
    {
        ArgumentNullException.ThrowIfNull(a);
        ArgumentNullException.ThrowIfNull(b);

        (int min, int max) = a.Id <= b.Id ? (a.Id, b.Id) : (b.Id, a.Id);
        Element first = a.Id <= b.Id ? a : b;
        Element second = a.Id <= b.Id ? b : a;

        if (UnstablePairs.Contains((min, max)))
        {
            return new CombinationResult(
                "Нестабильная смесь",
                "Компоненты предпочли не реагировать и разошлись по разным сосудам. Всех устраивает.",
                0,
                false,
                false,
                min,
                max);
        }

        if (SpecialRecipes.TryGetValue((min, max), out var recipe))
        {
            return new CombinationResult(recipe.Name, recipe.Description, recipe.Value, recipe.Dangerous, true, min, max);
        }

        return GenerateGeneric(first, second);
    }

    private static CombinationResult GenerateGeneric(Element first, Element second)
    {
        ulong seed = SeedHelper.Combine((ulong)first.Id, "alchemy", (ulong)second.Id);
        var random = new DeterministicRandom(seed);

        string name = $"{first.Stem}-{second.Stem}";
        double value = Math.Round(20.0 + ((first.Rarity + second.Rarity) * 500.0) + random.NextDouble(0.0, 40.0), 1);
        bool dangerous = first.Rarity > 0.6 || second.Rarity > 0.6 || random.NextBool(0.25);
        string flavor = random.Pick(GenericFlavor);
        string warning = dangerous
            ? "Классифицировано как «не рекомендуется к употреблению внутрь, наружу и вообще где-либо»."
            : "Классифицировано как «вероятно безопасно, но не подписывайтесь на гарантии».";

        string description = $"Соединение из «{first.Name}» и «{second.Name}». {flavor} {warning}";

        return new CombinationResult(name, description, value, dangerous, true, first.Id, second.Id);
    }

    /// <summary>
    /// Возвращает все возможные результаты комбинаций для каталога элементов.
    /// </summary>
    /// <returns>Список уникальных результатов (без учёта порядка элементов).</returns>
    public static IReadOnlyList<CombinationResult> EnumerateAllCombinations()
    {
        var results = new List<CombinationResult>();
        var catalog = ElementCatalog.All;
        for (int i = 0; i < catalog.Count; i++)
        {
            for (int j = i; j < catalog.Count; j++)
            {
                results.Add(Combine(catalog[i], catalog[j]));
            }
        }

        return results;
    }
}
