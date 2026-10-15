# src/Gameplay/Ship

Представление корабля: полёт, модули, стыковка.

**Реализовано:**
- `SystemFlightView` — **first-person полёт по звёздной системе** в духе Elite: скорость вдоль носа,
  тангаж/крен/рыскание, star dust, crosshair, scanner HUD, станции и рынок.
- Локальный host (`GameServer` + `GameClient` через loopback) использует тот же командный путь,
  что и будущий multiplayer.
- `MarketPanel` — таблица 17 товаров станции, цены покупки/продажи, запас и cargo.

Данные корабля генерируются в `DarkSpace.Core.Generation.ShipGenerator`. Физика и навигация —
в `DarkSpace.Core.Simulation.WorldSimulation`.
