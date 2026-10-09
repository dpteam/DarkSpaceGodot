using System;
using DarkSpace.Core.Data;

namespace DarkSpace.Core.Systems.Economy;

/// <summary>
/// Система валют: базовые курсы и плавающий обмен между кредитами, реликвиями и тёмными кристаллами.
/// </summary>
/// <remarks>
/// Все курсы задаются в кредитах за единицу валюты. Обменный курс зависит от типа экономики
/// системы (см. <see cref="EconomyType"/>): например, религиозная экономика ценит реликвии выше,
/// а пиратская — тёмные кристаллы.
/// </remarks>
public static class CurrencySystem
{
    /// <summary>Базовая стоимость единицы валюты в кредитах (индекс совпадает с <see cref="CurrencyType"/>).</summary>
    private static readonly double[] CreditsPerUnit =
    {
        1.0,      // Credits
        2500.0,   // Relics
        1200.0,   // DarkCrystals
    };

    /// <summary>Возвращает базовую стоимость единицы валюты в кредитах.</summary>
    /// <exception cref="ArgumentOutOfRangeException">Если <paramref name="currency"/> неизвестна.</exception>
    public static double BaseValueInCredits(CurrencyType currency)
    {
        int index = (int)currency;
        if (index < 0 || index >= CreditsPerUnit.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(currency), currency, "Неизвестный тип валюты.");
        }

        return CreditsPerUnit[index];
    }

    /// <summary>
    /// Возвращает премию к стоимости валюты в конкретной экономике (множитель ≥ 0).
    /// </summary>
    /// <param name="currency">Валюта.</param>
    /// <param name="economy">Тип экономики системы.</param>
    public static double EconomyPremium(CurrencyType currency, EconomyType economy)
    {
        return (currency, economy) switch
        {
            (CurrencyType.Relics, EconomyType.Religious) => 1.30,
            (CurrencyType.Relics, EconomyType.Military) => 0.85,
            (CurrencyType.Relics, EconomyType.Pirate) => 0.90,
            (CurrencyType.DarkCrystals, EconomyType.Pirate) => 1.30,
            (CurrencyType.DarkCrystals, EconomyType.Technocratic) => 1.15,
            (CurrencyType.DarkCrystals, EconomyType.Religious) => 0.85,
            (CurrencyType.Credits, EconomyType.Pirate) => 0.95,
            _ => 1.0,
        };
    }

    /// <summary>
    /// Возвращает обменный курс: сколько единиц <paramref name="to"/> дают за одну единицу <paramref name="from"/>.
    /// </summary>
    /// <param name="from">Исходная валюта.</param>
    /// <param name="to">Целевая валюта.</param>
    /// <param name="economy">Тип экономики, в которой совершается обмен.</param>
    /// <exception cref="ArgumentOutOfRangeException">Если какая-либо из валют неизвестна.</exception>
    public static double GetExchangeRate(CurrencyType from, CurrencyType to, EconomyType economy)
    {
        if (from == to)
        {
            return 1.0;
        }

        double fromValue = BaseValueInCredits(from) * EconomyPremium(from, economy);
        double toValue = BaseValueInCredits(to) * EconomyPremium(to, economy);
        return fromValue / toValue;
    }

    /// <summary>
    /// Конвертирует сумму из одной валюты в другую по текущему курсу экономики.
    /// </summary>
    /// <param name="amount">Сумма в исходной валюте.</param>
    /// <param name="from">Исходная валюта.</param>
    /// <param name="to">Целевая валюта.</param>
    /// <param name="economy">Тип экономики.</param>
    /// <returns>Сумма в целевой валюте, округлённая до двух знаков.</returns>
    public static double Convert(double amount, CurrencyType from, CurrencyType to, EconomyType economy)
        => Math.Round(amount * GetExchangeRate(from, to, economy), 2);
}
