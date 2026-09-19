"""ClipMemo main UI — dark compact panel, Indonesian strings."""

from __future__ import annotations

from typing import Callable, List, Optional

import customtkinter as ctk
import pyperclip

from clipmemo.storage import Memo, Storage

# Visual constants
PANEL_WIDTH = 380
PANEL_HEIGHT = 500
PREVIEW_LEN = 100
ACCENT = "#409CFF"

ctk.set_appearance_mode("dark")
ctk.set_default_color_theme("blue")


class EditDialog(ctk.CTkToplevel):
    """Modal dialog to edit memo text."""

    def __init__(
        self,
        master: ctk.CTk,
        initial: str,
        on_save: Callable[[str], None],
    ) -> None:
        super().__init__(master)
        self.title("Edit Memo")
        self.geometry("400x260")
        self.resizable(True, True)
        self.transient(master)
        self.grab_set()
        self._on_save = on_save

        ctk.CTkLabel(self, text="Edit teks:", font=ctk.CTkFont(size=13)).pack(
            anchor="w", padx=12, pady=(12, 4)
        )
        self._box = ctk.CTkTextbox(self, wrap="word", font=ctk.CTkFont(size=12))
        self._box.pack(fill="both", expand=True, padx=12, pady=4)
        self._box.insert("1.0", initial)

        btn_row = ctk.CTkFrame(self, fg_color="transparent")
        btn_row.pack(fill="x", padx=12, pady=12)
        ctk.CTkButton(btn_row, text="Batal", width=80, command=self.destroy).pack(
            side="right", padx=(8, 0)
        )
        ctk.CTkButton(btn_row, text="Simpan", width=80, command=self._save).pack(
            side="right"
        )
        self.after(50, self._box.focus_set)

    def _save(self) -> None:
        text = self._box.get("1.0", "end-1c")
        self._on_save(text)
        self.destroy()


class MemoRow(ctk.CTkFrame):
    """One memo row: click text to copy; 📌 / Edit / Hapus."""

    def __init__(
        self,
        master,
        memo: Memo,
        on_copy: Callable[[Memo], None],
        on_edit: Callable[[Memo], None],
        on_pin: Callable[[Memo], None],
        on_delete: Callable[[Memo], None],
        on_hint: Optional[Callable[[str], None]] = None,
        **kwargs,
    ) -> None:
        super().__init__(master, corner_radius=8, **kwargs)
        self.memo = memo
        self._on_hint = on_hint or (lambda _m: None)

        preview = memo.text.replace("\n", " ").strip()
        if len(preview) > PREVIEW_LEN:
            preview = preview[: PREVIEW_LEN - 1] + "…"

        left = ctk.CTkFrame(self, fg_color="transparent")
        left.pack(side="left", fill="both", expand=True, padx=(8, 4), pady=6)

        self._label = ctk.CTkLabel(
            left,
            text=preview,
            anchor="w",
            justify="left",
            font=ctk.CTkFont(size=12),
            cursor="hand2",
        )
        self._label.pack(fill="x")
        self._label.bind("<Button-1>", lambda e: on_copy(memo))
        self._label.bind(
            "<Enter>", lambda e: self._on_hint("Klik untuk salin")
        )
        self.bind("<Button-1>", lambda e: on_copy(memo))

        btns = ctk.CTkFrame(self, fg_color="transparent")
        btns.pack(side="right", padx=2, pady=4)

        pin_sym = "📌" if memo.pinned else "○"
        pin_hint = "Lepas sematan" if memo.pinned else "Semat"
        b_pin = ctk.CTkButton(
            btns,
            text=pin_sym,
            width=28,
            height=26,
            font=ctk.CTkFont(size=12),
            fg_color="transparent",
            hover_color=("gray75", "gray25"),
            command=lambda: on_pin(memo),
        )
        b_pin.pack(side="left", padx=1)
        b_pin.bind("<Enter>", lambda e, h=pin_hint: self._on_hint(h))

        b_edit = ctk.CTkButton(
            btns,
            text="✎",
            width=28,
            height=26,
            font=ctk.CTkFont(size=13),
            fg_color="transparent",
            hover_color=("gray75", "gray25"),
            command=lambda: on_edit(memo),
        )
        b_edit.pack(side="left", padx=1)
        b_edit.bind("<Enter>", lambda e: self._on_hint("Edit"))

        b_del = ctk.CTkButton(
            btns,
            text="✕",
            width=28,
            height=26,
            font=ctk.CTkFont(size=12),
            fg_color="transparent",
            text_color="#E07070",
            hover_color="#5A3030",
            command=lambda: on_delete(memo),
        )
        b_del.pack(side="left", padx=1)
        b_del.bind("<Enter>", lambda e: self._on_hint("Hapus"))


class ClipMemoApp(ctk.CTk):
    """Main ClipMemo window."""

    def __init__(
        self,
        storage: Storage,
        on_copy_notify: Optional[Callable[[str], None]] = None,
        on_quit_request: Optional[Callable[[], None]] = None,
        get_autostart: Optional[Callable[[], bool]] = None,
        set_autostart: Optional[Callable[[bool], None]] = None,
    ) -> None:
        super().__init__()
        self.storage = storage
        self.on_copy_notify = on_copy_notify
        self.on_quit_request = on_quit_request
        self.get_autostart = get_autostart
        self.set_autostart = set_autostart
        self._status_timer: Optional[str] = None

        self.title("ClipMemo")
        self.geometry(f"{PANEL_WIDTH}x{PANEL_HEIGHT}")
        self.minsize(PANEL_WIDTH, 360)
        self.protocol("WM_DELETE_WINDOW", self.hide_to_tray)

        # Header: CM badge + title + hotkey hint
        header = ctk.CTkFrame(self, fg_color="transparent")
        header.pack(fill="x", padx=12, pady=(10, 2))

        badge = ctk.CTkLabel(
            header,
            text="CM",
            width=28,
            height=28,
            corner_radius=6,
            fg_color=ACCENT,
            text_color="white",
            font=ctk.CTkFont(size=11, weight="bold"),
        )
        badge.pack(side="left", padx=(0, 8))

        ctk.CTkLabel(
            header,
            text="ClipMemo",
            font=ctk.CTkFont(size=16, weight="bold"),
        ).pack(side="left")

        ctk.CTkLabel(
            header,
            text="Ctrl+Shift+V",
            font=ctk.CTkFont(size=11),
            text_color="gray60",
        ).pack(side="right")

        # Search
        self._search = ctk.CTkEntry(
            self,
            placeholder_text="Cari…",
            font=ctk.CTkFont(size=12),
            height=32,
        )
        self._search.pack(fill="x", padx=12, pady=(6, 2))
        self._search.bind("<KeyRelease>", lambda e: self.refresh_list())

        # Status under search
        self._status = ctk.CTkLabel(
            self,
            text="",
            font=ctk.CTkFont(size=11),
            text_color="gray55",
            anchor="w",
        )
        self._status.pack(fill="x", padx=14, pady=(0, 2))

        # Scrollable list
        self._list = ctk.CTkScrollableFrame(self, fg_color=("gray90", "gray17"))
        self._list.pack(fill="both", expand=True, padx=10, pady=(4, 6))

        # Footer strip
        footer = ctk.CTkFrame(self, fg_color="transparent")
        footer.pack(fill="x", padx=12, pady=(0, 4))

        ctk.CTkButton(
            footer,
            text="Bersihkan",
            width=88,
            height=28,
            font=ctk.CTkFont(size=12),
            fg_color=("gray70", "gray30"),
            hover_color=("gray60", "gray35"),
            command=self._clear_unpinned,
        ).pack(side="left")

        ctk.CTkButton(
            footer,
            text="Sembunyikan",
            width=100,
            height=28,
            font=ctk.CTkFont(size=12),
            command=self.hide_to_tray,
        ).pack(side="right")

        # Autostart checkbox (settings strip)
        if self.get_autostart is not None and self.set_autostart is not None:
            auto_row = ctk.CTkFrame(self, fg_color="transparent")
            auto_row.pack(fill="x", padx=12, pady=(0, 10))
            initial = False
            try:
                initial = bool(self.get_autostart())
            except Exception:
                initial = False
            self._autostart_var = ctk.BooleanVar(value=initial)
            self._autostart_cb = ctk.CTkCheckBox(
                auto_row,
                text="Mulai otomatis saat Windows nyala",
                font=ctk.CTkFont(size=11),
                variable=self._autostart_var,
                command=self._on_autostart_toggle,
            )
            self._autostart_cb.pack(anchor="w")
        else:
            self._autostart_var = None
            self._autostart_cb = None

        self._rows: List[MemoRow] = []
        self.refresh_list()

    # --- visibility ---

    def show_panel(self) -> None:
        self.deiconify()
        self.lift()
        self.attributes("-topmost", True)
        self.after(200, lambda: self.attributes("-topmost", False))
        try:
            self.focus_force()
            self._search.focus_set()
        except Exception:
            pass
        self._sync_autostart_ui()

    def hide_to_tray(self) -> None:
        self.withdraw()

    def toggle_panel(self) -> None:
        try:
            if self.state() == "withdrawn" or not self.winfo_viewable():
                self.show_panel()
            else:
                self.hide_to_tray()
        except Exception:
            self.show_panel()

    # --- list ---

    def refresh_list(self) -> None:
        query = self._search.get().strip() if hasattr(self, "_search") else ""
        memos = self.storage.list_memos(query)
        for child in self._list.winfo_children():
            child.destroy()
        self._rows.clear()

        if not memos:
            msg = (
                "Salin teks apa saja — akan muncul di sini."
                if not query
                else "Tidak ada hasil."
            )
            ctk.CTkLabel(
                self._list, text=msg, text_color="gray60", font=ctk.CTkFont(size=12)
            ).pack(pady=24)
        else:
            for memo in memos:
                row = MemoRow(
                    self._list,
                    memo,
                    on_copy=self._copy_memo,
                    on_edit=self._edit_memo,
                    on_pin=self._toggle_pin,
                    on_delete=self._delete_memo,
                    on_hint=self._set_hint,
                )
                row.pack(fill="x", pady=2)
                self._rows.append(row)

        total = len(self.storage.list_memos())
        self._set_status(f"{total} memo")

    def schedule_refresh(self) -> None:
        """Thread-safe UI refresh."""
        try:
            self.after(0, self.refresh_list)
        except Exception:
            pass

    # --- actions ---

    def _copy_memo(self, memo: Memo) -> None:
        try:
            pyperclip.copy(memo.text)
            if self.on_copy_notify:
                self.on_copy_notify(memo.text)
            self._flash_status("Disalin.")
        except Exception:
            self._flash_status("Gagal menyalin.")

    def _edit_memo(self, memo: Memo) -> None:
        def save(new_text: str) -> None:
            if new_text.strip():
                self.storage.update_text(memo.id, new_text)
                self.refresh_list()

        EditDialog(self, memo.text, save)

    def _toggle_pin(self, memo: Memo) -> None:
        self.storage.set_pinned(memo.id, not memo.pinned)
        self.refresh_list()
        self._flash_status("Lepas." if memo.pinned else "Disemat.")

    def _delete_memo(self, memo: Memo) -> None:
        self.storage.delete(memo.id)
        self.refresh_list()
        self._flash_status("Dihapus.")

    def _clear_unpinned(self) -> None:
        n = self.storage.clear_unpinned()
        self.refresh_list()
        self._flash_status(f"Bersih · {n} dihapus." if n else "Tidak ada yang dihapus.")

    def _on_autostart_toggle(self) -> None:
        if self.set_autostart is None or self._autostart_var is None:
            return
        enabled = bool(self._autostart_var.get())
        try:
            self.set_autostart(enabled)
            self._flash_status(
                "Autostart aktif." if enabled else "Autostart nonaktif."
            )
        except Exception:
            self._flash_status("Gagal mengatur autostart.")
            self._sync_autostart_ui()

    def _sync_autostart_ui(self) -> None:
        if self._autostart_var is None or self.get_autostart is None:
            return
        try:
            self._autostart_var.set(bool(self.get_autostart()))
        except Exception:
            pass

    def _set_hint(self, msg: str) -> None:
        """Show hover hint without resetting the count timer aggressively."""
        self._set_status(msg)

    def _set_status(self, msg: str) -> None:
        try:
            self._status.configure(text=msg)
        except Exception:
            pass

    def _flash_status(self, msg: str) -> None:
        self._set_status(msg)
        if self._status_timer is not None:
            try:
                self.after_cancel(self._status_timer)
            except Exception:
                pass
        self._status_timer = self.after(1800, self._restore_count_status)

    def _restore_count_status(self) -> None:
        total = len(self.storage.list_memos())
        self._set_status(f"{total} memo")
        self._status_timer = None

    def request_quit(self) -> None:
        if self.on_quit_request:
            self.on_quit_request()
        else:
            self.destroy()
