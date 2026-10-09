@echo off
setlocal enableextensions
rem ============================================================
rem  Dark Space - run-game.cmd
rem  Запускает игру с compatibility-рендером (OpenGL/ANGLE).
rem
rem  ЗАЧЕМ: в RDP-сессии рендер Forward+ (Vulkan) падает в драйвере NVIDIA
rem  (nvcuda64.dll). Подробности - MEMORY.md, раздел "Известные проблемы".
rem
rem  Путь к Godot берётся по порядку:
rem    1) tools\godot-path.txt  (локальный файл, не коммитится)
rem    2) переменная окружения DARKSPACE_GODOT
rem    3) godot.exe в PATH
rem ============================================================

set "SCRIPT_DIR=%~dp0"
set "GODOT="

if exist "%SCRIPT_DIR%godot-path.txt" (
  for /f "usebackq delims=" %%p in ("%SCRIPT_DIR%godot-path.txt") do set "GODOT=%%p"
)
if not defined GODOT set "GODOT=%DARKSPACE_GODOT%"
if not defined GODOT for %%i in (godot.exe) do set "GODOT=%%~$PATH:i"

if not defined GODOT (
  echo [Dark Space] Не найден исполняемый файл Godot .NET.
  echo   1^) Скопируйте tools\godot-path.txt.example в tools\godot-path.txt
  echo      и укажите полный путь к Godot_..._mono_win64.exe, либо
  echo   2^) задайте переменную окружения DARKSPACE_GODOT, либо
  echo   3^) добавьте godot в PATH.
  exit /b 1
)

pushd "%SCRIPT_DIR%.." >nul
echo [Dark Space] Godot:   %GODOT%
echo [Dark Space] Project: %CD%
echo [Dark Space] Renderer: gl_compatibility ^(обход RDP-краша^)
"%GODOT%" --path "%CD%" --rendering-method gl_compatibility %*
set "RC=%ERRORLEVEL%"
popd >nul
endlocal & exit /b %RC%
