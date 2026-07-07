#!/usr/bin/env bash
#
# PharmaDocs — demo pauzeren.
# Stopt de PostgreSQL om studententegoed te sparen. De Container Apps schalen
# zelf naar nul na ~5 min inactiviteit, dus die hoeven niets.
#
# Vereist:  az CLI (ingelogd via `az login`).
# Gebruik:  bash scripts/demo-down.sh
#
set -euo pipefail

RG="${RG:-rg-pharmadocs}"
ROOT="$(cd "$(dirname "$0")/.." && pwd)"
SECRETS="$ROOT/infra/.deploy-secrets"

read_secret() { grep -E "^$1=" "$SECRETS" 2>/dev/null | head -1 | cut -d= -f2-; }
PG="${PG:-$(read_secret PG)}"
: "${PG:?zet PG (of zorg dat infra/.deploy-secrets bestaat)}"

state=$(az postgres flexible-server show -g "$RG" -n "$PG" --query state -o tsv)
if [ "$state" = "Stopped" ]; then
  echo "PostgreSQL ($PG) is al gestopt."
else
  echo "▶ PostgreSQL ($PG) stoppen…"
  az postgres flexible-server stop -g "$RG" -n "$PG" -o none
  echo "✅ Gepauzeerd. De containers schalen zelf naar nul."
  echo "   Azure start een gestopte server na 7 dagen automatisch terug op."
fi
