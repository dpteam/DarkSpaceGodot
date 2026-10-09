using System;
using System.Collections.Generic;
using DarkSpace.Core.Data;
using DarkSpace.Core.Generation;
using DarkSpace.Core.Systems.Physics;

namespace DarkSpace.Core.Systems.Orbital;

/// <summary>
/// Полная модель звёздной системы: звезда и планеты с орбитами, а также быстрые запросы
/// положения и ближайших тел по времени.
/// </summary>
/// <remarks>
/// Модель детерминирована (строится из сида системы) и не зависит от Godot, поэтому
/// используется и симуляцией (ядро), и слоем представления (3D-карта и полёт), без дублирования
/// логики генерации.
/// </remarks>
public sealed class StarSystemModel
{
    private readonly List<SystemBody> _bodies;

    /// <summary>Модель создаётся из готовых данных системы.</summary>
    /// <param name="system">Данные звёздной системы.</param>
    /// <exception cref="ArgumentNullException">Если <paramref name="system"/> равен <see langword="null"/>.</exception>
    public StarSystemModel(StarSystemData system)
    {
        ArgumentNullException.ThrowIfNull(system);
        System = system;
        Mu = SystemScale.GravitationalParameter(system.Star);
        StarRadius = SystemScale.StarRadius(system.Star);

        _bodies = new List<SystemBody>(system.PlanetCount);
        for (int i = 0; i < system.PlanetCount; i++)
        {
            OrbitData orbit = OrbitGenerator.GenerateOrbit(system, i, Mu);
            PlanetData planet = PlanetGenerator.GeneratePlanet(system, i);
            double radius = SystemScale.PlanetRadius(planet.Type);
            _bodies.Add(new SystemBody(i, orbit, planet, radius));
        }
    }

    /// <summary>Данные системы.</summary>
    public StarSystemData System { get; }

    /// <summary>Гравитационный параметр звезды μ (SU³/с²). Единый для орбит планет и гравитации корабля.</summary>
    public double Mu { get; }

    /// <summary>Радиус звезды в условных единицах системы.</summary>
    public double StarRadius { get; }

    /// <summary>Планеты системы вместе с орбитами.</summary>
    public IReadOnlyList<SystemBody> Bodies => _bodies;

    /// <summary>Количество планет в системе.</summary>
    public int BodyCount => _bodies.Count;

    /// <summary>Создаёт модель системы по галактике и индексу системы.</summary>
    /// <param name="galaxy">Галактика.</param>
    /// <param name="systemIndex">Индекс системы.</param>
    public static StarSystemModel Create(GalaxyData galaxy, int systemIndex)
    {
        ArgumentNullException.ThrowIfNull(galaxy);
        return new StarSystemModel(StarSystemGenerator.GenerateSystem(galaxy, systemIndex));
    }

    /// <summary>Возвращает тело по индексу.</summary>
    /// <exception cref="ArgumentOutOfRangeException">Если индекс вне диапазона.</exception>
    public SystemBody Body(int index)
    {
        if (index < 0 || index >= _bodies.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(index), index, $"Нет тела с индексом {index}.");
        }

        return _bodies[index];
    }

    /// <summary>Положение планеты в заданный момент времени.</summary>
    /// <param name="bodyIndex">Индекс планеты.</param>
    /// <param name="timeSeconds">Время, сек.</param>
    public Vector3D BodyPosition(int bodyIndex, double timeSeconds)
        => OrbitalMechanics.PositionAt(Body(bodyIndex).Orbit, timeSeconds);

    /// <summary>Расстояние от точки до планеты в заданный момент времени.</summary>
    public double DistanceToBody(int bodyIndex, Vector3D position, double timeSeconds)
        => (BodyPosition(bodyIndex, timeSeconds) - position).Length;

    /// <summary>Расстояние от точки до звезды (в начале координат).</summary>
    public static double StarDistance(Vector3D position) => position.Length;

    /// <summary>Круговая орбитальная скорость на радиусе r вокруг этой звезды: v = √(μ/r).</summary>
    public double CircularSpeed(double radius) => Astro.CircularSpeed(Mu, radius);

    /// <summary>Скорость убегания на радиусе r: v_esc = √(2μ/r).</summary>
    public double EscapeSpeed(double radius) => Astro.EscapeSpeed(Mu, radius);

    /// <summary>Радиус планеты по индексу, в условных единицах системы.</summary>
    public double BodyRadius(int bodyIndex) => Body(bodyIndex).Radius;

    /// <summary>Радиус стыковки с планетой: поверхность планеты плюс зазор.</summary>
    public double DockRadius(int bodyIndex) => Body(bodyIndex).Radius + SystemScale.DockingMargin;

    /// <summary>
    /// Возвращает индекс ближайшей планеты к точке, либо -1, если планет нет.
    /// </summary>
    public int NearestBody(Vector3D position, double timeSeconds)
    {
        int best = -1;
        double bestDistance = double.MaxValue;

        for (int i = 0; i < _bodies.Count; i++)
        {
            double d = DistanceToBody(i, position, timeSeconds);
            if (d < bestDistance)
            {
                bestDistance = d;
                best = i;
            }
        }

        return best;
    }
}
