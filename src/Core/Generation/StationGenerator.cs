using System;
using DarkSpace.Core.Data;
using DarkSpace.Core.Systems.Orbital;
using DarkSpace.Core.Utils;

namespace DarkSpace.Core.Generation;

/// <summary>
/// Процедурный генератор космических станций.
/// </summary>
/// <remarks>
/// Станция обращается вокруг планеты системы и детерминирована по сиду системы. Есть не во всех
/// системах: наличие определяется флагом <see cref="StarSystemData.HasStation"/>.
/// </remarks>
public static class StationGenerator
{
    private static readonly StationType[] Types =
    {
        StationType.Coriolis,
        StationType.Dodo,
        StationType.Torus,
        StationType.Icosahedral,
        StationType.Ocellus,
    };

    /// <summary>
    /// Генерирует станцию для системы (или <see langword="null"/>, если станции нет).
    /// </summary>
    /// <param name="system">Данные системы.</param>
    /// <param name="hostPlanetRadius">Радиус планеты-хозяина (для вычисления орбиты станции).</param>
    /// <param name="hostBodyIndex">Индекс планеты-хозяина.</param>
    /// <exception cref="ArgumentNullException">Если <paramref name="system"/> равен <see langword="null"/>.</exception>
    public static StationData? GenerateStation(StarSystemData system, double hostPlanetRadius, int hostBodyIndex = 0)
    {
        ArgumentNullException.ThrowIfNull(system);

        if (!system.HasStation || system.PlanetCount == 0)
        {
            return null;
        }

        ulong seed = SeedHelper.Combine(system.Seed, "station");
        var random = new DeterministicRandom(seed);

        StationType type = random.Pick(Types);
        double orbitRadius = hostPlanetRadius + SystemScale.StationOrbitMargin;
        double phase = random.NextDouble(0.0, Math.Tau);
        double period = random.NextDouble(
            SystemScale.StationPeriodMinSeconds,
            SystemScale.StationPeriodMaxSeconds);

        string name = $"{NameGenerator.CityName(random)}-{system.Name}";

        return new StationData(
            name,
            type,
            hostBodyIndex,
            Math.Round(orbitRadius, 4),
            Math.Round(phase, 4),
            Math.Round(period, 3),
            system.ControllingFaction,
            system.Economy,
            seed);
    }
}
