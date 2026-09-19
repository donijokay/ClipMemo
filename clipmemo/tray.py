"""System tray icon for ClipMemo."""

from __future__ import annotations

import threading
from typing import Callable, Optional

from PIL import Image, ImageDraw

try:
    import pystray
    from pystray import MenuItem as item
except ImportError:  # pragma: no cover
    pystray = None  # type: ignore
    item = None  # type: ignore


def _make_icon_image(size: int = 64) -> Image.Image:
    """Simple clipboard-style icon."""
    img = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    draw = ImageDraw.Draw(img)
    # Board
    margin = size // 8
    draw.rounded_rectangle(
        [margin, margin + 4, size - margin, size - margin],
        radius=size // 10,
        fill=(64, 156, 255, 255),
        outline=(30, 100, 200, 255),
        width=2,
    )
    # Clip top
    clip_w = size // 3
    cx = size // 2
    draw.rounded_rectangle(
        [cx - clip_w // 2, margin - 2, cx + clip_w // 2, margin + size // 5],
        radius=4,
        fill=(220, 230, 240, 255),
        outline=(180, 190, 200, 255),
        width=1,
    )
    # Lines
    for i in range(3):
        y = margin + size // 3 + i * (size // 8)
        draw.line(
            [margin + 8, y, size - margin - 8, y],
            fill=(255, 255, 255, 200),
            width=2,
        )
    return img


class TrayIcon:
    """pystray-based tray with Buka / Keluar menu."""

    def __init__(
        self,
        on_open: Callable[[], None],
        on_quit: Callable[[], None],
        title: str = "ClipMemo",
    ) -> None:
        self.on_open = on_open
        self.on_quit = on_quit
        self.title = title
        self._icon: Optional[object] = None
        self._thread: Optional[threading.Thread] = None

    def start(self) -> None:
        if pystray is None:
            return
        if self._thread and self._thread.is_alive():
            return
        image = _make_icon_image()
        menu = pystray.Menu(
            item("Buka", self._handle_open, default=True),
            item("Keluar", self._handle_quit),
        )
        self._icon = pystray.Icon("ClipMemo", image, self.title, menu)
        self._thread = threading.Thread(
            target=self._run,
            name="ClipMemo-Tray",
            daemon=True,
        )
        self._thread.start()

    def _run(self) -> None:
        try:
            if self._icon is not None:
                self._icon.run()  # type: ignore[attr-defined]
        except Exception:
            pass

    def _handle_open(self, icon=None, menu_item=None) -> None:
        try:
            self.on_open()
        except Exception:
            pass

    def _handle_quit(self, icon=None, menu_item=None) -> None:
        try:
            self.on_quit()
        except Exception:
            pass

    def stop(self) -> None:
        icon = self._icon
        self._icon = None
        if icon is not None:
            try:
                icon.stop()  # type: ignore[attr-defined]
            except Exception:
                pass
        t = self._thread
        if t and t.is_alive():
            t.join(timeout=2.0)
        self._thread = None
