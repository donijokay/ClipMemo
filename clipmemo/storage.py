"""Persistent storage for ClipMemo clipboard history."""

from __future__ import annotations

import json
import os
import sys
import threading
import uuid
from dataclasses import asdict, dataclass, field
from datetime import datetime, timezone
from pathlib import Path
from typing import List, Optional


MAX_UNPINNED = 100
MAX_TEXT_BYTES = 100 * 1024  # 100 KB


def get_data_dir() -> Path:
    """Return platform-specific ClipMemo data directory."""
    if sys.platform == "win32":
        base = os.environ.get("APPDATA")
        if base:
            return Path(base) / "ClipMemo"
        return Path.home() / "AppData" / "Roaming" / "ClipMemo"
    # Linux / macOS
    xdg = os.environ.get("XDG_CONFIG_HOME")
    if xdg:
        return Path(xdg) / "ClipMemo"
    return Path.home() / ".config" / "ClipMemo"


@dataclass
class Memo:
    id: str
    text: str
    created_at: str
    pinned: bool = False
    updated_at: Optional[str] = None

    @staticmethod
    def create(text: str, pinned: bool = False) -> "Memo":
        now = datetime.now(timezone.utc).isoformat()
        return Memo(
            id=str(uuid.uuid4()),
            text=text,
            created_at=now,
            pinned=pinned,
            updated_at=now,
        )


class Storage:
    """Thread-safe JSON-backed memo store."""

    def __init__(self, data_dir: Optional[Path] = None) -> None:
        self.data_dir = data_dir or get_data_dir()
        self.data_file = self.data_dir / "memos.json"
        self._lock = threading.RLock()
        self._memos: List[Memo] = []
        self._ensure_dir()
        self.load()

    def _ensure_dir(self) -> None:
        self.data_dir.mkdir(parents=True, exist_ok=True)

    def load(self) -> None:
        with self._lock:
            if not self.data_file.exists():
                self._memos = []
                return
            try:
                raw = json.loads(self.data_file.read_text(encoding="utf-8"))
                items = raw.get("memos", raw) if isinstance(raw, dict) else raw
                self._memos = []
                for item in items:
                    self._memos.append(
                        Memo(
                            id=item.get("id", str(uuid.uuid4())),
                            text=item.get("text", ""),
                            created_at=item.get("created_at", ""),
                            pinned=bool(item.get("pinned", False)),
                            updated_at=item.get("updated_at"),
                        )
                    )
                self._enforce_cap(save=False)
            except (json.JSONDecodeError, OSError, TypeError, KeyError):
                self._memos = []

    def save(self) -> None:
        with self._lock:
            self._ensure_dir()
            payload = {
                "version": 1,
                "memos": [asdict(m) for m in self._memos],
            }
            tmp = self.data_file.with_suffix(".tmp")
            tmp.write_text(
                json.dumps(payload, ensure_ascii=False, indent=2),
                encoding="utf-8",
            )
            tmp.replace(self.data_file)

    def _sorted(self) -> List[Memo]:
        """Pinned first (newest first), then unpinned newest first."""
        pinned = [m for m in self._memos if m.pinned]
        unpinned = [m for m in self._memos if not m.pinned]
        key = lambda m: m.updated_at or m.created_at
        pinned.sort(key=key, reverse=True)
        unpinned.sort(key=key, reverse=True)
        return pinned + unpinned

    def _enforce_cap(self, save: bool = True) -> None:
        """Keep at most MAX_UNPINNED unpinned memos; never drop pinned."""
        pinned = [m for m in self._memos if m.pinned]
        unpinned = [m for m in self._memos if not m.pinned]
        key = lambda m: m.updated_at or m.created_at
        unpinned.sort(key=key, reverse=True)
        if len(unpinned) > MAX_UNPINNED:
            unpinned = unpinned[:MAX_UNPINNED]
        self._memos = pinned + unpinned
        if save:
            self.save()

    def list_memos(self, query: str = "") -> List[Memo]:
        with self._lock:
            memos = self._sorted()
            if not query:
                return list(memos)
            q = query.casefold()
            return [m for m in memos if q in m.text.casefold()]

    def get(self, memo_id: str) -> Optional[Memo]:
        with self._lock:
            for m in self._memos:
                if m.id == memo_id:
                    return m
            return None

    def add_text(self, text: str) -> Optional[Memo]:
        """Add clipboard text if not empty/duplicate of last saved. Returns new Memo or None."""
        text = text or ""
        if not text.strip():
            return None
        encoded = text.encode("utf-8", errors="replace")
        if len(encoded) > MAX_TEXT_BYTES:
            return None
        with self._lock:
            # Skip duplicate of most recently added (by updated_at among all)
            if self._memos:
                latest = max(self._memos, key=lambda m: m.updated_at or m.created_at)
                if latest.text == text:
                    return None
            memo = Memo.create(text)
            self._memos.append(memo)
            self._enforce_cap(save=True)
            return memo

    def update_text(self, memo_id: str, text: str) -> bool:
        with self._lock:
            for m in self._memos:
                if m.id == memo_id:
                    m.text = text
                    m.updated_at = datetime.now(timezone.utc).isoformat()
                    self.save()
                    return True
            return False

    def set_pinned(self, memo_id: str, pinned: bool) -> bool:
        with self._lock:
            for m in self._memos:
                if m.id == memo_id:
                    m.pinned = pinned
                    m.updated_at = datetime.now(timezone.utc).isoformat()
                    self._enforce_cap(save=True)
                    return True
            return False

    def delete(self, memo_id: str) -> bool:
        with self._lock:
            before = len(self._memos)
            self._memos = [m for m in self._memos if m.id != memo_id]
            if len(self._memos) < before:
                self.save()
                return True
            return False

    def clear_unpinned(self) -> int:
        with self._lock:
            before = len(self._memos)
            self._memos = [m for m in self._memos if m.pinned]
            removed = before - len(self._memos)
            if removed:
                self.save()
            return removed
