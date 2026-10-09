namespace DarkSpace.Core.Simulation;

/// <summary>Команда отправки сообщения в чат.</summary>
/// <param name="PlayerId">Идентификатор игрока.</param>
/// <param name="Sequence">Порядковый номер команды.</param>
/// <param name="Text">Текст сообщения.</param>
public sealed record ChatCommand(int PlayerId, long Sequence, string Text) : IGameCommand
{
    /// <inheritdoc />
    public CommandType Type => CommandType.Chat;
}
