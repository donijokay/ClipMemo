@echo off
setlocal
cd /d "%~dp0"

echo Publishing ClipMemo (win-x64, framework-dependent — requires .NET 8 Desktop Runtime)...
dotnet publish ClipMemo\ClipMemo.csproj -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true -o publish\win-x64

if errorlevel 1 (
  echo Publish failed.
  exit /b 1
)

echo.
echo Done. Output: %~dp0publish\win-x64\ClipMemo.exe
echo Requires: .NET 8 Desktop Runtime https://dotnet.microsoft.com/download/dotnet/8.0
echo Data: %%AppData%%\ClipMemo
endlocal
