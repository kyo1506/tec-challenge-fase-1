# Script para configurar Kong com seu Microservico de Identidade
$KONG_ADMIN_URL = "http://localhost:8001"
$IDENTITY_SERVICE_URL = "http://host.docker.internal:5001"  # Seu microservico (HTTP)
$KEYCLOAK_URL = "http://localhost:8080"
$REALM = "fiap-cloud-games"

Write-Host "Configurando Kong com Microservico de Identidade..." -ForegroundColor Green

# Limpar configuracoes anteriores
Write-Host "Limpando configuracoes anteriores..." -ForegroundColor Yellow
try {
    Invoke-RestMethod -Uri "$KONG_ADMIN_URL/services/identity-microservice" -Method DELETE -ErrorAction SilentlyContinue
    Invoke-RestMethod -Uri "$KONG_ADMIN_URL/services/protected-apis" -Method DELETE -ErrorAction SilentlyContinue
} catch {
    # Ignorar erros de limpeza
}

# 1. Criar servico para seu microservico de identidade
$identityServiceBody = @{
    name = "identity-microservice"
    url = $IDENTITY_SERVICE_URL
    protocol = "http"
    host = "host.docker.internal"
    port = 5001
    path = "/"
} | ConvertTo-Json

try {
    $identityService = Invoke-RestMethod -Uri "$KONG_ADMIN_URL/services" -Method POST -Body $identityServiceBody -ContentType "application/json"
    Write-Host "Microservico de Identidade registrado: $($identityService.name)" -ForegroundColor Green
} catch {
    Write-Host "Erro ao criar servico de identidade: $($_.Exception.Message)" -ForegroundColor Yellow
    return
}

# 1.2. Criar servicos para outras APIs que usarao autenticacao
$protectedServiceBody = @{
    name = "protected-apis"
    url = "http://host.docker.internal:5003"  # Exemplo de outra API
    protocol = "http"
} | ConvertTo-Json

try {
    $protectedService = Invoke-RestMethod -Uri "$KONG_ADMIN_URL/services" -Method POST -Body $protectedServiceBody -ContentType "application/json"
    Write-Host "Servico de APIs protegidas criado: $($protectedService.name)" -ForegroundColor Green
} catch {
    Write-Host "Servico protegido ja existe ou erro: $($_.Exception.Message)" -ForegroundColor Yellow
}

# 2. Criar rotas para o microservico de identidade (endpoints publicos)
$identityRouteBody = @{
    name = "identity-public-route"
    service = @{ id = $identityService.id }
    paths = @("/v1", "/api/v1/auth")
    methods = @("GET", "POST", "PUT", "DELETE")
    strip_path = $false  # Manter o path original para preservar /v1
} | ConvertTo-Json -Depth 3

try {
    $identityRoute = Invoke-RestMethod -Uri "$KONG_ADMIN_URL/routes" -Method POST -Body $identityRouteBody -ContentType "application/json"
    Write-Host "Rota do microservico de identidade criada: $($identityRoute.name)" -ForegroundColor Green
} catch {
    Write-Host "Erro ao criar rota de identidade: $($_.Exception.Message)" -ForegroundColor Yellow
}

# 2.2. Criar rota para APIs protegidas (que precisam de autenticacao)
$protectedRouteBody = @{
    name = "protected-apis-route"
    service = @{ id = $protectedService.id }
    paths = @("/api/v1")
    methods = @("GET", "POST", "PUT", "DELETE", "PATCH")
    strip_path = $false
} | ConvertTo-Json -Depth 3

try {
    $protectedRoute = Invoke-RestMethod -Uri "$KONG_ADMIN_URL/routes" -Method POST -Body $protectedRouteBody -ContentType "application/json"
    Write-Host "Rota de APIs protegidas criada: $($protectedRoute.name)" -ForegroundColor Green
} catch {
    Write-Host "Erro ao criar rota protegida: $($_.Exception.Message)" -ForegroundColor Yellow
}

# 3. Configurar plugin de validacao externa para APIs protegidas
Write-Host "Configurando plugins..." -ForegroundColor Cyan

# Plugin que fara requisicao para seu microservico validar o token
$authPluginBody = @{
    name = "request-transformer"
    route = @{ id = $protectedRoute.id }
    config = @{
        add = @{
            headers = @("X-Auth-Service:identity-microservice")
        }
    }
} | ConvertTo-Json -Depth 4

try {
    $authPlugin = Invoke-RestMethod -Uri "$KONG_ADMIN_URL/plugins" -Method POST -Body $authPluginBody -ContentType "application/json"
    Write-Host "Plugin de transformacao configurado" -ForegroundColor Green
} catch {
    Write-Host "Erro ao configurar plugin: $($_.Exception.Message)" -ForegroundColor Red
}

# 4. Configurar Rate Limiting para proteger seu microservico
$rateLimitPluginBody = @{
    name = "rate-limiting"
    service = @{ id = $identityService.id }
    config = @{
        minute = 100
        hour = 1000
        policy = "local"
        fault_tolerant = $true
        hide_client_headers = $false
    }
} | ConvertTo-Json -Depth 4

try {
    $rateLimitPlugin = Invoke-RestMethod -Uri "$KONG_ADMIN_URL/plugins" -Method POST -Body $rateLimitPluginBody -ContentType "application/json"
    Write-Host "Rate limiting configurado para microservico de identidade" -ForegroundColor Green
} catch {
    Write-Host "Rate limiting nao configurado: $($_.Exception.Message)" -ForegroundColor Yellow
}

# 5. Configurar CORS para seu microservico
$corsPluginBody = @{
    name = "cors"
    service = @{ id = $identityService.id }
    config = @{
        origins = @("*")
        methods = @("GET", "POST", "PUT", "DELETE", "OPTIONS")
        headers = @("Accept", "Accept-Version", "Content-Length", "Content-MD5", "Content-Type", "Date", "Authorization")
        exposed_headers = @("X-Auth-Token")
        credentials = $true
        max_age = 3600
    }
} | ConvertTo-Json -Depth 4

try {
    $corsPlugin = Invoke-RestMethod -Uri "$KONG_ADMIN_URL/plugins" -Method POST -Body $corsPluginBody -ContentType "application/json"
    Write-Host "CORS configurado para microservico de identidade" -ForegroundColor Green
} catch {
    Write-Host "CORS nao configurado: $($_.Exception.Message)" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "Configuracao do Kong com Microservico de Identidade completa!" -ForegroundColor Green
Write-Host "Resumo da arquitetura:" -ForegroundColor Cyan
Write-Host "- Microservico de Identidade: http://localhost:8000/v1" -ForegroundColor White
Write-Host "- APIs Protegidas: http://localhost:8000/api/v1" -ForegroundColor White
Write-Host "- Rate Limiting: 100/min, 1000/hora" -ForegroundColor White
Write-Host "- CORS: Configurado" -ForegroundColor White
Write-Host ""
Write-Host "Arquitetura implementada:" -ForegroundColor Cyan
Write-Host "Cliente -> Kong -> Seu Microservico -> Keycloak" -ForegroundColor White
Write-Host ""
Write-Host "Endpoints disponiveis:" -ForegroundColor Cyan
Write-Host "1. Login: http://localhost:8000/v1/login" -ForegroundColor White
Write-Host "2. Registro: http://localhost:8000/v1/register" -ForegroundColor White
Write-Host "3. Profile: http://localhost:8000/v1/profile" -ForegroundColor White
Write-Host "4. Usuarios: http://localhost:8000/v1/users" -ForegroundColor White
Write-Host "5. APIs Protegidas: http://localhost:8000/api/v1/*" -ForegroundColor White
Write-Host ""
Write-Host "Proximos passos:" -ForegroundColor Cyan
Write-Host "1. Implemente endpoints de auth em seu microservico (porta 5001)" -ForegroundColor White
Write-Host "2. Configure middleware de validacao de token" -ForegroundColor White
Write-Host "3. Integre com Keycloak via seu microservico" -ForegroundColor White
Write-Host "4. Gerencie via Konga: http://localhost:1337" -ForegroundColor White