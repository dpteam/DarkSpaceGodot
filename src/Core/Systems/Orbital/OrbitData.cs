namespace DarkSpace.Core.Systems.Orbital;

/// <summary>
/// Кеплеровы элементы орбиты тела вокруг звезды.
/// </summary>
/// <param name="BodyIndex">Индекс планеты в системе (0 — ближайшая к звезде).</param>
/// <param name="SemiMajorAxis">Большая полуось орбиты (в условных единицах системы).</param>
/// <param name="Eccentricity">Эксцентриситет (0 — круговая, &lt;1 — эллиптическая).</param>
/// <param name="Inclination">Наклон орбиты к плоскости эклиптики, в радианах.</param>
/// <param name="LongitudeOfAscendingNode">Долгота восходящего узла Ω, в радианах.</param>
/// <param name="ArgumentOfPeriapsis">Аргумент перицентра ω, в радианах.</param>
/// <param name="MeanAnomalyAtEpoch">Средняя аномалия в начальный момент, в радианах.</param>
/// <param name="PeriodSeconds">Период обращения в секундах.</param>
/// <param name="Seed">Сид, из которого сгенерирована орбита.</param>
public sealed record OrbitData(
    int BodyIndex,
    double SemiMajorAxis,
    double Eccentricity,
    double Inclination,
    double LongitudeOfAscendingNode,
    double ArgumentOfPeriapsis,
    double MeanAnomalyAtEpoch,
    double PeriodSeconds,
    ulong Seed);
