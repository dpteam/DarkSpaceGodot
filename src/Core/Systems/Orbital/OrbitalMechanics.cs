using System;
using DarkSpace.Core.Data;

namespace DarkSpace.Core.Systems.Orbital;

/// <summary>
/// Кеплерова орбитальная механика: положение тела на орбите в заданный момент времени.
/// </summary>
/// <remarks>
/// Детерминирована и не зависит от Godot. Задача Кеплера решается методом Ньютона —
/// число итераций фиксировано, поэтому результат воспроизводим.
/// </remarks>
public static class OrbitalMechanics
{
    private const int KeplerIterations = 8;
    private const double TwoPi = Math.Tau;

    /// <summary>
    /// Возвращает положение тела на орбите в заданный момент времени.
    /// </summary>
    /// <param name="orbit">Кеплеровы элементы орбиты.</param>
    /// <param name="timeSeconds">Время с начала отсчёта, в секундах.</param>
    /// <exception cref="ArgumentNullException">Если <paramref name="orbit"/> равен <see langword="null"/>.</exception>
    public static Vector3D PositionAt(OrbitData orbit, double timeSeconds)
    {
        ArgumentNullException.ThrowIfNull(orbit);

        double meanAnomaly = Wrap(orbit.MeanAnomalyAtEpoch + (TwoPi * timeSeconds / orbit.PeriodSeconds));
        double eccentricAnomaly = SolveKepler(meanAnomaly, orbit.Eccentricity);

        // Положение в плоскости орбиты (перицентр — вдоль +X).
        double cosE = Math.Cos(eccentricAnomaly);
        double sinE = Math.Sin(eccentricAnomaly);
        double x = orbit.SemiMajorAxis * (cosE - orbit.Eccentricity);
        double y = orbit.SemiMajorAxis * Math.Sqrt(1.0 - (orbit.Eccentricity * orbit.Eccentricity)) * sinE;
        var inPlane = new Vector3D(x, y, 0.0);

        return RotateToEcliptic(inPlane, orbit);
    }

    /// <summary>
    /// Возвращает расстояние от звезды в заданный момент времени.
    /// </summary>
    public static double RadiusAt(OrbitData orbit, double timeSeconds)
    {
        ArgumentNullException.ThrowIfNull(orbit);
        double meanAnomaly = Wrap(orbit.MeanAnomalyAtEpoch + (TwoPi * timeSeconds / orbit.PeriodSeconds));
        double eccentricAnomaly = SolveKepler(meanAnomaly, orbit.Eccentricity);
        return orbit.SemiMajorAxis * (1.0 - (orbit.Eccentricity * Math.Cos(eccentricAnomaly)));
    }

    /// <summary>
    /// Решает уравнение Кеплера E − e·sinE = M методом Ньютона.
    /// </summary>
    /// <param name="meanAnomaly">Средняя аномалия M, в радианах.</param>
    /// <param name="eccentricity">Эксцентриситет e.</param>
    public static double SolveKepler(double meanAnomaly, double eccentricity)
    {
        double e = Math.Clamp(eccentricity, 0.0, 0.95);
        double m = Wrap(meanAnomaly);

        // Хорошее начальное приближение.
        double eccAnomaly = e < 0.8 ? m : Math.PI;

        for (int i = 0; i < KeplerIterations; i++)
        {
            double f = eccAnomaly - (e * Math.Sin(eccAnomaly)) - m;
            double fPrime = 1.0 - (e * Math.Cos(eccAnomaly));
            if (Math.Abs(fPrime) < 1e-12)
            {
                break;
            }

            eccAnomaly -= f / fPrime;
        }

        return eccAnomaly;
    }

    private static Vector3D RotateToEcliptic(Vector3D inPlane, OrbitData orbit)
    {
        // Поворот: сначала ω вокруг Z (в плоскости), затем i вокруг X, затем Ω вокруг Z.
        double cw = Math.Cos(orbit.ArgumentOfPeriapsis);
        double sw = Math.Sin(orbit.ArgumentOfPeriapsis);
        double ci = Math.Cos(orbit.Inclination);
        double si = Math.Sin(orbit.Inclination);
        double cn = Math.Cos(orbit.LongitudeOfAscendingNode);
        double sn = Math.Sin(orbit.LongitudeOfAscendingNode);

        // Rz(ω)
        double x1 = (inPlane.X * cw) - (inPlane.Y * sw);
        double y1 = (inPlane.X * sw) + (inPlane.Y * cw);
        double z1 = inPlane.Z;

        // Rx(i)
        double x2 = x1;
        double y2 = (y1 * ci) - (z1 * si);
        double z2 = (y1 * si) + (z1 * ci);

        // Rz(Ω)
        double x3 = (x2 * cn) - (y2 * sn);
        double y3 = (x2 * sn) + (y2 * cn);
        double z3 = z2;

        return new Vector3D(x3, y3, z3);
    }

    private static double Wrap(double angle)
    {
        double wrapped = angle % TwoPi;
        if (wrapped < 0)
        {
            wrapped += TwoPi;
        }

        return wrapped;
    }
}
