using System;
using DarkSpace.Core.Data;

namespace DarkSpace.Core.Systems.Physics;

/// <summary>
/// Астрофизические формулы проекта: единый источник истины по орбитальной и ракетной динамике.
/// </summary>
/// <remarks>
/// <para>
/// Единицы измерения самосогласованы: длина — условные единицы (SU), время — секунды, масса —
/// условные единицы. Гравитационный параметр μ задаётся в SU³/с². Все формулы размерно корректны,
/// поэтому физика игры «сходится» с законами нашей вселенной и проверяется тестами-инвариантами.
/// </para>
/// <para>
/// Соответствие законам Кеплера/Ньютона:
/// круговая скорость v = √(μ/r); скорость убегания v_esc = √(2μ/r);
/// vis-viva v² = μ·(2/r − 1/a); период T = 2π·√(a³/μ);
/// удельная энергия ε = v²/2 − μ/r; ракетное уравнение Δv = v_e·ln(m₀/m_f).
/// </para>
/// </remarks>
public static class Astro
{
    /// <summary>Стандартное ускорение свободного падения (для связи Isp и скорости истечения).</summary>
    public const double StandardGravity = 9.80665;

    /// <summary>Круговая орбитальная скорость на радиусе r: v = √(μ/r).</summary>
    /// <param name="mu">Гравитационный параметр.</param>
    /// <param name="radius">Радиус орбиты.</param>
    /// <exception cref="ArgumentOutOfRangeException">Если μ ≤ 0 или радиус ≤ 0.</exception>
    public static double CircularSpeed(double mu, double radius)
    {
        RequirePositive(mu, nameof(mu));
        RequirePositive(radius, nameof(radius));
        return Math.Sqrt(mu / radius);
    }

    /// <summary>Скорость убегания на радиусе r: v_esc = √(2μ/r) = √2·v_круг.</summary>
    /// <exception cref="ArgumentOutOfRangeException">Если μ ≤ 0 или радиус ≤ 0.</exception>
    public static double EscapeSpeed(double mu, double radius)
    {
        RequirePositive(mu, nameof(mu));
        RequirePositive(radius, nameof(radius));
        return Math.Sqrt(2.0 * mu / radius);
    }

    /// <summary>
    /// Скорость по уравнению vis-viva на данном радиусе и большой полуоси: v² = μ·(2/r − 1/a).
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">Если параметры не положительны.</exception>
    public static double VisVivaSpeed(double mu, double radius, double semiMajorAxis)
    {
        RequirePositive(mu, nameof(mu));
        RequirePositive(radius, nameof(radius));
        RequirePositive(semiMajorAxis, nameof(semiMajorAxis));

        double value = mu * ((2.0 / radius) - (1.0 / semiMajorAxis));
        return value <= 0 ? 0.0 : Math.Sqrt(value);
    }

    /// <summary>Период обращения по третьему закону Кеплера: T = 2π·√(a³/μ).</summary>
    /// <exception cref="ArgumentOutOfRangeException">Если μ ≤ 0 или a ≤ 0.</exception>
    public static double OrbitalPeriod(double mu, double semiMajorAxis)
    {
        RequirePositive(mu, nameof(mu));
        RequirePositive(semiMajorAxis, nameof(semiMajorAxis));
        return Math.Tau * Math.Sqrt((semiMajorAxis * semiMajorAxis * semiMajorAxis) / mu);
    }

    /// <summary>Удельная орбитальная энергия: ε = v²/2 − μ/r. Отрицательна для связанной орбиты.</summary>
    public static double SpecificOrbitalEnergy(double mu, double radius, double speed)
    {
        RequirePositive(radius, nameof(radius));
        return (speed * speed * 0.5) - (mu / radius);
    }

    /// <summary>
    /// Потенциал сглаженной (Пламмеровской) гравитации: Φ(r) = −μ/√(r²+ε²).
    /// Используется для проверки сохранения энергии при численном интегрировании со сглаживанием.
    /// </summary>
    /// <param name="mu">Гравитационный параметр.</param>
    /// <param name="radius">Расстояние до тела.</param>
    /// <param name="softening">Длина сглаживания (не отрицательна).</param>
    public static double SoftenedPotential(double mu, double radius, double softening)
    {
        if (softening < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(softening), softening, "Длина сглаживания не может быть отрицательной.");
        }

        return -mu / Math.Sqrt((radius * radius) + (softening * softening));
    }

    /// <summary>
    /// Полная удельная механическая энергия в сглаженном потенциале звезды (кинетика + потенциал).
    /// Инвариант движения: при отсутствии тяги и сопротивления сохраняется.
    /// </summary>
    public static double SoftenedSpecificEnergy(double mu, double softening, Vector3D position, Vector3D velocity)
        => SoftenedPotential(mu, position.Length, softening) + (velocity.Length * velocity.Length * 0.5);

    /// <summary>Модуль удельного углового момента h = |r × v|. Инвариант движения в центральном поле.</summary>
    public static double SpecificAngularMomentum(Vector3D position, Vector3D velocity)
        => Vector3D.Cross(position, velocity).Length;

    /// <summary>
    /// Круговая скорость в сглаженном поле: v = √( μ·r² / (r²+ε²)^{3/2} ).
    /// Именно эта скорость даёт минимальный радиальный дрейф при интегрировании со сглаживанием.
    /// </summary>
    public static double SoftenedCircularSpeed(double mu, double radius, double softening)
    {
        RequirePositive(mu, nameof(mu));
        RequirePositive(radius, nameof(radius));
        if (softening < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(softening), softening, "Длина сглаживания не может быть отрицательной.");
        }

        double r2 = radius * radius;
        double denom = Math.Pow(r2 + (softening * softening), 1.5);
        return Math.Sqrt(mu * r2 / denom);
    }

    /// <summary>Ускорение от сглаженной гравитации: a = −μ·r̅/(r²+ε²)^(3/2).</summary>
    /// <param name="mu">Гравитационный параметр.</param>
    /// <param name="position">Вектор от тела к точке.</param>
    /// <param name="softening">Длина сглаживания.</param>
    public static Vector3D SoftenedGravityAcceleration(double mu, Vector3D position, double softening)
    {
        double r = position.Length;
        if (r < 1e-9)
        {
            return Vector3D.Zero;
        }

        double denominator = Math.Pow((r * r) + (softening * softening), 1.5);
        return position * (-mu / denominator);
    }

    /// <summary>Ракетное уравнение Циолковского: Δv = v_e·ln(m₀/m_f).</summary>
    /// <param name="exhaustVelocity">Эффективная скорость истечения v_e.</param>
    /// <param name="initialMass">Начальная масса m₀.</param>
    /// <param name="finalMass">Конечная масса m_f (сухая).</param>
    /// <exception cref="ArgumentException">Если массы некорректны (m_f ≤ 0 или m_f &gt; m₀).</exception>
    public static double RocketDeltaV(double exhaustVelocity, double initialMass, double finalMass)
    {
        RequirePositive(exhaustVelocity, nameof(exhaustVelocity));
        RequirePositive(finalMass, nameof(finalMass));

        if (initialMass < finalMass)
        {
            throw new ArgumentException("Начальная масса не может быть меньше конечной.", nameof(initialMass));
        }

        return exhaustVelocity * Math.Log(initialMass / finalMass);
    }

    /// <summary>Секундный расход массы: mdot = F/v_e.</summary>
    /// <param name="thrust">Тяга (сила).</param>
    /// <param name="exhaustVelocity">Эффективная скорость истечения.</param>
    public static double MassFlowRate(double thrust, double exhaustVelocity)
    {
        RequirePositive(exhaustVelocity, nameof(exhaustVelocity));
        return Math.Max(0.0, thrust) / exhaustVelocity;
    }

    private static void RequirePositive(double value, string parameter)
    {
        if (value <= 0 || double.IsNaN(value) || double.IsInfinity(value))
        {
            throw new ArgumentOutOfRangeException(parameter, value, "Значение должно быть положительным и конечным.");
        }
    }
}
