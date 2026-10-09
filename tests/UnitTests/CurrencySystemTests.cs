using DarkSpace.Core.Data;
using DarkSpace.Core.Systems.Economy;
using Xunit;

namespace DarkSpace.Tests.Unit;

/// <summary>Тесты системы валют.</summary>
public sealed class CurrencySystemTests
{
    [Fact]
    public void BaseValues_OrderedCorrectly()
    {
        Assert.Equal(1.0, CurrencySystem.BaseValueInCredits(CurrencyType.Credits));
        Assert.True(CurrencySystem.BaseValueInCredits(CurrencyType.Relics)
                    > CurrencySystem.BaseValueInCredits(CurrencyType.Credits));
        Assert.True(CurrencySystem.BaseValueInCredits(CurrencyType.DarkCrystals)
                    > CurrencySystem.BaseValueInCredits(CurrencyType.Credits));
    }

    [Fact]
    public void SameCurrency_RateIsOne()
    {
        foreach (CurrencyType currency in Enum.GetValues<CurrencyType>())
        {
            Assert.Equal(1.0, CurrencySystem.GetExchangeRate(currency, currency, EconomyType.Industrial));
        }
    }

    [Fact]
    public void Convert_RoundTrip_IsApproximatelyStable()
    {
        double credits = 10_000;
        double relics = CurrencySystem.Convert(credits, CurrencyType.Credits, CurrencyType.Relics, EconomyType.Industrial);
        double back = CurrencySystem.Convert(relics, CurrencyType.Relics, CurrencyType.Credits, EconomyType.Industrial);
        Assert.Equal(credits, back, precision: 6);
    }

    [Fact]
    public void ReligiousEconomy_ValuesRelicsHigher()
    {
        double industrial = CurrencySystem.GetExchangeRate(CurrencyType.Credits, CurrencyType.Relics, EconomyType.Industrial);
        double religious = CurrencySystem.GetExchangeRate(CurrencyType.Credits, CurrencyType.Relics, EconomyType.Religious);
        // В религиозной экономике реликвия дороже => за тот же кредит дают меньше реликвий.
        Assert.True(religious < industrial);
    }

    [Fact]
    public void PirateEconomy_ValuesDarkCrystalsHigher()
    {
        double industrial = CurrencySystem.GetExchangeRate(CurrencyType.Credits, CurrencyType.DarkCrystals, EconomyType.Industrial);
        double pirate = CurrencySystem.GetExchangeRate(CurrencyType.Credits, CurrencyType.DarkCrystals, EconomyType.Pirate);
        Assert.True(pirate < industrial);
    }

    [Fact]
    public void InvalidCurrency_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => CurrencySystem.BaseValueInCredits((CurrencyType)999));
    }
}
