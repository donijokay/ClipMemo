# ClipMemo

**ClipMemo** — riwayat clipboard multi-item untuk **Windows 11**, gaya Gboard. Salin teks di mana saja; ClipMemo menyimpannya otomatis agar bisa dicari, disemat, diedit, dihapus, dan disalin ulang.

UI dalam **Bahasa Indonesia**. Versi **2.0.0** = aplikasi tray native **C# / WinForms** (mirip [AutoHDR](https://github.com/donijokay/AutoHDR)), bukan Python.

---

## Unduhan Windows (exe)

Rilis GitHub berisi **`ClipMemo.zip`** (di dalamnya `ClipMemo.exe`):

1. Buka [Releases](https://github.com/donijokay/ClipMemo/releases)
2. Unduh `ClipMemo.zip`, ekstrak, jalankan **`ClipMemo.exe`**
3. Ikon **CM** muncul di system tray — klik kanan → **Buka**, atau tekan **Ctrl+Shift+V**

Build otomatis: workflow **Build Windows exe** (`dotnet publish` self-contained win-x64 di `windows-latest`).

Tidak perlu menginstal .NET Runtime (build self-contained).

---

## Fitur

| Fitur | Keterangan |
|-------|------------|
| Auto-simpan | Poll clipboard ~0.5s; lewati kosong, duplikat terakhir, dan &gt;100KB |
| Banyak memo | Edit, pin (tersemat), hapus, cari live, klik baris = salin ulang |
| System tray | Ikon **CM** (kotak biru); menu **Buka** / **Autostart** / **Keluar** |
| Autostart | Tray → Autostart → `HKCU\...\Run` menunjuk ke `ClipMemo.exe` |
| Hotkey | **Ctrl+Shift+V** (`RegisterHotKey`) tampilkan/sembunyikan panel |
| Persistensi | `%APPDATA%\ClipMemo\memos.json` + `settings.json` |
| Cap | Maks **100** memo tidak tersemat; yang dipin tidak pernah di-evict |
| UI | Panel gelap ringkas ~380×500 (WinForms) |

---

## Build dari sumber

### Persyaratan
- Windows + [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

### Debug
```bat
cd ClipMemo
dotnet restore
dotnet build -c Release
dotnet run --project ClipMemo\ClipMemo.csproj
```

### Publish (single-file, win-x64)
Jalankan `publish.bat`, atau:

```bat
dotnet publish ClipMemo\ClipMemo.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o publish\win-x64
```

Output: `publish\win-x64\ClipMemo.exe`

---

## Penggunaan

1. Salin teks (Ctrl+C) — memo baru muncul di daftar.
2. Buka panel: **Ctrl+Shift+V** atau tray **CM** → **Buka**.
3. **Klik** baris → teks disalin kembali (“Disalin.”).
4. Tombol baris: **📌** semat/lepas · **✎** edit · **✕** hapus.
5. Kolom **Cari…** untuk filter.
6. **Bersihkan** — hapus semua memo yang tidak tersemat.
7. **Sembunyikan** / tutup jendela — app tetap di tray. **Keluar** dari tray untuk stop.

---

## Struktur proyek

```
ClipMemo/
├── ClipMemo.sln
├── ClipMemo/
│   ├── ClipMemo.csproj
│   ├── Program.cs
│   ├── AppIcon.cs
│   ├── app.manifest
│   ├── Assets/clipmemo.ico|png
│   ├── Forms/   TrayApplicationContext, MainForm, EditMemoForm
│   ├── Models/  Memo, AppSettings
│   ├── Services/ ClipboardWatcher, MemoStore, StartupService, HotkeyService, SettingsService
│   └── Native/  NativeMethods.cs
├── publish.bat
├── LICENSE
└── README.md
```

Data runtime: `%APPDATA%\ClipMemo\`.

---

## Privasi

Semua memo hanya di komputer Anda. Tidak ada upload atau telemetri.

---

## Lisensi

MIT License — lihat [`LICENSE`](LICENSE). Copyright (c) 2026 donijokay.
