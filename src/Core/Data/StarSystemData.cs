namespace DarkSpace.Core.Data;

/// <summary>
/// Описание звёздной системы.
/// </summary>
/// <param name="GalaxyIndex">Индекс галактики-родителя.</param>
/// <param name="Index">Индекс системы внутри галактики.</param>
/// <param name="Name">Процедурное имя системы.</param>
/// <param name="Star">Спектральный класс/тип основной звезды.</param>
/// <param name="PlanetCount">Количество планет (1–12).</param>
/// <param name="HasAsteroidBelt">Есть ли пояс астероидов.</param>
/// <param name="HasStation">Есть ли космическая станция.</param>
/// <param name="Economy">Тип экономики системы.</param>
/// <param name="ControllingFaction">Контролирующая фракция.</param>
/// <param name="X">Координата X центра системы в галактическом пространстве.</param>
/// <param name="Y">Координата Y центра системы в галактическом пространстве.</param>
/// <param name="Z">Координата Z центра системы в галактическом пространстве.</param>
/// <param name="Seed">Сид, из которого сгенерирована система.</param>
public sealed record StarSystemData(
    int GalaxyIndex,
    int Index,
    string Name,
    SpectralClass Star,
    int PlanetCount,
    bool HasAsteroidBelt,
    bool HasStation,
    EconomyType Economy,
    FactionType ControllingFaction,
    double X,
    double Y,
    double Z,
    ulong Seed);
