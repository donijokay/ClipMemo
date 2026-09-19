"""Poll system clipboard and notify on new text."""

from __future__ import annotations

import threading
import time
from typing import Callable, Optional

import pyperclip

from clipmemo.storage import MAX_TEXT_BYTES


class ClipboardWatcher:
    """Background thread that polls the clipboard ~every 0.5s."""

    def __init__(
        self,
        on_new_text: Callable[[str], None],
        poll_interval: float = 0.5,
    ) -> None:
        self.on_new_text = on_new_text
        self.poll_interval = poll_interval
        self._stop = threading.Event()
        self._thread: Optional[threading.Thread] = None
        self._last_seen: Optional[str] = None
        self._suppress_until: float = 0.0
        self._lock = threading.Lock()

    def start(self) -> None:
        if self._thread and self._thread.is_alive():
            return
        self._stop.clear()
        # Seed last_seen so we don't immediately capture current clipboard as "new"
        try:
            self._last_seen = pyperclip.paste()
        except Exception:
            self._last_seen = None
        self._thread = threading.Thread(
            target=self._run,
            name="ClipMemo-ClipboardWatcher",
            daemon=True,
        )
        self._thread.start()

    def stop(self, timeout: float = 2.0) -> None:
        self._stop.set()
        t = self._thread
        if t and t.is_alive():
            t.join(timeout=timeout)
        self._thread = None

    def suppress_next(self, seconds: float = 1.0) -> None:
        """Ignore clipboard changes for a short time (e.g. after we copy ourselves)."""
        with self._lock:
            self._suppress_until = time.monotonic() + seconds
            try:
                self._last_seen = pyperclip.paste()
            except Exception:
                pass

    def notify_copied(self, text: str) -> None:
        """Update last_seen after app copies text to clipboard."""
        with self._lock:
            self._last_seen = text
            self._suppress_until = time.monotonic() + 1.0

    def _run(self) -> None:
        while not self._stop.is_set():
            try:
                self._tick()
            except Exception:
                pass
            self._stop.wait(self.poll_interval)

    def _tick(self) -> None:
        try:
            text = pyperclip.paste()
        except Exception:
            return
        if text is None:
            return
        if not isinstance(text, str):
            text = str(text)

        with self._lock:
            if time.monotonic() < self._suppress_until:
                self._last_seen = text
                return
            if text == self._last_seen:
                return
            self._last_seen = text

        if not text.strip():
            return
        try:
            if len(text.encode("utf-8", errors="replace")) > MAX_TEXT_BYTES:
                return
        except Exception:
            return

        try:
            self.on_new_text(text)
        except Exception:
            pass
