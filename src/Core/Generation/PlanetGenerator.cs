using System.Collections.Generic;
using DarkSpace.Core.Data;
using DarkSpace.Core.Utils;

namespace DarkSpace.Core.Generation;

/// <summary>
/// Процедурный генератор планет.
/// </summary>
/// <remarks>
/// Генерация детерминирована по паре (systemSeed, planetIndex). Описание планеты включает
/// элементы чёрного юмора (см. <see cref="DescriptionGenerator"/>).
/// </remarks>
public static class PlanetGenerator
{
    private static readonly string[] Atmospheres =
    {
        "азота и метана", "углекислого газа", "кислорода и аргона", "серы и аммиака",
        "водорода и гелия", "неона и пыли", "хлора и оксидов", "протухших обещаний",
    };

    private static readonly string[] CommonResources =
    {
        "Железо", "Кремний", "Углерод", "Кислород", "Водород", "Алюминий", "Медь", "Титан",
    };

    private static readonly string[] RareResources =
    {
        "Золото", "Уран", "Плутоний", "Эктоплазма", "Тёмная Материя", "Философский Камень",
    };

    /// <summary>
    /// Генерирует одну планету.
    /// </summary>
    /// <param name="system">Система-родитель.</param>
    /// <param name="planetIndex">Индекс планеты внутри системы.</param>
    /// <returns>Детерминированное описание планеты.</returns>
    /// <exception cref="ArgumentNullException">Если <paramref name="system"/> равен <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Если индекс планеты вне допустимого диапазона.</exception>
    public static PlanetData GeneratePlanet(StarSystemData system, int planetIndex)
    {
        ArgumentNullException.ThrowIfNull(system);

        if (planetIndex < 0 || planetIndex >= system.PlanetCount)
        {
            throw new ArgumentOutOfRangeException(
                nameof(planetIndex),
                planetIndex,
                $"Индекс планеты должен быть в диапазоне 0–{system.PlanetCount - 1}.");
        }

        ulong seed = SeedHelper.Combine(system.Seed, "planet", (ulong)planetIndex);
        var random = new DeterministicRandom(seed);

        string name = $"{system.Name} {RomanNumeral(planetIndex + 1)}";
        PlanetType type = PickPlanetType(random, system.Star);
        double gravity = Math.Round(random.NextGaussian(1.0, 0.6) + (type == PlanetType.GasGiant ? 1.2 : 0.0), 2);
        gravity = Math.Max(0.05, gravity);

        string atmosphere = type == PlanetType.GasGiant
            ? "водорода, гелия и чьего-то самолюбия"
            : random.Pick(Atmospheres);

        double temperature = Math.Round(BaseTemperature(type) + random.NextGaussian(0.0, 40.0), 1);
        bool hasLife = type is PlanetType.Oceanic or PlanetType.Jungle or PlanetType.Rocky
            && random.NextBool(0.35);
        bool aggressiveFauna = hasLife && random.NextBool(0.5);
        bool flora = hasLife && type != PlanetType.Rocky && random.NextBool(0.7);

        IReadOnlyList<string> resources = GenerateResources(random, type);

        var core = new PlanetData(
            name,
            type,
            gravity,
            atmosphere,
            temperature,
            hasLife,
            aggressiveFauna,
            flora,
            resources,
            string.Empty,
            seed);

        string description = DescriptionGenerator.DescribePlanet(random, core);
        return core with { Description = description };
    }

    private static IReadOnlyList<string> GenerateResources(DeterministicRandom random, PlanetType type)
    {
        var resources = new List<string>();
        int commonCount = random.NextInt(1, 4);
        for (int i = 0; i < commonCount; i++)
        {
            string resource = random.Pick(CommonResources);
            if (!resources.Contains(resource))
            {
                resources.Add(resource);
            }
        }

        double rareChance = type switch
        {
            PlanetType.Radioactive => 0.7,
            PlanetType.Volcanic => 0.5,
            PlanetType.Exotic => 0.9,
            PlanetType.Toxic => 0.4,
            _ => 0.15,
        };

        if (random.NextBool(rareChance))
        {
            resources.Add(random.Pick(RareResources));
        }

        return resources;
    }

    private static PlanetType PickPlanetType(DeterministicRandom random, SpectralClass star)
    {
        // Возле голубых/белых звёзд чаще встречаются горячие типы планет.
        int roll = random.NextInt(100);
        bool hotStar = star is SpectralClass.O or SpectralClass.B or SpectralClass.A;

        return roll switch
        {
            < 22 => PlanetType.Rocky,
            < 36 => hotStar ? PlanetType.Volcanic : PlanetType.GasGiant,
            < 48 => PlanetType.Ice,
            < 58 => PlanetType.Oceanic,
            < 68 => hotStar ? PlanetType.Volcanic : PlanetType.Desert,
            < 78 => PlanetType.Toxic,
            < 86 => PlanetType.Radioactive,
            < 94 => PlanetType.Jungle,
            _ => PlanetType.Exotic,
        };
    }

    private static double BaseTemperature(PlanetType type) => type switch
    {
        PlanetType.Volcanic => 400.0,
        PlanetType.Desert => 80.0,
        PlanetType.Toxic => 120.0,
        PlanetType.Radioactive => 200.0,
        PlanetType.GasGiant => -120.0,
        PlanetType.Ice => -160.0,
        PlanetType.Oceanic => 15.0,
        PlanetType.Jungle => 30.0,
        PlanetType.Exotic => 50.0,
        _ => 10.0,
    };

    private static string RomanNumeral(int value) => value switch
    {
        1 => "I",
        2 => "II",
        3 => "III",
        4 => "IV",
        5 => "V",
        6 => "VI",
        7 => "VII",
        8 => "VIII",
        9 => "IX",
        10 => "X",
        11 => "XI",
        12 => "XII",
        _ => value.ToString(),
    };
}
