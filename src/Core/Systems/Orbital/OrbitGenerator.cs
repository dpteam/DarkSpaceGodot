using DarkSpace.Core.Data;
using DarkSpace.Core.Generation;
using DarkSpace.Core.Utils;

namespace DarkSpace.Core.Systems.Orbital;

/// <summary>
/// Процедурный генератор орбит планет звёздной системы.
/// </summary>
/// <remarks>
/// Орбиты строятся от сида системы и детерминированы: большие полуоси растут с индексом планеты
/// (орбитальные «слоты»), а период вычисляется по третьему закону Кеплера. Наклон, эксцентриситет
/// и фазы — случайные, но воспроизводимые.
/// </remarks>
public static class OrbitGenerator
{
    /// <summary>
    /// Гравитационный параметр звезды в условных единицах. Подобран так, чтобы внутренняя орбита
    /// делала оборот за ~6 секунд (наглядно на карте), а орбитальная скорость планет оставалась
    /// ниже тяговых возможностей корабля — иначе стыковка была бы физически невозможна.
    /// </summary>
    public const double GravitationalParameter = 250.0;

    /// <summary>Большая полуось самой внутренней орбиты (условные единицы).</summary>
    public const double InnerOrbit = 6.0;

    /// <summary>Шаг между орбитами по большой полуоси (условные единицы).</summary>
    public const double OrbitSpacing = 4.5;

    /// <summary>
    /// Генерирует орбиты для всех планет системы.
    /// </summary>
    /// <param name="system">Звёздная система.</param>
    /// <returns>Список орбит, по одной на каждую планету, в порядке возрастания полуоси.</returns>
    /// <exception cref="ArgumentNullException">Если <paramref name="system"/> равен <see langword="null"/>.</exception>
    public static IReadOnlyList<OrbitData> GenerateOrbits(StarSystemData system)
    {
        ArgumentNullException.ThrowIfNull(system);

        var orbits = new List<OrbitData>(system.PlanetCount);
        for (int i = 0; i < system.PlanetCount; i++)
        {
            orbits.Add(GenerateOrbit(system, i));
        }

        return orbits;
    }

    /// <summary>
    /// Генерирует орбиту одной планеты.
    /// </summary>
    /// <param name="system">Звёздная система.</param>
    /// <param name="planetIndex">Индекс планеты.</param>
    /// <exception cref="ArgumentNullException">Если <paramref name="system"/> равен <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Если индекс вне диапазона.</exception>
    public static OrbitData GenerateOrbit(StarSystemData system, int planetIndex)
    {
        ArgumentNullException.ThrowIfNull(system);

        if (planetIndex < 0 || planetIndex >= system.PlanetCount)
        {
            throw new ArgumentOutOfRangeException(
                nameof(planetIndex),
                planetIndex,
                $"Индекс планеты должен быть в диапазоне 0–{system.PlanetCount - 1}.");
        }

        ulong seed = SeedHelper.Combine(system.Seed, "orbit", (ulong)planetIndex);
        var random = new DeterministicRandom(seed);

        // Большая полуось растёт с индексом планеты, с небольшим разбросом.
        double spacingJitter = random.NextDouble(0.9, 1.15);
        double semiMajorAxis = InnerOrbit + (planetIndex * OrbitSpacing * spacingJitter);

        // Эксцентриситет: у газовых гигантов и дальних орбит чуть выше.
        double eccentricity = Math.Round(random.NextDouble(0.0, 0.28), 4);
        if (planetIndex >= 4)
        {
            eccentricity = Math.Round(random.NextDouble(0.0, 0.4), 4);
        }

        double inclination = Math.Round(random.NextDouble(-0.16, 0.16), 4); // ±~9°
        double node = Math.Round(random.NextDouble(0.0, Math.Tau), 4);
        double periapsis = Math.Round(random.NextDouble(0.0, Math.Tau), 4);
        double meanAnomaly = Math.Round(random.NextDouble(0.0, Math.Tau), 4);

        double period = ComputePeriod(semiMajorAxis);

        return new OrbitData(
            planetIndex,
            Math.Round(semiMajorAxis, 4),
            eccentricity,
            inclination,
            node,
            periapsis,
            meanAnomaly,
            Math.Round(period, 4),
            seed);
    }

    /// <summary>
    /// Вычисляет период обращения по третьему закону Кеплера: T = 2π·√(a³/μ).
    /// </summary>
    /// <param name="semiMajorAxis">Большая полуось.</param>
    /// <exception cref="ArgumentOutOfRangeException">Если полуось не положительна.</exception>
    public static double ComputePeriod(double semiMajorAxis)
    {
        if (semiMajorAxis <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(semiMajorAxis), semiMajorAxis, "Полуось должна быть положительной.");
        }

        return Math.Tau * Math.Sqrt((semiMajorAxis * semiMajorAxis * semiMajorAxis) / GravitationalParameter);
    }
}
