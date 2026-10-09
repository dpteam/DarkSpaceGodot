namespace DarkSpace.Core.Systems.Economy;

/// <summary>
/// Торговый товар (коммодити) в духе Elite.
/// </summary>
/// <remarks>
/// Цена товара в системе зависит от типа экономики: <c>цена = база + экономический_фактор × уровень_экономики</c>.
/// Отрицательный фактор означает, что товар здесь производят (дёшево), положительный — завозят (дорого).
/// </remarks>
/// <param name="Id">Идентификатор товара.</param>
/// <param name="Name">Название.</param>
/// <param name="Unit">Единица измерения (т, кг, г).</param>
/// <param name="BasePrice">Базовая цена.</param>
/// <param name="EconomicFactor">Экономический фактор (знак и величина влияния экономики).</param>
/// <param name="BaseQuantity">Базовый запас на станции.</param>
public sealed record Commodity(
    int Id,
    string Name,
    string Unit,
    int BasePrice,
    int EconomicFactor,
    int BaseQuantity);
