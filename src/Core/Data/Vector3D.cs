using System;

namespace DarkSpace.Core.Data;

/// <summary>
/// Простой трёхмерный вектор в чистом ядре (без зависимости от Godot).
/// </summary>
/// <remarks>
/// Используется орбитальной механикой и, в перспективе, 3D-полётом (см. TECH-004).
/// Двойная точность — ради детерминизма между клиентом и сервером.
/// </remarks>
/// <param name="X">Компонента X.</param>
/// <param name="Y">Компонента Y.</param>
/// <param name="Z">Компонента Z.</param>
public readonly record struct Vector3D(double X, double Y, double Z)
{
    /// <summary>Нулевой вектор.</summary>
    public static Vector3D Zero => new(0.0, 0.0, 0.0);

    /// <summary>Длина вектора.</summary>
    public double Length => Math.Sqrt((X * X) + (Y * Y) + (Z * Z));

    /// <summary>Скалярное произведение.</summary>
    public static double Dot(Vector3D a, Vector3D b) => (a.X * b.X) + (a.Y * b.Y) + (a.Z * b.Z);

    /// <summary>Векторное произведение.</summary>
    public static Vector3D Cross(Vector3D a, Vector3D b)
        => new((a.Y * b.Z) - (a.Z * b.Y), (a.Z * b.X) - (a.X * b.Z), (a.X * b.Y) - (a.Y * b.X));

    /// <summary>Сложение векторов.</summary>
    public static Vector3D operator +(Vector3D a, Vector3D b) => new(a.X + b.X, a.Y + b.Y, a.Z + b.Z);

    /// <summary>Вычитание векторов.</summary>
    public static Vector3D operator -(Vector3D a, Vector3D b) => new(a.X - b.X, a.Y - b.Y, a.Z - b.Z);

    /// <summary>Умножение вектора на скаляр.</summary>
    public static Vector3D operator *(Vector3D a, double scalar) => new(a.X * scalar, a.Y * scalar, a.Z * scalar);

    /// <summary>Умножение скаляра на вектор.</summary>
    public static Vector3D operator *(double scalar, Vector3D a) => a * scalar;

    /// <summary>Возвращает нормализованную копию (или нулевой вектор, если длина равна нулю).</summary>
    public Vector3D Normalized()
    {
        double length = Length;
        return length > 1e-9 ? new Vector3D(X / length, Y / length, Z / length) : Zero;
    }
}
