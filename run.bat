@echo off
cd /d "%~dp0"
python -m clipmemo
if errorlevel 1 (
    echo.
    echo Gagal menjalankan ClipMemo. Pastikan Python terpasang dan dependencies sudah diinstal:
    echo   pip install -r requirements.txt
    pause
)
