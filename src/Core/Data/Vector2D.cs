using System;

namespace DarkSpace.Core.Data;

/// <summary>
/// Простой двумерный вектор в чистом ядре (без зависимости от Godot).
/// </summary>
/// <remarks>
/// Используется для динамики корабля в рамках тик-симуляции. Двойная точность выбрана
/// осознанно: детерминизм и совместимость между клиентом и сервером важнее экономии памяти.
/// </remarks>
/// <param name="X">Компонента X.</param>
/// <param name="Y">Компонента Y.</param>
public readonly record struct Vector2D(double X, double Y)
{
    /// <summary>Нулевой вектор.</summary>
    public static Vector2D Zero => new(0.0, 0.0);

    /// <summary>Длина вектора.</summary>
    public double Length => Math.Sqrt((X * X) + (Y * Y));

    /// <summary>Сложение векторов.</summary>
    public static Vector2D operator +(Vector2D a, Vector2D b) => new(a.X + b.X, a.Y + b.Y);

    /// <summary>Вычитание векторов.</summary>
    public static Vector2D operator -(Vector2D a, Vector2D b) => new(a.X - b.X, a.Y - b.Y);

    /// <summary>Умножение вектора на скаляр.</summary>
    public static Vector2D operator *(Vector2D a, double scalar) => new(a.X * scalar, a.Y * scalar);

    /// <summary>Умножение скаляра на вектор.</summary>
    public static Vector2D operator *(double scalar, Vector2D a) => new(a.X * scalar, a.Y * scalar);

    /// <summary>Возвращает нормализованную копию вектора (или нулевой вектор, если длина равна нулю).</summary>
    public Vector2D Normalized()
    {
        double length = Length;
        return length > 1e-9 ? new Vector2D(X / length, Y / length) : Zero;
    }

    /// <summary>Ограничивает длину вектора значением <paramref name="maxLength"/>.</summary>
    public Vector2D ClampLength(double maxLength)
    {
        double length = Length;
        return length > maxLength && length > 1e-9 ? new Vector2D(X / length * maxLength, Y / length * maxLength) : this;
    }
}
