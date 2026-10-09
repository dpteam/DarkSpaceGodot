using System;
using DarkSpace.Core.Data;
using DarkSpace.Core.Utils;

namespace DarkSpace.Core.Systems.Economy;

/// <summary>
/// Рынок станции: цены и запасы товаров, зависящие от экономики системы (в духе Elite).
/// </summary>
/// <remarks>
/// <para>
/// Формула цены портирована из Elite: <c>цена = max(1, база + экономический_фактор × уровень_экономики)</c>.
/// Уровень экономики: 0 — богатая индустриальная (продовольствие дорого, механизмы дёшево),
/// 7 — бедная аграрная (наоборот). Плюс небольшая воспроизводимая флуктуация по сиду системы.
/// </para>
/// <para>
/// Рынок хранит запас каждого товара: покупка уменьшает запас, продажа — увеличивает.
/// Состояние рынка — часть мира (детерминировано по сиду системы).
/// </para>
/// </remarks>
public sealed class StationMarket
{
    /// <summary>Доля цены, которую станция платит игроку при продаже (наценка станции).</summary>
    public const double SellMargin = 0.82;

    private readonly int _economyLevel;
    private readonly int[] _stock;
    private readonly int[] _fluctuation;

    /// <summary>Создаёт рынок для станции с заданной экономикой и сидом (для флуктуации).</summary>
    /// <param name="economy">Тип экономики системы.</param>
    /// <param name="seed">Сид системы (для воспроизводимой флуктуации цен/запасов).</param>
    public StationMarket(EconomyType economy, ulong seed)
    {
        _economyLevel = EconomyLevel(economy);
        var random = new DeterministicRandom(SeedHelper.Combine(seed, "market"));

        int count = CommodityCatalog.Count;
        _stock = new int[count];
        _fluctuation = new int[count];

        for (int id = 0; id < count; id++)
        {
            Commodity c = CommodityCatalog.GetById(id);
            // Запас: базовый, скорректированный экономикой (что производят — того больше).
            int stockJitter = random.NextInt(0, 6);
            _stock[id] = Math.Max(0, c.BaseQuantity + stockJitter - (c.EconomicFactor * _economyLevel / 4));
            // Флуктуация цены: небольшой воспроизводимый разброс.
            _fluctuation[id] = random.NextInt(-2, 3);
        }
    }

    /// <summary>Уровень экономики (0 — индустриальная … 7 — аграрная), как в Elite.</summary>
    public int EconomyLevelValue => _economyLevel;

    /// <summary>Цена покупки товара игроком у станции.</summary>
    /// <exception cref="ArgumentOutOfRangeException">Если идентификатор товара вне диапазона.</exception>
    public int BuyPrice(int commodityId)
    {
        Commodity c = CommodityCatalog.GetById(commodityId);
        int price = c.BasePrice + (c.EconomicFactor * _economyLevel) + _fluctuation[commodityId];
        return Math.Max(1, price);
    }

    /// <summary>Цена продажи товара игроком станции.</summary>
    /// <exception cref="ArgumentOutOfRangeException">Если идентификатор товара вне диапазона.</exception>
    public int SellPrice(int commodityId)
        => Math.Max(1, (int)Math.Round(BuyPrice(commodityId) * SellMargin));

    /// <summary>Текущий запас товара на станции.</summary>
    /// <exception cref="ArgumentOutOfRangeException">Если идентификатор товара вне диапазона.</exception>
    public int Stock(int commodityId)
    {
        _ = CommodityCatalog.GetById(commodityId);
        return _stock[commodityId];
    }

    /// <summary>Уменьшает запас (при покупке игроком). Возвращает фактически снятое количество.</summary>
    public int RemoveStock(int commodityId, int quantity)
    {
        int taken = Math.Min(Math.Max(0, quantity), _stock[commodityId]);
        _stock[commodityId] -= taken;
        return taken;
    }

    /// <summary>Увеличивает запас (при продаже игроком).</summary>
    public void AddStock(int commodityId, int quantity)
    {
        if (quantity > 0)
        {
            _stock[commodityId] += quantity;
        }
    }

    /// <summary>
    /// Сопоставление типа экономики проекта уровню 0–7 (как в Elite): 0 — богатая индустриальная,
    /// 7 — бедная аграрная.
    /// </summary>
    public static int EconomyLevel(EconomyType economy) => economy switch
    {
        EconomyType.Industrial => 0,
        EconomyType.Technocratic => 2,
        EconomyType.Military => 3,
        EconomyType.Pirate => 4,
        EconomyType.Religious => 5,
        EconomyType.Agricultural => 7,
        _ => 4,
    };
}
