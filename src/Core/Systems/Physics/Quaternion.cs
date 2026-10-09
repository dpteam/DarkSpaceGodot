using System;
using DarkSpace.Core.Data;

namespace DarkSpace.Core.Systems.Physics;

/// <summary>
/// Кватернион вращения (w, x, y, z). Ориентация корабля в ядре.
/// </summary>
/// <remarks>
/// Используется для 6-степенной модели полёта: положение + ориентация. Кватернионы выбраны вместо
/// углов Эйлера, потому что они не имеют «шарнирного замка» (gimbal lock) и устойчиво
/// интегрируются по угловой скорости. Единичный кватернион q вращает вектор по формуле v' = q·v·q⁻¹.
/// </remarks>
/// <param name="W">Скалярная часть.</param>
/// <param name="X">Компонента X векторной части.</param>
/// <param name="Y">Компонента Y векторной части.</param>
/// <param name="Z">Компонента Z векторной части.</param>
public readonly record struct Quaternion(double W, double X, double Y, double Z)
{
    /// <summary>Единичный (нулевой поворот) кватернион.</summary>
    public static Quaternion Identity => new(1.0, 0.0, 0.0, 0.0);

    /// <summary>Возвращает кватернион, поворачивающий на угол <paramref name="angle"/> вокруг оси <paramref name="axis"/>.</summary>
    /// <param name="axis">Ось вращения (нормализуется).</param>
    /// <param name="angle">Угол в радианах.</param>
    public static Quaternion FromAxisAngle(Vector3D axis, double angle)
    {
        Vector3D a = axis.Normalized();
        double half = angle * 0.5;
        double s = Math.Sin(half);
        return new Quaternion(Math.Cos(half), a.X * s, a.Y * s, a.Z * s);
    }

    /// <summary>Произведение кватернионов (композиция поворотов).</summary>
    public static Quaternion operator *(Quaternion a, Quaternion b) => new(
        (a.W * b.W) - (a.X * b.X) - (a.Y * b.Y) - (a.Z * b.Z),
        (a.W * b.X) + (a.X * b.W) + (a.Y * b.Z) - (a.Z * b.Y),
        (a.W * b.Y) - (a.X * b.Z) + (a.Y * b.W) + (a.Z * b.X),
        (a.W * b.Z) + (a.X * b.Y) - (a.Y * b.X) + (a.Z * b.W));

    /// <summary>Норма кватерниона.</summary>
    public double Length => Math.Sqrt((W * W) + (X * X) + (Y * Y) + (Z * Z));

    /// <summary>Возвращает нормализованный кватернион.</summary>
    public Quaternion Normalized()
    {
        double length = Length;
        return length > 1e-12
            ? new Quaternion(W / length, X / length, Y / length, Z / length)
            : Identity;
    }

    /// <summary>Поворачивает вектор этим кватернионом (v' = q·v·q⁻¹).</summary>
    /// <param name="v">Вектор в локальной системе координат.</param>
    public Vector3D Rotate(Vector3D v)
    {
        var qv = new Vector3D(X, Y, Z);
        Vector3D t = Vector3D.Cross(qv, v) * 2.0;
        return v + (t * W) + Vector3D.Cross(qv, t);
    }

    /// <summary>
    /// Интегрирует ориентацию по угловой скорости за шаг <paramref name="deltaSeconds"/>:
    /// q' = normalize(q + 0.5·(0, ω)⊗q·dt). Угловая скорость задана в мировых координатах.
    /// </summary>
    public Quaternion Integrate(Vector3D angularVelocity, double deltaSeconds)
    {
        var omega = new Quaternion(0.0, angularVelocity.X, angularVelocity.Y, angularVelocity.Z);
        Quaternion dq = omega * this;
        return new Quaternion(
            W + (0.5 * dq.W * deltaSeconds),
            X + (0.5 * dq.X * deltaSeconds),
            Y + (0.5 * dq.Y * deltaSeconds),
            Z + (0.5 * dq.Z * deltaSeconds)).Normalized();
    }
}
