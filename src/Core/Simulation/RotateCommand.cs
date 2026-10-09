namespace DarkSpace.Core.Simulation;

/// <summary>
/// Команда управления ориентацией корабля (органы управления / реактивная система).
/// </summary>
/// <remarks>
/// Задаёт угловое ускорение вокруг локальных осей корабля: <see cref="Pitch"/> — тангаж
/// (вокруг «вправо»), <see cref="Yaw"/> — рыскание (вокруг «вверх»), <see cref="Roll"/> —
/// крен (вокруг носа). Каждая компонента обычно в диапазоне -1..1. Итоговая ориентация
/// интегрируется по угловой скорости, поэтому корабль обладает инерцией вращения.
/// </remarks>
/// <param name="PlayerId">Идентификатор игрока.</param>
/// <param name="Sequence">Порядковый номер команды.</param>
/// <param name="Pitch">Тангаж (-1 — нос вниз, +1 — нос вверх).</param>
/// <param name="Yaw">Рыскание (-1 — влево, +1 — вправо).</param>
/// <param name="Roll">Крен (-1 — влево, +1 — вправо).</param>
public sealed record RotateCommand(int PlayerId, long Sequence, double Pitch, double Yaw, double Roll) : IGameCommand
{
    /// <inheritdoc />
    public CommandType Type => CommandType.Rotate;
}
