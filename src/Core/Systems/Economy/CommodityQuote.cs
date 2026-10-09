namespace DarkSpace.Core.Systems.Economy;

/// <summary>
/// Котировка товара на рынке станции: цены и запас (для отображения в UI).
/// </summary>
/// <param name="CommodityId">Идентификатор товара.</param>
/// <param name="Name">Название товара.</param>
/// <param name="Unit">Единица измерения.</param>
/// <param name="BuyPrice">Цена покупки игроком.</param>
/// <param name="SellPrice">Цена продажи игроком.</param>
/// <param name="Stock">Текущий запас на станции.</param>
public sealed record CommodityQuote(
    int CommodityId,
    string Name,
    string Unit,
    int BuyPrice,
    int SellPrice,
    int Stock);
