# PharmaDocs - demo pauzeren (PowerShell / Windows).
# Stopt de PostgreSQL om studententegoed te sparen. De Container Apps schalen
# zelf naar nul na ~5 min inactiviteit.
#
# Vereist:  Azure CLI (ingelogd via `az login`).
# Gebruik:  .\scripts\demo-down.ps1
$ErrorActionPreference = 'Stop'

$RG = if ($env:RG) { $env:RG } else { 'rg-pharmadocs' }
$root = Split-Path -Parent $PSScriptRoot
$secretsFile = Join-Path $root 'infra\.deploy-secrets'

$az = (Get-Command az -ErrorAction SilentlyContinue).Source
if (-not $az) { $az = 'C:\Program Files\Microsoft SDKs\Azure\CLI2\wbin\az.cmd' }
if (-not (Test-Path $az)) { throw 'Azure CLI (az) niet gevonden. Installeer az of log in met `az login`.' }

function Read-Secret([string]$key) {
  if (Test-Path $secretsFile) {
    $line = Select-String -Path $secretsFile -Pattern "^$key=" | Select-Object -First 1
    if ($line) { return ($line.Line -split '=', 2)[1] }
  }
  return $null
}
$PG = if ($env:PG) { $env:PG } else { Read-Secret 'PG' }
if (-not $PG) { throw 'PG onbekend - zet $env:PG of zorg dat infra/.deploy-secrets bestaat.' }

$state = & $az postgres flexible-server show -g $RG -n $PG --query state -o tsv
if ($state -eq 'Stopped') {
  Write-Host "PostgreSQL ($PG) is al gestopt."
} else {
  Write-Host "> PostgreSQL ($PG) stoppen..."
  & $az postgres flexible-server stop -g $RG -n $PG -o none
  Write-Host 'Gepauzeerd. De containers schalen zelf naar nul.'
  Write-Host 'Azure start een gestopte server na 7 dagen automatisch terug op.'
}
