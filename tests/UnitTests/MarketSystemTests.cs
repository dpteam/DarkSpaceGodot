using DarkSpace.Core.Data;
using DarkSpace.Core.Systems.Economy;
using DarkSpace.Core.Systems.Items;
using Xunit;

namespace DarkSpace.Tests.Unit;

/// <summary>Тесты торговой системы.</summary>
public sealed class MarketSystemTests
{
    private static Item Weapon => ItemCatalog.GetById(0);       // «Аргумент»
    private static Item Consumable => ItemCatalog.GetById(12); // «Не умирай»

    [Fact]
    public void BuyPrice_IsGreaterThanSellPrice()
    {
        long buy = MarketSystem.GetBuyPrice(Weapon, EconomyType.Industrial, 1.0, 0);
        long sell = MarketSystem.GetSellPrice(Weapon, EconomyType.Industrial, 1.0, 0);
        Assert.True(buy > sell);
    }

    [Fact]
    public void HigherDemand_RaisesPrice()
    {
        long low = MarketSystem.GetBuyPrice(Weapon, EconomyType.Industrial, 0.5, 0);
        long high = MarketSystem.GetBuyPrice(Weapon, EconomyType.Industrial, 2.0, 0);
        Assert.True(high > low);
    }

    [Fact]
    public void GoodReputation_LowersBuyPrice()
    {
        long neutral = MarketSystem.GetBuyPrice(Weapon, EconomyType.Military, 1.0, 0);
        long honored = MarketSystem.GetBuyPrice(Weapon, EconomyType.Military, 1.0, 1000);
        Assert.True(honored < neutral);
    }

    [Fact]
    public void IndustrialEconomy_MakesShipModulesCheaperThanAgricultural()
    {
        Item module = ItemCatalog.GetById(8); // двигатель
        long industrial = MarketSystem.GetBuyPrice(module, EconomyType.Industrial, 1.0, 0);
        long agricultural = MarketSystem.GetBuyPrice(module, EconomyType.Agricultural, 1.0, 0);
        Assert.True(industrial < agricultural);
    }

    [Fact]
    public void ReligiousEconomy_MakesRelicsCheaper()
    {
        Item relic = ItemCatalog.GetById(20);
        long religious = MarketSystem.GetBuyPrice(relic, EconomyType.Religious, 1.0, 0);
        long military = MarketSystem.GetBuyPrice(relic, EconomyType.Military, 1.0, 0);
        Assert.True(religious < military);
    }

    [Fact]
    public void Price_IsNeverBelowOne()
    {
        for (int id = 0; id < ItemCatalog.Count; id++)
        {
            long price = MarketSystem.GetBuyPrice(ItemCatalog.GetById(id), EconomyType.Pirate, 0.25, -1000);
            Assert.True(price >= 1);
        }
    }

    [Fact]
    public void CategoryMultiplier_DefaultsToOneForUndefinedPairs()
    {
        // Пиратская экономика не задаёт цену на квестовые предметы => множитель по умолчанию 1.0.
        Assert.Equal(1.0, MarketSystem.CategoryMultiplier(EconomyType.Pirate, ItemCategory.QuestItem));
        // А вот оружие пираты ценят дешевле 1.0.
        Assert.True(MarketSystem.CategoryMultiplier(EconomyType.Pirate, ItemCategory.Weapon) < 1.0);
    }

    [Fact]
    public void NullItem_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => MarketSystem.GetBuyPrice(null!, EconomyType.Industrial, 1.0, 0));
        Assert.Throws<ArgumentNullException>(() => MarketSystem.GetSellPrice(null!, EconomyType.Industrial, 1.0, 0));
    }
}
