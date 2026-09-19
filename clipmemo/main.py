"""ClipMemo application entry — wires storage, watcher, UI, tray, hotkey."""

from __future__ import annotations

import sys
import threading
from typing import Callable, Optional

from clipmemo.clipboard_watcher import ClipboardWatcher
from clipmemo.storage import Storage
from clipmemo.tray import TrayIcon
from clipmemo.ui import ClipMemoApp


class HotkeyManager:
    """Register Ctrl+Shift+V with keyboard or pynput; degrade gracefully."""

    def __init__(self, callback: Callable[[], None]) -> None:
        self.callback = callback
        self._backend: Optional[str] = None
        self._listener = None

    def start(self) -> Optional[str]:
        # Prefer `keyboard` on Windows
        if sys.platform == "win32":
            try:
                import keyboard

                keyboard.add_hotkey("ctrl+shift+v", self._safe_cb, suppress=False)
                self._backend = "keyboard"
                return self._backend
            except Exception:
                pass

        # Fallback: pynput GlobalHotKeys
        try:
            from pynput import keyboard as pk

            self._listener = pk.GlobalHotKeys({"<ctrl>+<shift>+v": self._safe_cb})
            self._listener.start()
            self._backend = "pynput"
            return self._backend
        except Exception:
            pass

        self._backend = None
        return None

    def _safe_cb(self) -> None:
        try:
            self.callback()
        except Exception:
            pass

    def stop(self) -> None:
        if self._backend == "keyboard":
            try:
                import keyboard

                keyboard.unhook_all_hotkeys()
            except Exception:
                try:
                    import keyboard

                    keyboard.unhook_all()
                except Exception:
                    pass
        if self._listener is not None:
            try:
                self._listener.stop()
            except Exception:
                pass
            self._listener = None
        self._backend = None


class Application:
    def __init__(self) -> None:
        self.storage = Storage()
        self._shutting_down = False
        self._shutdown_lock = threading.Lock()
        self.watcher: Optional[ClipboardWatcher] = None
        self.tray: Optional[TrayIcon] = None
        self.hotkeys: Optional[HotkeyManager] = None
        self.app: Optional[ClipMemoApp] = None

    def run(self) -> None:
        self.app = ClipMemoApp(
            storage=self.storage,
            on_copy_notify=self._on_app_copy,
            on_quit_request=self.shutdown,
        )

        self.watcher = ClipboardWatcher(on_new_text=self._on_clipboard_text)
        self.watcher.start()

        self.tray = TrayIcon(
            on_open=self._open_from_tray,
            on_quit=self.shutdown,
        )
        self.tray.start()

        self.hotkeys = HotkeyManager(callback=self._toggle_from_hotkey)
        backend = self.hotkeys.start()
        if backend:
            print(f"ClipMemo: hotkey Ctrl+Shift+V aktif via {backend}", flush=True)
        else:
            print(
                "ClipMemo: hotkey tidak tersedia — gunakan ikon baki sistem.",
                flush=True,
            )

        self.app.show_panel()
        try:
            self.app.mainloop()
        finally:
            self.shutdown()

    def _on_clipboard_text(self, text: str) -> None:
        memo = self.storage.add_text(text)
        if memo and self.app:
            self.app.schedule_refresh()

    def _on_app_copy(self, text: str) -> None:
        if self.watcher:
            self.watcher.notify_copied(text)

    def _open_from_tray(self) -> None:
        if self.app and not self._shutting_down:
            self.app.after(0, self.app.show_panel)

    def _toggle_from_hotkey(self) -> None:
        if self.app and not self._shutting_down:
            self.app.after(0, self.app.toggle_panel)

    def shutdown(self) -> None:
        with self._shutdown_lock:
            if self._shutting_down:
                return
            self._shutting_down = True

        if self.hotkeys:
            try:
                self.hotkeys.stop()
            except Exception:
                pass
            self.hotkeys = None

        if self.watcher:
            try:
                self.watcher.stop()
            except Exception:
                pass
            self.watcher = None

        if self.tray:
            try:
                self.tray.stop()
            except Exception:
                pass
            self.tray = None

        if self.app:
            app = self.app
            self.app = None
            try:
                app.quit()
            except Exception:
                pass
            try:
                app.destroy()
            except Exception:
                pass


def main() -> None:
    Application().run()


if __name__ == "__main__":
    main()
