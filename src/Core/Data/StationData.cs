namespace DarkSpace.Core.Data;

/// <summary>
/// Описание космической станции в звёздной системе.
/// </summary>
/// <param name="Name">Название станции.</param>
/// <param name="Type">Тип станции.</param>
/// <param name="HostBodyIndex">Индекс планеты, вокруг которой обращается станция.</param>
/// <param name="OrbitRadius">Радиус орбиты станции вокруг планеты (в условных единицах).</param>
/// <param name="OrbitPhase">Начальная фаза орбиты станции, в радианах.</param>
/// <param name="PeriodSeconds">Период обращения станции вокруг планеты, в секундах.</param>
/// <param name="Faction">Контролирующая фракция.</param>
/// <param name="Economy">Тип экономики станции.</param>
/// <param name="Seed">Сид, из которого сгенерирована станция.</param>
public sealed record StationData(
    string Name,
    StationType Type,
    int HostBodyIndex,
    double OrbitRadius,
    double OrbitPhase,
    double PeriodSeconds,
    FactionType Faction,
    EconomyType Economy,
    ulong Seed);
