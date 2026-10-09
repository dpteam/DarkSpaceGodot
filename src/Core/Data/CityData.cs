using System.Collections.Generic;

namespace DarkSpace.Core.Data;

/// <summary>
/// Описание поселения (города) на планете.
/// </summary>
/// <param name="Name">Процедурное название поселения.</param>
/// <param name="Type">Тип поселения.</param>
/// <param name="Population">Численность населения.</param>
/// <param name="Faction">Контролирующая фракция.</param>
/// <param name="Economy">Тип экономики поселения.</param>
/// <param name="Infrastructure">Список доступных инфраструктурных объектов.</param>
/// <param name="Description">Сгенерированное описание с элементами чёрного юмора.</param>
/// <param name="Seed">Сид, из которого сгенерировано поселение.</param>
public sealed record CityData(
    string Name,
    CityType Type,
    long Population,
    FactionType Faction,
    EconomyType Economy,
    IReadOnlyList<InfrastructureType> Infrastructure,
    string Description,
    ulong Seed);
