"""System tray icon for ClipMemo — CM badge icon."""

from __future__ import annotations

import threading
from typing import Callable, Optional

from PIL import Image, ImageDraw, ImageFont

try:
    import pystray
    from pystray import MenuItem as item
except ImportError:  # pragma: no cover
    pystray = None  # type: ignore
    item = None  # type: ignore


def _make_icon_image(size: int = 64) -> Image.Image:
    """Rounded square blue (#409CFF) with white bold letters CM centered."""
    img = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    draw = ImageDraw.Draw(img)
    margin = max(1, size // 16)
    draw.rounded_rectangle(
        [margin, margin, size - margin - 1, size - margin - 1],
        radius=size // 5,
        fill=(64, 156, 255, 255),
    )

    font = None
    # Try common bold TrueType fonts; fall back to default
    for name in (
        "arialbd.ttf",
        "Arial Bold.ttf",
        "DejaVuSans-Bold.ttf",
        "LiberationSans-Bold.ttf",
        "/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf",
        "/usr/share/fonts/truetype/liberation/LiberationSans-Bold.ttf",
        "C:/Windows/Fonts/arialbd.ttf",
        "C:/Windows/Fonts/segoeuib.ttf",
    ):
        try:
            font = ImageFont.truetype(name, size=int(size * 0.42))
            break
        except OSError:
            continue
    if font is None:
        try:
            font = ImageFont.load_default()
        except Exception:
            font = None

    text = "CM"
    if font is not None:
        try:
            bbox = draw.textbbox((0, 0), text, font=font)
            tw, th = bbox[2] - bbox[0], bbox[3] - bbox[1]
            tx = (size - tw) // 2 - bbox[0]
            ty = (size - th) // 2 - bbox[1]
        except Exception:
            tw, th = size // 2, size // 2
            tx, ty = size // 4, size // 4
        draw.text((tx, ty), text, fill=(255, 255, 255, 255), font=font)
    else:
        draw.text((size // 4, size // 4), text, fill=(255, 255, 255, 255))
    return img


class TrayIcon:
    """pystray-based tray with Buka / Autostart / Keluar menu."""

    def __init__(
        self,
        on_open: Callable[[], None],
        on_quit: Callable[[], None],
        title: str = "ClipMemo",
        get_autostart: Optional[Callable[[], bool]] = None,
        set_autostart: Optional[Callable[[bool], None]] = None,
    ) -> None:
        self.on_open = on_open
        self.on_quit = on_quit
        self.title = title
        self.get_autostart = get_autostart
        self.set_autostart = set_autostart
        self._icon: Optional[object] = None
        self._thread: Optional[threading.Thread] = None

    def _autostart_checked(self, item_=None) -> bool:
        if self.get_autostart is None:
            return False
        try:
            return bool(self.get_autostart())
        except Exception:
            return False

    def _build_menu(self):
        entries = [item("Buka", self._handle_open, default=True)]
        if self.get_autostart is not None and self.set_autostart is not None:
            entries.append(
                item(
                    "Autostart",
                    self._handle_autostart,
                    checked=self._autostart_checked,
                )
            )
        entries.append(item("Keluar", self._handle_quit))
        return pystray.Menu(*entries)

    def start(self) -> None:
        if pystray is None:
            return
        if self._thread and self._thread.is_alive():
            return
        image = _make_icon_image()
        menu = self._build_menu()
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

    def _handle_autostart(self, icon=None, menu_item=None) -> None:
        if self.set_autostart is None or self.get_autostart is None:
            return
        try:
            current = bool(self.get_autostart())
            self.set_autostart(not current)
        except Exception:
            pass
        # Refresh checked state
        try:
            if self._icon is not None:
                self._icon.update_menu()  # type: ignore[attr-defined]
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
