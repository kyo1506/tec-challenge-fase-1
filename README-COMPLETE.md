# 🔐 FCG Identity - Microserviços de Autenticação

Sistema completo de autenticação e autorização para microserviços usando Kong Gateway, Keycloak e biblioteca cliente personalizada.

## 🏗️ Arquitetura

```
┌─────────────────┐    ┌──────────────┐    ┌─────────────────┐    ┌─────────────┐
│   Client Apps   │───▶│ Kong Gateway │───▶│ Identity Service│───▶│  Keycloak   │
└─────────────────┘    └──────────────┘    └─────────────────┘    └─────────────┘
                              │                       │
                              ▼                       │
                    ┌──────────────────┐             │
                    │ Other Services   │◄────────────┘
                    │ (uses FCG.Client)│
                    └──────────────────┘
```

### Componentes

- **Kong Gateway**: API Gateway para roteamento e rate limiting
- **Konga**: Interface administrativa para Kong
- **Identity Service**: Microserviço de autenticação/autorização
- **Keycloak**: Servidor de identidade (SSO/OAuth2/OIDC)
- **Fcg.Identity.Client**: Biblioteca para integração de outros microserviços

## 🚀 Quick Start

### 1. Subir a Infraestrutura

```bash
# Clonar o repositório
git clone https://github.com/kyo1506/tech-challenge-fiap-auth.git
cd tech-challenge-fiap-auth

# Subir todos os serviços
docker-compose up -d

# Configurar Kong (aguarde ~30s para inicialização)
.\kong-microservice-setup.ps1
```

### 2. Acessar Interfaces

- **Kong Admin**: http://localhost:8001
- **Konga**: http://localhost:1337 (admin/adminpass)  
- **Keycloak**: http://localhost:8080 (admin/admin123)
- **API Gateway**: http://localhost:8000
- **Identity Service**: http://localhost:5001

### 3. Criar Microserviço com Autenticação

```bash
# Criar novo projeto
dotnet new webapi -n MeuMicroservico

# Adicionar a biblioteca
dotnet add package Fcg.Identity.Client
# OU em desenvolvimento:
dotnet add reference ../src/Fcg.Identity.Client/Fcg.Identity.Client.csproj
```

```csharp
// Program.cs
using Fcg.Identity.Client.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddIdentityClient(builder.Configuration);

var app = builder.Build();

app.UseIdentityAuthentication(); // ANTES de UseAuthorization
app.UseAuthorization();
app.MapControllers();
app.Run();
```

```csharp
// Controllers/ExemplosController.cs
[ApiController]
[Route("api/[controller]")]
public class ExemplosController : ControllerBase
{
    [HttpGet]
    [RequirePermission("examples", "read")]
    public IActionResult Get()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Ok($"Olá {userId}!");
    }
}
```

## 📦 Fcg.Identity.Client Library

### Instalação
```bash
dotnet add package Fcg.Identity.Client
```

### Configuração
```json
// appsettings.json
{
  "ConnectionStrings": {
    "IdentityService": "http://localhost:8000"
  }
}
```

### Uso com Atributos (Recomendado)
```csharp
[RequirePermission("users", "read")]    // Apenas leitura
[RequirePermission("users", "write")]   // Criação/edição
[RequirePermission("users", "delete")]  // Deleção
[RequirePermission("users", "manage")]  // Administração completa
```

### Uso Manual
```csharp
public class ExemploController : ControllerBase
{
    private readonly IIdentityClient _identityClient;

    public ExemploController(IIdentityClient identityClient)
    {
        _identityClient = identityClient;
    }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var token = Request.Headers["Authorization"]
            .FirstOrDefault()?.Replace("Bearer ", "");
            
        var user = await _identityClient.ValidateTokenAsync(token);
        if (user == null) return Unauthorized();
        
        var hasPermission = await _identityClient.ValidatePermissionAsync(
            token, "examples", "read");
        if (!hasPermission) return Forbid();
        
        return Ok(user);
    }
}
```

## 🔐 Sistema de Permissões

### Recursos Padrão
- **users**: `read`, `write`, `delete`, `manage`
- **games**: `read`, `write`, `delete`, `manage`
- **orders**: `read`, `write`, `delete`, `manage`
- **profile**: `read`, `write`
- **reports**: `read`, `generate`

### Roles e Permissões

| Role | Descrição | Permissões |
|------|-----------|------------|
| **admin** | Administrador completo | `*:*` (todas) |
| **manager** | Gerente | `users:read,write`, `games:*`, `orders:*`, `reports:read` |
| **user** | Usuário regular | `profile:*`, `games:read`, `orders:read,write` |
| **customer** | Cliente | `profile:*`, `games:read`, `orders:read,write` |

## 🌐 Endpoints da API

### Identity Service

```bash
# Validar token
GET /v1/validate-token
Authorization: Bearer {token}

# Validar permissão  
POST /v1/validate-permission
{
  "resource": "users",
  "action": "read"
}

# Health check
GET /health
```

### Kong Gateway

```bash
# Através do Kong (Produção)
GET http://localhost:8000/v1/validate-token

# Direto no serviço (Desenvolvimento)  
GET http://localhost:5001/v1/validate-token
```

## 🐳 Docker Compose

O projeto inclui configuração completa com:

- **Kong**: Gateway na porta 8000
- **Konga**: Admin UI na porta 1337
- **PostgreSQL**: Bancos para Kong e Konga
- **Keycloak**: Servidor de identidade na porta 8080
- **Identity Service**: Microserviço na porta 5001

```yaml
services:
  kong-gateway:
    image: kong:latest
    ports:
      - "8000:8000"   # Proxy
      - "8001:8001"   # Admin API
  
  identity-microservice:
    build: .
    ports:
      - "5001:5001"
```

## 🧪 Testando

### 1. Obter Token
```bash
curl -X POST "http://localhost:8080/realms/fiap-cloud-games/protocol/openid-connect/token" \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -d "username=admin&password=admin123&grant_type=password&client_id=fcg-client"
```

### 2. Testar Endpoint Protegido
```bash
curl -X GET "http://localhost:8000/v1/validate-token" \
  -H "Authorization: Bearer {seu_token}"
```

### 3. Projeto de Exemplo
```bash
cd examples/ExampleMicroservice/ExampleMicroservice
dotnet run

# Testar
curl -X GET "https://localhost:7001/api/v1/users" \
  -H "Authorization: Bearer {token}"
```

## 📁 Estrutura do Projeto

```
├── docker-compose.yml              # Orquestração completa
├── kong-microservice-setup.ps1    # Script de configuração Kong
├── src/
│   ├── Fcg.Identity.Api/          # Microserviço de identidade
│   ├── Fcg.Identity.Client/       # 📦 Biblioteca cliente
│   ├── Fcg.Identity.Domain/       # Domínio/interfaces
│   ├── Fcg.Identity.Infrastructure/# Serviços externos
│   └── Fcg.Identity.Shared/       # Modelos compartilhados
├── examples/
│   └── ExampleMicroservice/       # Exemplo de uso
└── packages/
    └── Fcg.Identity.Client.1.0.0.nupkg  # Pacote NuGet
```

## 🔧 Configuração Avançada

### Múltiplas Configurações
```csharp
// Por environment
builder.Services.AddIdentityClient(builder.Configuration);

// URL customizada  
builder.Services.AddIdentityClient("http://custom-identity-service");

// Configuração completa
builder.Services.AddIdentityClient(options =>
{
    options.BaseUrl = "http://localhost:8000";
    options.Timeout = TimeSpan.FromSeconds(30);
    options.EnableRetry = true;
    options.MaxRetryAttempts = 3;
});
```

### Fallback/Resilência
```csharp
// Health check
var isHealthy = await _identityClient.IsHealthyAsync();

// Circuit breaker pattern
services.AddIdentityClient(configuration)
    .AddPolicyHandler(GetRetryPolicy());
```

## 📊 Monitoramento

### Logs
```csharp
builder.Services.AddLogging(config =>
{
    config.AddConsole();
    config.SetMinimumLevel(LogLevel.Debug);
});
```

### Health Checks
```csharp
builder.Services.AddHealthChecks()
    .AddCheck<IdentityServiceHealthCheck>("identity");
```

### Métricas Kong
- Rate limiting
- Request/response times  
- Error rates
- Throughput

## 🤝 Contribuição

1. Fork o projeto
2. Crie sua feature branch (`git checkout -b feature/nova-funcionalidade`)
3. Commit suas mudanças (`git commit -m 'Adiciona nova funcionalidade'`)
4. Push para a branch (`git push origin feature/nova-funcionalidade`)
5. Abra um Pull Request

## 📄 Licença

Este projeto está licenciado sob a Licença MIT - veja o arquivo [LICENSE](LICENSE) para detalhes.

## 🆘 Suporte

- **Documentação**: [README da biblioteca](src/Fcg.Identity.Client/README.md)
- **Exemplo**: [ExampleMicroservice](examples/ExampleMicroservice/README.md)
- **Issues**: [GitHub Issues](https://github.com/kyo1506/tech-challenge-fiap-auth/issues)

---

**Tech Challenge FIAP** - Cloud Computing & DevOps  
*Implementação completa de microserviços com autenticação centralizada*