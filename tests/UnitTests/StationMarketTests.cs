using DarkSpace.Core.Data;
using DarkSpace.Core.Systems.Economy;
using Xunit;

namespace DarkSpace.Tests.Unit;

/// <summary>Тесты рынка станции (в духе Elite: цена = база + экономический фактор × экономика).</summary>
public sealed class StationMarketTests
{
    [Fact]
    public void EconomyLevel_IndustrialIsLow_AgriculturalIsHigh()
    {
        Assert.Equal(0, StationMarket.EconomyLevel(EconomyType.Industrial));
        Assert.Equal(7, StationMarket.EconomyLevel(EconomyType.Agricultural));
    }

    [Fact]
    public void Food_IsCheaperInAgriculturalThanIndustrial()
    {
        // Продовольствие (id 0, фактор -2): в аграрной экономике (уровень 7) дешевле.
        var agri = new StationMarket(EconomyType.Agricultural, 1);
        var industrial = new StationMarket(EconomyType.Industrial, 1);
        Assert.True(agri.BuyPrice(0) < industrial.BuyPrice(0),
            $"Продовольствие: аграрная {agri.BuyPrice(0)} vs индустриальная {industrial.BuyPrice(0)}.");
    }

    [Fact]
    public void Luxuries_AreMoreExpensiveInAgriculturalThanIndustrial()
    {
        // Роскошь (id 5, фактор +8): в аграрной экономике дороже.
        var agri = new StationMarket(EconomyType.Agricultural, 1);
        var industrial = new StationMarket(EconomyType.Industrial, 1);
        Assert.True(agri.BuyPrice(5) > industrial.BuyPrice(5),
            $"Роскошь: аграрная {agri.BuyPrice(5)} vs индустриальная {industrial.BuyPrice(5)}.");
    }

    [Fact]
    public void PriceFormula_MatchesEliteDerivation()
    {
        // Ровно один товар и детерминированная флуктуация: сравним с формулой напрямую.
        var market = new StationMarket(EconomyType.Industrial, 42);
        Commodity food = CommodityCatalog.GetById(0);
        int expected = food.BasePrice + (food.EconomicFactor * market.EconomyLevelValue);
        // С учётом флуктуации -2..+2.
        Assert.InRange(market.BuyPrice(0), expected - 2, expected + 2);
    }

    [Fact]
    public void SellPrice_IsLessThanBuyPrice()
    {
        var market = new StationMarket(EconomyType.Technocratic, 7);
        for (int id = 0; id < CommodityCatalog.Count; id++)
        {
            Assert.True(market.SellPrice(id) < market.BuyPrice(id),
                $"Цена продажи должна быть ниже цены покупки для товара {id}.");
        }
    }

    [Fact]
    public void Stock_DepletesAndReplenishes()
    {
        var market = new StationMarket(EconomyType.Industrial, 3);
        int initial = market.Stock(9);
        Assert.True(initial > 0);

        int taken = market.RemoveStock(9, 5);
        Assert.Equal(Math.Min(5, initial), taken);
        Assert.Equal(initial - taken, market.Stock(9));

        market.AddStock(9, 4);
        Assert.Equal(initial - taken + 4, market.Stock(9));
    }

    [Fact]
    public void Price_IsDeterministicBySeed()
    {
        var a = new StationMarket(EconomyType.Pirate, 12345);
        var b = new StationMarket(EconomyType.Pirate, 12345);
        for (int id = 0; id < CommodityCatalog.Count; id++)
        {
            Assert.Equal(a.BuyPrice(id), b.BuyPrice(id));
            Assert.Equal(a.Stock(id), b.Stock(id));
        }
    }

    [Fact]
    public void Catalog_Has17Commodities()
    {
        Assert.Equal(17, CommodityCatalog.Count);
    }
}
