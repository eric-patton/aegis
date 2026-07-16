@echo off
rem Builds and runs Aegis. Any arguments pass through to the game,
rem e.g.: run.bat --seed 42 --pilot

dotnet build "%~dp0Aegis.sln" -v q --nologo
if errorlevel 1 exit /b 1

"%~dp0src\Aegis.Cli\bin\Debug\net10.0\aegis.exe" %*
