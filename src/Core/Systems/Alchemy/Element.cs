namespace DarkSpace.Core.Systems.Alchemy;

/// <summary>
/// Базовый алхимический элемент.
/// </summary>
/// <param name="Id">Уникальный числовой идентификатор (используется в сидах и комбинациях).</param>
/// <param name="Name">Полное название элемента.</param>
/// <param name="Stem">Корень для составных имён (например, «Гидро» для водорода).</param>
/// <param name="Symbol">Условный символ.</param>
/// <param name="Rarity">Редкость от 0 (обычный) до 1 (легендарный).</param>
/// <param name="Description">Краткое описание с оттенком чёрного юмора.</param>
public sealed record Element(
    int Id,
    string Name,
    string Stem,
    string Symbol,
    double Rarity,
    string Description);
