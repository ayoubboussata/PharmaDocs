// PharmaDocs — Infra-as-Code (Azure Container Apps + PostgreSQL).
//
// Beschrijft de volledige cloud-infrastructuur declaratief. Vervangt de
// imperatieve `az`-commando's uit deploy.sh. Wat Bicep NIET kan — Docker-images
// bouwen en pushen — blijft in de wrapper `deploy.sh` (de ACR moet bestaan
// vóór je kunt pushen, daarom bootstrapt de wrapper de registry eerst).
//
// Uitrollen:  az deployment group create -g <rg> --template-file infra/main.bicep --parameters ...

targetScope = 'resourceGroup'

// ── Parameters ───────────────────────────────────────────────────────────
@description('Regio voor alle resources (behalve eventueel de ACR).')
param location string = resourceGroup().location

@description('Regio voor de Container Registry (Student-abonnementen laten niet elke regio toe).')
param acrLocation string = location

@description('Voorvoegsel voor resourcenamen.')
param namePrefix string = 'pharmadocs'

@description('Globaal unieke naam voor de Container Registry (enkel letters/cijfers).')
param acrName string

@description('Naam van de PostgreSQL Flexible Server (globaal uniek).')
param pgServerName string

@description('Container-image-tag om uit te rollen (bv. de git-SHA of "latest").')
param imageTag string = 'latest'

param pgAdminUser string = 'pharmadocs'
param pgDatabaseName string = 'pharmadocs'

@description('E-mail van de eerste admin (tenant-admin van de default-apotheek).')
param adminEmail string = 'admin@pharmadocs.be'

@description('E-mail van de operator (SystemAdmin) die apotheken (tenants) aanmaakt.')
param operatorEmail string = 'operator@pharmadocs.be'

@secure()
param anthropicApiKey string
@secure()
param voyageApiKey string
@secure()
param pgAdminPassword string
@secure()
param jwtKey string
@secure()
param adminPassword string
@secure()
param operatorPassword string

// ── Afgeleide namen ──────────────────────────────────────────────────────
var envName = 'cae-${namePrefix}'
var lawName = 'law-${namePrefix}'
var aiAppName = '${namePrefix}-ai'
var apiAppName = '${namePrefix}-api'
var webAppName = '${namePrefix}-web'

// ── Container Registry ───────────────────────────────────────────────────
resource acr 'Microsoft.ContainerRegistry/registries@2023-07-01' = {
  name: acrName
  location: acrLocation
  sku: {
    name: 'Basic'
  }
  properties: {
    adminUserEnabled: true
  }
}

// ── PostgreSQL Flexible Server (+ pgvector, firewall, database) ───────────
resource pg 'Microsoft.DBforPostgreSQL/flexibleServers@2024-08-01' = {
  name: pgServerName
  location: location
  sku: {
    name: 'Standard_B1ms'
    tier: 'Burstable'
  }
  properties: {
    version: '16'
    administratorLogin: pgAdminUser
    administratorLoginPassword: pgAdminPassword
    storage: {
      storageSizeGB: 32
    }
    highAvailability: {
      mode: 'Disabled'
    }
    backup: {
      backupRetentionDays: 7
      geoRedundantBackup: 'Disabled'
    }
  }
}

// pgvector toelaten — anders faalt `CREATE EXTENSION vector` in de EF-migratie.
resource pgVector 'Microsoft.DBforPostgreSQL/flexibleServers/configurations@2024-08-01' = {
  parent: pg
  name: 'azure.extensions'
  properties: {
    value: 'VECTOR'
    source: 'user-override'
  }
}

// Enkel Azure-diensten toelaten (0.0.0.0–0.0.0.0), afgeschermd door het wachtwoord.
resource pgFirewall 'Microsoft.DBforPostgreSQL/flexibleServers/firewallRules@2024-08-01' = {
  parent: pg
  name: 'AllowAllAzureServices'
  properties: {
    startIpAddress: '0.0.0.0'
    endIpAddress: '0.0.0.0'
  }
}

resource pgDatabase 'Microsoft.DBforPostgreSQL/flexibleServers/databases@2024-08-01' = {
  parent: pg
  name: pgDatabaseName
  properties: {
    charset: 'UTF8'
    collation: 'en_US.utf8'
  }
}

// ── Log Analytics + Container Apps-omgeving ──────────────────────────────
resource law 'Microsoft.OperationalInsights/workspaces@2023-09-01' = {
  name: lawName
  location: location
  properties: {
    sku: {
      name: 'PerGB2018'
    }
    retentionInDays: 30
  }
}

resource env 'Microsoft.App/managedEnvironments@2024-03-01' = {
  name: envName
  location: location
  properties: {
    appLogsConfiguration: {
      destination: 'log-analytics'
      logAnalyticsConfiguration: {
        customerId: law.properties.customerId
        sharedKey: law.listKeys().primarySharedKey
      }
    }
  }
}

// ── Herbruikbare registry-config voor de container-apps ──────────────────
var registryConfig = [
  {
    server: acr.properties.loginServer
    username: acr.listCredentials().username
    passwordSecretRef: 'acr-password'
  }
]
var acrPasswordSecret = {
  name: 'acr-password'
  value: acr.listCredentials().passwords[0].value
}

// De verbindingsstring valideert het servercertificaat én de hostname (VerifyFull).
var dbConnectionString = 'Host=${pg.properties.fullyQualifiedDomainName};Port=5432;Database=${pgDatabaseName};Username=${pgAdminUser};Password=${pgAdminPassword};Ssl Mode=VerifyFull'

// ── AI-service (interne ingress) ─────────────────────────────────────────
resource aiApp 'Microsoft.App/containerApps@2024-03-01' = {
  name: aiAppName
  location: location
  properties: {
    managedEnvironmentId: env.id
    configuration: {
      activeRevisionsMode: 'Single'
      ingress: {
        external: false
        targetPort: 8000
        transport: 'auto'
      }
      registries: registryConfig
      secrets: [
        acrPasswordSecret
        {
          name: 'anthropic-key'
          value: anthropicApiKey
        }
        {
          name: 'voyage-key'
          value: voyageApiKey
        }
      ]
    }
    template: {
      containers: [
        {
          name: 'ai'
          image: '${acr.properties.loginServer}/${namePrefix}-ai:${imageTag}'
          resources: {
            cpu: json('0.5')
            memory: '1Gi'
          }
          env: [
            {
              name: 'ANTHROPIC_API_KEY'
              secretRef: 'anthropic-key'
            }
            {
              name: 'VOYAGE_API_KEY'
              secretRef: 'voyage-key'
            }
          ]
        }
      ]
      scale: {
        // Altijd minstens 1 replica warm → geen cold start (24/7 direct beschikbaar).
        // Zet op 0 om naar nul te schalen bij inactiviteit (goedkoper, wel cold start).
        minReplicas: 1
        maxReplicas: 2
      }
    }
  }
}

// ── Backend (interne ingress, orchestrator) ──────────────────────────────
resource apiApp 'Microsoft.App/containerApps@2024-03-01' = {
  name: apiAppName
  location: location
  dependsOn: [
    pgVector
    pgFirewall
    pgDatabase
  ]
  properties: {
    managedEnvironmentId: env.id
    configuration: {
      activeRevisionsMode: 'Single'
      ingress: {
        external: false
        targetPort: 8080
        transport: 'auto'
      }
      registries: registryConfig
      secrets: [
        acrPasswordSecret
        {
          name: 'db-conn'
          value: dbConnectionString
        }
        {
          name: 'jwt-key'
          value: jwtKey
        }
        {
          name: 'admin-password'
          value: adminPassword
        }
        {
          name: 'operator-password'
          value: operatorPassword
        }
      ]
    }
    template: {
      containers: [
        {
          name: 'api'
          image: '${acr.properties.loginServer}/${namePrefix}-api:${imageTag}'
          resources: {
            cpu: json('0.5')
            memory: '1Gi'
          }
          env: [
            {
              name: 'ConnectionStrings__DefaultConnection'
              secretRef: 'db-conn'
            }
            {
              name: 'Jwt__Key'
              secretRef: 'jwt-key'
            }
            {
              name: 'Seed__AdminEmail'
              value: adminEmail
            }
            {
              name: 'Seed__AdminPassword'
              secretRef: 'admin-password'
            }
            {
              name: 'Seed__OperatorEmail'
              value: operatorEmail
            }
            {
              name: 'Seed__OperatorPassword'
              secretRef: 'operator-password'
            }
            {
              name: 'AiService__BaseUrl'
              value: 'https://${aiApp.properties.configuration.ingress.fqdn}'
            }
            {
              name: 'ASPNETCORE_ENVIRONMENT'
              value: 'Production'
            }
          ]
        }
      ]
      scale: {
        // Altijd minstens 1 replica warm → geen cold start (24/7 direct beschikbaar).
        // Zet op 0 om naar nul te schalen bij inactiviteit (goedkoper, wel cold start).
        minReplicas: 1
        maxReplicas: 2
      }
    }
  }
}

// ── Front-end (publieke ingress) ─────────────────────────────────────────
resource webApp 'Microsoft.App/containerApps@2024-03-01' = {
  name: webAppName
  location: location
  properties: {
    managedEnvironmentId: env.id
    configuration: {
      activeRevisionsMode: 'Single'
      ingress: {
        external: true
        targetPort: 80
        transport: 'auto'
      }
      registries: registryConfig
      secrets: [
        acrPasswordSecret
      ]
    }
    template: {
      containers: [
        {
          name: 'web'
          image: '${acr.properties.loginServer}/${namePrefix}-web:${imageTag}'
          resources: {
            cpu: json('0.5')
            memory: '1Gi'
          }
          env: [
            {
              name: 'API_URL'
              value: 'https://${apiApp.properties.configuration.ingress.fqdn}'
            }
            {
              name: 'API_HOST'
              value: apiApp.properties.configuration.ingress.fqdn
            }
          ]
        }
      ]
      scale: {
        // Altijd minstens 1 replica warm → geen cold start (24/7 direct beschikbaar).
        // Zet op 0 om naar nul te schalen bij inactiviteit (goedkoper, wel cold start).
        minReplicas: 1
        maxReplicas: 2
      }
    }
  }
}

// ── Outputs ──────────────────────────────────────────────────────────────
output webUrl string = 'https://${webApp.properties.configuration.ingress.fqdn}'
output apiFqdn string = apiApp.properties.configuration.ingress.fqdn
output aiFqdn string = aiApp.properties.configuration.ingress.fqdn
output acrLoginServer string = acr.properties.loginServer
