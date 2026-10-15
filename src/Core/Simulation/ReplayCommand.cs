namespace DarkSpace.Core.Simulation;

/// <summary>Команда replay, применяемая перед шагом с указанным нулевым индексом тика.</summary>
/// <param name="Tick">Индекс тика от начала replay (0 — перед первым Advance).</param>
/// <param name="Command">Игровая команда. Порядок элементов на одном тике сохраняется.</param>
public sealed record ReplayCommand(int Tick, IGameCommand Command);
