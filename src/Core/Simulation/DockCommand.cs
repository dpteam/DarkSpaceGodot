namespace DarkSpace.Core.Simulation;

/// <summary>
/// Команда стыковки (посадки) с целевой планетой.
/// </summary>
/// <remarks>
/// Применяется только если назначена цель и корабль находится в радиусе стыковки
/// (см. <see cref="WorldSimulation.DockRange"/>). Иначе команда игнорируется.
/// </remarks>
/// <param name="PlayerId">Идентификатор игрока.</param>
/// <param name="Sequence">Порядковый номер команды.</param>
public sealed record DockCommand(int PlayerId, long Sequence) : IGameCommand
{
    /// <inheritdoc />
    public CommandType Type => CommandType.Dock;
}
