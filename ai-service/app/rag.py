"""
RAG-antwoordgeneratie: vraag + opgehaalde fragmenten -> gegrond antwoord via Claude.

De .NET-backend doet de vectorzoektocht (pgvector) en stuurt de vraag met de
meest relevante fragmenten hierheen; Claude formuleert een antwoord dat enkel op
die fragmenten steunt, met bronvermelding. Systeemprompt: prompts/rag_answer.md.
"""

from __future__ import annotations

from pathlib import Path

import anthropic

from .config import get_settings

_PROMPT_FILE = Path(__file__).resolve().parent.parent / "prompts" / "rag_answer.md"


def _load_system_prompt() -> str:
    raw = _PROMPT_FILE.read_text(encoding="utf-8")
    marker = "\n---\n"
    return raw.split(marker, 1)[1].strip() if marker in raw else raw.strip()


SYSTEM_PROMPT = _load_system_prompt()


class AnswerError(Exception):
    """Antwoordgeneratie mislukt (geen sleutel, weigering of upstream-fout)."""


def _build_user_message(question: str, contexts: list[dict]) -> str:
    if not contexts:
        return (
            f"Vraag: {question}\n\n"
            "Er zijn geen relevante fragmenten gevonden in de procedures."
        )
    blocks = "\n\n".join(
        f"[Bron: {c['sourceName']}]\n{c['content']}" for c in contexts
    )
    return f"Vraag: {question}\n\nFragmenten uit de interne procedures:\n\n{blocks}"


def answer_question(question: str, contexts: list[dict]) -> str:
    """Genereert een gegrond antwoord op basis van de fragmenten."""
    settings = get_settings()
    if not settings.ai_enabled:
        raise AnswerError("ANTHROPIC_API_KEY ontbreekt; de kennisassistent is niet beschikbaar.")

    client = anthropic.Anthropic(api_key=settings.anthropic_api_key)

    try:
        response = client.messages.create(
            model=settings.anthropic_model,
            max_tokens=1024,
            system=SYSTEM_PROMPT,
            messages=[{"role": "user", "content": _build_user_message(question, contexts)}],
        )
    except anthropic.APIStatusError as exc:
        raise AnswerError(f"Claude API-fout ({exc.status_code}).") from exc
    except anthropic.APIConnectionError as exc:
        raise AnswerError("Kon de Claude API niet bereiken.") from exc

    if response.stop_reason == "refusal":
        raise AnswerError("Het model weigerde deze vraag te beantwoorden.")

    text = "".join(block.text for block in response.content if block.type == "text").strip()
    if not text:
        raise AnswerError("Leeg antwoord van het model.")
    return text
