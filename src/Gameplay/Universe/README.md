# src/Gameplay/Universe

Представление вселенной: галактики, звёздные системы, планеты в сцене.

**Реализовано:** `StarSystemView` — 3D-карта звёздной системы с реальными кеплеровыми орбитами,
геометрическим масштабом (`SystemScale`), floating origin, zoom и устойчивым выбором объектов
экранным радиусом с hover-подсветкой.

План улучшений карты и планет — в `ROADMAP.md`.

Данные и орбитальная механика — в `DarkSpace.Core.Generation` и `DarkSpace.Core.Systems.Orbital`
(`StarSystemModel`, `OrbitalMechanics`).
