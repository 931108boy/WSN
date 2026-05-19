@echo off
setlocal

cd /d "%~dp0"

set "EXE=%~dp0powercontrol\bin\HighCpu64\powercontrol.exe"
set "PROJECT=%~dp0powercontrol\powercontrol.csproj"

echo Building latest UI...
dotnet build "%PROJECT%" -c Release -p:Platform=x64 -p:OutputPath=bin\HighCpu64\
if errorlevel 1 (
    echo.
    echo Build failed. Please check the messages above.
    pause
    exit /b 1
)

start "" "%EXE%"
exit /b 0
