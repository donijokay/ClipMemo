"""Windows (and optional Linux) autostart helpers for ClipMemo."""

from __future__ import annotations

import json
import os
import sys
from pathlib import Path
from typing import Any, Dict

from clipmemo.storage import get_data_dir

APP_NAME = "ClipMemo"
RUN_VALUE_NAME = "ClipMemo"


def _settings_path() -> Path:
    return get_data_dir() / "settings.json"


def _load_settings() -> Dict[str, Any]:
    path = _settings_path()
    if not path.exists():
        return {}
    try:
        data = json.loads(path.read_text(encoding="utf-8"))
        return data if isinstance(data, dict) else {}
    except (json.JSONDecodeError, OSError, TypeError):
        return {}


def _save_settings(data: Dict[str, Any]) -> None:
    path = _settings_path()
    path.parent.mkdir(parents=True, exist_ok=True)
    tmp = path.with_suffix(".tmp")
    tmp.write_text(json.dumps(data, ensure_ascii=False, indent=2), encoding="utf-8")
    tmp.replace(path)


def _persist_preference(enabled: bool) -> None:
    data = _load_settings()
    data["autostart"] = bool(enabled)
    _save_settings(data)


def _start_cmd_path() -> Path:
    return get_data_dir() / "start_clipmemo.cmd"


def _resolve_python_launcher() -> str:
    """Prefer pythonw on Windows; fall back to current interpreter."""
    if sys.platform == "win32":
        exe = Path(sys.executable)
        # Prefer pythonw.exe beside python.exe (no console flash)
        candidates = [
            exe.with_name("pythonw.exe"),
            exe.parent / "pythonw.exe",
        ]
        for c in candidates:
            if c.is_file():
                return str(c)
    return sys.executable


def _write_start_cmd() -> Path:
    """Write APPDATA\\ClipMemo\\start_clipmemo.cmd that launches the app."""
    data_dir = get_data_dir()
    data_dir.mkdir(parents=True, exist_ok=True)
    cmd_path = _start_cmd_path()
    launcher = _resolve_python_launcher()
    # Use quoted paths; /c style via cmd file content
    lines = [
        "@echo off",
        f'"{launcher}" -m clipmemo',
    ]
    cmd_path.write_text("\r\n".join(lines) + "\r\n", encoding="utf-8")
    return cmd_path


def _win_reg_get() -> bool:
    try:
        import winreg
    except ImportError:
        return False
    try:
        with winreg.OpenKey(
            winreg.HKEY_CURRENT_USER,
            r"Software\Microsoft\Windows\CurrentVersion\Run",
            0,
            winreg.KEY_READ,
        ) as key:
            try:
                winreg.QueryValueEx(key, RUN_VALUE_NAME)
                return True
            except FileNotFoundError:
                return False
    except OSError:
        return False


def _win_reg_set(enabled: bool) -> None:
    import winreg

    key_path = r"Software\Microsoft\Windows\CurrentVersion\Run"
    if enabled:
        cmd_path = _write_start_cmd()
        # Register the .cmd so it runs without needing shell:startup
        value = f'"{cmd_path}"'
        with winreg.OpenKey(
            winreg.HKEY_CURRENT_USER, key_path, 0, winreg.KEY_SET_VALUE
        ) as key:
            winreg.SetValueEx(key, RUN_VALUE_NAME, 0, winreg.REG_SZ, value)
    else:
        try:
            with winreg.OpenKey(
                winreg.HKEY_CURRENT_USER, key_path, 0, winreg.KEY_SET_VALUE
            ) as key:
                try:
                    winreg.DeleteValue(key, RUN_VALUE_NAME)
                except FileNotFoundError:
                    pass
        except OSError:
            pass


def _xdg_desktop_path() -> Path:
    config = os.environ.get("XDG_CONFIG_HOME")
    if config:
        base = Path(config)
    else:
        base = Path.home() / ".config"
    return base / "autostart" / "clipmemo.desktop"


def _linux_is_enabled() -> bool:
    return _xdg_desktop_path().is_file()


def _linux_set_enabled(enabled: bool) -> None:
    desk = _xdg_desktop_path()
    if enabled:
        desk.parent.mkdir(parents=True, exist_ok=True)
        exe = sys.executable
        content = "\n".join(
            [
                "[Desktop Entry]",
                "Type=Application",
                "Name=ClipMemo",
                "Comment=Clipboard history",
                f"Exec={exe} -m clipmemo",
                "X-GNOME-Autostart-enabled=true",
                "Hidden=false",
                "",
            ]
        )
        desk.write_text(content, encoding="utf-8")
    else:
        try:
            if desk.exists():
                desk.unlink()
        except OSError:
            pass


def is_enabled() -> bool:
    """Return whether autostart is currently enabled.

    On Windows the Run registry key is the source of truth.
    Preference is also mirrored in settings.json.
    """
    if sys.platform == "win32":
        return _win_reg_get()
    if sys.platform.startswith("linux"):
        return _linux_is_enabled()
    # Other platforms: fall back to settings preference only
    return bool(_load_settings().get("autostart", False))


def set_enabled(enabled: bool) -> None:
    """Enable or disable autostart; persist preference to settings.json."""
    enabled = bool(enabled)
    if sys.platform == "win32":
        _win_reg_set(enabled)
    elif sys.platform.startswith("linux"):
        try:
            _linux_set_enabled(enabled)
        except OSError:
            pass
    _persist_preference(enabled)
