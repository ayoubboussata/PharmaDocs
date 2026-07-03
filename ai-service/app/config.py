"""Configuratie uit omgevingsvariabelen / .env."""

from __future__ import annotations

import os
from functools import lru_cache

from dotenv import load_dotenv

load_dotenv()  # laadt ai-service/.env als die bestaat


class Settings:
    def __init__(self) -> None:
        self.anthropic_api_key: str | None = os.getenv("ANTHROPIC_API_KEY")
        # Standaard het meest capabele model; overschrijfbaar naar bv. claude-sonnet-5
        # (goedkoper) via ANTHROPIC_MODEL in .env.
        self.anthropic_model: str = os.getenv("ANTHROPIC_MODEL", "claude-opus-4-8")

        # Voyage AI — embeddings voor de RAG-kennisassistent (Fase 4).
        self.voyage_api_key: str | None = os.getenv("VOYAGE_API_KEY")
        # Meertalig (NL/FR), 1024 dimensies. De pgvector-kolom is op 1024 vastgezet.
        self.voyage_model: str = os.getenv("VOYAGE_MODEL", "voyage-3.5")
        self.embedding_dimension: int = 1024

    @property
    def ai_enabled(self) -> bool:
        """True als er een Claude-sleutel is; anders draait enkel de tekstextractie."""
        return bool(self.anthropic_api_key)

    @property
    def embeddings_enabled(self) -> bool:
        """True als er een Voyage-sleutel is; anders is de RAG-indexering uit."""
        return bool(self.voyage_api_key)


@lru_cache
def get_settings() -> Settings:
    return Settings()
