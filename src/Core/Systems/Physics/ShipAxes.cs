using DarkSpace.Core.Data;

namespace DarkSpace.Core.Systems.Physics;

/// <summary>
/// Локальные оси корабля в мировых координатах ядра.
/// </summary>
/// <remarks>
/// Правая тройка (right, up, forward) = (+X, +Z, −Y). Такой выбор согласован с отображением
/// координат ядра в сцену Godot: локальный «вперёд» корабля выглядит как −Z (нос) в движке,
/// «вверх» — как +Y, «вправо» — как +X. Тяга двигателя идёт вдоль <see cref="Forward"/>,
/// а органы управления (pitch/yaw/roll) — вокруг <see cref="Right"/>/<see cref="Up"/>/<see cref="Forward"/>.
/// </remarks>
public static class ShipAxes
{
    /// <summary>Локальное «вправо».</summary>
    public static readonly Vector3D Right = new(1.0, 0.0, 0.0);

    /// <summary>Локальное «вверх».</summary>
    public static readonly Vector3D Up = new(0.0, 0.0, 1.0);

    /// <summary>Локальное «вперёд» (нос корабля).</summary>
    public static readonly Vector3D Forward = new(0.0, -1.0, 0.0);

    /// <summary>
    /// Собирает вектор в локальной системе корабля в мировые координаты по текущей ориентации.
    /// </summary>
    /// <param name="orientation">Ориентация корабля.</param>
    /// <param name="forward">Компонента вдоль носа (-1..1).</param>
    /// <param name="strafe">Компонента вправо (латеральные двигатели, -1..1).</param>
    /// <param name="lift">Компонента вверх (вертикальные двигатели, -1..1).</param>
    public static Vector3D LocalToWorld(Quaternion orientation, double forward, double strafe, double lift)
    {
        Vector3D local = (Forward * forward) + (Right * strafe) + (Up * lift);
        return orientation.Rotate(local);
    }

    /// <summary>
    /// Раскладывает мировой вектор на локальные компоненты корабля (вперёд/вбок/вверх) по его ориентации.
    /// Обратная операция к <see cref="LocalToWorld"/>.
    /// </summary>
    /// <param name="orientation">Ориентация корабля.</param>
    /// <param name="world">Вектор в мировых координатах.</param>
    public static (double Forward, double Strafe, double Lift) WorldToLocal(Quaternion orientation, Vector3D world)
    {
        var inverse = new Quaternion(orientation.W, -orientation.X, -orientation.Y, -orientation.Z);
        Vector3D local = inverse.Rotate(world);
        return (Vector3D.Dot(local, Forward), Vector3D.Dot(local, Right), Vector3D.Dot(local, Up));
    }
}
