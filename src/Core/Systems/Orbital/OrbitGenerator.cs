using DarkSpace.Core.Data;
using DarkSpace.Core.Generation;
using DarkSpace.Core.Systems.Physics;
using DarkSpace.Core.Utils;

namespace DarkSpace.Core.Systems.Orbital;

/// <summary>
/// Процедурный генератор орбит планет звёздной системы.
/// </summary>
/// <remarks>
/// Орбиты строятся от сида системы и детерминированы: большие полуоси растут с индексом планеты
/// (орбитальные «слоты»), а период вычисляется по третьему закону Кеплера через гравитационный
/// параметр звезды μ. Один и тот же μ используется и для гравитации корабля (см.
/// <see cref="StarSystemModel"/>), поэтому орбиты планет и динамика корабля согласованы.
/// </remarks>
public static class OrbitGenerator
{
    /// <summary>Большая полуось самой внутренней орбиты (условные единицы).</summary>
    public const double InnerOrbit = 6.0;

    /// <summary>Шаг между орбитами по большой полуоси (условные единицы).</summary>
    public const double OrbitSpacing = 4.5;

    /// <summary>
    /// Гравитационный параметр по умолчанию (используется там, где звезда неизвестна, и в тестах).
    /// </summary>
    public const double DefaultGravitationalParameter = 250.0;

    /// <summary>
    /// Генерирует орбиты для всех планет системы.
    /// </summary>
    /// <param name="system">Звёздная система.</param>
    /// <param name="mu">Гравитационный параметр звезды (по умолчанию — <see cref="DefaultGravitationalParameter"/>).</param>
    /// <returns>Список орбит, по одной на каждую планету, в порядке возрастания полуоси.</returns>
    /// <exception cref="ArgumentNullException">Если <paramref name="system"/> равен <see langword="null"/>.</exception>
    public static IReadOnlyList<OrbitData> GenerateOrbits(StarSystemData system, double mu = DefaultGravitationalParameter)
    {
        ArgumentNullException.ThrowIfNull(system);

        var orbits = new List<OrbitData>(system.PlanetCount);
        for (int i = 0; i < system.PlanetCount; i++)
        {
            orbits.Add(GenerateOrbit(system, i, mu));
        }

        return orbits;
    }

    /// <summary>
    /// Генерирует орбиту одной планеты.
    /// </summary>
    /// <param name="system">Звёздная система.</param>
    /// <param name="planetIndex">Индекс планеты.</param>
    /// <param name="mu">Гравитационный параметр звезды.</param>
    /// <exception cref="ArgumentNullException">Если <paramref name="system"/> равен <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Если индекс вне диапазона.</exception>
    public static OrbitData GenerateOrbit(StarSystemData system, int planetIndex, double mu = DefaultGravitationalParameter)
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

        // Эксцентриситет: у дальних орбит чуть выше.
        double eccentricity = Math.Round(random.NextDouble(0.0, 0.28), 4);
        if (planetIndex >= 4)
        {
            eccentricity = Math.Round(random.NextDouble(0.0, 0.4), 4);
        }

        double inclination = Math.Round(random.NextDouble(-0.16, 0.16), 4); // ±~9°
        double node = Math.Round(random.NextDouble(0.0, Math.Tau), 4);
        double periapsis = Math.Round(random.NextDouble(0.0, Math.Tau), 4);
        double meanAnomaly = Math.Round(random.NextDouble(0.0, Math.Tau), 4);

        double period = ComputePeriod(semiMajorAxis, mu);

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
    /// <param name="mu">Гравитационный параметр звезды.</param>
    /// <exception cref="ArgumentOutOfRangeException">Если полуось не положительна.</exception>
    public static double ComputePeriod(double semiMajorAxis, double mu = DefaultGravitationalParameter)
        => Astro.OrbitalPeriod(mu, semiMajorAxis);
}
