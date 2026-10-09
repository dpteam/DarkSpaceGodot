using DarkSpace.Core.Data;
using DarkSpace.Core.Generation;
using DarkSpace.Core.Systems.Economy;
using DarkSpace.Core.Systems.Factions;
using DarkSpace.Core.Systems.Items;
using DarkSpace.Core.Systems.Travel;
using Xunit;

namespace DarkSpace.Tests.Integration;

/// <summary>
/// Интеграционный тест полного конвейера Этапа 2:
/// галактика → расы → система → планеты → поселения → предметы → экономика → репутация → прыжок.
/// </summary>
public sealed class Stage2PipelineIntegrationTests
{
    private const ulong WorldSeed = 0x535441474532UL; // "STAGE2"

    [Fact]
    public void FullPipeline_RunsWithoutErrors_AndIsDeterministic()
    {
        Report first = RunPipeline();
        Report second = RunPipeline();

        Assert.Equal(first, second);
    }

    [Fact]
    public void FullPipeline_ProducesRichContent()
    {
        Report report = RunPipeline();

        Assert.True(report.Races >= RaceGenerator.MinRacesPerGalaxy);
        Assert.True(report.Systems > 0);
        Assert.True(report.Planets > 0);
        Assert.True(report.Items > 0);
        Assert.All(Enum.GetValues<ItemCategory>(), c => Assert.NotEmpty(ItemCatalog.ByCategory(c)));
    }

    private static Report RunPipeline()
    {
        GalaxyData galaxy = GalaxyGenerator.GenerateGalaxy(WorldSeed, 0);
        var races = RaceGenerator.GenerateGalaxyRaces(galaxy.Seed);

        int systems = 0;
        int planets = 0;
        int cities = 0;
        long totalPopulation = 0;
        int items = 0;
        long totalMarketValue = 0;

        int sampleSystems = Math.Min(galaxy.StarCount, 8);
        for (int s = 0; s < sampleSystems; s++)
        {
            StarSystemData system = StarSystemGenerator.GenerateSystem(galaxy, s);
            systems++;

            for (int p = 0; p < system.PlanetCount; p++)
            {
                PlanetData planet = PlanetGenerator.GeneratePlanet(system, p);
                planets++;

                foreach (CityData city in CityGenerator.GenerateCities(system, planet))
                {
                    cities++;
                    totalPopulation += city.Population;

                    // Экономика: цена случайного предмета в экономике поселения.
                    Item item = ItemGenerator.GenerateItem(city.Seed);
                    items++;
                    totalMarketValue += MarketSystem.GetBuyPrice(item, city.Economy, 1.0, 0);
                }
            }
        }

        // Репутация влияет на цены: проверим на первом предмете.
        Item sample = ItemGenerator.GenerateItem(galaxy.Seed);
        long hatedPrice = MarketSystem.GetBuyPrice(sample, galaxy.Type is GalaxyType.Spiral ? EconomyType.Industrial : EconomyType.Agricultural, 1.0, -1000);
        long honoredPrice = MarketSystem.GetBuyPrice(sample, galaxy.Type is GalaxyType.Spiral ? EconomyType.Industrial : EconomyType.Agricultural, 1.0, 1000);

        // Валюта: конвертация кредитов в реликвии и обратно в религиозной экономике.
        double relics = CurrencySystem.Convert(10_000, CurrencyType.Credits, CurrencyType.Relics, EconomyType.Religious);
        double backToCredits = CurrencySystem.Convert(relics, CurrencyType.Relics, CurrencyType.Credits, EconomyType.Religious);

        // Прыжок через чёрную дыру.
        ShipData ship = ShipGenerator.GenerateShip(galaxy.Seed, ShipType.Cruiser);
        JumpResult jump = BlackHoleTravel.ComputeJump(galaxy.Seed, ship, 1);

        return new Report(
            Races: races.Count,
            Systems: systems,
            Planets: planets,
            Cities: cities,
            TotalPopulation: totalPopulation,
            Items: items,
            TotalMarketValue: totalMarketValue,
            HatedPrice: hatedPrice,
            HonoredPrice: honoredPrice,
            Relics: relics,
            BackToCredits: backToCredits,
            JumpOutcome: jump.Outcome);
    }

    /// <summary>Свёртка результатов конвейера для проверки детерминизма.</summary>
    private sealed record Report(
        int Races,
        int Systems,
        int Planets,
        int Cities,
        long TotalPopulation,
        int Items,
        long TotalMarketValue,
        long HatedPrice,
        long HonoredPrice,
        double Relics,
        double BackToCredits,
        JumpOutcome JumpOutcome);
}
