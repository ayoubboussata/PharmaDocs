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

from .config import get_settings
from .extraction import ExtractionError, extract_invoice

app = FastAPI(
    title="PharmaDocs AI-service",
    version="0.2.0",
    description="Interne AI-microservice: PDF-extractie en (later) RAG-chat.",
)

MAX_BYTES = 10 * 1024 * 1024  # 10 MB


@app.get("/health")
def health() -> dict[str, object]:
    """Liveness-check + of AI-extractie beschikbaar is (API-sleutel aanwezig)."""
    settings = get_settings()
    return {"status": "ok", "aiEnabled": settings.ai_enabled, "model": settings.anthropic_model}


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
