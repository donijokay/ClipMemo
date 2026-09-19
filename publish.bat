@echo off
setlocal
cd /d "%~dp0"

echo Publishing ClipMemo (win-x64, self-contained, single-file)...
dotnet publish ClipMemo\ClipMemo.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o publish\win-x64

if errorlevel 1 (
  echo Publish failed.
  exit /b 1
)

echo.
echo Done. Output: %~dp0publish\win-x64\ClipMemo.exe
echo Data: %%AppData%%\ClipMemo\memos.json and settings.json
endlocal
