namespace DarkSpace.Core.Simulation;

/// <summary>Команда прыжка через чёрную дыру в другую галактику.</summary>
/// <param name="PlayerId">Идентификатор игрока.</param>
/// <param name="Sequence">Порядковый номер команды.</param>
/// <param name="TargetGalaxyIndex">Индекс галактики назначения (0–256).</param>
public sealed record JumpCommand(int PlayerId, long Sequence, int TargetGalaxyIndex) : IGameCommand
{
    /// <inheritdoc />
    public CommandType Type => CommandType.Jump;
}
