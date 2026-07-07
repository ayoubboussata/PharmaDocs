#!/usr/bin/env bash
#
# PharmaDocs — demo opwarmen.
# Start de (gepauzeerde) PostgreSQL en wekt de Container Apps (scale-to-zero)
# door de front-end een paar keer op te vragen. Draai dit ~2 min vóór een demo,
# op wifi. Zo blijft de omgeving in rust bijna gratis, maar is ze snel wanneer je
# ze nodig hebt.
#
# Vereist:  az CLI (ingelogd via `az login`).
# Gebruik:  bash scripts/demo-up.sh
#
set -euo pipefail

RG="${RG:-rg-pharmadocs}"
ROOT="$(cd "$(dirname "$0")/.." && pwd)"
SECRETS="$ROOT/infra/.deploy-secrets"

# Instance-specifieke namen komen uit infra/.deploy-secrets (gitignored), met
# fallback op omgevingsvariabelen. We lezen enkel PG en WEB_FQDN — geen wachtwoorden.
read_secret() { grep -E "^$1=" "$SECRETS" 2>/dev/null | head -1 | cut -d= -f2-; }
PG="${PG:-$(read_secret PG)}"
WEB_FQDN="${WEB_FQDN:-$(read_secret WEB_FQDN)}"
: "${PG:?zet PG (of zorg dat infra/.deploy-secrets bestaat)}"
: "${WEB_FQDN:?zet WEB_FQDN (of zorg dat infra/.deploy-secrets bestaat)}"

echo "▶ PostgreSQL ($PG) starten indien gestopt…"
state=$(az postgres flexible-server show -g "$RG" -n "$PG" --query state -o tsv)
if [ "$state" = "Stopped" ]; then
  az postgres flexible-server start -g "$RG" -n "$PG" -o none
  echo "  gestart (~1-2 min opstarttijd)."
else
  echo "  status is al '$state' — niets te doen."
fi

echo "▶ Front-end opwarmen (scale-to-zero wekken)…"
for i in 1 2 3 4; do
  code=$(curl -s -o /dev/null -w "%{http_code}" --max-time 60 "https://$WEB_FQDN/" || echo "000")
  echo "  poging $i → HTTP $code"
  [ "$code" = "200" ] && break
  sleep 5
done

echo ""
echo "✅ Klaar voor de demo:  https://$WEB_FQDN"
echo "   Login met de admin uit infra/.deploy-secrets."
echo "   Na de demo pauzeren:  bash scripts/demo-down.sh"
