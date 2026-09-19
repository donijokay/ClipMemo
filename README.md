# ClipMemo

Aplikasi system-tray untuk Windows 11 yang **menyimpan riwayat clipboard multi-item** (salin teks di mana saja), lalu memudahkan **cari, semat, edit, hapus, dan salin ulang**.

System-tray app for Windows 11 that **keeps a multi-item clipboard history** (copy text anywhere), then lets you **search, pin, edit, delete, and copy again**.

> **v2.0.0:** Native C# / WinForms tray app. Framework-dependent win-x64 build (requires .NET 8 Desktop Runtime). Hotkey **Ctrl+Shift+V**, tray icon **CM**, optional start with Windows.

> **v2.0.0:** Aplikasi tray native C# / WinForms. Build win-x64 framework-dependent (perlu .NET 8 Desktop Runtime). Hotkey **Ctrl+Shift+V**, ikon tray **CM**, opsi mulai bersama Windows.

---

## Bahasa Indonesia

### Download (siap pakai)
1. Install **.NET 8 Desktop Runtime (x64)** — wajib:  
   https://dotnet.microsoft.com/download/dotnet/8.0  
   Pilih **Desktop Runtime** → Windows x64.
2. Ambil `ClipMemo.zip` dari [Releases](https://github.com/donijokay/ClipMemo/releases).
3. Extract, jalankan `ClipMemo.exe`.

### Persyaratan
- Windows 11 (disarankan; Windows 10 mungkin berjalan)
- [.NET 8 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/8.0) untuk menjalankan build dari Releases
- .NET 8 SDK hanya jika kamu ingin build dari sumber

### Build
```bat
cd ClipMemo
dotnet restore
dotnet build -c Release
```

### Publish (single-file, win-x64, framework-dependent)
Jalankan `publish.bat`, atau:
```bat
dotnet publish ClipMemo\ClipMemo.csproj -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true -o publish\win-x64
```
Output: `publish\win-x64\ClipMemo.exe`  
(PC target tetap perlu .NET 8 Desktop Runtime.)

### Penggunaan
1. Jalankan `ClipMemo.exe` — ikon **CM** muncul di system tray.
2. Salin teks di mana saja (Ctrl+C) — memo baru muncul di daftar.
3. **Ctrl+Shift+V** atau menu tray **Buka** — tampilkan panel.
4. Menu tray:
   - **Settings…** — ubah shortcut global dan Start with Windows
   - **Buka** — panel utama
   - **Mulai bersama Windows** — autostart (HKCU Run)
   - **Keluar**
5. Di panel: klik baris = salin ulang; Semat / Edit / Hapus; kolom cari; Bersihkan (hapus yang tidak tersemat).
6. Data disimpan di `%AppData%\ClipMemo\`.

### Konfigurasi
Folder: `%AppData%\ClipMemo\`  
(`Environment.SpecialFolder.ApplicationData` → biasanya `C:\Users\<user>\AppData\Roaming\ClipMemo\`)

| File | Isi |
|------|-----|
| `memos.json` | Riwayat memo (teks, pin, timestamp) |
| `settings.json` | Pengaturan (mis. autostart) |

| Batas | Nilai |
|--------|------|
| Memo tidak tersemat | maks. 100 (yang dipin tidak dihapus otomatis) |
| Ukuran teks | lewati salinan &gt; ~100 KB |
| Hotkey default | Ctrl+Shift+V |

### Catatan
- Hanya teks (clipboard format teks); gambar belum didukung di v2.0.0.
- Tutup jendela = sembunyi ke tray (bukan keluar). Keluar lewat menu tray.
- Build Releases **bukan** self-contained agar ukuran kecil.

---

## English

### Download (ready to run)
1. Install **.NET 8 Desktop Runtime (x64)** first:  
   https://dotnet.microsoft.com/download/dotnet/8.0  
   Choose **Desktop Runtime** → Windows x64.
2. Get `ClipMemo.zip` from [Releases](https://github.com/donijokay/ClipMemo/releases).
3. Extract and run `ClipMemo.exe`.

### Requirements
- Windows 11 (recommended; Windows 10 may work)
- [.NET 8 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/8.0) to run the Releases build
- .NET 8 SDK only if you build from source

### Build
```bat
cd ClipMemo
dotnet restore
dotnet build -c Release
```

### Publish (single-file, win-x64, framework-dependent)
Run `publish.bat`, or:
```bat
dotnet publish ClipMemo\ClipMemo.csproj -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true -o publish\win-x64
```
Output: `publish\win-x64\ClipMemo.exe`  
(Target PCs still need the .NET 8 Desktop Runtime.)

### Usage
1. Run `ClipMemo.exe` — a **CM** icon appears in the system tray.
2. Copy text anywhere (Ctrl+C) — new memos appear in the list.
3. **Ctrl+Shift+V** or tray **Open** — show the panel.
4. Tray menu: Open, Settings… (change shortcut), Start with Windows, Exit.
5. In the panel: click a row to copy again; Pin / Edit / Delete; search; Clear unpinned.
6. Data lives under `%AppData%\ClipMemo\`.

### Configuration
Path: `%AppData%\ClipMemo\`  
(Resolved via `Environment.SpecialFolder.ApplicationData`.)

| File | Contents |
|------|----------|
| `memos.json` | Memo history (text, pin, timestamps) |
| `settings.json` | Settings (e.g. autostart) |

| Limit | Value |
|--------|------|
| Unpinned memos | max 100 (pinned are never auto-evicted) |
| Text size | skips pastes larger than ~100 KB |
| Default hotkey | Ctrl+Shift+V |

### Notes
- Text clipboard only in v2.0.0 (images not supported yet).
- Closing the window hides to the tray; use tray Exit to quit.
- Release builds are **not** self-contained so the download stays small.

---

## License

MIT — see [`LICENSE`](LICENSE).
