@echo off
setlocal enableextensions
rem ============================================================
rem  Dark Space - build-and-test.cmd
rem  Сборка и тесты без GUI (headless). Удобно и надёжно по RDP/CI.
rem ============================================================

pushd "%~dp0.." >nul

echo [Dark Space] dotnet build DarkSpace.sln
dotnet build DarkSpace.sln -v minimal
if errorlevel 1 ( echo [ОШИБКА] Сборка не прошла. & popd >nul & exit /b 1 )

echo.
echo [Dark Space] Unit tests
dotnet test tests/UnitTests/UnitTests.csproj -v minimal
if errorlevel 1 ( echo [ОШИБКА] Unit-тесты упали. & popd >nul & exit /b 1 )

echo.
echo [Dark Space] Integration tests
dotnet test tests/IntegrationTests/IntegrationTests.csproj -v minimal
if errorlevel 1 ( echo [ОШИБКА] Интеграционные тесты упали. & popd >nul & exit /b 1 )

echo.
echo [Dark Space] Готово: сборка и тесты прошли.
popd >nul
endlocal & exit /b 0
