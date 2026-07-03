"""
PharmaDocs AI-service (Python FastAPI).

Aparte microservice die door de .NET-backend intern wordt aangeroepen voor
de AI-taken.

  POST /extract          ontvangt een PDF, geeft de ruwe tekst terug (Dag 3).
  POST /extract-invoice  PDF -> tekst -> Claude -> gestructureerde JSON (Dag 4).
"""

from __future__ import annotations

import io

import pdfplumber
from fastapi import FastAPI, File, HTTPException, UploadFile
from pydantic import BaseModel

from .config import get_settings
from .embeddings import EmbeddingError, chunk_text, embed_texts
from .extraction import ExtractionError, extract_invoice
from .rag import AnswerError, answer_question

app = FastAPI(
    title="PharmaDocs AI-service",
    version="0.3.0",
    description="Interne AI-microservice: PDF-extractie, factuurextractie en RAG-embeddings.",
)

MAX_BYTES = 10 * 1024 * 1024  # 10 MB


@app.get("/health")
def health() -> dict[str, object]:
    """Liveness-check + of AI-extractie en embeddings beschikbaar zijn (sleutels aanwezig)."""
    settings = get_settings()
    return {
        "status": "ok",
        "aiEnabled": settings.ai_enabled,
        "model": settings.anthropic_model,
        "embeddingsEnabled": settings.embeddings_enabled,
        "embeddingModel": settings.voyage_model,
        "embeddingDimension": settings.embedding_dimension,
    }


def _extract_text(pdf_bytes: bytes) -> tuple[str, int]:
    """Haalt de tekst uit alle pagina's. Geeft (tekst, aantal_paginas) terug."""
    parts: list[str] = []
    with pdfplumber.open(io.BytesIO(pdf_bytes)) as pdf:
        for page in pdf.pages:
            parts.append(page.extract_text() or "")
        page_count = len(pdf.pages)
    return "\n\n".join(parts).strip(), page_count


async def _read_pdf(file: UploadFile) -> tuple[str, int]:
    """Valideert de upload en geeft (tekst, aantal_paginas). Gooit HTTPException."""
    if file.content_type not in ("application/pdf", "application/octet-stream"):
        raise HTTPException(status_code=415, detail="Enkel PDF-bestanden worden ondersteund.")

    data = await file.read()
    if not data:
        raise HTTPException(status_code=400, detail="Leeg bestand ontvangen.")
    if len(data) > MAX_BYTES:
        raise HTTPException(status_code=413, detail="Bestand te groot (max. 10 MB).")

    try:
        text, page_count = _extract_text(data)
    except Exception as exc:  # onleesbare/corrupte PDF
        raise HTTPException(
            status_code=422,
            detail="Kon de PDF niet lezen (mogelijk beschadigd of geen tekstlaag).",
        ) from exc

    if not text:
        raise HTTPException(
            status_code=422,
            detail="Geen tekst gevonden in de PDF (scan zonder OCR?).",
        )
    return text, page_count


@app.post("/extract")
async def extract(file: UploadFile = File(...)) -> dict[str, object]:
    """Ontvangt een PDF en geeft de geëxtraheerde ruwe tekst terug."""
    text, page_count = await _read_pdf(file)
    return {
        "fileName": file.filename,
        "pageCount": page_count,
        "characterCount": len(text),
        "text": text,
    }


@app.post("/extract-invoice")
async def extract_invoice_endpoint(file: UploadFile = File(...)) -> dict[str, object]:
    """PDF -> tekst -> Claude -> gestructureerde factuur-JSON."""
    text, page_count = await _read_pdf(file)

    try:
        invoice = extract_invoice(text)
    except ExtractionError as exc:
        # 503: AI-luik niet beschikbaar (geen sleutel) of upstream-fout.
        raise HTTPException(status_code=503, detail=str(exc)) from exc

    return {
        "fileName": file.filename,
        "pageCount": page_count,
        "invoice": invoice,
    }


@app.post("/embed-document")
async def embed_document(file: UploadFile = File(...)) -> dict[str, object]:
    """
    PDF -> tekst -> chunks -> embeddings (Voyage). Geeft de stukken met hun
    vectoren terug; de .NET-backend slaat ze op in pgvector (RAG-indexering).
    """
    text, page_count = await _read_pdf(file)
    chunks = chunk_text(text)

    try:
        vectors = embed_texts(chunks, input_type="document")
    except EmbeddingError as exc:
        raise HTTPException(status_code=503, detail=str(exc)) from exc

    return {
        "fileName": file.filename,
        "pageCount": page_count,
        "chunks": [
            {"index": i, "content": content, "embedding": vector}
            for i, (content, vector) in enumerate(zip(chunks, vectors))
        ],
    }


# --- RAG-chat (Fase 4 Dag 9): query-embedding + gegrond antwoord ---


class EmbedQueryRequest(BaseModel):
    text: str


@app.post("/embed-query")
def embed_query(req: EmbedQueryRequest) -> dict[str, object]:
    """Embedt één vraag (input_type=query) voor de vectorzoektocht in de backend."""
    try:
        vectors = embed_texts([req.text], input_type="query")
    except EmbeddingError as exc:
        raise HTTPException(status_code=503, detail=str(exc)) from exc
    return {"embedding": vectors[0]}


class AnswerContext(BaseModel):
    sourceName: str
    content: str


class AnswerRequest(BaseModel):
    question: str
    contexts: list[AnswerContext] = []


@app.post("/answer")
def answer(req: AnswerRequest) -> dict[str, object]:
    """Genereert een gegrond antwoord op basis van de meegestuurde fragmenten (Claude)."""
    try:
        text = answer_question(req.question, [c.model_dump() for c in req.contexts])
    except AnswerError as exc:
        raise HTTPException(status_code=503, detail=str(exc)) from exc
    return {"answer": text}
