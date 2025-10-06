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
- **Orquestração**: Kubernetes + Minikube  
- **Gateway**: Kong Gateway  
- **Identidade**: Keycloak (OpenID Connect) 

---

## ✅ Testes

A aplicação segue os princípios de **Test-Driven Development (TDD)**, com testes unitários que validam regras de negócio, fluxos de uso, exceções e comportamentos esperados.

Utiliza:

- **xUnit**  
- **Moq**  
- **FluentAssertions**  

---

## 📈 Monitoramento e Health Checks

A aplicação possui um único endpoint de verificação de saúde:

- **`/health`**: Retorna um JSON com o status da API e dependências como banco de dados.

> 🔄 A interface gráfica **HealthChecksUI** foi removida para simplificar o monitoramento.

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

### Utilizando Docker (Desenvolvimento)

Na raiz do projeto, execute:

```bash
docker-compose build --no-cache
docker-compose up
```

A aplicação estará acessível em:
- API: http://localhost:5001
- Swagger: http://localhost:5001/swagger
- Health Check: http://localhost:5001/health

### Utilizando Kubernetes (Produção)

Para deploy em Kubernetes com Minikube:

```powershell
# 1. Executar deploy completo
cd k8s\production
.\deploy.ps1

# 2. Configurar Keycloak manualmente (seguir guia)
# Ver: KEYCLOAK_MANUAL_SETUP.md

# 3. Configurar Kong Gateway (opcional)
.\..\..\kong-microservice-setup.ps1
```

**Serviços disponíveis:**
- Kong Gateway: http://localhost:8000
- Keycloak: http://localhost:8080
- Identity API: http://localhost:5001 (via port-forward)
- Konga (Kong Admin): http://localhost:1337

**📖 Documentação adicional:**
- [Configuração Manual do Keycloak](KEYCLOAK_MANUAL_SETUP.md)
- [Guia Completo](README-COMPLETE.md)

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

1. Acesse `/v1/auth/login` e faça login 
2. Copie o `accessToken` retornado 
3. Utilize no header `Authorization: Bearer {seu_token}` 
4. Quando necessário, renove com `/refresh-token`

---

## 👥 Contato

- **Vinicius Freire** - **Willian Costa**

📄 Licença: MIT 
🧪 Desenvolvido com foco em escalabilidade, segurança e boas práticas RESTful.
