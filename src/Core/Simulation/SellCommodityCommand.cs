namespace DarkSpace.Core.Simulation;

/// <summary>Команда продажи товара на рынке станции (требует стыковки со станцией).</summary>
/// <param name="PlayerId">Идентификатор игрока.</param>
/// <param name="Sequence">Порядковый номер команды.</param>
/// <param name="CommodityId">Идентификатор товара из каталога товаров.</param>
/// <param name="Quantity">Количество.</param>
public sealed record SellCommodityCommand(int PlayerId, long Sequence, int CommodityId, int Quantity) : IGameCommand
{
    /// <inheritdoc />
    public CommandType Type => CommandType.SellCommodity;
}
