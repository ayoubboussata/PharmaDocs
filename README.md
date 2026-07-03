<div align="center">

# PharmaDocs

**AI-gedreven documentverwerking & interne kennisassistent voor apotheken**

![.NET](https://img.shields.io/badge/.NET-8.0-512BD4)
![PostgreSQL](https://img.shields.io/badge/PostgreSQL-16%20%2B%20pgvector-4169E1)
![Python](https://img.shields.io/badge/Python-FastAPI-009688)
![React](https://img.shields.io/badge/React-Vite%20%2B%20TS-61DAFB)
![License](https://img.shields.io/badge/license-MIT-green)

</div>

## Overzicht

PharmaDocs automatiseert het administratieve papierwerk van een apotheekgroep. Facturen en bestelbonnen worden geüpload en automatisch omgezet naar gestructureerde data, en een interne chatbot beantwoordt procedurevragen op basis van de eigen documenten — met bronvermelding.

Het project is opgebouwd rond een duidelijke scheiding van verantwoordelijkheden: een **ASP.NET Core-backend** orkestreert en beheert de data, een **Python-microservice** verzorgt de AI-taken, en een **React-frontend** vormt de gebruikersinterface.

## Kernfunctionaliteit

**1 · Slimme documentverwerking**
Upload een factuur (PDF) → de tekst wordt geëxtraheerd → omgezet naar gestructureerde JSON (leverancier, factuurnummer, datum, subtotaal, btw-tarief en btw-bedrag, totaalbedrag, lijnitems) → opgeslagen in de databank → getoond in een doorzoekbare tabel met detailweergave.

**2 · Interne kennisassistent (RAG)**
Procedure-documenten worden geïndexeerd als vector-embeddings. Een chat-endpoint haalt de meest relevante fragmenten op en genereert een antwoord met **bronvermelding**, en geeft expliciet aan wanneer het antwoord niet in de documenten staat — cruciaal tegen hallucinaties in een medische context.

## Architectuur

```
React (Vite)  ──HTTP/JSON──►  ASP.NET Core API  ──►  PostgreSQL (+ pgvector)
                                     │
                                     │  (interne HTTP-call)
                                     ▼
                          Python FastAPI (AI-service)
                                     │
                                     ▼
                            Anthropic Claude API
```

De **.NET-backend is de enige poort naar de front-end** en orkestreert alles; de **Python-service doet uitsluitend het AI-werk**. Zo werken beide technologieën samen, elk in hun sterkte.

De backend volgt een gelaagde architectuur — `Controllers → Services → Repositories → DbContext` — met DTO's en dependency injection, zodat elke laag geïsoleerd en testbaar blijft.

## Technologie

| Laag | Technologie |
| --- | --- |
| Backend | ASP.NET Core Web API (.NET 8), Entity Framework Core, JWT-auth |
| Database | PostgreSQL 16 + pgvector |
| AI-service | Python + FastAPI |
| AI | Anthropic Claude API (extractie + chat) + Voyage AI (embeddings) |
| Front-end | React + Vite + TypeScript + Tailwind CSS |
| Infra | Docker Compose |

## Lokaal draaien

**Vereisten:** .NET 8 SDK, Docker Desktop.

```bash
# 1. Start PostgreSQL (met pgvector) via Docker
docker compose up -d

# 2. Start de API — migraties worden automatisch toegepast
cd backend/PharmaDocs.Api
dotnet run --launch-profile http
```

De API draait op `http://localhost:5035`, met Swagger op `http://localhost:5035/swagger`.

Voor het uploaden en extraheren van facturen draait ook de Python AI-service mee (zie [`ai-service/`](ai-service/)); de backend roept die intern aan op `http://localhost:8000`.

### Endpoints (huidige stand)

| Methode | Route | Auth | Omschrijving |
| --- | --- | --- | --- |
| `POST` | `/api/auth/register` | — | Account aanmaken, geeft een JWT terug |
| `POST` | `/api/auth/login` | — | Inloggen, geeft een JWT terug |
| `POST` | `/api/documents/upload` | 🔒 | Upload een factuur-PDF → interne AI-extractie → opgeslagen document |
| `GET` | `/api/documents` | 🔒 | Overzicht van verwerkte documenten |
| `GET` | `/api/documents/{id}` | 🔒 | Detail met geëxtraheerde factuur en lijnitems |
| `PUT` | `/api/documents/{id}/invoice` | 🔒 | Handmatige correctie van de factuurkop en lijnitems |
| `POST` | `/api/knowledge/documents` | 🔒 | Procedure-PDF indexeren (chunken → embeddings → pgvector) |
| `GET` | `/api/knowledge/sources` | 🔒 | Overzicht van geïndexeerde procedures |

> Bij een upload legt de backend het document eerst als `Pending` vast en roept dan de Python-service aan. Lukt de extractie, dan wordt het `Processed` met de factuurgegevens; faalt ze (AI onbereikbaar, onleesbare PDF), dan wordt het `Failed` met een foutboodschap — een upload gaat dus nooit verloren.

## Projectstructuur

```
PharmaDocs/
├── global.json               # pint het SDK op .NET 8
├── docker-compose.yml        # PostgreSQL 16 + pgvector
├── backend/
│   └── PharmaDocs.Api/        # ASP.NET Core Web API (orkestrator)
│       ├── Controllers/       # HTTP-endpoints
│       ├── Services/          # bedrijfslogica
│       ├── Repositories/      # data-toegang
│       ├── Data/              # EF Core DbContext
│       ├── Models/            # entiteiten
│       ├── DTOs/              # transferobjecten
│       └── Migrations/        # EF Core-migraties
├── ai-service/               # Python FastAPI (AI-taken)
└── frontend/                 # React + Vite + TypeScript
```

## Roadmap

- [x] **Fundament** — backend-skelet, datamodel, EF Core-migratie, gelaagde architectuur
- [x] **Authenticatie (JWT)** + front-end skelet — register/login, BCrypt, beveiligde endpoints, React-loginscherm
- [x] **Python AI-service** — FastAPI met PDF-tekstextractie (`POST /extract`)
- [x] **AI-factuurextractie** — `POST /extract-invoice`: PDF → Claude → gestructureerde JSON (strikte tool-use)
- [x] **Koppeling `.NET ↔ Python`** — `POST /api/documents/upload`: backend orkestreert de AI-call en slaat het resultaat op in de DB
- [x] **Front-end upload & overzicht** — drag & drop-upload met laadindicator, overzichtstabel met statussen (Verwerkt/Mislukt/In behandeling)
- [x] **Detailweergave + handmatige correctie** — bewerkbare factuurkop en lijnitems, filter/zoeken op leverancier/status
- [x] **RAG-indexering** — procedures chunken → Voyage-embeddings → opslaan in pgvector
- [ ] RAG-chat met bronvermelding (retrieval + Claude)

## Licentie

MIT
