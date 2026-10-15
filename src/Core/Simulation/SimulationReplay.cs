using System;
using System.Collections.Generic;
using System.Linq;
using DarkSpace.Core.Systems.Physics;

namespace DarkSpace.Core.Simulation;

/// <summary>Воспроизводит упорядоченные команды в новой симуляции без Godot, wall-clock и render FPS.</summary>
/// <remarks>
/// Начальные условия — стандартный spawn в системе (0, 0), заданные сид, профиль и режим полёта.
/// Это runner для регрессий, а не восстановление произвольного сохранения или runtime-рекордер.
/// </remarks>
public static class SimulationReplay
{
    /// <summary>Выполняет фиксированное число тиков и возвращает снимок с его каноническим hash.</summary>
    /// <param name="worldSeed">Мировой сид; одинаковые входные данные воспроизводят тот же сценарий.</param>
    /// <param name="tickSeconds">Конечный положительный шаг времени в секундах.</param>
    /// <param name="tickCount">Число шагов; 0 возвращает начальное состояние.</param>
    /// <param name="commands">Команды по неубыванию Tick; внутри тика порядок значим, Sequence не сортируется.</param>
    /// <param name="flightMode">Режим поступательной динамики.</param>
    /// <param name="profile">Физический профиль; null — стандартный профиль корабля.</param>
    /// <exception cref="ArgumentNullException">Список, элемент или команда равны null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Шаг, число тиков, режим или индекс тика недопустимы.</exception>
    /// <exception cref="ArgumentException">Команды не упорядочены по тикам.</exception>
    public static ReplayResult Run(
        ulong worldSeed,
        double tickSeconds,
        int tickCount,
        IReadOnlyList<ReplayCommand> commands,
        FlightMode flightMode = FlightMode.EliteAligned,
        ShipPhysicsProfile? profile = null)
    {
        ArgumentNullException.ThrowIfNull(commands);
        if (!double.IsFinite(tickSeconds) || tickSeconds <= 0.0)
        {
            throw new ArgumentOutOfRangeException(nameof(tickSeconds));
        }

        ArgumentOutOfRangeException.ThrowIfNegative(tickCount);
        if (!Enum.IsDefined(flightMode))
        {
            throw new ArgumentOutOfRangeException(nameof(flightMode));
        }

        // Validate the entire schedule before generating a world or executing any command.
        ReplayCommand[] schedule = commands.ToArray();
        int previousTick = -1;
        foreach (ReplayCommand entry in schedule)
        {
            ArgumentNullException.ThrowIfNull(entry);
            ArgumentNullException.ThrowIfNull(entry.Command);
            if (entry.Tick < 0 || entry.Tick >= tickCount)
            {
                throw new ArgumentOutOfRangeException(nameof(commands), "Replay tick is outside its duration.");
            }

            if (entry.Tick < previousTick)
            {
                throw new ArgumentException("Replay commands must be ordered by tick.", nameof(commands));
            }

            previousTick = entry.Tick;
        }

        var simulation = new WorldSimulation(worldSeed, profile) { FlightMode = flightMode };
        int nextCommand = 0;
        for (int tick = 0; tick < tickCount; tick++)
        {
            while (nextCommand < schedule.Length && schedule[nextCommand].Tick == tick)
            {
                simulation.Enqueue(schedule[nextCommand].Command);
                nextCommand++;
            }

            simulation.Advance(tickSeconds);
        }

        SimulationSnapshot snapshot = simulation.CaptureSnapshot();
        return new ReplayResult(snapshot, SnapshotDigest.Compute(snapshot));
    }
}
