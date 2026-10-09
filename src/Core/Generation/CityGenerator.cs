using System;
using System.Collections.Generic;
using DarkSpace.Core.Data;
using DarkSpace.Core.Utils;

namespace DarkSpace.Core.Generation;

/// <summary>
/// Процедурный генератор поселений и их инфраструктуры.
/// </summary>
/// <remarks>
/// Поселения генерируются на планетах. Наличие и число поселений зависит от того,
/// пригодна ли планета для жизни, есть ли в системе станция и какова экономика системы.
/// Генерация детерминирована по паре (planetSeed, cityIndex).
/// </remarks>
public static class CityGenerator
{
    private static readonly Dictionary<CityType, string[]> Flavor = new()
    {
        [CityType.Megacity] =
        [
            "Население превышает разумное, но никто не рискует это озвучить.",
            "Здесь продают воздух в бутылках, и это считается заботой о клиенте.",
            "Три уровня власти, четыре уровня пробок и пять уровней сожаления.",
        ],
        [CityType.Colony] = new[]
        {
            "Основана с энтузиазмом. Развивается с осторожностью. Стоит из упрямства.",
            "Дефицит всего, кроме вакансий и оптимизма.",
            "Здесь все друг друга знают. Это и хорошо, и смертельно опасно.",
        },
        [CityType.MiningSettlement] = new[]
        {
            "Добывают руду и жалуются на норму выработки. Иногда наоборот.",
            "Население приезжает работать и уезжает, как только накопит на билет.",
            "Единственное развлечение здесь — слухи о том, что где-то есть развлечения.",
        },
        [CityType.ReligiousCommunity] = new[]
        {
            "Прихожане калибруют души трижды в день и списывают это как медицину.",
            "Все тексты священны. Никто их не читал. Все их цитируют.",
            "Здесь продают прощение оптом и в розницу. Скидка за объём.",
        },
        [CityType.MilitaryBase] = new[]
        {
            "Гарнизон охраняет периметр от врага, климата и скуки.",
            "Здесь всё по уставу, включая погоду — её просто заносят в отчёт как норму.",
            "Приказы приходят быстрее, чем их успевают понять.",
        },
        [CityType.PirateDen] = new[]
        {
            "Здесь уважают закон ровно настолько, насколько он вооружён.",
            "Демократия: один корабль — один голос. Причаливайте на чём есть.",
            "Полиция не заходит. Полиция присылает открытки.",
        },
        [CityType.TradingHub] = new[]
        {
            "Торгуют всем. Совесть не проходит сертификацию и потому не в наличии.",
            "Курс валют меняется быстрее, чем вы успеваете его спросить.",
            "Логистика священна, накладная — божественна.",
        },
        [CityType.ResearchOutpost] = new[]
        {
            "Учёные изучают планету и постепенно понимают, зачем им это.",
            "Научный прорыв объявлен трижды и отменён дважды.",
            "Опасные опыты проводятся строго по расписанию обеденного перерыва.",
        },
    };

    /// <summary>
    /// Генерирует поселения на конкретной планете.
    /// </summary>
    /// <param name="system">Система-родитель.</param>
    /// <param name="planet">Планета.</param>
    /// <returns>Список поселений (может быть пустым, если планета необитаема).</returns>
    /// <exception cref="ArgumentNullException">Если <paramref name="system"/> или <paramref name="planet"/> равны <see langword="null"/>.</exception>
    public static IReadOnlyList<CityData> GenerateCities(StarSystemData system, PlanetData planet)
    {
        ArgumentNullException.ThrowIfNull(system);
        ArgumentNullException.ThrowIfNull(planet);

        var random = new DeterministicRandom(SeedHelper.Combine(planet.Seed, "cities"));

        // Необитаемые и вовсе непригодные миры заселяются редко.
        double baseChance = planet.HasLife ? 0.75 : planet.Type == PlanetType.Rocky ? 0.25 : 0.08;
        if (system.HasStation)
        {
            baseChance += 0.15;
        }

        if (!random.NextBool(baseChance))
        {
            return Array.Empty<CityData>();
        }

        int cityCount = 1 + (random.NextBool(0.4) ? 1 : 0) + (random.NextBool(0.15) ? 1 : 0);
        var cities = new List<CityData>(cityCount);
        for (int i = 0; i < cityCount; i++)
        {
            cities.Add(GenerateCity(system, planet, i));
        }

        return cities;
    }

    /// <summary>
    /// Генерирует конкретное поселение.
    /// </summary>
    /// <param name="system">Система-родитель.</param>
    /// <param name="planet">Планета.</param>
    /// <param name="cityIndex">Индекс поселения на планете.</param>
    public static CityData GenerateCity(StarSystemData system, PlanetData planet, int cityIndex)
    {
        ArgumentNullException.ThrowIfNull(system);
        ArgumentNullException.ThrowIfNull(planet);

        ulong seed = SeedHelper.Combine(planet.Seed, "city", (ulong)cityIndex);
        var random = new DeterministicRandom(seed);

        string name = NameGenerator.CityName(random);
        CityType type = PickCityType(random, system, planet);
        long population = PickPopulation(random, type);
        EconomyType economy = type switch
        {
            CityType.MiningSettlement => EconomyType.Industrial,
            CityType.ReligiousCommunity => EconomyType.Religious,
            CityType.MilitaryBase => EconomyType.Military,
            CityType.PirateDen => EconomyType.Pirate,
            CityType.TradingHub => EconomyType.Industrial,
            CityType.ResearchOutpost => EconomyType.Technocratic,
            _ => system.Economy,
        };

        IReadOnlyList<InfrastructureType> infrastructure = PickInfrastructure(random, type, population);
        string description =
            $"Поселение {name} ({TypeLabel(type)}), население {FormatPopulation(population)}. {random.Pick(Flavor[type])}";

        return new CityData(name, type, population, system.ControllingFaction, economy, infrastructure, description, seed);
    }

    private static CityType PickCityType(DeterministicRandom random, StarSystemData system, PlanetData planet)
    {
        // Тип во многом следует за фракцией и экономикой системы.
        if (system.ControllingFaction == FactionType.FreeClans && random.NextBool(0.5))
        {
            return CityType.PirateDen;
        }

        if (system.ControllingFaction == FactionType.OrderOfKeepers && random.NextBool(0.5))
        {
            return CityType.ReligiousCommunity;
        }

        if (system.ControllingFaction == FactionType.EmpireOfDawn && random.NextBool(0.45))
        {
            return CityType.MilitaryBase;
        }

        if (system.ControllingFaction == FactionType.VoidSyndicate && random.NextBool(0.4))
        {
            return CityType.TradingHub;
        }

        int roll = random.NextInt(100);
        if (roll < 8) return CityType.Megacity;
        if (roll < 30) return CityType.Colony;
        if (roll < 52) return CityType.MiningSettlement;
        if (roll < 62) return CityType.ReligiousCommunity;
        if (roll < 72) return CityType.MilitaryBase;
        if (roll < 80) return CityType.PirateDen;
        if (roll < 90) return CityType.TradingHub;
        return CityType.ResearchOutpost;
    }

    private static long PickPopulation(DeterministicRandom random, CityType type) => type switch
    {
        CityType.Megacity => random.NextLong(1_000_000_000L, 20_000_000_000L),
        CityType.Colony => random.NextLong(10_000L, 5_000_000L),
        CityType.MiningSettlement => random.NextLong(50L, 20_000L),
        CityType.ReligiousCommunity => random.NextLong(200L, 500_000L),
        CityType.MilitaryBase => random.NextLong(500L, 100_000L),
        CityType.PirateDen => random.NextLong(100L, 50_000L),
        CityType.TradingHub => random.NextLong(5_000L, 2_000_000L),
        CityType.ResearchOutpost => random.NextLong(100L, 50_000L),
        _ => random.NextLong(100L, 10_000L),
    };

    private static IReadOnlyList<InfrastructureType> PickInfrastructure(
        DeterministicRandom random,
        CityType type,
        long population)
    {
        var set = new List<InfrastructureType>();

        // Крупные поселения практически всегда имеют космопорт и рынок.
        if (population > 100_000 || type is CityType.Megacity or CityType.TradingHub or CityType.Colony)
        {
            set.Add(InfrastructureType.Spaceport);
        }

        if (population > 1_000)
        {
            set.Add(InfrastructureType.Market);
            set.Add(InfrastructureType.RefuelStation);
        }

        switch (type)
        {
            case CityType.Megacity:
                set.AddRange([InfrastructureType.RepairDock, InfrastructureType.Bar, InfrastructureType.Brothel, InfrastructureType.Temple, InfrastructureType.Prison]);
                break;
            case CityType.MiningSettlement:
                set.Add(InfrastructureType.RepairDock);
                set.Add(InfrastructureType.Bar);
                break;
            case CityType.ReligiousCommunity:
                set.Add(InfrastructureType.Temple);
                break;
            case CityType.MilitaryBase:
                set.AddRange([InfrastructureType.RepairDock, InfrastructureType.Prison]);
                break;
            case CityType.PirateDen:
                set.AddRange([InfrastructureType.Bar, InfrastructureType.Brothel, InfrastructureType.RepairDock]);
                break;
            case CityType.TradingHub:
                set.AddRange([InfrastructureType.Bar, InfrastructureType.RepairDock]);
                break;
            case CityType.ResearchOutpost:
                set.Add(InfrastructureType.Bar);
                break;
        }

        if (random.NextBool(0.3))
        {
            set.Add(InfrastructureType.Temple);
        }

        // Убираем дубликаты, сохраняя порядок, и сортируем для детерминизма представления.
        var ordered = new List<InfrastructureType>();
        foreach (InfrastructureType item in set)
        {
            if (!ordered.Contains(item))
            {
                ordered.Add(item);
            }
        }

        ordered.Sort();
        return ordered;
    }

    private static string TypeLabel(CityType type) => type switch
    {
        CityType.Megacity => "мегаполис",
        CityType.Colony => "колония",
        CityType.MiningSettlement => "шахтёрский посёлок",
        CityType.ReligiousCommunity => "религиозная община",
        CityType.MilitaryBase => "военная база",
        CityType.PirateDen => "пиратский притон",
        CityType.TradingHub => "торговый узел",
        CityType.ResearchOutpost => "научный аванпост",
        _ => "поселение",
    };

    private static string FormatPopulation(long population) => population switch
    {
        >= 1_000_000_000 => $"{population / 1_000_000_000.0:0.#} млрд",
        >= 1_000_000 => $"{population / 1_000_000.0:0.#} млн",
        >= 1_000 => $"{population / 1_000.0:0.#} тыс.",
        _ => population.ToString(),
    };
}
