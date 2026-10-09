# src/Network/Client

Клиентская часть мультиплеера.

**Реализовано (ядро):** транспорт-абстракция и `GameClient` в `src/Core/Network`
(отправка команд, приём снимков состояния). В тестах и в режиме «одиночная игра = локальный хост»
используется `LoopbackTransport`.

**Здесь (слой движка, Этап 4):** реализация `IMessageTransport` поверх `ENetMultiplayerPeer`
/`WebSocketMultiplayerPeer`, клиентское предсказание и интерполяция.

См. ADR-006/ADR-007 в `MEMORY.md`.
