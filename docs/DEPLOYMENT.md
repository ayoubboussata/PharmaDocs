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
- **Een draaiende lokale Docker** — het script bouwt de images lokaal en pusht ze naar de registry (server-side `az acr build` is op sommige abonnementen, o.a. *Azure for Students*, niet beschikbaar)
- De AI-sleutels als omgevingsvariabelen

> **Regio's.** Sommige abonnementen (o.a. *Azure for Students*) beperken de toegelaten regio's, en niet elke dienst is in elke toegelaten regio beschikbaar. Zet `LOCATION` op een toegelaten regio; staat een dienst (bv. PostgreSQL) die regio niet toe, kies dan een andere toegelaten regio voor dat onderdeel. De containers kunnen cross-region uit de registry pullen.

## Uitrollen (Infra-as-Code met Bicep)

De infrastructuur staat declaratief in [`infra/main.bicep`](../infra/main.bicep) — één bestand dat álle Azure-resources beschrijft (registry, PostgreSQL + pgvector, Container Apps-omgeving, de drie apps met hun ingress/secrets/env). Het script [`infra/deploy.sh`](../infra/deploy.sh) is een **dunne wrapper** die enkel doet wat Bicep niet kan.

```bash
export ANTHROPIC_API_KEY=sk-ant-...
export VOYAGE_API_KEY=pa-...

bash infra/deploy.sh
```

De wrapper:

1. registreert de resource-providers en maakt de resource group;
2. **bootstrapt de Container Registry** (moet bestaan vóór je kunt pushen) en **bouwt + pusht de drie images lokaal** met Docker (`az acr build` is op *Azure for Students* niet beschikbaar);
3. rolt `main.bicep` uit via `az deployment group create` — dat maakt PostgreSQL (met `azure.extensions = VECTOR`), de Container Apps-omgeving en de drie apps, met de sleutels + DB-connectiestring als **secrets**;
4. leest de outputs uit (publieke URL, admin-login) en bewaart ze in `infra/.deploy-secrets`.

De interne URL's (front-end → backend → AI-service) koppelt Bicep zelf via resource-referenties (`aiApp.properties.configuration.ingress.fqdn`), geen handmatige stappen nodig. De backend past de EF Core-migraties automatisch toe bij het opstarten.

> **Waarom Bicep i.p.v. pure `az`-commando's:** de volledige infra is nu versioneerbaar, herhaalbaar en te previewen met `az deployment group what-if` vóór je iets wijzigt. De ACR staat ook in de template (idempotent), zodat de infra volledig als code beschreven is; de bootstrap in de wrapper is enkel nodig omdat images niet naar een onbestaande registry gepusht kunnen worden.
>
> **Alleen de infra opnieuw uitrollen** (zonder images te herbouwen), bv. na een `main.bicep`-wijziging:
> ```bash
> az deployment group create -g rg-pharmadocs --template-file infra/main.bicep \
>   --parameters acrName=<acr> pgServerName=<pg> imageTag=<tag> \
>   anthropicApiKey=... voyageApiKey=... pgAdminPassword=... jwtKey=... adminPassword=...
> ```

## Configuratie in productie

| Instelling | Bron |
| --- | --- |
| `ConnectionStrings__DefaultConnection` | secret op `pharmadocs-api` (met `Ssl Mode=VerifyFull` — certificaat + hostname gevalideerd) |
| `Jwt__Key` | secret op `pharmadocs-api` (willekeurig gegenereerd) |
| `Seed__AdminEmail` / `Seed__AdminPassword` | eerste admin (registratie is admin-only); wachtwoord als secret, login bewaard in `infra/.deploy-secrets` |
| `AiService__BaseUrl` | interne URL van `pharmadocs-ai` |
| `ANTHROPIC_API_KEY`, `VOYAGE_API_KEY` | secrets op `pharmadocs-ai` |
| `API_URL`, `API_HOST` | interne URL van `pharmadocs-api` (voor de nginx-proxy) |

De .NET-config leest deze omgevingsvariabelen automatisch (`__` = geneste sectie); er staan **geen** sleutels in de images of in Git.

## Beschikbaarheid vs. kosten

Twee knoppen bepalen de afweging:

- **Container Apps `minReplicas`** — op **1** (huidige keuze in `main.bicep`) blijft er altijd één replica warm → **geen cold start**, 24/7 direct beschikbaar. Op **0** schalen ze naar nul bij inactiviteit (~5 min) → goedkoper, maar de eerste bezoeker daarna krijgt een korte cold start.
- **PostgreSQL** — de grootste vaste kost. Je kunt ze pauzeren tussen demo's (goedkoop, maar de start vanuit `Stopped` duurt op het Burstable-tier **variabel/soms lang**), of gewoon **aan laten** tijdens een actieve periode (betrouwbaar, ~€15/maand).

> Voor een sollicitatieperiode: alles aan laten = altijd meteen live. Voor lange inactieve periodes: DB pauzeren en `minReplicas` op 0 zetten.

De hulpscripts pauzeren/hervatten de DB (ze lezen de servernaam uit `infra/.deploy-secrets`):

```bash
# macOS / Linux / Git Bash
bash scripts/demo-up.sh     # start de DB + warmt de front-end op → klaar om te tonen
bash scripts/demo-down.sh   # pauzeert de DB weer (containers schalen zelf naar nul)
```

```powershell
# Windows / PowerShell (bash niet nodig)
.\scripts\demo-up.ps1
.\scripts\demo-down.ps1
```

> Blokkeert PowerShell het script (execution policy)? Draai eenmalig in die sessie:
> `Set-ExecutionPolicy -Scope Process -ExecutionPolicy Bypass`, of gebruik
> `powershell -ExecutionPolicy Bypass -File scripts\demo-up.ps1`.

Handmatig kan ook:
```bash
az postgres flexible-server stop  -g rg-pharmadocs -n <pg-naam>
az postgres flexible-server start -g rg-pharmadocs -n <pg-naam>
```

## De database afschermen (productie)

Standaard maakt het script de PostgreSQL met `--public-access 0.0.0.0`: een publiek endpoint dat enkel Azure-diensten toelaat, afgeschermd door het wachtwoord. Voldoende voor een demo, maar niet ideaal.

**De firewall beperken tot enkel de Container Apps-omgeving werkt niet betrouwbaar op het Consumption-plan** — dat heeft geen stabiel, kenbaar uitgaand IP (het `staticIp` van de omgeving is het *inkomende* IP; egress gebeurt via een wisselende set IP's). Empirisch getest: de DB firewallen op het `staticIp` verbrak de connectie.

**De juiste productie-oplossing is private networking**: de DB krijgt géén publiek endpoint en is enkel bereikbaar binnen een VNet.

```bash
# 1. VNet met een subnet voor de apps (min. /23) en een gedelegeerd subnet voor de DB
az network vnet create -g rg-pharmadocs -n vnet-pharmadocs \
  --address-prefixes 10.20.0.0/16 \
  --subnet-name snet-apps --subnet-prefixes 10.20.0.0/23
az network vnet subnet create -g rg-pharmadocs --vnet-name vnet-pharmadocs -n snet-db \
  --address-prefixes 10.20.2.0/24 \
  --delegations Microsoft.DBforPostgreSQL/flexibleServers

# 2. PostgreSQL met PRIVATE access (geen publiek endpoint) + eigen private DNS-zone
az postgres flexible-server create -g rg-pharmadocs -n <pg-naam> -l <regio> \
  --admin-user pharmadocs --admin-password <sterk-wachtwoord> \
  --tier Burstable --sku-name Standard_B1ms --storage-size 32 --version 16 \
  --vnet vnet-pharmadocs --subnet snet-db \
  --private-dns-zone pharmadocs.private.postgres.database.azure.com --yes

# 3. Container Apps-omgeving VNet-geïntegreerd (egress via het apps-subnet)
SNET=$(az network vnet subnet show -g rg-pharmadocs --vnet-name vnet-pharmadocs -n snet-apps --query id -o tsv)
az containerapp env create -g rg-pharmadocs -n cae-pharmadocs -l <regio> \
  --infrastructure-subnet-resource-id "$SNET"
```

De web-app houdt gewoon publieke ingress; enkel het egress-verkeer (backend → DB) loopt privé binnen de VNet. De connectiestring blijft dezelfde host (`<pg-naam>.postgres.database.azure.com`) — die resolvet nu binnen de VNet naar het private adres. `Ssl Mode=VerifyFull` blijft van toepassing.

> Op een *Azure for Students*-abonnement kan VNet-geïntegreerde Container Apps tegen regio-/feature-limieten aanlopen; test dit op een regulier abonnement.

## Opruimen

```bash
az group delete -n rg-pharmadocs --yes --no-wait
```
