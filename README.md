# 🏗️ Arquitetura Técnica - FCG Identity Microservice

## 📋 Índice
- [🏗️ Arquitetura Técnica - FCG Identity Microservice](#️-arquitetura-técnica---fcg-identity-microservice)
  - [📋 Índice](#-índice)
  - [🎯 Visão Geral da Arquitetura](#-visão-geral-da-arquitetura)
  - [🏛️ Arquitetura de Sistema](#️-arquitetura-de-sistema)
    - [🔧 Componentes Principais](#-componentes-principais)
  - [📊 Monitoramento e Observabilidade](#-monitoramento-e-observabilidade)
    - [📈 Observability Stack Completa](#-observability-stack-completa)
    - [🔍 Log Flow \& Correlation com Elasticsearch](#-log-flow--correlation-com-elasticsearch)
    - [📊 Elasticsearch Configuration](#-elasticsearch-configuration)
  - [🔐 Fluxo de Autenticação](#-fluxo-de-autenticação)
    - [🚪 Processo de Login Completo](#-processo-de-login-completo)
  - [🛡️ Segurança e Rate Limiting](#️-segurança-e-rate-limiting)
    - [🚦 Rate Limiting Strategy](#-rate-limiting-strategy)
  - [⚙️ Especificações Técnicas](#️-especificações-técnicas)
    - [🏷️ Versões e Dependências](#️-versões-e-dependências)
    - [📊 Performance Specifications](#-performance-specifications)

---

## 🎯 Visão Geral da Arquitetura

O **FCG Identity Microservice** é um sistema de autenticação e autorização enterprise desenvolvido em **.NET 9** seguindo os princípios de **Clean Architecture** e **Domain-Driven Design (DDD)**. O sistema utiliza **Keycloak** como Identity Provider e está deployado em **AWS EKS** com **Kong Ingress Controller**.

```mermaid
graph TB
    subgraph "Client Layer"
        WEB[Web Application]
        MOB[Mobile App]
        API_CLIENT[API Client]
    end

    subgraph "API Gateway Layer"
        ALB[AWS Application Load Balancer]
        KONG[Kong Ingress Controller]
    end

    subgraph "Application Layer"
        IDENTITY[Identity API<br/>.NET 9]
        KC[Keycloak<br/>Identity Provider]
    end

    subgraph "Data Layer"
        KCDB[(Keycloak Database<br/>PostgreSQL)]
    end

    subgraph "Observability Layer"
        ELK[Elasticsearch<br/>Log Centralization]
        NR[New Relic APM<br/>Metrics & Tracing]
    end

    subgraph "Infrastructure Layer"
        EKS[AWS EKS Cluster]
        ECR[AWS ECR Registry]
    end

    WEB --> ALB
    MOB --> ALB
    API_CLIENT --> ALB
    ALB --> KONG
    KONG --> IDENTITY
    KONG --> KC
    IDENTITY -.-> KC
    KC --> KCDB
    IDENTITY --> ELK
    IDENTITY --> NR
    KC --> ELK
    
    style IDENTITY fill:#e1f5fe
    style KC fill:#fff3e0
    style KONG fill:#f3e5f5
    style EKS fill:#e8f5e8
    style ELK fill:#00bcd4,color:#fff
```

---

## 🏛️ Arquitetura de Sistema

### 🔧 Componentes Principais

```mermaid
C4Context
    title System Context Diagram - FCG Identity Microservice

    Person(user, "User", "End user accessing the platform")
    Person(admin, "Administrator", "System administrator")
    
    System(identity, "FCG Identity API", ".NET 9 microservice for authentication and user management")
    System_Ext(keycloak, "Keycloak", "Identity Provider & OpenID Connect server")
    System_Ext(kong, "Kong Ingress", "API Gateway with rate limiting and CORS")
    
    SystemDb(kcdb, "Keycloak Database", "PostgreSQL database storing user data")
    
    System_Ext(elasticsearch, "Elasticsearch Cloud", "Centralized logging and search")
    System_Ext(newrelic, "New Relic", "APM and monitoring platform")
    System_Ext(github, "GitHub Actions", "CI/CD pipeline")
    System_Ext(aws, "AWS EKS", "Kubernetes cluster hosting")

    Rel(user, kong, "Makes API requests", "HTTPS")
    Rel(admin, kong, "Manages users", "HTTPS")
    Rel(kong, identity, "Routes requests", "HTTP")
    Rel(kong, keycloak, "Routes auth requests", "HTTP")
    Rel(identity, keycloak, "Validates tokens & manages users", "HTTP")
    Rel(keycloak, kcdb, "Stores user data", "SQL")
    Rel(identity, elasticsearch, "Ships structured logs", "HTTPS")
    Rel(identity, newrelic, "Sends telemetry", "HTTPS")
    Rel(github, aws, "Deploys containers", "kubectl")
```

---

## 📊 Monitoramento e Observabilidade

### 📈 Observability Stack Completa

```mermaid
flowchart TB
    subgraph "Application Layer"
        APP[Identity API<br/>.NET 9]
        KC_APP[Keycloak]
    end
    
    subgraph "Logging Layer"
        SERILOG[Serilog<br/>Structured Logging]
        CONSOLE[Console Sink<br/>JSON Format]
        ELASTIC_SINK[Elasticsearch Sink<br/>fcg-logs-{yyyy.MM.dd}]
    end
    
    subgraph "Metrics & Tracing"
        NR_AGENT[New Relic Agent]
        NR_API[New Relic API]
    end
    
    subgraph "Kong Observability"
        KONG_LOGS[Kong Access Logs]
        HEADERS[Custom Headers<br/>X-Kong-Request-Id<br/>X-Correlation-ID]
    end
    
    subgraph "Kubernetes Monitoring"
        K8S_LOGS[Pod Logs]
        HEALTH_CHECKS[Health Probes<br/>Liveness/Readiness]
    end
    
    subgraph "Log Centralization (GCP)"
        ELASTICSEARCH[Elasticsearch Cloud<br/>us-central1.gcp.elastic.cloud]
        ELASTIC_API[Elasticsearch API<br/>API Key Authentication]
    end
    
    subgraph "External Monitoring"
        NR_DASHBOARD[New Relic Dashboard]
        ALERTS[Custom Alerts]
    end

    APP --> SERILOG
    APP --> NR_AGENT
    KC_APP --> K8S_LOGS
    SERILOG --> CONSOLE
    SERILOG --> ELASTIC_SINK
    ELASTIC_SINK --> ELASTIC_API
    ELASTIC_API --> ELASTICSEARCH
    NR_AGENT --> NR_API
    KONG_LOGS --> HEADERS
    CONSOLE --> K8S_LOGS
    K8S_LOGS --> NR_DASHBOARD
    NR_API --> NR_DASHBOARD
    NR_DASHBOARD --> ALERTS
    
    style APP fill:#1976d2,color:#fff
    style ELASTICSEARCH fill:#00bcd4,color:#fff
    style NR_DASHBOARD fill:#4caf50,color:#fff
    style ALERTS fill:#f44336,color:#fff
```

### 🔍 Log Flow & Correlation com Elasticsearch

```mermaid
sequenceDiagram
    participant Client
    participant Kong
    participant API as Identity API
    participant Serilog
    participant Console as Console Logs
    participant Elastic as Elasticsearch
    participant K8s as Kubernetes
    participant NR as New Relic

    Client->>+Kong: HTTP Request
    Kong->>Kong: Generate Request ID<br/>X-Kong-Request-Id: uuid
    Kong->>Kong: Generate Correlation ID<br/>X-Correlation-ID: uuid#counter
    Kong->>+API: Forward with Headers
    
    API->>API: Extract Headers<br/>Set Log Context
    API->>+Serilog: Log with Context<br/>{RequestId, CorrelationId, UserId}
    
    par Dual Logging Strategy
        Serilog->>+Console: JSON Console Output
        Console->>K8s: Kubernetes Logs
    and 
        Serilog->>+Elastic: Ship to Elasticsearch<br/>Index: fcg-logs-{yyyy.MM.dd}
    end
    
    API->>+NR: Send Telemetry<br/>(Performance, Errors)
    
    K8s-->>-NR: Forward Logs<br/>(Optional Log Forwarding)
    
    Note over Elastic,NR: Centralized logging & APM monitoring
    Note over Serilog,NR: Correlation across all services
    
    Serilog-->>-API: Log Written
    API-->>-Kong: Response
    Kong-->>-Client: Response + Headers<br/>(Request-Id, Latency)
```

### 📊 Elasticsearch Configuration

```json
{
  "Serilog": {
    "Using": [
      "Serilog.Sinks.Console", 
      "Serilog.Sinks.Elasticsearch", 
      "NewRelic.LogEnrichers.Serilog"
    ],
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "Microsoft.Hosting.Lifetime": "Information",
        "Microsoft.AspNetCore.Authentication": "Information",
        "System": "Warning"
      }
    },
    "WriteTo": [
      {
        "Name": "Console",
        "Args": {
          "outputTemplate": "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception} | RequestId: {RequestId} | CorrelationId: {CorrelationId} | UserId: {UserId} | Username: {Username} | SessionId: {SessionId} | Application: {ApplicationName}"
        }
      },
      {
        "Name": "Elasticsearch",
        "Args": {
          "nodeUris": "https://my-elasticsearch-project-ba9d4e.es.us-central1.gcp.elastic.cloud",
          "indexFormat": "fcg-logs-{0:yyyy.MM.dd}",
          "typeName": null,
          "autoRegisterTemplate": true,
          "apiKey": "${ELASTICSEARCH_API_KEY}"
        }
      }
    ],
    "Enrich": [
      "FromLogContext", 
      "WithMachineName", 
      "WithEnvironmentName",
      "WithNewRelicLogsInContext"
    ],
    "Properties": {
      "ServiceName": "fcg-identity-service",
      "Application": "FCG Identity API"
    }
  }
}
```

---

## 🔐 Fluxo de Autenticação

### 🚪 Processo de Login Completo

```mermaid
sequenceDiagram
    participant Client
    participant Kong as Kong Ingress
    participant API as Identity API
    participant KC as Keycloak
    participant DB as PostgreSQL
    participant Elastic as Elasticsearch
    participant NR as New Relic

    Note over Client,NR: User Authentication Flow with Full Observability
    
    Client->>+Kong: POST /identity/v1/auth/login<br/>{email, password}
    Kong->>Kong: Rate Limiting Check<br/>(200/min, 2000/hour)
    Kong->>Kong: CORS Validation
    Kong->>Kong: Add Headers<br/>(X-Kong-Request-Id, X-Correlation-ID)
    Kong->>+API: Forward Request<br/>(Strip /identity prefix)
    
    API->>+KC: POST /realms/fiap-cloud-games/protocol/openid-connect/token<br/>grant_type=password
    KC->>+DB: Validate Credentials
    DB-->>-KC: User Data
    KC->>KC: Generate JWT Token<br/>(Access + Refresh)
    KC-->>-API: TokenResponse<br/>{access_token, refresh_token}
    
    API->>API: Map to Response Model
    
    par Dual Observability
        API->>+Elastic: Ship Structured Log<br/>Authentication Success/Failure
        API->>+NR: Log Authentication Event<br/>Performance Metrics
    end
    
    API-->>-Kong: 200 OK<br/>TokenResponse
    Kong-->>-Client: Response + Kong Headers<br/>(RateLimit-Remaining, etc.)
    
    Note over Elastic,NR: Logs centralized in Elasticsearch + APM in New Relic
```

---

## 🛡️ Segurança e Rate Limiting

### 🚦 Rate Limiting Strategy

```mermaid
flowchart TD
    REQUEST[Incoming Request] --> KONG_RL[Kong Rate Limiting Plugin]
    KONG_RL --> RL_CHECK{Rate Limit Check}
    
    RL_CHECK -->|Within Limits| ALLOW[Allow Request]
    RL_CHECK -->|Exceeded| DENY[HTTP 429<br/>Too Many Requests]
    
    ALLOW --> HEADERS[Add Rate Limit Headers<br/>RateLimit-Limit: 200<br/>RateLimit-Remaining: 199<br/>X-RateLimit-Limit-Hour: 2000]
    
    subgraph "Rate Limits"
        PER_MIN[200 requests/minute]
        PER_HOUR[2000 requests/hour]
        BY_IP[Rate limited by IP]
    end
    
    subgraph "Observability Integration"
        LOG_ALLOW[Log Allowed Request<br/>→ Elasticsearch]
        LOG_DENY[Log Rate Limited Request<br/>→ Elasticsearch + Alert]
    end
    
    HEADERS --> FORWARD[Forward to Backend]
    FORWARD --> LOG_ALLOW
    DENY --> LOG_DENY
    DENY --> RESPONSE[Return Error Response<br/>+ Retry-After header]
    
    style DENY fill:#f44336,color:#fff
    style ALLOW fill:#4caf50,color:#fff
    style KONG_RL fill:#ff9800,color:#fff
    style LOG_DENY fill:#ff5722,color:#fff
```

---

## ⚙️ Especificações Técnicas

### 🏷️ Versões e Dependências

| Componente | Versão | Descrição |
|------------|--------|-----------|
| **.NET** | 9.0 | Framework principal |
| **ASP.NET Core** | 9.0.9 | Web API framework |
| **Keycloak** | 22.0 | Identity Provider |
| **PostgreSQL** | 13-alpine | Database para Keycloak |
| **Kong Ingress** | Latest | API Gateway |
| **Kubernetes** | v1.34 | Container orchestration |
| **AWS EKS** | v1.34 | Managed Kubernetes |
| **New Relic Agent** | 10.45.0 | APM monitoring |
| **Serilog** | 9.0.0 | Structured logging |
| **Elasticsearch** | 8.x | Log centralization (GCP Cloud) |

### 📊 Performance Specifications

| Métrica | Valor | Observação |
|---------|-------|------------|
| **Response Time** | ~100-200ms (p95) | Incluindo validação Keycloak |
| **Throughput** | 200 req/min | Rate limit por IP |
| **Uptime SLA** | 99.9% | Monitorado via New Relic |
| **Kong Proxy Latency** | ~1ms | Overhead mínimo |
| **Log Indexing Latency** | <5s | Elasticsearch real-time indexing |
| **Memory Usage** | <4GB per pod | Otimizado para .NET 9 |
| **CPU Usage** | <50% average | m7i.flex.large instances |

---

**Documento Gerado em:** `07/10/2025`  
**Versão da Aplicação:** `1.0.0`  
**Ambiente:** `Production (AWS EKS + Elasticsearch Cloud)`  
**Stack de Observabilidade:** `✅ Elasticsearch + New Relic + Kong`  
**Status:** `✅ Produção com Observabilidade Completa`