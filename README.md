# 🎮 Tech Challenge - Fase 2

**Plataforma de Venda de Jogos Digitais e Gestão de Transações Financeiras**

---

## 📌 Visão Geral

A API desenvolvida oferece uma solução completa para comercialização de jogos digitais, com funcionalidades que abrangem:

- 🔐 **Autenticação e Autorização**: Registro, login, redefinição de senha e confirmação de e-mail  
- 🕹️ **Gestão de Jogos**: Cadastro completo do catálogo de jogos  
- 💸 **Promoções**: Criação e gerenciamento de descontos promocionais  
- 💳 **Transações Financeiras**: Compra, reembolso, depósito e saque de saldo  
- 📚 **Biblioteca do Usuário**: Armazenamento e gerenciamento dos jogos adquiridos

---

## 🧱 Estrutura do Projeto

O projeto segue os princípios de **Domain-Driven Design (DDD)** e utiliza **injeção de dependência** para garantir modularidade, coesão e manutenção facilitada.

### 🔧 Camadas

- **Application** – Camada de orquestração da lógica de aplicação  
- **Domain** – Regras de negócio e entidades do domínio  
- **Data** – Implementações de repositórios e acesso a dados  
- **Infrastructure** – Integrações externas (como serviços de e-mail)  
- **Shared** – DTOs, modelos base, Requests/Responses e validações  
- **Tests** – Contém os testes unitários da aplicação

---

## 🔗 Endpoints da API

### 🛡️ Autenticação (`/v1/auth`)
| Método | Rota | Descrição |
|--------|------|-----------|
| GET    | `/` | Listar todos os usuários |
| GET    | `/{id}` | Obter usuário específico com permissões |
| PUT    | `/{id}` | Atualizar usuário |
| DELETE | `/{id}` | Excluir usuário |
| POST   | `/register` | Registrar nova conta |
| POST   | `/login` | Login do usuário |
| POST   | `/refresh-token` | Renovar token JWT |
| POST   | `/first-access` | Redefinir senha no primeiro acesso |
| GET    | `/reset-password/{email}` | Enviar link de redefinição de senha |
| POST   | `/reset-password` | Redefinir senha |
| GET    | `/confirm-email/{email}` | Enviar link de confirmação de e-mail |
| POST   | `/confirm-email` | Confirmar e-mail |

### 🎮 Jogos (`/v1/games`)
| Método | Rota | Descrição |
|--------|------|-----------|
| GET    | `/` | Listar todos os jogos |
| POST   | `/` | Criar novo jogo |
| GET    | `/{id}` | Obter jogo por ID |
| PUT    | `/{id}` | Atualizar jogo |
| DELETE | `/{id}` | Excluir jogo |

### 🏷️ Promoções (`/v1/promotions`)
| Método | Rota | Descrição |
|--------|------|-----------|
| GET    | `/` | Listar promoções ativas |
| POST   | `/` | Criar nova promoção |
| GET    | `/{id}` | Obter promoção por ID |
| PUT    | `/{id}` | Atualizar promoção |
| DELETE | `/{id}` | Excluir promoção |
| POST   | `/{promotionId}/promotion-games` | Adicionar jogos à promoção |
| PUT    | `/promotion-games/{promotionGameId}` | Atualizar item da promoção |
| DELETE | `/promotion-games/{promotionGameId}` | Remover jogo da promoção |

### 💰 Transações (`/v1/transactions`)
| Método | Rota | Descrição |
|--------|------|-----------|
| POST   | `/purchase` | Comprar jogo |
| PUT    | `/refund-purchase` | Solicitar reembolso |
| POST   | `/deposit` | Depositar saldo |
| PUT    | `/withdraw` | Sacar saldo |

### 📚 Biblioteca (`/v1/user-libraries`)
| Método | Rota | Descrição |
|--------|------|-----------|
| GET    | `/{userId}` | Consultar jogos adquiridos |

---

## 📦 Modelos de Dados

### 🔐 Autenticação
- `LoginDto`: E-mail e senha  
- `CreateUserDto`: Cadastro de usuário com e-mail, permissões e role  
- `UserDto`: Dados completos do usuário  
- `ChangePasswordDto`: Redefinição de senha  

### 🎮 Jogos
- `GameAddRequest`: Nome e preço do jogo  
- `GameUpdateRequest`: Dados completos do jogo  
- `GameResponse`: ID, nome, status, preço e datas  

### 🏷️ Promoções
- `PromotionAddRequest`: Nome, datas e jogos da promoção  
- `PromotionGameAddRequest`: ID do jogo e percentual de desconto  
- `PromotionResponse`: Dados da promoção  

### 💳 Transações
- `PurchaseGameRequest`: ID do usuário, jogo e promoção (opcional)  
- `BalanceRequest`: ID do usuário e valor  
- `RefundPurchaseRequest`: ID do usuário e jogo  

---

## ⚙️ Recursos Técnicos

- **Linguagem**: C#  
- **Framework**: ASP.NET Core (.NET 9)  
- **Arquitetura**: MVC + DDD  
- **Testes**: TDD  
- **Autenticação**: JWT com refresh token + Keycloak  
- **Validação**: Data Annotations, FluentValidation, EF Mapping  
- **Documentação**: OpenAPI / Swagger 3.0.4  
- **Serviços**: Serviço de e-mail mockado (por segurança)  
- **Banco de dados**: PostgreSQL  
- **Orquestração**: Kubernetes (AWS EKS)  
- **Gateway**: Kong Ingress Controller  
- **Identidade**: Keycloak (OpenID Connect)  
- **Monitoramento**: New Relic APM  
- **Container Registry**: AWS ECR 

---

## ✅ Testes

A aplicação segue os princípios de **Test-Driven Development (TDD)**, com testes unitários que validam regras de negócio, fluxos de uso, exceções e comportamentos esperados.

Utiliza:

- **xUnit**  
- **Moq**  
- **FluentAssertions**  

---

## 📈 Monitoramento e Observabilidade

### 🏥 Health Checks
- **`/health`**: Status da API e dependências (banco de dados, Keycloak)

### 📊 New Relic APM
- **Performance Monitoring**: Métricas de aplicação em tempo real
- **Error Tracking**: Rastreamento automático de erros e exceções  
- **Distributed Tracing**: Rastreamento de requests através dos serviços
- **Dashboard**: Métricas personalizadas de negócio

### 🔍 Kong Ingress Controller Monitoring
- **Request ID**: Tracking único via `X-Kong-Request-Id`
- **Correlation ID**: Rastreamento via `X-Correlation-ID` (UUID#counter)
- **Latency Headers**: `X-Kong-Upstream-Latency`, `X-Kong-Proxy-Latency`
- **Rate Limiting Headers**: Informações de uso e limites

### 📝 Logging
- **Structured Logging**: Logs JSON via Serilog
- **Context Enrichment**: JWT claims (RequestId, UserId, Username, SessionId)
- **Console Output**: Logs centralizados no Kubernetes

---

## 🏗️ Arquitetura Técnica

### ☁️ **AWS EKS Cluster**
```
Cluster: fcg-identity (sa-east-1)
├── Nodes: m7i-flex.large (2 vCPUs, 8GB RAM)
├── Kubernetes: v1.34 (Amazon Linux)
└── Storage: EBS gp3 (encrypted)
```

### 🌐 **Kong Ingress Controller**
```
Internet → AWS Load Balancer → Kong Ingress Controller
                             ├── Identity API (:80)
                             └── Keycloak (:8080)
```

**Kong Plugins Ativos:**
- **Rate Limiting**: 200/min, 2000/hora
- **CORS**: Origens configuráveis  
- **Correlation ID**: UUID#counter tracking
- **Request ID**: Identificação única de requests

### 🔐 **Identity & Auth Flow**
```
Client → Kong Ingress → Identity API → Keycloak → PostgreSQL
       ↓
   Rate Limiting (200/min)
   CORS Headers
   Correlation ID
   Request Tracking
```

### 📊 **Observability Stack**
```
Application → Serilog → Console Logs → Kubernetes
            ↓
         New Relic APM → Dashboards & Alerts
            ↓
    Kong Headers → Request Correlation
```

### 🔄 **CI/CD Pipeline**
```
GitHub → Actions → Docker Build → ECR Push → EKS Deploy
      ↓
   .NET Tests → Security Scan → Health Check → Production
```

---

## 🧠 Regras de Negócio

### 🔐 Autenticação
- Senha forte (mín. 8 caracteres, maiúscula, minúscula, número e caractere especial)  
- Confirmação de e-mail obrigatória  
- Controle de acesso baseado em roles e claims  

### 💳 Transações
- Validação de saldo  
- Prevenção de compras duplicadas  
- Regras para reembolso  
- Aplicação automática de promoções válidas  

### 🏷️ Promoções
- Datas válidas (início < fim)  
- Descontos entre 1% e 100%  
- Não remover jogos com compras vinculadas  

### 🎮 Jogos
- Nome único por jogo  

### 📚 Biblioteca
- Sem duplicações de jogos para o mesmo usuário  

---

## 🚀 Como Executar

### 🐳 Desenvolvimento Local (Docker)

Para desenvolvimento local, execute:

```bash
docker-compose build --no-cache
docker-compose up
```

A aplicação estará acessível em:
- **Identity API**: http://localhost:5001
- **Swagger**: http://localhost:5001/swagger  
- **Health Check**: http://localhost:5001/health
- **Keycloak**: http://localhost:8080

### ☁️ Produção (AWS EKS + Kong Ingress Controller)

O sistema está deployado na AWS com arquitetura otimizada:

```bash
# Deploy via CI/CD Pipeline (.github/workflows/deploy.yml)
# Ou deploy manual:
kubectl apply -f k8s/production/
```

**🌐 URLs de Produção:**
- **Identity API**: https://api.fcg-identity.com
- **Keycloak**: https://keycloak.fcg-identity.com
- **Health Check**: https://api.fcg-identity.com/health
- **Swagger**: https://api.fcg-identity.com/swagger

**🔧 Funcionalidades Kong Ingress Controller:**
- ✅ **Rate Limiting**: 200 requests/minuto, 2000/hora
- ✅ **CORS**: Configurado para origens permitidas
- ✅ **Correlation ID**: Tracking automático (X-Correlation-ID)
- ✅ **Load Balancing**: AWS Application Load Balancer
- ✅ **SSL/TLS**: Certificados automáticos

**📖 Documentação adicional:**
- [AWS Setup Guide](AWS_SETUP_GUIDE.md) - Setup completo EKS + Kong Ingress Controller
- [Kong Migration Guide](KONG_MIGRATION_GUIDE.md) - Migração para Kong Ingress Controller  
- [Kong Status Final](KONG_STATUS_FINAL.md) - Status atual da implementação Kong
- [K8s Cleanup Report](K8S_CLEANUP_REPORT.md) - Relatório de limpeza dos arquivos K8s
- [Keycloak Manual Setup](KEYCLOAK_MANUAL_SETUP.md) - Configuração Keycloak

### 🚀 **Scripts de Deploy**
- `scripts/remove-kong-gateway.ps1` - Remoção Kong Gateway standalone  
- `.github/workflows/deploy.yml` - Pipeline CI/CD automatizado
- `deploy-eks.sh` / `deploy-eks.ps1` - Deploy manual EKS

## Usuários Padrão (Seed)

Após o primeiro build da aplicação, o serviço de Seed criará os seguintes usuários:

### ADMIN
```json
{
  "email": "vinicius_pinheiro05@hotmail.com",
  "password": "Default@123"
}
```

### USER
```json
{
  "email": "vinicius_pinheiro02@hotmail.com",
  "password": "Default@123"
}
```

Utilize-os para fazer login e testar as funcionalidades da aplicação.

---

## 🔐 Autenticação da API

### 🔑 **Fluxo de Login**
1. **POST** `https://api.fcg-identity.com/v1/auth/login`
2. Copie o `accessToken` retornado
3. Use no header: `Authorization: Bearer {seu_token}`
4. Renove com `/v1/auth/refresh-token` quando necessário

### 📋 **Headers Automáticos Kong**
Todas as requests incluem automaticamente:
```http
X-Kong-Request-Id: uuid-unique-per-request
X-Correlation-ID: uuid#counter-tracking  
X-Kong-Upstream-Latency: tempo-ms
X-Kong-Proxy-Latency: tempo-ms
```

### ⚡ **Rate Limiting**
- **200 requests/minuto** por IP
- **2000 requests/hora** por IP
- Headers informativos:
  ```http
  RateLimit-Limit: 200
  RateLimit-Remaining: 199
  X-RateLimit-Limit-Hour: 2000
  ```

### 🌐 **CORS**
Configurado para desenvolvimento e produção:
```http
Access-Control-Allow-Origin: configurável
Access-Control-Allow-Credentials: true
Access-Control-Expose-Headers: X-Auth-Token,X-Correlation-ID
```

---

## 🛠️ Troubleshooting

### 🔍 **Verificações Básicas**
```bash
# Status do cluster
kubectl get pods -n identity-system

# Status dos ingresses
kubectl get ingress -n identity-system

# Logs da aplicação
kubectl logs -f deployment/identity-api -n identity-system

# Kong plugins ativos
kubectl get kongplugin -n identity-system
```

### ⚠️ **Problemas Comuns**

#### **429 Too Many Requests**
- Rate limit atingido (200/min ou 2000/hora)
- Aguarde reset ou verifique headers `RateLimit-Reset`

#### **502 Bad Gateway**
- Verifique se backend está saudável: `/health`
- Confirme se Kong Ingress Controller está ativo

#### **CORS Errors**
- Verifique configuração do plugin `keycloak-cors` ou `identity-cors`
- Confirme origem permitida nas configurações CORS

#### **SSL/TLS Issues**
- Verifique se certificados estão válidos
- Use HTTP para desenvolvimento local

### 📊 **Monitoramento em Tempo Real**
```bash
# Kong Request IDs em tempo real
curl -H "Host: api.fcg-identity.com" https://api.fcg-identity.com/health

# Response headers úteis:
# X-Kong-Request-Id: tracking único
# X-Correlation-ID: correlação de requests  
# RateLimit-Remaining: requests restantes
```

---

## � Status do Projeto

### ✅ **Produção** 
- **Cluster EKS**: `fcg-identity` (sa-east-1) - ✅ Ativo
- **Kong Ingress Controller**: ✅ Funcional com plugins 
- **Identity API**: ✅ Healthy (Rate Limiting: 200/min)
- **Keycloak**: ✅ Funcional (OpenID Connect)
- **New Relic**: ✅ Monitoramento ativo
- **CI/CD Pipeline**: ✅ Deploy automatizado

### 🔧 **Funcionalidades Ativas**
- ✅ Autenticação JWT + Keycloak integration
- ✅ Rate Limiting (200/min, 2000/hora)  
- ✅ CORS configurado
- ✅ Correlation ID tracking
- ✅ Health checks
- ✅ Structured logging
- ✅ Error handling & monitoring

### 📈 **Métricas Atuais**
- **Latência Kong Proxy**: ~1ms
- **Uptime**: 99.9% (monitorado via New Relic)
- **Requests processadas**: Rate limiting funcional
- **Recursos**: Otimizado pós-limpeza Kong Gateway

---

## �👥 Contato

- **Vinicius Freire**

---

📄 **Licença**: MIT  
🧪 **Stack**: .NET 9 + Kong Ingress Controller + AWS EKS + Keycloak + New Relic  
🏗️ **Arquitetura**: DDD + TDD + Kong Ingress Controller  
🚀 **Status**: Produção-ready com observabilidade completa
