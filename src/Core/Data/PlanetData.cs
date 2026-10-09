using System.Collections.Generic;

namespace DarkSpace.Core.Data;

/// <summary>
/// Описание планеты.
/// </summary>
/// <param name="Name">Процедурное имя планеты.</param>
/// <param name="Type">Тип планеты.</param>
/// <param name="Gravity">Гравитация в единицах g (1.0 = земная).</param>
/// <param name="Atmosphere">Краткое описание химического состава атмосферы.</param>
/// <param name="TemperatureCelsius">Средняя температура поверхности в градусах Цельсия.</param>
/// <param name="HasLife">Есть ли на планете жизнь.</param>
/// <param name="HasAggressiveFauna">Опасна ли местная фауна.</param>
/// <param name="HasFlora">Есть ли растительность.</param>
/// <param name="Resources">Список доступных для добычи ресурсов.</param>
/// <param name="Description">Сгенерированное описание с элементами чёрного юмора.</param>
/// <param name="Seed">Сид, из которого сгенерирована планета.</param>
public sealed record PlanetData(
    string Name,
    PlanetType Type,
    double Gravity,
    string Atmosphere,
    double TemperatureCelsius,
    bool HasLife,
    bool HasAggressiveFauna,
    bool HasFlora,
    IReadOnlyList<string> Resources,
    string Description,
    ulong Seed);
