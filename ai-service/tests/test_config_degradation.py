"""
Tests voor de veilige degradatie: zonder API-sleutels zijn de AI-luiken 'uit'
(de endpoints geven dan 503 i.p.v. te crashen).
"""

from __future__ import annotations

import pytest

from app.config import Settings
from app.embeddings import EmbeddingError, embed_texts


def test_zonder_sleutels_zijn_de_ai_luiken_uit(monkeypatch: pytest.MonkeyPatch):
    monkeypatch.delenv("ANTHROPIC_API_KEY", raising=False)
    monkeypatch.delenv("VOYAGE_API_KEY", raising=False)

    settings = Settings()

    assert settings.ai_enabled is False
    assert settings.embeddings_enabled is False


def test_met_sleutels_zijn_de_ai_luiken_aan(monkeypatch: pytest.MonkeyPatch):
    monkeypatch.setenv("ANTHROPIC_API_KEY", "sk-ant-test")
    monkeypatch.setenv("VOYAGE_API_KEY", "pa-test")

    settings = Settings()

    assert settings.ai_enabled is True
    assert settings.embeddings_enabled is True


def test_embed_zonder_voyage_sleutel_gooit_embeddingerror(monkeypatch: pytest.MonkeyPatch):
    # get_settings() is lru_cached; leeg de cache zodat de ontbrekende sleutel telt.
    monkeypatch.delenv("VOYAGE_API_KEY", raising=False)
    from app import config

    config.get_settings.cache_clear()

    with pytest.raises(EmbeddingError):
        embed_texts(["wat tekst"], input_type="document")

    config.get_settings.cache_clear()
