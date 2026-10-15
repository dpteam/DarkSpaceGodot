# tools — вспомогательные скрипты разработки

Небольшие скрипты для повседневной работы над **Dark Space** (Windows).

Текущее игровое состояние и план переделки — `../README.md` и `../ROADMAP.md`.

## Быстрый старт

| Скрипт | Что делает |
|---|---|
| `open-editor.cmd` | Открывает редактор Godot **с compatibility-рендером** (обход RDP-краша). |
| `run-game.cmd` | Запускает игру с compatibility-рендером. |
| `build-and-test.cmd` | `dotnet build` + unit- и интеграционные тесты (headless). |

## Игровые сцены

Главное меню открывается обычным запуском проекта. Из него доступны:

- **Карта системы** — орбиты, масштаб, выбор планет;
- **Полёт по системе** — first-person cockpit, star dust, scanner, станции и рынок.

2D-сцена старого `FlightSandbox` удалена: она больше не является эталоном полёта.

## Почему compatibility-рендер?

В **RDP-сессии** рендер **Forward+ (Vulkan)** падает внутри драйвера NVIDIA
(`nvcuda64.dll`, `0xc0000005`). Это проблема окружения, а не кода игры.
Обход — запускать редактор/игру с `--rendering-method gl_compatibility`
(OpenGL → ANGLE → Direct3D 11). Подробности и полный разбор — в `../MEMORY.md`,
раздел «Известные проблемы» (ENV-001).

> Целевой рендер игры — **Forward+**. Compatibility используется только как обход для RDP.

## Путь к движку

Скрипты ищут исполняемый файл Godot по порядку:

1. `tools/godot-path.txt` — локальный файл (в git не попадает).
2. Переменная окружения `DARKSPACE_GODOT`.
3. `godot.exe` в `PATH`.

Чтобы задать путь первым способом:

```bat
copy tools\godot-path.txt.example tools\godot-path.txt
rem откройте godot-path.txt и впишите полный путь к Godot_..._mono_win64.exe
```

Нужна именно **.NET/Mono-сборка** Godot (в имени файла есть `mono`).

Папка `refs/` содержит локальные материалы для исследования Elite/NES Elite и добавлена в `.gitignore`.
Эти исходники не входят в поставку Dark Space.

## Передача аргументов

Любые дополнительные аргументы пробрасываются в Godot. Примеры:

```bat
run-game.cmd --headless --quit-after 120
open-editor.cmd --verbose
```
