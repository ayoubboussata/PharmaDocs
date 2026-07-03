"""
AI-extractie: ruwe factuurtekst -> gestructureerde JSON via de Claude API.

Aanpak: geforceerde tool-use met een strikt JSON-schema (`strict: true`).
Zo garandeert de API dat het antwoord exact aan het schema voldoet.
De systeemprompt staat in prompts/invoice_extraction.md (versioneerbaar).
"""

from __future__ import annotations

from pathlib import Path

import anthropic

from .config import get_settings

# --- Systeemprompt inladen (het gedeelte onder de scheidingslijn) ---
_PROMPT_FILE = Path(__file__).resolve().parent.parent / "prompts" / "invoice_extraction.md"


def _load_system_prompt() -> str:
    raw = _PROMPT_FILE.read_text(encoding="utf-8")
    # Alles na de eerste horizontale lijn '---' is de eigenlijke prompt.
    marker = "\n---\n"
    return raw.split(marker, 1)[1].strip() if marker in raw else raw.strip()


SYSTEM_PROMPT = _load_system_prompt()

# --- Strikt JSON-schema voor het tool-argument (matcht de .NET ExtractedInvoice) ---
INVOICE_TOOL = {
    "name": "record_invoice",
    "description": "Registreert de uit de factuur geëxtraheerde gegevens.",
    "strict": True,
    "input_schema": {
        "type": "object",
        "additionalProperties": False,
        "properties": {
            "supplierName": {"type": "string"},
            "invoiceNumber": {"type": "string"},
            "invoiceDate": {
                "type": ["string", "null"],
                "description": "ISO-datum YYYY-MM-DD, of null.",
            },
            "totalAmount": {"type": "number"},
            "currency": {"type": "string"},
            "lineItems": {
                "type": "array",
                "items": {
                    "type": "object",
                    "additionalProperties": False,
                    "properties": {
                        "description": {"type": "string"},
                        "quantity": {"type": "number"},
                        "unitPrice": {"type": "number"},
                        "lineTotal": {"type": "number"},
                    },
                    "required": ["description", "quantity", "unitPrice", "lineTotal"],
                },
            },
        },
        "required": [
            "supplierName",
            "invoiceNumber",
            "invoiceDate",
            "totalAmount",
            "currency",
            "lineItems",
        ],
    },
}


class ExtractionError(Exception):
    """Extractie mislukt (API-fout, weigering, of geen tool-antwoord)."""


def extract_invoice(text: str) -> dict:
    """Zet ruwe factuurtekst om naar een gestructureerd dict volgens INVOICE_TOOL."""
    settings = get_settings()
    if not settings.ai_enabled:
        raise ExtractionError("ANTHROPIC_API_KEY ontbreekt; AI-extractie is niet beschikbaar.")

    client = anthropic.Anthropic(api_key=settings.anthropic_api_key)

    try:
        response = client.messages.create(
            model=settings.anthropic_model,
            max_tokens=4096,
            system=SYSTEM_PROMPT,
            tools=[INVOICE_TOOL],
            tool_choice={"type": "tool", "name": "record_invoice"},
            messages=[{"role": "user", "content": text}],
        )
    except anthropic.APIStatusError as exc:  # 4xx/5xx van de API
        raise ExtractionError(f"Claude API-fout ({exc.status_code}).") from exc
    except anthropic.APIConnectionError as exc:
        raise ExtractionError("Kon de Claude API niet bereiken.") from exc

    if response.stop_reason == "refusal":
        raise ExtractionError("Het model weigerde deze inhoud te verwerken.")

    for block in response.content:
        if block.type == "tool_use" and block.name == "record_invoice":
            return dict(block.input)

    raise ExtractionError("Geen gestructureerd antwoord ontvangen van het model.")
