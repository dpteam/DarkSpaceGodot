namespace DarkSpace.Core.Simulation;

/// <summary>
/// Команда тяги двигателей корабля в его <b>локальной</b> системе координат.
/// </summary>
/// <remarks>
/// Тяга задаётся относительно носа корабля (как в Frontier/Elite), а не в мировых осях:
/// <see cref="Forward"/> — вдоль носа (главный двигатель), <see cref="Strafe"/> — вбок,
/// <see cref="Lift"/> — вверх (латеральные/вертикальные двигатели). Мировое направление тяги
/// вычисляется по текущей ориентации корабля. Каждая компонента обычно в диапазоне -1..1.
/// </remarks>
/// <param name="PlayerId">Идентификатор игрока.</param>
/// <param name="Sequence">Порядковый номер команды.</param>
/// <param name="Forward">Тяга вдоль носа (-1 — назад, +1 — вперёд).</param>
/// <param name="Strafe">Тяга вбок (-1 — влево, +1 — вправо).</param>
/// <param name="Lift">Тяга вверх (-1 — вниз, +1 — вверх).</param>
public sealed record MoveCommand(int PlayerId, long Sequence, double Forward, double Strafe = 0.0, double Lift = 0.0) : IGameCommand
{
    /// <inheritdoc />
    public CommandType Type => CommandType.Move;
}
