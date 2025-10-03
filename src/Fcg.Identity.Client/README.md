# FCG Identity Client

Biblioteca cliente para integração com o microserviço de identidade FCG. Fornece autenticação e autorização centralizadas para outros microserviços.

## 📦 Instalação

```bash
# Em desenvolvimento, referencie o projeto diretamente
dotnet add reference ../Fcg.Identity.Client/Fcg.Identity.Client.csproj

# Em produção, instale via NuGet (quando publicado)
dotnet add package Fcg.Identity.Client
```

## 🚀 Configuração Rápida

### 1. No `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "IdentityService": "http://localhost:8000"
  }
}
```

### 2. No `Program.cs` ou `Startup.cs`:

```csharp
using Fcg.Identity.Client.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Adicionar o cliente de identidade
builder.Services.AddIdentityClient(builder.Configuration);

var app = builder.Build();

// Usar middleware de autenticação automática
app.UseIdentityAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
```

## �️ Configurações Avançadas

### Configuração Personalizada

```csharp
services.AddIdentityClient(options =>
{
    options.BaseUrl = "http://identity-service:5001";
    options.Timeout = TimeSpan.FromSeconds(60);
    options.DefaultHeaders.Add("X-Service-Name", "users-microservice");
    options.EnableRetry = true;
    options.MaxRetryAttempts = 5;
});
```

### Configuração via IConfiguration

```csharp
// Usando ConnectionStrings
services.AddIdentityClient(configuration);

// OU usando seção específica
services.AddIdentityClient(configuration, "IdentityClient");
```

## 🔐 Atributos de Autorização

### Uso Básico nos Controllers

```csharp
[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    [HttpGet]
    [RequirePermission("users", "read")]
    public async Task<IActionResult> GetUsers()
    {
        // Usuário já foi autenticado e autorizado automaticamente
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Ok(new { message = "Lista de usuários", userId });
    }

    [HttpPost]
    [RequirePermission("users", "write")]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request)
    {
        // Lógica para criar usuário
        return Ok(new { message = "Usuário criado" });
    }

    [HttpDelete("{id}")]
    [RequirePermission("users", "delete")]
    public async Task<IActionResult> DeleteUser(int id)
    {
        // Lógica para deletar usuário
        return Ok(new { message = $"Usuário {id} deletado" });
    }
}
```

### Múltiplas Permissões

```csharp
[HttpGet("reports")]
[RequirePermission("reports", "read")]
[RequirePermission("users", "read")] // Precisa das duas permissões
public async Task<IActionResult> GetUserReports()
{
    return Ok(new { message = "Relatórios de usuários" });
}
```

## 🔧 Uso Manual do Cliente

```csharp
public class UsersController : ControllerBase
{
    private readonly IIdentityClient _identityClient;

    public UsersController(IIdentityClient identityClient)
    {
        _identityClient = identityClient;
    }

    [HttpGet("manual-validation")]
    public async Task<IActionResult> GetUsersManual()
    {
        var token = GetTokenFromRequest();
        
        // Validar token
        var user = await _identityClient.ValidateTokenAsync(token);
        if (user == null)
            return Unauthorized();

        // Verificar permissão
        var hasPermission = await _identityClient.ValidatePermissionAsync(token, "users", "read");
        if (!hasPermission)
            return Forbid("Sem permissão para listar usuários");

        // Lógica do controller
        var users = GetAllUsers();
        return Ok(users);
    }
    
    private string? GetTokenFromRequest()
    {
        return Request.Headers["Authorization"]
            .FirstOrDefault()?
            .Replace("Bearer ", "");
    }
}
```

## 🏗️ Padrões de Autorização

### Recursos e Ações Suportadas

- **users**: `read`, `write`, `delete`, `manage`
- **games**: `read`, `write`, `delete`, `manage`  
- **orders**: `read`, `write`, `delete`, `manage`
- **reports**: `read`, `generate`
- **profile**: `read`, `write`

### Matriz de Permissões por Role

| Role | users | games | orders | reports | profile |
|------|-------|-------|--------|---------|---------|
| **admin** | manage | manage | manage | read, generate | read, write |
| **manager** | read, write | read, write, delete | read, write | read | read, write |
| **user/customer** | - | read | read, write* | - | read, write* |

*Apenas próprios recursos

## 📊 Health Check

```csharp
[HttpGet("health")]
public async Task<IActionResult> CheckHealth([FromServices] IIdentityClient identityClient)
{
    var isHealthy = await identityClient.IsHealthyAsync();
    return isHealthy ? Ok("Identity service is healthy") : StatusCode(503, "Identity service unavailable");
}
```

## 🔧 Configuração por Ambiente

### appsettings.Development.json
```json
{
  "ConnectionStrings": {
    "IdentityService": "http://localhost:5001"
  }
}
```

### appsettings.Production.json (via Kong)
```json
{
  "ConnectionStrings": {
    "IdentityService": "http://kong-gateway:8000"
  }
}
```

### Docker Compose
```yaml
version: '3.8'
services:
  users-microservice:
    build: .
    environment:
      - ConnectionStrings__IdentityService=http://identity-microservice:5001
    depends_on:
      - identity-microservice

  identity-microservice:
    image: fcg-identity-api
    ports:
      - "5001:5001"
```

## 🚨 Tratamento de Erros

### Middleware de Fallback

```csharp
public class IdentityFallbackMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<IdentityFallbackMiddleware> _logger;

    public IdentityFallbackMiddleware(RequestDelegate next, ILogger<IdentityFallbackMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, IIdentityClient identityClient)
    {
        try
        {
            // Verificar se serviço de identidade está disponível
            var isHealthy = await identityClient.IsHealthyAsync();
            if (!isHealthy)
            {
                _logger.LogWarning("Identity service is not healthy");
                // Implementar fallback: autenticação local, modo degradado, etc.
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking identity service health");
            // Implementar estratégia de fallback
        }

        await _next(context);
    }
    {
        // Apenas usuários com permissão "users:delete" chegam aqui
        DeleteUserById(id);
        return NoContent();
    }
}
```

## 🎯 Permissões Disponíveis

O microserviço de identidade suporta as seguintes permissões por padrão:

### Roles e Permissões

| Role | Permissões |
|------|------------|
| **admin** | `users:*`, `games:*`, `orders:*`, `reports:*`, `system:admin` |
| **manager** | `users:read`, `users:write`, `games:*`, `orders:*`, `reports:read` |
| **user** | `profile:*`, `games:read`, `orders:read`, `orders:write` |
| **customer** | `profile:*`, `games:read`, `orders:read`, `orders:write` |

### Recursos Comuns

- **users**: `read`, `write`, `delete`, `manage`
- **games**: `read`, `write`, `delete`, `manage`  
- **orders**: `read`, `write`, `delete`, `manage`
- **profile**: `read`, `write`
- **reports**: `read`, `generate`
- **system**: `admin`

## 🔍 Exemplos de Uso

### Microserviço de Usuários

```csharp
[ApiController]
[Route("api/v1/[controller]")]
public class UsersController : ControllerBase
{
    [HttpGet]
    [RequirePermission("users", "read")]
    public async Task<IActionResult> ListUsers() { /* ... */ }

    [HttpPost]
    [RequirePermission("users", "write")]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request) { /* ... */ }

    [HttpDelete("{id}")]
    [RequirePermission("users", "delete")]
    public async Task<IActionResult> DeleteUser(int id) { /* ... */ }
}
```

### Microserviço de Jogos

```csharp
[ApiController]
[Route("api/v1/[controller]")]
public class GamesController : ControllerBase
{
    [HttpGet]
    [RequirePermission("games", "read")]
    public async Task<IActionResult> ListGames() { /* ... */ }

    [HttpPost]
    [RequirePermission("games", "write")]
    public async Task<IActionResult> CreateGame([FromBody] CreateGameRequest request) { /* ... */ }

    [HttpPut("{id}")]
    [RequirePermission("games", "write")]
    public async Task<IActionResult> UpdateGame(int id, [FromBody] UpdateGameRequest request) { /* ... */ }
}
```

### Microserviço de Pedidos

```csharp
[ApiController]
[Route("api/v1/[controller]")]
public class OrdersController : ControllerBase
{
    [HttpGet]
    [RequirePermission("orders", "read")]
    public async Task<IActionResult> ListOrders() { /* ... */ }

    [HttpPost]
    [RequirePermission("orders", "write")]
    public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequest request) { /* ... */ }

    [HttpGet("my-orders")]
    [RequirePermission("orders", "read")]
    public async Task<IActionResult> GetMyOrders()
    {
        var userId = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        // Retornar apenas pedidos do usuário atual
        return Ok(GetOrdersByUserId(userId));
    }
}
```

## 🛡️ Health Check

```csharp
[HttpGet("health")]
public async Task<IActionResult> HealthCheck([FromServices] IIdentityClient identityClient)
{
    var isHealthy = await identityClient.IsHealthyAsync();
    
    return Ok(new 
    { 
        service = "Users Microservice",
        status = isHealthy ? "healthy" : "unhealthy",
        identity_service = isHealthy ? "connected" : "disconnected",
        timestamp = DateTime.UtcNow
    });
}
```

## 🔧 Configuração Avançada

### Timeout Personalizado

```csharp
builder.Services.AddIdentityClient(client =>
{
    client.BaseAddress = new Uri("http://localhost:8000");
    client.Timeout = TimeSpan.FromSeconds(10);
    client.DefaultRequestHeaders.Add("X-Service-Name", "users-microservice");
});
```

### Com Políticas de Retry

```csharp
using Polly;
using Polly.Extensions.Http;

builder.Services.AddIdentityClient("http://localhost:8000")
    .AddPolicyHandler(GetRetryPolicy());

static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
{
    return HttpPolicyExtensions
        .HandleTransientHttpError()
        .WaitAndRetryAsync(
            retryCount: 3,
            sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));
}
```

## 🚨 Tratamento de Erros

```csharp
[HttpGet]
public async Task<IActionResult> GetProtectedResource([FromServices] IIdentityClient identityClient)
{
    try
    {
        var token = Request.Headers["Authorization"]
            .FirstOrDefault()?.Replace("Bearer ", "");

        var user = await identityClient.ValidateTokenAsync(token);
        
        if (user == null)
        {
            return Unauthorized(new { message = "Token inválido ou expirado" });
        }

        // Verificar se o serviço está saudável
        var isHealthy = await identityClient.IsHealthyAsync();
        if (!isHealthy)
        {
            return StatusCode(503, new { message = "Serviço de identidade indisponível" });
        }

        return Ok(new { message = "Recurso protegido acessado com sucesso", user });
    }
    catch (HttpRequestException ex)
    {
        return StatusCode(503, new { message = "Erro de comunicação com serviço de identidade", error = ex.Message });
    }
    catch (TaskCanceledException ex)
    {
        return StatusCode(408, new { message = "Timeout na comunicação com serviço de identidade", error = ex.Message });
    }
    catch (Exception ex)
    {
        return StatusCode(500, new { message = "Erro interno", error = ex.Message });
    }
}
```

## 📚 Referência de API

### IIdentityClient Interface

```csharp
public interface IIdentityClient
{
    /// <summary>
    /// Valida um token JWT e retorna as informações do usuário
    /// </summary>
    /// <param name="token">Token JWT para validar</param>
    /// <returns>Informações do usuário ou null se inválido</returns>
    Task<AuthenticatedUser?> ValidateTokenAsync(string token);

    /// <summary>
    /// Valida se o usuário tem permissão para executar uma ação em um recurso
    /// </summary>
    /// <param name="token">Token JWT do usuário</param>
    /// <param name="resource">Recurso (ex: "users", "games")</param>
    /// <param name="action">Ação (ex: "read", "write", "delete")</param>
    /// <returns>True se autorizado, false caso contrário</returns>
    Task<bool> ValidatePermissionAsync(string token, string resource, string action);

    /// <summary>
    /// Verifica se o serviço de identidade está disponível
    /// </summary>
    /// <returns>True se saudável, false caso contrário</returns>
    Task<bool> IsHealthyAsync();
}
```

### AuthenticatedUser Model

```csharp
public class AuthenticatedUser
{
    public Guid UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public IEnumerable<string> Roles { get; set; } = new List<string>();
    public DateTime? ExpiresAt { get; set; }
}
```

### RequirePermissionAttribute

```csharp
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true)]
public class RequirePermissionAttribute : Attribute, IAuthorizationFilter
{
    public string Resource { get; }
    public string Action { get; }

    public RequirePermissionAttribute(string resource, string action)
    {
        Resource = resource;
        Action = action;
    }
}
```

## 📝 Exemplo Completo de Projeto

### Program.cs Completo

```csharp
using Fcg.Identity.Client.Extensions;
using Microsoft.AspNetCore.Authorization;

var builder = WebApplication.CreateBuilder(args);

// Adicionar serviços
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configurar cliente de identidade
builder.Services.AddIdentityClient(builder.Configuration);

// Configurar autorização
builder.Services.AddAuthorization();

var app = builder.Build();

// Configurar pipeline de desenvolvimento
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Pipeline de requisição
app.UseRouting();

// IMPORTANTE: UseIdentityAuthentication deve vir ANTES de UseAuthorization
app.UseIdentityAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
```

### appsettings.json Completo

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "ConnectionStrings": {
    "IdentityService": "http://localhost:8000"
  },
  "IdentityClient": {
    "BaseUrl": "http://localhost:8000",
    "Timeout": "00:00:30",
    "EnableRetry": true,
    "MaxRetryAttempts": 3,
    "DefaultHeaders": {
      "X-Service-Name": "example-microservice"
    }
  }
}
```

## 🧪 Testes

### Teste de Integração

```csharp
[Test]
public async Task ValidateToken_ValidToken_ReturnsUser()
{
    // Arrange
    var client = new IdentityClient(httpClient, configuration);
    var validToken = "valid-jwt-token";

    // Act
    var result = await client.ValidateTokenAsync(validToken);

    // Assert
    Assert.That(result, Is.Not.Null);
    Assert.That(result.Email, Is.Not.Empty);
}

[Test]
public async Task ValidatePermission_UserHasPermission_ReturnsTrue()
{
    // Arrange
    var client = new IdentityClient(httpClient, configuration);
    var token = "user-with-read-permission";

    // Act
    var result = await client.ValidatePermissionAsync(token, "users", "read");

    // Assert
    Assert.That(result, Is.True);
}
```

## 🐛 Troubleshooting

### Problemas Comuns

1. **401 Unauthorized**: Verifique se o token está sendo enviado corretamente no header `Authorization: Bearer <token>`

2. **503 Service Unavailable**: O serviço de identidade pode estar indisponível. Verifique:
   - Se o Kong Gateway está rodando
   - Se o microserviço de identidade está rodando
   - Se a URL está configurada corretamente

3. **Timeout**: Configure timeout adequado para seu ambiente:
   ```csharp
   services.AddIdentityClient(options => 
   {
       options.Timeout = TimeSpan.FromSeconds(60);
   });
   ```

### Logs para Debug

```csharp
builder.Services.AddLogging(config =>
{
    config.AddConsole();
    config.SetMinimumLevel(LogLevel.Debug);
});
```

## 🤝 Contribuição

1. Fork o projeto
2. Crie sua feature branch (`git checkout -b feature/nova-funcionalidade`)
3. Commit suas mudanças (`git commit -m 'Adiciona nova funcionalidade'`)
4. Push para a branch (`git push origin feature/nova-funcionalidade`)
5. Abra um Pull Request

## 📄 Licença

Este projeto está licenciado sob a Licença MIT - veja o arquivo [LICENSE](../../../LICENSE) para detalhes.
        }

        return Ok(new { data = "Recurso protegido", user = user.Username });
    }
    catch (HttpRequestException)
    {
        return StatusCode(503, new { message = "Erro de comunicação com serviço de identidade" });
    }
    catch (Exception)
    {
        return StatusCode(500, new { message = "Erro interno do servidor" });
    }
}
```

## 📊 Métricas e Logs

Adicione logs para monitorar a integração:

```csharp
[HttpGet]
public async Task<IActionResult> GetUsers(
    [FromServices] IIdentityClient identityClient,
    [FromServices] ILogger<UsersController> logger)
{
    var token = Request.Headers["Authorization"]
        .FirstOrDefault()?.Replace("Bearer ", "");

    logger.LogInformation("Validando token para listagem de usuários");
    
    var user = await identityClient.ValidateTokenAsync(token);
    
    if (user == null)
    {
        logger.LogWarning("Tentativa de acesso com token inválido");
        return Unauthorized();
    }

    logger.LogInformation("Usuário {Username} autenticado com sucesso", user.Username);
    
    var hasPermission = await identityClient.ValidatePermissionAsync(token, "users", "read");
    
    if (!hasPermission)
    {
        logger.LogWarning("Usuário {Username} sem permissão para listar usuários", user.Username);
        return Forbid();
    }

    logger.LogInformation("Usuário {Username} autorizado para listar usuários", user.Username);
    
    var users = GetAllUsers();
    return Ok(users);
}
```

## ✨ Conclusão

Com este cliente, seus microserviços podem:

✅ **Validar tokens automaticamente**  
✅ **Verificar permissões granulares**  
✅ **Acessar informações do usuário**  
✅ **Implementar autorização baseada em roles**  
✅ **Monitorar saúde do serviço de identidade**  

O cliente abstrai toda a complexidade de comunicação com o microserviço de identidade, permitindo que você foque na lógica de negócio dos seus microserviços! 🚀