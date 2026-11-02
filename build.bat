@echo off
setlocal
pushd "%~dp0"

echo Installing Python dependencies...
py -3.14 -m pip install -r requirements.txt
if errorlevel 1 goto failed

set "DOTNET_EXE=%ProgramFiles%\dotnet\dotnet.exe"
if not exist "%DOTNET_EXE%" (
    where dotnet >nul 2>nul
    if errorlevel 1 (
        echo.
        echo Error: .NET SDK not found. Install the .NET 8 SDK or add dotnet to PATH.
        goto failed
    )
    set "DOTNET_EXE=dotnet"
)

echo.
echo Publishing TyrannoTranslate...
"%DOTNET_EXE%" publish TyrannoTranslate\TyrannoTranslate.csproj -c Release -r win-x64
if errorlevel 1 goto failed

echo.
echo Starting TyrannoTranslate...
"%DOTNET_EXE%" run --project TyrannoTranslate
if errorlevel 1 goto failed

echo.
echo Build and run completed. Press any key to close this window.
pause >nul
popd
exit /b 0

:failed
echo.
echo Build script stopped with an error. Review the message above.
echo Press any key to close this window.
pause >nul
popd
exit /b 1
