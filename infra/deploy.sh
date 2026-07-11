#!/usr/bin/env bash
#
# PharmaDocs → Azure Container Apps (Infra-as-Code via Bicep).
#
# De infrastructuur zelf staat declaratief in `infra/main.bicep`. Dit script is
# een dunne wrapper die enkel doet wat Bicep NIET kan: de resource group klaarzetten,
# de Docker-images bouwen en naar de registry pushen, en dan de Bicep-template
# uitrollen. Daarna leest het de outputs (URL, admin-login) uit.
#
# Vereist:  az CLI (ingelogd via `az login`) + een draaiende lokale Docker, en de
#           sleutels als omgevingsvariabelen:
#     export ANTHROPIC_API_KEY=sk-ant-...
#     export VOYAGE_API_KEY=pa-...
#
# Gebruik:  bash infra/deploy.sh
#
set -euo pipefail

: "${ANTHROPIC_API_KEY:?zet ANTHROPIC_API_KEY}"
: "${VOYAGE_API_KEY:?zet VOYAGE_API_KEY}"

# ── Instellingen (overschrijfbaar via omgevingsvariabelen) ───────────────
LOCATION="${LOCATION:-swedencentral}"
ACR_LOCATION="${ACR_LOCATION:-$LOCATION}"   # ACR mag in een andere toegelaten regio (Student)
SUFFIX="${SUFFIX:-$(openssl rand -hex 3)}"
RG="${RG:-rg-pharmadocs}"
ACR="crpharmadocs${SUFFIX}"          # globaal uniek, enkel letters/cijfers
PG="pg-pharmadocs-${SUFFIX}"
PG_PASSWORD="$(openssl rand -base64 24 | tr -dc 'A-Za-z0-9')Aa1!"
JWT_KEY="$(openssl rand -base64 48)"
# Eerste admin (tenant-admin van de default-apotheek). Wachtwoord gegenereerd + bewaard.
ADMIN_EMAIL="${ADMIN_EMAIL:-admin@pharmadocs.be}"
ADMIN_PASSWORD="$(openssl rand -base64 18 | tr -dc 'A-Za-z0-9')Aa1!"
# Operator (SystemAdmin) die apotheken (tenants) aanmaakt. Idem gegenereerd + bewaard.
OPERATOR_EMAIL="${OPERATOR_EMAIL:-operator@pharmadocs.be}"
OPERATOR_PASSWORD="$(openssl rand -base64 18 | tr -dc 'A-Za-z0-9')Aa1!"

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

# De registry moet bestaan vóór we images kunnen pushen — Bicep kan niet bouwen/pushen.
# main.bicep beschrijft de ACR ook (idempotent), zodat de infra volledig als code staat.
echo "▶ Container Registry bootstrappen ($ACR)"
az acr create -g "$RG" -n "$ACR" -l "$ACR_LOCATION" --sku Basic --admin-enabled true -o none
ACR_SERVER=$(az acr show -n "$ACR" --query loginServer -o tsv)

echo "▶ Docker-images bouwen en naar de registry pushen"
az acr login -n "$ACR" -o none
for svc in "api:backend/PharmaDocs.Api" "ai:ai-service" "web:frontend"; do
  name="pharmadocs-${svc%%:*}"
  ctx="${svc#*:}"
  echo "  · $name"
  docker build -q -t "$ACR_SERVER/$name:latest" "$ROOT/$ctx"
  docker push -q "$ACR_SERVER/$name:latest"
done

echo "▶ Infrastructuur uitrollen via Bicep (main.bicep)"
DEPLOYMENT="pharmadocs-$(date +%Y%m%d%H%M%S)"
az deployment group create -g "$RG" -n "$DEPLOYMENT" \
  --template-file "$ROOT/infra/main.bicep" \
  --parameters \
    location="$LOCATION" acrLocation="$ACR_LOCATION" \
    acrName="$ACR" pgServerName="$PG" imageTag=latest \
    adminEmail="$ADMIN_EMAIL" operatorEmail="$OPERATOR_EMAIL" \
    anthropicApiKey="$ANTHROPIC_API_KEY" voyageApiKey="$VOYAGE_API_KEY" \
    pgAdminPassword="$PG_PASSWORD" jwtKey="$JWT_KEY" \
    adminPassword="$ADMIN_PASSWORD" operatorPassword="$OPERATOR_PASSWORD" \
  -o none

WEB_URL=$(az deployment group show -g "$RG" -n "$DEPLOYMENT" --query properties.outputs.webUrl.value -o tsv)
WEB_FQDN="${WEB_URL#https://}"

# Login-gegevens lokaal bewaren (gitignored) — je hebt de admin nodig om in te loggen.
{
  echo "WEB_FQDN=$WEB_FQDN"
  echo "ADMIN_EMAIL=$ADMIN_EMAIL"
  echo "ADMIN_PASSWORD=$ADMIN_PASSWORD"
  echo "OPERATOR_EMAIL=$OPERATOR_EMAIL"
  echo "OPERATOR_PASSWORD=$OPERATOR_PASSWORD"
  echo "PG=$PG"
} >> "$ROOT/infra/.deploy-secrets"

echo ""
echo "✅ Klaar!  Open:  $WEB_URL"
echo "   Resource group: $RG   ·   Registry: $ACR   ·   DB: $PG"
echo "   Admin-login:    $ADMIN_EMAIL"
echo "   Operator-login: $OPERATOR_EMAIL   (maakt apotheken aan)"
echo "   Wachtwoorden opgeslagen in infra/.deploy-secrets"
