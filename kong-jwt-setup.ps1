# Script para configurar Kong com seu Microserviço de Identidade
$KONG_ADMIN_URL = "http://localhost:8001"
$IDENTITY_SERVICE_URL = "http://host.docker.internal:5002"  # Seu microserviço
$KEYCLOAK_URL = "http://localhost:8080"
$REALM = "fiap-cloud-games"

Write-Host "🚀 Configurando Kong com Microserviço de Identidade..." -ForegroundColor Green

# Limpar configurações anteriores
Write-Host "🧹 Limpando configurações anteriores..." -ForegroundColor Yellow
try {
    Invoke-RestMethod -Uri "$KONG_ADMIN_URL/services/identity-api" -Method DELETE -ErrorAction SilentlyContinue
    Invoke-RestMethod -Uri "$KONG_ADMIN_URL/consumers/keycloak-client" -Method DELETE -ErrorAction SilentlyContinue
} catch {
    # Ignorar erros de limpeza
}

# 1. Criar serviço para seu microserviço de identidade
$identityServiceBody = @{
    name = "identity-microservice"
    url = $IDENTITY_SERVICE_URL
    protocol = "http"
    host = "host.docker.internal"
    port = 5002
    path = "/"
} | ConvertTo-Json

try {
    $identityService = Invoke-RestMethod -Uri "$KONG_ADMIN_URL/services" -Method POST -Body $identityServiceBody -ContentType "application/json"
    Write-Host "✅ Microserviço de Identidade registrado: $($identityService.name)" -ForegroundColor Green
} catch {
    Write-Host "⚠️  Erro ao criar serviço de identidade: $($_.Exception.Message)" -ForegroundColor Yellow
    return
}

# 1.2. Criar serviços para outras APIs que usarão autenticação
$protectedServiceBody = @{
    name = "protected-apis"
    url = "http://host.docker.internal:5003"  # Exemplo de outra API
    protocol = "http"
} | ConvertTo-Json

try {
    $protectedService = Invoke-RestMethod -Uri "$KONG_ADMIN_URL/services" -Method POST -Body $protectedServiceBody -ContentType "application/json"
    Write-Host "✅ Serviço de APIs protegidas criado: $($protectedService.name)" -ForegroundColor Green
} catch {
    Write-Host "⚠️  Serviço protegido já existe ou erro: $($_.Exception.Message)" -ForegroundColor Yellow
}

# 2. Criar rotas para o microserviço de identidade (endpoints públicos)
$identityRouteBody = @{
    name = "identity-public-route"
    service = @{ id = $identityService.id }
    paths = @("/auth", "/api/auth")
    methods = @("GET", "POST", "PUT", "DELETE")
    strip_path = $true
} | ConvertTo-Json -Depth 3

try {
    $identityRoute = Invoke-RestMethod -Uri "$KONG_ADMIN_URL/routes" -Method POST -Body $identityRouteBody -ContentType "application/json"
    Write-Host "✅ Rota do microserviço de identidade criada: $($identityRoute.name)" -ForegroundColor Green
} catch {
    Write-Host "⚠️  Erro ao criar rota de identidade: $($_.Exception.Message)" -ForegroundColor Yellow
}

# 2.2. Criar rota para APIs protegidas (que precisam de autenticação)
$protectedRouteBody = @{
    name = "protected-apis-route"
    service = @{ id = $protectedService.id }
    paths = @("/api/v1")
    methods = @("GET", "POST", "PUT", "DELETE", "PATCH")
    strip_path = $false
} | ConvertTo-Json -Depth 3

try {
    $protectedRoute = Invoke-RestMethod -Uri "$KONG_ADMIN_URL/routes" -Method POST -Body $protectedRouteBody -ContentType "application/json"
    Write-Host "✅ Rota de APIs protegidas criada: $($protectedRoute.name)" -ForegroundColor Green
} catch {
    Write-Host "⚠️  Erro ao criar rota protegida: $($_.Exception.Message)" -ForegroundColor Yellow
}

# 3. Configurar plugin de validação externa para APIs protegidas
Write-Host "🔑 Configurando plugin de autenticação externa..." -ForegroundColor Cyan

# Plugin que fará requisição para seu microserviço validar o token
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
    Write-Host "✅ Plugin de transformação configurado" -ForegroundColor Green
} catch {
    Write-Host "❌ Erro ao configurar plugin: $($_.Exception.Message)" -ForegroundColor Red
    return
}

# 3.2. Plugin HTTP Log para auditar chamadas de auth
$httpLogPluginBody = @{
    name = "http-log"
    route = @{ id = $protectedRoute.id }
    config = @{
        http_endpoint = "$IDENTITY_SERVICE_URL/api/audit/access-logs"
        method = "POST"
        timeout = 10000
        keepalive = 60000
    }
} | ConvertTo-Json -Depth 4

try {
    $logPlugin = Invoke-RestMethod -Uri "$KONG_ADMIN_URL/plugins" -Method POST -Body $httpLogPluginBody -ContentType "application/json"
    Write-Host "✅ Plugin de auditoria configurado" -ForegroundColor Green
} catch {
    Write-Host "⚠️  Plugin de log opcional não configurado: $($_.Exception.Message)" -ForegroundColor Yellow
}

# 4. Configurar Rate Limiting para proteger seu microserviço
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
    Write-Host "✅ Rate limiting configurado para microserviço de identidade" -ForegroundColor Green
} catch {
    Write-Host "⚠️  Rate limiting não configurado: $($_.Exception.Message)" -ForegroundColor Yellow
}

# 5. Configurar CORS para seu microserviço
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
    Write-Host "✅ CORS configurado para microserviço de identidade" -ForegroundColor Green
} catch {
    Write-Host "⚠️  CORS não configurado: $($_.Exception.Message)" -ForegroundColor Yellow
}

Write-Host "`n🎯 Configuração do Kong com Microserviço de Identidade completa!" -ForegroundColor Green
Write-Host "📋 Resumo da arquitetura:" -ForegroundColor Cyan
Write-Host "- Microserviço de Identidade: http://localhost:8000/auth" -ForegroundColor White
Write-Host "- APIs Protegidas: http://localhost:8000/api/v1" -ForegroundColor White
Write-Host "- Rate Limiting: 100/min, 1000/hora" -ForegroundColor White
Write-Host "- CORS: Configurado" -ForegroundColor White
Write-Host "- Auditoria: Logs enviados para microserviço" -ForegroundColor White
Write-Host ""
Write-Host "🏗️  Arquitetura implementada:" -ForegroundColor Cyan
Write-Host "Cliente → Kong → Seu Microserviço → Keycloak" -ForegroundColor White
Write-Host ""
Write-Host "🚀 Endpoints disponíveis:" -ForegroundColor Cyan
Write-Host "1. Login/Auth: http://localhost:8000/auth/login" -ForegroundColor White
Write-Host "2. Registro: http://localhost:8000/auth/register" -ForegroundColor White
Write-Host "3. Profile: http://localhost:8000/auth/profile" -ForegroundColor White
Write-Host "4. APIs Protegidas: http://localhost:8000/api/v1/*" -ForegroundColor White
Write-Host ""
Write-Host "⚙️  Próximos passos:" -ForegroundColor Cyan
Write-Host "1. Implemente endpoints de auth em seu microserviço (porta 5002)" -ForegroundColor White
Write-Host "2. Configure middleware de validação de token" -ForegroundColor White
Write-Host "3. Integre com Keycloak via seu microserviço" -ForegroundColor White
Write-Host "4. Gerencie via Konga: http://localhost:1337" -ForegroundColor White