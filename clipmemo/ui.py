"""ClipMemo main UI — dark compact panel, Indonesian strings."""

from __future__ import annotations

import tkinter as tk
from typing import Callable, List, Optional

import customtkinter as ctk
import pyperclip

from clipmemo.storage import Memo, Storage

# Visual constants
PANEL_WIDTH = 400
PANEL_HEIGHT = 520
PREVIEW_LEN = 120

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
        self.geometry("420x280")
        self.resizable(True, True)
        self.transient(master)
        self.grab_set()
        self._on_save = on_save

        ctk.CTkLabel(self, text="Edit teks memo:", font=ctk.CTkFont(size=13)).pack(
            anchor="w", padx=12, pady=(12, 4)
        )
        self._box = ctk.CTkTextbox(self, wrap="word", font=ctk.CTkFont(size=12))
        self._box.pack(fill="both", expand=True, padx=12, pady=4)
        self._box.insert("1.0", initial)

        btn_row = ctk.CTkFrame(self, fg_color="transparent")
        btn_row.pack(fill="x", padx=12, pady=12)
        ctk.CTkButton(btn_row, text="Batal", width=90, command=self.destroy).pack(
            side="right", padx=(8, 0)
        )
        ctk.CTkButton(btn_row, text="Simpan", width=90, command=self._save).pack(
            side="right"
        )
        self.after(50, self._box.focus_set)

    def _save(self) -> None:
        text = self._box.get("1.0", "end-1c")
        self._on_save(text)
        self.destroy()


class MemoRow(ctk.CTkFrame):
    """One memo row: preview, pin, edit, delete."""

    def __init__(
        self,
        master,
        memo: Memo,
        on_copy: Callable[[Memo], None],
        on_edit: Callable[[Memo], None],
        on_pin: Callable[[Memo], None],
        on_delete: Callable[[Memo], None],
        **kwargs,
    ) -> None:
        super().__init__(master, corner_radius=8, **kwargs)
        self.memo = memo

        preview = memo.text.replace("\n", " ").strip()
        if len(preview) > PREVIEW_LEN:
            preview = preview[: PREVIEW_LEN - 1] + "…"
        pin_mark = "📌 " if memo.pinned else ""

        left = ctk.CTkFrame(self, fg_color="transparent")
        left.pack(side="left", fill="both", expand=True, padx=(8, 4), pady=6)

        self._label = ctk.CTkLabel(
            left,
            text=f"{pin_mark}{preview}",
            anchor="w",
            justify="left",
            font=ctk.CTkFont(size=12),
            cursor="hand2",
        )
        self._label.pack(fill="x")
        self._label.bind("<Button-1>", lambda e: on_copy(memo))
        self.bind("<Button-1>", lambda e: on_copy(memo))

        btns = ctk.CTkFrame(self, fg_color="transparent")
        btns.pack(side="right", padx=4, pady=4)

        pin_text = "Lepas" if memo.pinned else "Semat"
        ctk.CTkButton(
            btns, text=pin_text, width=52, height=26, font=ctk.CTkFont(size=11),
            command=lambda: on_pin(memo),
        ).pack(side="left", padx=2)
        ctk.CTkButton(
            btns, text="Edit", width=44, height=26, font=ctk.CTkFont(size=11),
            command=lambda: on_edit(memo),
        ).pack(side="left", padx=2)
        ctk.CTkButton(
            btns, text="Hapus", width=50, height=26, font=ctk.CTkFont(size=11),
            fg_color="#8B3A3A", hover_color="#A04545",
            command=lambda: on_delete(memo),
        ).pack(side="left", padx=2)


class ClipMemoApp(ctk.CTk):
    """Main ClipMemo window."""

    def __init__(
        self,
        storage: Storage,
        on_copy_notify: Optional[Callable[[str], None]] = None,
        on_quit_request: Optional[Callable[[], None]] = None,
    ) -> None:
        super().__init__()
        self.storage = storage
        self.on_copy_notify = on_copy_notify
        self.on_quit_request = on_quit_request

        self.title("ClipMemo")
        self.geometry(f"{PANEL_WIDTH}x{PANEL_HEIGHT}")
        self.minsize(PANEL_WIDTH, 360)
        self.protocol("WM_DELETE_WINDOW", self.hide_to_tray)

        # Header
        header = ctk.CTkFrame(self, fg_color="transparent")
        header.pack(fill="x", padx=12, pady=(12, 4))
        ctk.CTkLabel(
            header,
            text="ClipMemo",
            font=ctk.CTkFont(size=18, weight="bold"),
        ).pack(side="left")
        ctk.CTkLabel(
            header,
            text="Ctrl+Shift+V",
            font=ctk.CTkFont(size=11),
            text_color="gray70",
        ).pack(side="right")

        # Search
        search_frame = ctk.CTkFrame(self, fg_color="transparent")
        search_frame.pack(fill="x", padx=12, pady=4)
        self._search = ctk.CTkEntry(
            search_frame,
            placeholder_text="Cari memo…",
            font=ctk.CTkFont(size=12),
        )
        self._search.pack(fill="x")
        self._search.bind("<KeyRelease>", lambda e: self.refresh_list())

        # Status
        self._status = ctk.CTkLabel(
            self, text="", font=ctk.CTkFont(size=11), text_color="gray60", anchor="w"
        )
        self._status.pack(fill="x", padx=14, pady=(2, 0))

        # Scrollable list
        self._list = ctk.CTkScrollableFrame(self, fg_color=("gray90", "gray17"))
        self._list.pack(fill="both", expand=True, padx=10, pady=8)

        # Footer actions
        footer = ctk.CTkFrame(self, fg_color="transparent")
        footer.pack(fill="x", padx=12, pady=(0, 12))
        ctk.CTkButton(
            footer, text="Segarkan", width=90, height=28, command=self.refresh_list
        ).pack(side="left")
        ctk.CTkButton(
            footer,
            text="Hapus tidak tersemat",
            width=150,
            height=28,
            fg_color="#5A4A2A",
            hover_color="#6B5A35",
            command=self._clear_unpinned,
        ).pack(side="left", padx=6)
        ctk.CTkButton(
            footer, text="Sembunyikan", width=100, height=28, command=self.hide_to_tray
        ).pack(side="right")

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
            msg = ("Salin teks apa saja — akan muncul di sini." if not query else "Tidak ada hasil pencarian.")
            ctk.CTkLabel(
                self._list, text=msg, text_color="gray60", font=ctk.CTkFont(size=12)
            ).pack(pady=20)
        else:
            for memo in memos:
                row = MemoRow(
                    self._list,
                    memo,
                    on_copy=self._copy_memo,
                    on_edit=self._edit_memo,
                    on_pin=self._toggle_pin,
                    on_delete=self._delete_memo,
                )
                row.pack(fill="x", pady=3)
                self._rows.append(row)

        total = len(self.storage.list_memos())
        pinned = sum(1 for m in self.storage.list_memos() if m.pinned)
        self._status.configure(
            text=f"{total} memo · {pinned} tersemat · klik untuk salin ulang"
        )

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
            self._flash_status("Disalin ke clipboard.")
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

    def _delete_memo(self, memo: Memo) -> None:
        self.storage.delete(memo.id)
        self.refresh_list()

    def _clear_unpinned(self) -> None:
        n = self.storage.clear_unpinned()
        self.refresh_list()
        self._flash_status(f"{n} memo tidak tersemat dihapus.")

    def _flash_status(self, msg: str) -> None:
        self._status.configure(text=msg)
        self.after(2000, self.refresh_list)

    def request_quit(self) -> None:
        if self.on_quit_request:
            self.on_quit_request()
        else:
            self.destroy()
