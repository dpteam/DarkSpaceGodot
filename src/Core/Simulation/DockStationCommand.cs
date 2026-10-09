namespace DarkSpace.Core.Simulation;

/// <summary>
/// Команда стыковки с космической станцией системы.
/// </summary>
/// <remarks>
/// Применяется, если в системе есть станция и корабль находится в её радиусе стыковки
/// (см. <see cref="DarkSpace.Core.Systems.Orbital.StarSystemModel.StationDockRadius"/>).
/// После стыковки открывается доступ к рынку станции.
/// </remarks>
/// <param name="PlayerId">Идентификатор игрока.</param>
/// <param name="Sequence">Порядковый номер команды.</param>
public sealed record DockStationCommand(int PlayerId, long Sequence) : IGameCommand
{
    /// <inheritdoc />
    public CommandType Type => CommandType.DockStation;
}
