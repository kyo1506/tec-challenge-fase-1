# 📊 Observability Stack Setup Guide

## Visão Geral

A stack de observabilidade foi implementada com componentes separados para produção no Kubernetes, seguindo as melhores práticas:

- **OpenTelemetry Collector**: Recebe telemetria das aplicações
- **Prometheus**: Armazena métricas
- **Tempo**: Armazena traces distribuídos
- **Loki**: Armazena logs estruturados
- **Grafana**: Visualização unificada

## 🚀 Deploy no Kubernetes

### 1. Aplicar os manifestos na ordem correta:

```bash
# 1. Volumes persistentes
kubectl apply -f k8s/production/volumes.yaml

# 2. ConfigMaps atualizados
kubectl apply -f k8s/production/configmaps.yaml

# 3. Stack de observabilidade
kubectl apply -f k8s/production/observability.yaml

# 4. Deploy da aplicação (já com variáveis OTEL)
kubectl apply -f k8s/production/deployment.yaml
```

### 2. Verificar o status dos pods:

```bash
kubectl get pods -n identity-system

# Aguarde todos ficarem Running:
# - otel-collector-xxxxx (2 replicas)
# - prometheus-xxxxx
# - tempo-xxxxx
# - loki-xxxxx
# - grafana-xxxxx
# - identity-api-xxxxx
```

### 3. Acessar o Grafana:

```bash
# Port-forward local
kubectl port-forward -n identity-system svc/grafana-service 3000:3000

# Acesse: http://localhost:3000
# Usuário: admin
# Senha: fcg-admin-2024
```

## 📊 Datasources Configurados Automaticamente

Ao acessar o Grafana, você terá 3 datasources pré-configurados:

1. **Prometheus** (default)
   - URL: `http://prometheus-service:9090`
   - Métricas da aplicação e infraestrutura

2. **Tempo**
   - URL: `http://tempo-service:3200`
   - Traces distribuídos
   - Correlação automática com logs

3. **Loki**
   - URL: `http://loki-service:3100`
   - Logs estruturados
   - Correlação automática com traces

## 🔍 Explorando os Dados

### Ver Traces:
1. Acesse **Explore** no Grafana
2. Selecione **Tempo**
3. Pesquise por `service.name="fcg-identity-api"`

### Ver Logs:
1. Acesse **Explore** no Grafana
2. Selecione **Loki**
3. Query: `{service_name="fcg-identity-api"}`

### Ver Métricas:
1. Acesse **Explore** no Grafana
2. Selecione **Prometheus**
3. Query exemplo: `http_server_request_duration_seconds_bucket`

## 🔗 Correlação Automática

O Grafana está configurado para correlação automática:

- **Trace → Logs**: Clique em um span para ver os logs relacionados
- **Logs → Trace**: Clique em um log com trace_id para ver o trace completo
- **Trace → Metrics**: Veja métricas relacionadas ao serviço do trace

## 📦 Armazenamento

Volumes persistentes criados:

- **Prometheus**: 10Gi (15 dias de retenção)
- **Tempo**: 10Gi (7 dias de retenção)
- **Loki**: 20Gi (7 dias de retenção)
- **Grafana**: 2Gi (dashboards e configurações)

## 🔄 Desenvolvimento Local (docker-compose)

Para desenvolvimento local, use a imagem all-in-one:

```bash
docker-compose up -d

# Acesse o Grafana: http://localhost:3000
# Usuário: admin
# Senha: admin
```

A aplicação já está configurada para enviar telemetria para `http://otel-lgtm:4318`.

## 🎯 Próximos Passos

1. **Criar Dashboards Customizados**
   - Importe dashboards da comunidade
   - Crie dashboards específicos para FCG Identity API

2. **Configurar Alertas**
   - Configure alertas no Grafana
   - Integre com Slack/Email

3. **Adicionar Mais Microserviços**
   - Todos devem apontar para `http://otel-collector-service:4318`
   - Use o mesmo padrão de variáveis OTEL

## ⚠️ Nota sobre New Relic

O deployment atual mantém o New Relic configurado. Se quiser usar **apenas OpenTelemetry**:

1. Remova as variáveis `NEW_RELIC_*` do `deployment.yaml`
2. Remova o pacote `NewRelic.Agent` do `.csproj`
3. Remova a configuração do New Relic no `Program.cs`

Para usar **ambos** (recomendado durante transição):
- New Relic: APM e alertas enterprise
- OpenTelemetry: Observabilidade local e desenvolvimento
