using System.Collections.Generic;
using DarkSpace.Core.Data;

namespace DarkSpace.Core.Simulation;

/// <summary>
/// Сериализуемый снимок состояния мира на некоторый тик.
/// </summary>
/// <remarks>
/// Снапшоты передаются от сервера клиентам (см. <c>DarkSpace.Core.Network</c>) и используются
/// для синхронизации. Снимок неизменяем и не содержит ссылок на изменяемое состояние мира.
/// </remarks>
/// <param name="WorldSeed">Мировой сид вселенной.</param>
/// <param name="Tick">Номер тика.</param>
/// <param name="Position">Позиция корабля игрока в пространстве системы.</param>
/// <param name="Velocity">Скорость корабля игрока.</param>
/// <param name="ThrustX">Текущая компонента X вектора тяги.</param>
/// <param name="ThrustY">Текущая компонента Y вектора тяги.</param>
/// <param name="Credits">Баланс кредитов.</param>
/// <param name="Fuel">Текущее топливо.</param>
/// <param name="GalaxyIndex">Индекс текущей галактики.</param>
/// <param name="SystemIndex">Индекс текущей системы.</param>
/// <param name="SystemName">Название текущей системы.</param>
/// <param name="Economy">Тип экономики текущей системы.</param>
/// <param name="Faction">Контролирующая фракция текущей системы.</param>
/// <param name="Destroyed">Уничтожен ли корабль.</param>
/// <param name="Cargo">Содержимое трюма (id предмета → количество).</param>
/// <param name="ChatLog">Последние сообщения чата.</param>
public sealed record SimulationSnapshot(
    ulong WorldSeed,
    long Tick,
    Vector2D Position,
    Vector2D Velocity,
    double ThrustX,
    double ThrustY,
    double Credits,
    double Fuel,
    int GalaxyIndex,
    int SystemIndex,
    string SystemName,
    EconomyType Economy,
    FactionType Faction,
    bool Destroyed,
    IReadOnlyDictionary<int, int> Cargo,
    IReadOnlyList<string> ChatLog);
