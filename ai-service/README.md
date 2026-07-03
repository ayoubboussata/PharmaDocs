# PharmaDocs — AI-service

Interne Python-microservice (FastAPI) voor de AI-taken. Wordt **enkel intern** door de .NET-backend aangeroepen, nooit rechtstreeks door de front-end.

## Verantwoordelijkheid

| Fase | Endpoint | Taak |
| --- | --- | --- |
| 2 (Dag 3) | `POST /extract` | PDF ontvangen → ruwe tekst extraheren (pdfplumber) |
| 2 (Dag 4) | `POST /extract-invoice` | PDF → tekst → Claude → gestructureerde JSON (strikte tool-use) |
| 4 | `POST /chat` e.a. | RAG: embeddings + chat met bronvermelding *(volgt)* |

## Lokaal draaien

```bash
cd ai-service
python -m venv .venv
.venv/Scripts/activate        # Windows (PowerShell/Git Bash)
pip install -r requirements.txt
uvicorn app.main:app --reload --port 8000
```

Interactieve docs: `http://localhost:8000/docs`.

## Endpoints

| Methode | Route | Omschrijving |
| --- | --- | --- |
| `GET` | `/health` | Liveness-check (+ `aiEnabled`, `model`) |
| `POST` | `/extract` | PDF → `{ fileName, pageCount, characterCount, text }` |
| `POST` | `/extract-invoice` | PDF → `{ fileName, pageCount, invoice: { supplierName, invoiceNumber, invoiceDate, totalAmount, currency, lineItems[] } }` |

Foutcodes: `415` (geen PDF), `413` (> 10 MB), `422` (onleesbaar / geen tekstlaag), `400` (leeg), `503` (AI niet beschikbaar / upstream-fout).

## Configuratie

Kopieer `.env.example` naar `.env` en zet je `ANTHROPIC_API_KEY` (nodig vanaf Dag 4). De `.env` staat in `.gitignore`.

## Sample-data

Fictieve test-PDF's staan in `../sample-data/` (facturen + procedures), te (her)genereren met:

```bash
python ../scripts/generate_samples.py
```
