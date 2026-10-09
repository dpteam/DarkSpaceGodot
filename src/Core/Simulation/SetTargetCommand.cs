namespace DarkSpace.Core.Simulation;

/// <summary>Команда назначения целевой планеты в текущей системе.</summary>
/// <param name="PlayerId">Идентификатор игрока.</param>
/// <param name="Sequence">Порядковый номер команды.</param>
/// <param name="BodyIndex">Индекс планеты-цели, либо -1 для снятия цели.</param>
public sealed record SetTargetCommand(int PlayerId, long Sequence, int BodyIndex) : IGameCommand
{
    /// <inheritdoc />
    public CommandType Type => CommandType.SetTarget;
}
