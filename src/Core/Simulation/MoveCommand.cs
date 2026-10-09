namespace DarkSpace.Core.Simulation;

/// <summary>Команда задания вектора тяги корабля.</summary>
/// <param name="PlayerId">Идентификатор игрока.</param>
/// <param name="Sequence">Порядковый номер команды.</param>
/// <param name="ThrustX">Компонента X вектора тяги (обычно в диапазоне -1..1).</param>
/// <param name="ThrustY">Компонента Y вектора тяги (обычно в диапазоне -1..1).</param>
public sealed record MoveCommand(int PlayerId, long Sequence, double ThrustX, double ThrustY) : IGameCommand
{
    /// <inheritdoc />
    public CommandType Type => CommandType.Move;
}
