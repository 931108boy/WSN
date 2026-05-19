@echo off
setlocal

cd /d "%~dp0"

set "EXE=%~dp0powercontrol\bin\HighCpu\powercontrol.exe"
set "PROJECT=%~dp0powercontrol\powercontrol.csproj"

if not exist "%EXE%" (
    echo HighCpu executable not found. Building it first...
    dotnet build "%PROJECT%" -c Release -p:OutputPath=bin\HighCpu\
    if errorlevel 1 (
        echo.
        echo Build failed. Please check the messages above.
        pause
        exit /b 1
    )
)

start "" "%EXE%"
exit /b 0
