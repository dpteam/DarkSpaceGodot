namespace DarkSpace.Core.Simulation;

/// <summary>Итог воспроизведения команд из нового мира с заданным сидом.</summary>
/// <param name="Snapshot">Итоговый снимок. Это наблюдаемое состояние, не полный save/checkpoint.</param>
/// <param name="SnapshotHash">SHA-256 канонического снимка (hex, нижний регистр).</param>
public sealed record ReplayResult(SimulationSnapshot Snapshot, string SnapshotHash);
