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

O **FCG Identity Microservice** é um sistema de autenticação e autorização enterprise desenvolvido em **.NET 10** seguindo os princípios de **Clean Architecture** e **Domain-Driven Design (DDD)**. O sistema utiliza **Keycloak** como Identity Provider e está deployado em **AWS EKS** com **Kong Ingress Controller**.

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
        IDENTITY[Identity API<br/>.NET 10]
        KC[Keycloak<br/>Identity Provider]
    end

    subgraph "Data Layer"
        KCDB[(Keycloak Database<br/>PostgreSQL)]
    end

    subgraph "Observability Layer"
        PROM[Prometheus<br/>Metrics Collection]
        LOKI[Loki<br/>Log Aggregation]
        TEMPO[Tempo<br/>Distributed Tracing]
        GRAFANA[Grafana<br/>Visualization & Explore]
        OTEL[OpenTelemetry Collector<br/>Telemetry Pipeline]
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
    IDENTITY --> OTEL
    OTEL --> PROM
    OTEL --> LOKI
    OTEL --> TEMPO
    GRAFANA --> PROM
    GRAFANA --> LOKI
    GRAFANA --> TEMPO
    
    style IDENTITY fill:#e1f5fe
    style KC fill:#fff3e0
    style KONG fill:#f3e5f5
    style EKS fill:#e8f5e8
    style GRAFANA fill:#f46800,color:#fff
    style PROM fill:#e6522c,color:#fff
```

---

## 🏛️ Arquitetura de Sistema

### 🔧 Componentes Principais

```mermaid
C4Context
    title System Context Diagram - FCG Identity Microservice

    Person(user, "User", "End user accessing the platform")
    Person(admin, "Administrator", "System administrator")
    
    System(identity, "FCG Identity API", ".NET 10 microservice for authentication and user management")
    System_Ext(keycloak, "Keycloak", "Identity Provider & OpenID Connect server")
    System_Ext(kong, "Kong Ingress", "API Gateway with rate limiting and CORS")
    
    SystemDb(kcdb, "Keycloak Database", "PostgreSQL database storing user data")
    
    System_Ext(grafana, "Grafana", "Observability visualization platform")
    System_Ext(prometheus, "Prometheus", "Metrics collection and monitoring")
    System_Ext(loki, "Loki", "Log aggregation system")
    System_Ext(tempo, "Tempo", "Distributed tracing backend")
    System_Ext(github, "GitHub Actions", "CI/CD pipeline")
    System_Ext(aws, "AWS EKS", "Kubernetes cluster hosting")

    Rel(user, kong, "Makes API requests", "HTTPS")
    Rel(admin, kong, "Manages users", "HTTPS")
    Rel(kong, identity, "Routes requests", "HTTP")
    Rel(kong, keycloak, "Routes auth requests", "HTTP")
    Rel(identity, keycloak, "Validates tokens & manages users", "HTTP")
    Rel(keycloak, kcdb, "Stores user data", "SQL")
    Rel(identity, prometheus, "Exposes metrics", "HTTP")
    Rel(identity, loki, "Ships logs via OTLP", "HTTP")
    Rel(identity, tempo, "Ships traces via OTLP", "HTTP")
    Rel(admin, grafana, "Monitors system", "HTTPS")
    Rel(grafana, prometheus, "Queries metrics", "HTTP")
    Rel(grafana, loki, "Queries logs", "HTTP")
    Rel(grafana, tempo, "Queries traces", "HTTP")
    Rel(github, aws, "Deploys containers", "kubectl")
```

---

## 📊 Monitoramento e Observabilidade

### 📈 Observability Stack Completa - Prometheus, Loki, Tempo & Grafana

```mermaid
flowchart TB
    subgraph "Application Layer"
        APP[Identity API<br/>.NET 10]
        KC_APP[Keycloak]
    end
    
    subgraph "Telemetry Collection"
        OTEL[OpenTelemetry Collector<br/>OTLP Receiver]
        CONSOLE[Console Logs<br/>JSON Format]
    end
    
    subgraph "Kong Observability"
        KONG_LOGS[Kong Access Logs]
        HEADERS[Custom Headers<br/>X-Kong-Request-Id<br/>X-Correlation-ID]
    end
    
    subgraph "Kubernetes Monitoring"
        K8S_LOGS[Pod Logs]
        HEALTH_CHECKS[Health Probes<br/>Liveness/Readiness]
        PROMTAIL[Promtail<br/>Log Scraper]
    end
    
    subgraph "Storage Backends"
        PROMETHEUS[Prometheus<br/>Time Series Database]
        LOKI[Loki<br/>Log Aggregator]
        TEMPO[Tempo<br/>Trace Storage]
    end
    
    subgraph "Visualization"
        GRAFANA[Grafana<br/>Unified Observability UI]
        EXPLORE[Explore Interface<br/>Ad-hoc Queries]
    end

    APP -->|Metrics| OTEL
    APP -->|Traces| OTEL
    APP --> CONSOLE
    KC_APP --> K8S_LOGS
    CONSOLE --> K8S_LOGS
    K8S_LOGS --> PROMTAIL
    PROMTAIL -->|Scrape Logs| LOKI
    OTEL -->|Store Metrics| PROMETHEUS
    OTEL -->|Store Traces| TEMPO
    KONG_LOGS --> HEADERS
    GRAFANA -->|Query| PROMETHEUS
    GRAFANA -->|Query| LOKI
    GRAFANA -->|Query| TEMPO
    GRAFANA --> EXPLORE
    HEALTH_CHECKS -.->|Monitor| PROMETHEUS
    
    style APP fill:#1976d2,color:#fff
    style GRAFANA fill:#f46800,color:#fff
    style PROMETHEUS fill:#e6522c,color:#fff
    style LOKI fill:#00a273,color:#fff
    style TEMPO fill:#f44336,color:#fff
    style OTEL fill:#425cc7,color:#fff
```

### 🔍 Telemetry Flow & Correlation com OpenTelemetry

```mermaid
sequenceDiagram
    participant Client
    participant Kong
    participant API as Identity API
    participant OTEL as OpenTelemetry Collector
    participant Console as Console Logs
    participant Promtail
    participant Prom as Prometheus
    participant Loki
    participant Tempo
    participant Grafana

    Client->>+Kong: HTTP Request
    Kong->>Kong: Generate Request ID<br/>X-Kong-Request-Id: uuid
    Kong->>Kong: Generate Correlation ID<br/>X-Correlation-ID: uuid#counter
    Kong->>+API: Forward with Headers
    
    API->>API: Extract Headers<br/>Set Trace Context
    
    par Telemetry Collection
        API->>+OTEL: Export Metrics<br/>(http_server_*, custom metrics)
        OTEL->>+Prom: Store Time Series
    and
        API->>+OTEL: Export Traces<br/>(OTLP Protocol)
        OTEL->>+Tempo: Store Traces
    and
        API->>+Console: JSON Structured Logs
        Console->>+Promtail: Scrape Pod Logs
        Promtail->>+Loki: Ship Logs
    end
    
    Note over Grafana: Unified Observability Platform
    Grafana->>Prom: Query Metrics<br/>(PromQL)
    Grafana->>Loki: Query Logs<br/>(LogQL)
    Grafana->>Tempo: Query Traces<br/>(TraceQL)
    
    Note over Grafana: Correlation via trace_id, span_id
    Note over OTEL,Loki: Full observability stack in-cluster
    
    API-->>-Kong: Response
    Kong-->>-Client: Response + Headers<br/>(Request-Id, Latency)
```

### 📊 OpenTelemetry Configuration

```yaml
# OpenTelemetry Collector Configuration
receivers:
  otlp:
    protocols:
      grpc:
        endpoint: 0.0.0.0:4317
      http:
        endpoint: 0.0.0.0:4318

processors:
  batch:
    timeout: 10s
    send_batch_size: 1024
  memory_limiter:
    check_interval: 1s
    limit_mib: 512

exporters:
  prometheus:
    endpoint: "0.0.0.0:8889"
    namespace: fcg_identity
  otlp/tempo:
    endpoint: tempo:4317
    tls:
      insecure: true
  loki:
    endpoint: http://loki:3100/loki/api/v1/push

service:
  pipelines:
    metrics:
      receivers: [otlp]
      processors: [memory_limiter, batch]
      exporters: [prometheus]
    traces:
      receivers: [otlp]
      processors: [memory_limiter, batch]
      exporters: [otlp/tempo]
    logs:
      receivers: [otlp]
      processors: [memory_limiter, batch]
      exporters: [loki]
```

**Datasources Grafana:**
```yaml
apiVersion: 1
datasources:
  - name: Prometheus
    type: prometheus
    access: proxy
    url: http://prometheus:9090
    uid: prometheus
    isDefault: true
    editable: false

  - name: Loki
    type: loki
    access: proxy
    url: http://loki:3100
    uid: loki
    editable: false
    jsonData:
      derivedFields:
        - datasourceUid: tempo
          matcherRegex: "trace_id=(\\w+)"
          name: TraceID
          url: "$${__value.raw}"

  - name: Tempo
    type: tempo
    access: proxy
    url: http://tempo:3200
    uid: tempo
    editable: false
    jsonData:
      tracesToLogs:
        datasourceUid: loki
        filterByTraceID: true
        filterBySpanID: false
      tracesToMetrics:
        datasourceUid: prometheus
      serviceMap:
        datasourceUid: prometheus
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
    participant OTEL as OpenTelemetry
    participant Grafana

    Note over Client,Grafana: User Authentication Flow with Full Observability
    
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
    
    par Telemetry Collection
        API->>+OTEL: Export Trace<br/>Authentication Span
        API->>+OTEL: Export Metrics<br/>auth_success_total
    end
    
    API-->>-Kong: 200 OK<br/>TokenResponse
    Kong-->>-Client: Response + Kong Headers<br/>(RateLimit-Remaining, etc.)
    
    Note over OTEL,Grafana: Observability via Prometheus + Loki + Tempo
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
        LOG_ALLOW[Log Allowed Request<br/>→ Loki via Promtail]
        LOG_DENY[Log Rate Limited Request<br/>→ Loki + Prometheus Counter]
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
| **.NET** | 10.0 | Framework principal |
| **ASP.NET Core** | 10.0.1 | Web API framework |
| **Keycloak** | 22.0 | Identity Provider |
| **PostgreSQL** | 13-alpine | Database para Keycloak |
| **Kong Ingress** | Latest | API Gateway |
| **Kubernetes** | v1.34 | Container orchestration |
| **AWS EKS** | v1.34 | Managed Kubernetes |
| **Prometheus** | 3.2.1 | Metrics collection |
| **Loki** | 3.3.1 | Log aggregation |
| **Tempo** | 2.7.2 | Distributed tracing |
| **Grafana** | 11.3.1 | Observability visualization |
| **OpenTelemetry Collector** | 0.115.1 | Telemetry pipeline |
| **Promtail** | 3.3.1 | Log scraper |

### 📊 Performance Specifications

| Métrica | Valor | Observação |
|---------|-------|------------|
| **Response Time** | ~100-200ms (p95) | Incluindo validação Keycloak |
| **Throughput** | 200 req/min | Rate limit por IP |
| **Uptime SLA** | 99.9% | Monitorado via Prometheus |
| **Kong Proxy Latency** | ~1ms | Overhead mínimo |
| **Metrics Scrape Interval** | 15s | Prometheus collection |
| **Log Indexing Latency** | <3s | Loki real-time indexing |
| **Trace Sampling Rate** | 100% | All traces captured in Tempo |
| **Memory Usage** | <4GB per pod | Otimizado para .NET 10 |
| **CPU Usage** | <50% average | m7i.flex.large instances |

---

**Documento Gerado em:** `21/12/2025`  
**Versão da Aplicação:** `1.0.0`  
**Ambiente:** `Production (AWS EKS)`  
**Stack de Observabilidade:** `✅ Prometheus + Loki + Tempo + Grafana + OpenTelemetry`  
**Dashboards:** `Manual creation via Grafana Explore UI`  
**Status:** `✅ Produção com Observabilidade Completa`