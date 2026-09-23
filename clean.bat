@echo off
setlocal

echo === Fermer Visual Studio avant de continuer (sinon .vs peut rester verrouille) ===
pause

set ROOT=%~dp0

echo.
echo === Suppression du cache .vs (une seule fois, a la racine de la solution) ===
if exist "%ROOT%.vs" rmdir /s /q "%ROOT%.vs" 2>nul

call :clean_restore "%ROOT%"
call :clean_restore "%ROOT%HotsReplayReader.Updater"
call :clean_restore "%ROOT%..\Heroes.Element"
call :clean_restore "%ROOT%..\Heroes.Icons"
call :clean_restore "%ROOT%..\Heroes.StormReplayParser"

echo.
echo === Restore final a la racine ===
cd /d "%ROOT%"
dotnet restore

echo.
echo === Build ===
dotnet build

pause
exit /b

:clean_restore
pushd "%~1" 2>nul
if errorlevel 1 (
    echo   [ATTENTION] Dossier introuvable : %~1
    goto :eof
)
echo.
echo --- %~1 ---
dotnet clean
if exist obj rmdir /s /q obj 2>nul
if exist bin rmdir /s /q bin 2>nul
dotnet restore
popd
goto :eof