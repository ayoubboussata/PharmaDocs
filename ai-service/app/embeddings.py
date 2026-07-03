"""
Embeddings voor de RAG-kennisassistent (Fase 4).

Genereert vectoren via Voyage AI. Twee stappen:
  - `chunk_text`  : deelt lange proceduretekst op in overlappende stukken.
  - `embed_texts` : zet een lijst stukken om naar vectoren (Voyage).

Wordt aangeroepen door de .NET-backend via `POST /embed-document`; de backend
slaat de stukken + vectoren op in pgvector.
"""

from __future__ import annotations

import re

import voyageai

from .config import get_settings


class EmbeddingError(Exception):
    """Embeddings mislukt (geen sleutel of upstream-fout)."""


# Splitst op zinseindes (. ! ?) gevolgd door witruimte. Bewust eenvoudig gehouden.
_SENTENCE_SPLIT = re.compile(r"(?<=[.!?])\s+")


def chunk_text(text: str, target_chars: int = 600, overlap_sentences: int = 1) -> list[str]:
    """
    Deelt tekst op in stukken van ~`target_chars`, robuust ongeacht hoe de PDF de
    regels afbreekt: eerst wordt alle witruimte (incl. newlines van woordwrap)
    genormaliseerd, dan wordt op zinnen gesplitst en worden zinnen samengevoegd tot
    een stuk groot genoeg is. `overlap_sentences` zinnen worden meegenomen naar het
    volgende stuk zodat context aan de rand niet verloren gaat.
    """
    normalized = re.sub(r"\s+", " ", text).strip()
    if not normalized:
        return []

    sentences = [s.strip() for s in _SENTENCE_SPLIT.split(normalized) if s.strip()]
    if not sentences:
        return [normalized]

    chunks: list[str] = []
    current: list[str] = []
    length = 0
    for sentence in sentences:
        if current and length + len(sentence) + 1 > target_chars:
            chunks.append(" ".join(current))
            # Start het nieuwe stuk met de laatste zin(nen) als overlap.
            current = current[-overlap_sentences:] if overlap_sentences else []
            length = sum(len(s) + 1 for s in current)
        current.append(sentence)
        length += len(sentence) + 1

    if current:
        chunks.append(" ".join(current))
    return chunks


def embed_texts(texts: list[str], input_type: str) -> list[list[float]]:
    """
    Zet teksten om naar vectoren. `input_type` is 'document' (bij indexeren) of
    'query' (bij een vraag) — Voyage optimaliseert de vector daarop.
    """
    settings = get_settings()
    if not settings.embeddings_enabled:
        raise EmbeddingError("VOYAGE_API_KEY ontbreekt; embeddings zijn niet beschikbaar.")
    if not texts:
        return []

    client = voyageai.Client(api_key=settings.voyage_api_key)
    try:
        result = client.embed(
            texts,
            model=settings.voyage_model,
            input_type=input_type,
            output_dimension=settings.embedding_dimension,
        )
    except Exception as exc:  # netwerk-/API-fout van Voyage
        raise EmbeddingError(f"Voyage-embeddings mislukten: {exc}") from exc

    return result.embeddings
