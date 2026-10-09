namespace DarkSpace.Core.Simulation;

/// <summary>Команда продажи предмета на рынке текущей системы.</summary>
/// <param name="PlayerId">Идентификатор игрока.</param>
/// <param name="Sequence">Порядковый номер команды.</param>
/// <param name="ItemId">Идентификатор базового предмета из каталога.</param>
/// <param name="Quantity">Количество.</param>
public sealed record SellItemCommand(int PlayerId, long Sequence, int ItemId, int Quantity) : IGameCommand
{
    /// <inheritdoc />
    public CommandType Type => CommandType.SellItem;
}
