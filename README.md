# ClipMemo

**ClipMemo** is a multi-item clipboard history app for Windows 11 (also runs on Linux), inspired by Gboard’s clipboard sheet. Copy text anywhere — ClipMemo auto-saves it so you can search, pin, edit, delete, and re-copy later.

UI strings are in **Indonesian**. This README is bilingual.

---


## Unduhan Windows (exe)

Rilis GitHub menyertakan **`ClipMemo.zip`** (berisi `ClipMemo.exe`), mirip AutoHDR:

1. Buka [Releases](https://github.com/donijokay/ClipMemo/releases)
2. Unduh `ClipMemo.zip`, ekstrak, jalankan `ClipMemo.exe`

Build otomatis: workflow **Build Windows exe** (PyInstaller di `windows-latest`).

## Fitur / Features

| Fitur | Description |
|-------|-------------|
| Auto-simpan | Polls clipboard ~every 0.5s; skips empty, duplicate-of-last, and >100KB |
| Banyak memo | Edit, pin (tersemat), hapus, cari live, klik untuk salin ulang |
| System tray | Ikon **CM** (badge biru); menu **Buka** / **Autostart** / **Keluar** |
| Autostart | Toggle di UI (“Mulai otomatis saat Windows nyala”) atau tray — daftar di Run registry (Windows) |
| Hotkey | **Ctrl+Shift+V** (keyboard on Windows; pynput fallback; degrades if unavailable) |
| Persistensi | `%APPDATA%\ClipMemo\` (Windows) · `~/.config/ClipMemo` (Linux) |
| Cap | Max **100** unpinned memos; pinned are never dropped |
| UI | Dark compact ~380×500 panel (customtkinter), minimal Indonesia |

---

## Persyaratan / Requirements

- Python **3.9+**
- Windows 11 recommended (Linux OK for development)
- Dependencies in `requirements.txt`:
  - `customtkinter`, `pystray`, `Pillow`, `pyperclip`
  - `keyboard` (Windows) and/or `pynput` for global hotkey

On Windows, tray/hotkey may need the app (or terminal) run **as Administrator** once if the hotkey library requires elevated hooks — usually normal user rights are enough.

---

## Instalasi Windows 11

1. Install [Python 3](https://www.python.org/downloads/) and tick **Add Python to PATH**.
2. Open **Command Prompt** or PowerShell in this folder:

```bat
cd path\to\ClipMemo
python -m venv .venv
.venv\Scripts\activate
pip install -r requirements.txt
```

3. Run:

```bat
python -m clipmemo
```

Or double-click **`run.bat`** (uses system `python`).

4. **Autostart** — di panel, centang **Mulai otomatis saat Windows nyala**, atau klik kanan ikon tray → **Autostart**.  
   ClipMemo menulis `%APPDATA%\ClipMemo\start_clipmemo.cmd` dan mendaftarkannya di  
   `HKCU\Software\Microsoft\Windows\CurrentVersion\Run` (nama nilai `ClipMemo`).  
   Preferensi juga disimpan di `settings.json`.

---

## Penggunaan / Usage

1. Salin teks di mana saja (Ctrl+C) — memo baru muncul di daftar.
2. Buka panel dengan **Ctrl+Shift+V** atau klik kanan ikon tray **CM** → **Buka**.
3. **Klik** baris memo → teks disalin kembali ke clipboard (“Disalin.”).
4. Tombol baris: **📌** semat/lepas · **✎** edit · **✕** hapus (hint di status saat hover).
5. Ketik di kolom **Cari…** untuk filter live.
6. **Bersihkan** — hapus semua memo yang tidak tersemat.
7. **Sembunyikan** / tutup jendela — app tetap di tray. **Keluar** dari tray untuk stop penuh.

---

## Struktur proyek

```
ClipMemo/
├── clipmemo/
│   ├── __init__.py
│   ├── __main__.py
│   ├── main.py              # App wiring, hotkey, shutdown
│   ├── storage.py           # JSON persistence + pin/cap
│   ├── clipboard_watcher.py # 0.5s poll
│   ├── ui.py                # customtkinter panel (ID)
│   ├── tray.py              # pystray CM icon + Buka/Autostart/Keluar
│   └── autostart.py         # Windows Run + settings.json (+ XDG optional)
├── requirements.txt
├── run.bat
├── README.md
└── .gitignore
```

Data: `memos.json` dan `settings.json` di direktori config di atas.

---

## Linux (dev)

```bash
cd ClipMemo
python3 -m venv .venv
source .venv/bin/activate
pip install -r requirements.txt
# On Linux, install a tkinter package if needed, e.g.:
#   sudo apt install python3-tk
python -m clipmemo
```

Autostart di Linux memakai file XDG `~/.config/autostart/clipmemo.desktop` jika diaktifkan. Hotkey/tray support varies by desktop environment; UI and clipboard history still work.

---

## Privasi / Privacy

Semua memo disimpan **hanya di komputer Anda** (`%APPDATA%\\ClipMemo`). Tidak ada upload atau telemetri.

---

## Lisensi / License

MIT License — lihat [`LICENSE`](LICENSE).

