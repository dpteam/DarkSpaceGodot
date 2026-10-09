# src/Gameplay/Ship

Представление корабля: полёт, модули, стыковка.

**Реализовано:**
- `FlightSandbox` — 2D-тренажёр команд/тик-модели (локальный хост).
- `SystemFlightView` — **3D-полёт по звёздной системе** (гравитация звезды, наведение `T`,
  стыковка `F`) через тот же локальный хост (server+client/loopback).

Данные корабля генерируются в `DarkSpace.Core.Generation.ShipGenerator`. Физика и навигация —
в `DarkSpace.Core.Simulation.WorldSimulation`.
