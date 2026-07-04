# Deployment — Azure Container Apps

PharmaDocs draait op Azure als drie containers in één Container Apps-omgeving, met een beheerde PostgreSQL:

- **`pharmadocs-web`** (publiek) — de React-SPA achter nginx; proxyt `/api` naar de backend.
- **`pharmadocs-api`** (intern) — de .NET-backend (orchestrator).
- **`pharmadocs-ai`** (intern) — de Python AI-service.
- **PostgreSQL Flexible Server** (16) met de `pgvector`-extensie toegelaten.

Enkel de front-end is publiek; backend en AI-service zijn **intern** (enkel bereikbaar binnen de omgeving). De front-end blijft same-origin met de API (nginx-reverse-proxy) → geen CORS.

## Waarom Container Apps

- **Schaalt naar nul** bij inactiviteit → nagenoeg gratis voor een demo (je betaalt enkel de PostgreSQL en de Container Registry).
- Past bij de microservice-opzet: elke dienst apart schaalbaar, met interne service-to-service-communicatie.
- TLS wordt aan de ingress geregeld; de containers spreken intern gewoon HTTP/HTTPS.

## Vereisten

- [Azure CLI](https://learn.microsoft.com/cli/azure/install-azure-cli) — ingelogd met `az login`
- Een actief Azure-abonnement
- De AI-sleutels als omgevingsvariabelen

## Uitrollen

```bash
export ANTHROPIC_API_KEY=sk-ant-...
export VOYAGE_API_KEY=pa-...

bash infra/deploy.sh
```

Het script (zie [`infra/deploy.sh`](../infra/deploy.sh)):

1. maakt de resource group, Container Registry en bouwt de drie images in de cloud (`az acr build` — geen lokale Docker nodig);
2. maakt de PostgreSQL Flexible Server en zet `azure.extensions = VECTOR` (nodig voor `CREATE EXTENSION vector`);
3. maakt de Container Apps-omgeving en de drie apps, met de sleutels en de DB-connectiestring als **secrets**;
4. koppelt de interne URL's (front-end → backend → AI-service) automatisch.

Op het einde print het de publieke URL. De backend past de EF Core-migraties automatisch toe bij het opstarten.

## Configuratie in productie

| Instelling | Bron |
| --- | --- |
| `ConnectionStrings__DefaultConnection` | secret op `pharmadocs-api` (met `Ssl Mode=Require`) |
| `Jwt__Key` | secret op `pharmadocs-api` (willekeurig gegenereerd) |
| `AiService__BaseUrl` | interne URL van `pharmadocs-ai` |
| `ANTHROPIC_API_KEY`, `VOYAGE_API_KEY` | secrets op `pharmadocs-ai` |
| `API_URL`, `API_HOST` | interne URL van `pharmadocs-api` (voor de nginx-proxy) |

De .NET-config leest deze omgevingsvariabelen automatisch (`__` = geneste sectie); er staan **geen** sleutels in de images of in Git.

## Kosten drukken

- Container Apps staan op `min-replicas 0` → schalen naar nul bij inactiviteit.
- De grootste vaste kost is PostgreSQL. Tussen demo's:
  ```bash
  az postgres flexible-server stop  -g rg-pharmadocs -n <pg-naam>
  az postgres flexible-server start -g rg-pharmadocs -n <pg-naam>
  ```

## Opruimen

```bash
az group delete -n rg-pharmadocs --yes --no-wait
```
