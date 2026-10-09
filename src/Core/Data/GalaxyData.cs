namespace DarkSpace.Core.Data;

/// <summary>
/// Описание галактики, полученное процедурной генерацией.
/// </summary>
/// <param name="Index">8-битный индекс галактики (0–255).</param>
/// <param name="Name">Процедурное имя галактики.</param>
/// <param name="Type">Тип галактики.</param>
/// <param name="StarCount">Количество звёздных систем в галактике.</param>
/// <param name="Density">Относительная плотность звёзд вдоль радиуса (0..1).</param>
/// <param name="HasCentralBlackHole">Есть ли в ядре сверхмассивная чёрная дыра.</param>
/// <param name="Seed">Сид, из которого сгенерирована галактика.</param>
public sealed record GalaxyData(
    int Index,
    string Name,
    GalaxyType Type,
    int StarCount,
    double Density,
    bool HasCentralBlackHole,
    ulong Seed);
