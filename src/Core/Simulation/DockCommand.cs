namespace DarkSpace.Core.Simulation;

/// <summary>
/// Команда стыковки (посадки) с целевой планетой.
/// </summary>
/// <remarks>
/// Применяется только если назначена цель и корабль находится в радиусе стыковки
/// (поверхность планеты плюс зазор, см. <see cref="DarkSpace.Core.Systems.Orbital.StarSystemModel.DockRadius"/>).
/// Иначе команда игнорируется.
/// </remarks>
/// <param name="PlayerId">Идентификатор игрока.</param>
/// <param name="Sequence">Порядковый номер команды.</param>
public sealed record DockCommand(int PlayerId, long Sequence) : IGameCommand
{
    /// <inheritdoc />
    public CommandType Type => CommandType.Dock;
}
