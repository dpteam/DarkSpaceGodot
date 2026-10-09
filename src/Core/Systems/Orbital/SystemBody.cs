namespace DarkSpace.Core.Systems.Orbital;

using DarkSpace.Core.Data;

/// <summary>
/// Небесное тело звёздной системы: планета вместе со своей орбитой и радиусом.
/// </summary>
/// <param name="Index">Индекс планеты в системе.</param>
/// <param name="Orbit">Кеплеровы элементы орбиты.</param>
/// <param name="Planet">Данные планеты.</param>
/// <param name="Radius">Радиус планеты в условных единицах системы.</param>
public sealed record SystemBody(int Index, OrbitData Orbit, PlanetData Planet, double Radius);
