"""Gedeelde helper om een system-prompt uit prompts/<naam>.md te laden."""

from __future__ import annotations

from pathlib import Path

_PROMPTS_DIR = Path(__file__).resolve().parent.parent / "prompts"


def load_system_prompt(filename: str) -> str:
    """
    Leest prompts/<filename> en geeft het deel na de eerste horizontale lijn '---'
    terug (de eigenlijke prompt; alles ervoor is toelichting/metadata).
    """
    raw = (_PROMPTS_DIR / filename).read_text(encoding="utf-8")
    marker = "\n---\n"
    return raw.split(marker, 1)[1].strip() if marker in raw else raw.strip()
