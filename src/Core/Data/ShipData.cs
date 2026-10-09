using System.Collections.Generic;

namespace DarkSpace.Core.Data;

/// <summary>
/// Описание корабля, полученное процедурной генерацией.
/// </summary>
/// <param name="Name">Процедурное имя корабля.</param>
/// <param name="Type">Класс корабля.</param>
/// <param name="CargoCapacity">Грузоподъёмность в тоннах.</param>
/// <param name="MaxSpeed">Максимальная скорость в условных единицах.</param>
/// <param name="Maneuverability">Манёвренность (0..1).</param>
/// <param name="Armor">Прочность брони.</param>
/// <param name="Shields">Мощность щитов.</param>
/// <param name="PowerDraw">Энергопотребление модулей.</param>
/// <param name="JumpRange">Дальность прыжка в световых годах.</param>
/// <param name="Modules">Список установленных модулей.</param>
/// <param name="Description">Сгенерированное описание с элементами чёрного юмора.</param>
/// <param name="Seed">Сид, из которого сгенерирован корабль.</param>
public sealed record ShipData(
    string Name,
    ShipType Type,
    double CargoCapacity,
    double MaxSpeed,
    double Maneuverability,
    double Armor,
    double Shields,
    double PowerDraw,
    double JumpRange,
    IReadOnlyList<string> Modules,
    string Description,
    ulong Seed);
