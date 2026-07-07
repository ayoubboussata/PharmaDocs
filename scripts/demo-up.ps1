# PharmaDocs - demo opwarmen (PowerShell / Windows).
# Start de (gepauzeerde) PostgreSQL en wekt de Container Apps (scale-to-zero)
# door de front-end een paar keer op te vragen. Draai ~2 min voor een demo, op wifi.
#
# Vereist:  Azure CLI (ingelogd via `az login`).
# Gebruik:  .\scripts\demo-up.ps1
$ErrorActionPreference = 'Stop'

$RG = if ($env:RG) { $env:RG } else { 'rg-pharmadocs' }
$root = Split-Path -Parent $PSScriptRoot
$secretsFile = Join-Path $root 'infra\.deploy-secrets'

# az lokaliseren: PATH, anders het standaard installatiepad.
$az = (Get-Command az -ErrorAction SilentlyContinue).Source
if (-not $az) { $az = 'C:\Program Files\Microsoft SDKs\Azure\CLI2\wbin\az.cmd' }
if (-not (Test-Path $az)) { throw 'Azure CLI (az) niet gevonden. Installeer az of log in met `az login`.' }

# Servernaam + web-URL uit infra/.deploy-secrets (gitignored) lezen; enkel deze twee.
function Read-Secret([string]$key) {
  if (Test-Path $secretsFile) {
    $line = Select-String -Path $secretsFile -Pattern "^$key=" | Select-Object -First 1
    if ($line) { return ($line.Line -split '=', 2)[1] }
  }
  return $null
}
$PG = if ($env:PG) { $env:PG } else { Read-Secret 'PG' }
$WEB = if ($env:WEB_FQDN) { $env:WEB_FQDN } else { Read-Secret 'WEB_FQDN' }
if (-not $PG) { throw 'PG onbekend - zet $env:PG of zorg dat infra/.deploy-secrets bestaat.' }
if (-not $WEB) { throw 'WEB_FQDN onbekend - zet $env:WEB_FQDN of zorg dat infra/.deploy-secrets bestaat.' }

Write-Host "> PostgreSQL ($PG) starten indien gestopt..."
$state = & $az postgres flexible-server show -g $RG -n $PG --query state -o tsv
if ($state -eq 'Stopped') {
  & $az postgres flexible-server start -g $RG -n $PG -o none
  Write-Host '  gestart (~1-2 min opstarttijd).'
} else {
  Write-Host "  status is al '$state' - niets te doen."
}

Write-Host '> Front-end opwarmen (scale-to-zero wekken)...'
for ($i = 1; $i -le 4; $i++) {
  try {
    $r = Invoke-WebRequest -Uri "https://$WEB/" -UseBasicParsing -TimeoutSec 60
    Write-Host "  poging $i -> HTTP $($r.StatusCode)"
    if ($r.StatusCode -eq 200) { break }
  } catch {
    Write-Host "  poging $i -> nog niet bereikbaar"
  }
  Start-Sleep -Seconds 5
}

Write-Host ''
Write-Host "Klaar voor de demo:  https://$WEB"
Write-Host 'Login met de admin uit infra/.deploy-secrets.'
Write-Host 'Na de demo pauzeren:  .\scripts\demo-down.ps1'
