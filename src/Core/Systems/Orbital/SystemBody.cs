namespace DarkSpace.Core.Systems.Orbital;

using DarkSpace.Core.Data;

/// <summary>
/// Небесное тело звёздной системы: планета вместе со своей орбитой.
/// </summary>
/// <param name="Index">Индекс планеты в системе.</param>
/// <param name="Orbit">Кеплеровы элементы орбиты.</param>
/// <param name="Planet">Данные планеты.</param>
public sealed record SystemBody(int Index, OrbitData Orbit, PlanetData Planet);
