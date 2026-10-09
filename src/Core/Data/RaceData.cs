using System.Collections.Generic;

namespace DarkSpace.Core.Data;

/// <summary>
/// Описание разумной расы, полученное процедурной генерацией.
/// </summary>
/// <param name="Archetype">Базовый архетип расы.</param>
/// <param name="Name">Процедурное название расы.</param>
/// <param name="Culture">Краткое описание культуры.</param>
/// <param name="Traits">Ключевые особенности (черты).</param>
/// <param name="HomeworldPreference">Предпочитаемый тип родного мира.</param>
/// <param name="Description">Сгенерированное описание с элементами чёрного юмора.</param>
/// <param name="Seed">Сид, из которого сгенерирована раса.</param>
public sealed record RaceData(
    RaceArchetype Archetype,
    string Name,
    string Culture,
    IReadOnlyList<string> Traits,
    string HomeworldPreference,
    string Description,
    ulong Seed);
