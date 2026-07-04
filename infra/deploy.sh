#!/usr/bin/env bash
#
# PharmaDocs → Azure Container Apps.
# Rolt de drie diensten (front-end, backend, AI-service) + PostgreSQL uit.
#
# Vereist:  az CLI (ingelogd via `az login`), en de sleutels als omgevingsvariabelen:
#     export ANTHROPIC_API_KEY=sk-ant-...
#     export VOYAGE_API_KEY=pa-...
#
# Gebruik:  bash infra/deploy.sh
#
set -euo pipefail

: "${ANTHROPIC_API_KEY:?zet ANTHROPIC_API_KEY}"
: "${VOYAGE_API_KEY:?zet VOYAGE_API_KEY}"

# ── Instellingen (overschrijfbaar via omgevingsvariabelen) ───────────────
LOCATION="${LOCATION:-westeurope}"
SUFFIX="${SUFFIX:-$(openssl rand -hex 3)}"
RG="${RG:-rg-pharmadocs}"
ACR="crpharmadocs${SUFFIX}"          # globaal uniek, enkel letters/cijfers
ENVIRONMENT="cae-pharmadocs"
PG="pg-pharmadocs-${SUFFIX}"
PG_ADMIN="pharmadocs"
PG_DB="pharmadocs"
PG_PASSWORD="$(openssl rand -base64 24 | tr -dc 'A-Za-z0-9')Aa1!"
JWT_KEY="$(openssl rand -base64 48)"
# Eerste admin (registratie is admin-only). Wachtwoord wordt gegenereerd en als
# Container Apps-secret bewaard; het adres is overschrijfbaar via ADMIN_EMAIL.
ADMIN_EMAIL="${ADMIN_EMAIL:-admin@pharmadocs.be}"
ADMIN_PASSWORD="$(openssl rand -base64 18 | tr -dc 'A-Za-z0-9')Aa1!"

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
# Op Windows (Git Bash) verstaat de native az-CLI geen /d/-paden → omzetten naar D:/.
if command -v cygpath >/dev/null 2>&1; then ROOT="$(cygpath -m "$ROOT")"; fi

echo "▶ Providers registreren (idempotent) — kan de eerste keer enkele minuten duren"
for ns in Microsoft.ContainerRegistry Microsoft.App Microsoft.OperationalInsights Microsoft.DBforPostgreSQL; do
  az provider register --namespace "$ns" --wait -o none
done
az extension add --name containerapp --upgrade --only-show-errors -o none 2>/dev/null || true

echo "▶ Resource group ($RG)"
az group create -n "$RG" -l "$LOCATION" -o none

echo "▶ Container Registry ($ACR)"
az acr create -g "$RG" -n "$ACR" --sku Basic --admin-enabled true -o none
ACR_SERVER=$(az acr show -n "$ACR" --query loginServer -o tsv)

# Lokaal bouwen + pushen (server-side `az acr build` is niet beschikbaar op o.a.
# Student-abonnementen). Vereist een draaiende lokale Docker.
echo "▶ Docker-images bouwen en naar de registry pushen"
az acr login -n "$ACR" -o none
for svc in "api:backend/PharmaDocs.Api" "ai:ai-service" "web:frontend"; do
  name="pharmadocs-${svc%%:*}"
  ctx="${svc#*:}"
  echo "  · $name"
  docker build -q -t "$ACR_SERVER/$name:latest" "$ROOT/$ctx"
  docker push -q "$ACR_SERVER/$name:latest"
done

echo "▶ PostgreSQL Flexible Server ($PG) — kan enkele minuten duren"
az postgres flexible-server create -g "$RG" -n "$PG" -l "$LOCATION" \
  --admin-user "$PG_ADMIN" --admin-password "$PG_PASSWORD" \
  --tier Burstable --sku-name Standard_B1ms --storage-size 32 --version 16 \
  --public-access 0.0.0.0 --yes -o none
# pgvector toelaten — anders faalt `CREATE EXTENSION vector` in de EF-migratie.
az postgres flexible-server parameter set -g "$RG" -s "$PG" --name azure.extensions --value VECTOR -o none

echo "▶ Container Apps-omgeving ($ENVIRONMENT)"
az containerapp env create -g "$RG" -n "$ENVIRONMENT" -l "$LOCATION" -o none

ACR_USER=$(az acr credential show -n "$ACR" --query username -o tsv)
ACR_PASS=$(az acr credential show -n "$ACR" --query 'passwords[0].value' -o tsv)
REG=(--registry-server "$ACR_SERVER" --registry-username "$ACR_USER" --registry-password "$ACR_PASS")

echo "▶ AI-service (interne ingress)"
az containerapp create -g "$RG" -n pharmadocs-ai --environment "$ENVIRONMENT" \
  --image "$ACR_SERVER/pharmadocs-ai:latest" "${REG[@]}" \
  --target-port 8000 --ingress internal --min-replicas 0 --max-replicas 2 \
  --secrets anthropic-key="$ANTHROPIC_API_KEY" voyage-key="$VOYAGE_API_KEY" \
  --env-vars ANTHROPIC_API_KEY=secretref:anthropic-key VOYAGE_API_KEY=secretref:voyage-key -o none
AI_FQDN=$(az containerapp show -g "$RG" -n pharmadocs-ai --query properties.configuration.ingress.fqdn -o tsv)

echo "▶ Backend (interne ingress)"
# Ssl Mode=VerifyFull valideert het servercertificaat én de hostname (geen MITM op de
# DB-link). Azure PostgreSQL gebruikt een publiek vertrouwd certificaat, dus de
# CA-bundle in de container volstaat — geen eigen root-certificaat nodig.
CONN="Host=${PG}.postgres.database.azure.com;Port=5432;Database=${PG_DB};Username=${PG_ADMIN};Password=${PG_PASSWORD};Ssl Mode=VerifyFull"
az containerapp create -g "$RG" -n pharmadocs-api --environment "$ENVIRONMENT" \
  --image "$ACR_SERVER/pharmadocs-api:latest" "${REG[@]}" \
  --target-port 8080 --ingress internal --min-replicas 0 --max-replicas 2 \
  --secrets db-conn="$CONN" jwt-key="$JWT_KEY" admin-password="$ADMIN_PASSWORD" \
  --env-vars ConnectionStrings__DefaultConnection=secretref:db-conn Jwt__Key=secretref:jwt-key \
             Seed__AdminEmail="$ADMIN_EMAIL" Seed__AdminPassword=secretref:admin-password \
             AiService__BaseUrl="https://$AI_FQDN" ASPNETCORE_ENVIRONMENT=Production -o none
API_FQDN=$(az containerapp show -g "$RG" -n pharmadocs-api --query properties.configuration.ingress.fqdn -o tsv)

echo "▶ Front-end (publieke ingress)"
az containerapp create -g "$RG" -n pharmadocs-web --environment "$ENVIRONMENT" \
  --image "$ACR_SERVER/pharmadocs-web:latest" "${REG[@]}" \
  --target-port 80 --ingress external --min-replicas 0 --max-replicas 2 \
  --env-vars API_URL="https://$API_FQDN" API_HOST="$API_FQDN" -o none
WEB_FQDN=$(az containerapp show -g "$RG" -n pharmadocs-web --query properties.configuration.ingress.fqdn -o tsv)

# Login-gegevens lokaal bewaren (gitignored) — je hebt de admin nodig om in te loggen
# en van daaruit accounts aan te maken (registratie is admin-only).
{
  echo "WEB_FQDN=$WEB_FQDN"
  echo "ADMIN_EMAIL=$ADMIN_EMAIL"
  echo "ADMIN_PASSWORD=$ADMIN_PASSWORD"
  echo "PG=$PG"
} >> "$ROOT/infra/.deploy-secrets"

echo ""
echo "✅ Klaar!  Open:  https://$WEB_FQDN"
echo "   Resource group: $RG   ·   Registry: $ACR   ·   DB: $PG"
echo "   Admin-login: $ADMIN_EMAIL   ·   wachtwoord opgeslagen in infra/.deploy-secrets"
