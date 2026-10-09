using System.Collections.Generic;
using System.Text;
using DarkSpace.Core.Utils;

namespace DarkSpace.Core.Generation;

/// <summary>
/// Процедурный генератор имён (галактики, системы, планеты, корабли, расы).
/// </summary>
/// <remarks>
/// Имена собираются из слогов и полностью детерминированы по переданному генератору.
/// Одна и та же последовательность вызовов на одинаковом сиде даёт одинаковые имена.
/// </remarks>
public static class NameGenerator
{
    private static readonly string[] Prefixes =
    {
        "Od", "Xer", "Vor", "Kal", "Nyx", "Zar", "Dra", "Aqu", "Sol", "Tau",
        "Ark", "Bel", "Cor", "Dun", "Eth", "Fen", "Gam", "Hel", "Ith", "Jor",
        "Kry", "Lum", "Mor", "Nal", "Ori", "Pyr", "Qen", "Rho", "Sir", "Tha",
    };

    private static readonly string[] Middles =
    {
        "a", "e", "i", "o", "u", "ae", "ia", "or", "an", "el",
        "un", "yr", "ax", "on", "is", "em", "ar", "ul", "en", "ov",
    };

    private static readonly string[] Suffixes =
    {
        "lutai", "gan", "dris", "thar", "nis", "dor", "mek", "zon", "ves", "kur",
        "rak", "ion", "ara", "eth", "ony", "ux", "il", "ath", "orn", "ael",
    };

    private static readonly string[] RomanNumerals =
    {
        "I", "II", "III", "IV", "V", "VI", "VII", "VIII", "IX", "X",
        "XI", "XII", "XIII", "XIV", "XV", "XVI", "XVII", "XVIII", "XIX", "XX",
    };

    private static readonly string[] ShipPrefixes =
    {
        "Утренняя", "Последняя", "Тихая", "Проклятая", "Весёлая", "Усталая",
        "Голодная", "Ржавая", "Беспечная", "Отчаянная", "Дешёвая", "Святая",
    };

    private static readonly string[] ShipNouns =
    {
        "Звезда", "Надежда", "Пылинка", "Гипотеза", "Ошибка", "Гарантия",
        "Накладная", "Молитва", "Скидка", "Претензия", "Аномалия", "Рассрочка",
    };

    /// <summary>Генерирует имя галактики.</summary>
    public static string GalaxyName(DeterministicRandom random)
        => BuildWord(random, 2, 3);

    /// <summary>Генерирует имя звёздной системы.</summary>
    public static string StarSystemName(DeterministicRandom random)
        => BuildWord(random, 1, 2);

    /// <summary>Генерирует имя планеты (с римским индексом).</summary>
    public static string PlanetName(DeterministicRandom random)
        => $"{BuildWord(random, 1, 2)} {random.Pick(RomanNumerals)}";

    /// <summary>Генерирует имя корабля в стиле чёрного юмора.</summary>
    public static string ShipName(DeterministicRandom random)
        => $"«{random.Pick(ShipPrefixes)} {random.Pick(ShipNouns)}»";

    /// <summary>Генерирует имя расы или народа.</summary>
    public static string RaceName(DeterministicRandom random)
    {
        string baseName = BuildWord(random, 1, 2);
        string[] epithets =
        {
            "цы", "ане", "иды", "ары", "эны", "ори",
        };
        return baseName + random.Pick(epithets);
    }

    /// <summary>Генерирует имя персонажа.</summary>
    public static string CharacterName(DeterministicRandom random)
        => BuildWord(random, 1, 2);

    private static string BuildWord(DeterministicRandom random, int minSyllables, int maxSyllables)
    {
        int syllables = random.NextInt(minSyllables, maxSyllables + 1);
        var builder = new StringBuilder();
        builder.Append(random.Pick(Prefixes));
        for (int i = 0; i < syllables; i++)
        {
            builder.Append(random.Pick(Middles));
        }

        builder.Append(random.Pick(Suffixes));
        return builder.ToString();
    }

    /// <summary>Возвращает копию всех словарей имён (для тестов и расширения).</summary>
    public static IReadOnlyDictionary<string, IReadOnlyList<string>> GetDictionaries()
        => new Dictionary<string, IReadOnlyList<string>>
        {
            ["prefixes"] = Prefixes,
            ["middles"] = Middles,
            ["suffixes"] = Suffixes,
            ["shipPrefixes"] = ShipPrefixes,
            ["shipNouns"] = ShipNouns,
        };
}
