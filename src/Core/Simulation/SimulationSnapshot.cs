using System.Collections.Generic;
using DarkSpace.Core.Data;
using DarkSpace.Core.Systems.Physics;

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
/// <param name="SimTime">Модельное время симуляции в секундах.</param>
/// <param name="Position">Позиция корабля игрока в системе (3D).</param>
/// <param name="Velocity">Скорость корабля игрока (3D).</param>
/// <param name="Orientation">Ориентация корабля (кватернион).</param>
/// <param name="AngularVelocity">Угловая скорость корабля (3D).</param>
/// <param name="Thrust">Текущая команда тяги в локальной системе (вперёд/вбок/вверх).</param>
/// <param name="Rotation">Текущая команда вращения (тангаж/рыскание/крен).</param>
/// <param name="TargetBodyIndex">Индекс выбранной планеты-цели, либо -1.</param>
/// <param name="DockedBodyIndex">Индекс планеты, с которой корабль состыкован, либо -1.</param>
/// <param name="Credits">Баланс кредитов.</param>
/// <param name="Fuel">Остаток топлива (по массе).</param>
/// <param name="ShipMass">Текущая полная масса корабля.</param>
/// <param name="RemainingDeltaV">Остаток характеристической скорости (Δv).</param>
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
    double SimTime,
    Vector3D Position,
    Vector3D Velocity,
    Quaternion Orientation,
    Vector3D AngularVelocity,
    Vector3D Thrust,
    Vector3D Rotation,
    int TargetBodyIndex,
    int DockedBodyIndex,
    double Credits,
    double Fuel,
    double ShipMass,
    double RemainingDeltaV,
    int GalaxyIndex,
    int SystemIndex,
    string SystemName,
    EconomyType Economy,
    FactionType Faction,
    bool Destroyed,
    IReadOnlyDictionary<int, int> Cargo,
    IReadOnlyList<string> ChatLog);
