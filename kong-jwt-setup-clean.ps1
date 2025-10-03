# Script para configurar Kong com JWT e Keycloak
$KONG_ADMIN_URL = "http://localhost:8001"
$KEYCLOAK_URL = "http://localhost:8080"
$REALM = "fiap-cloud-games"

Write-Host "Configurando Kong com JWT e Keycloak..." -ForegroundColor Green

# Limpar configuracoes anteriores
Write-Host "Limpando configuracoes anteriores..." -ForegroundColor Yellow
try {
    Invoke-RestMethod -Uri "$KONG_ADMIN_URL/services/identity-api" -Method DELETE -ErrorAction SilentlyContinue
    Invoke-RestMethod -Uri "$KONG_ADMIN_URL/consumers/keycloak-client" -Method DELETE -ErrorAction SilentlyContinue
} catch {
    # Ignorar erros de limpeza
}

# 1. Criar um servico de exemplo
$serviceBody = @{
    name = "identity-api"
    url = "http://host.docker.internal:5002"
} | ConvertTo-Json

try {
    $service = Invoke-RestMethod -Uri "$KONG_ADMIN_URL/services" -Method POST -Body $serviceBody -ContentType "application/json"
    Write-Host "Servico criado: $($service.name)" -ForegroundColor Green
} catch {
    Write-Host "Erro ao criar servico: $($_.Exception.Message)" -ForegroundColor Yellow
    return
}

# 2. Criar uma rota para o servico
$routeBody = @{
    name = "identity-route"
    service = @{ id = $service.id }
    paths = @("/api/identity")
    methods = @("GET", "POST", "PUT", "DELETE")
} | ConvertTo-Json -Depth 3

try {
    $route = Invoke-RestMethod -Uri "$KONG_ADMIN_URL/routes" -Method POST -Body $routeBody -ContentType "application/json"
    Write-Host "Rota criada: $($route.name)" -ForegroundColor Green
} catch {
    Write-Host "Erro ao criar rota: $($_.Exception.Message)" -ForegroundColor Yellow
}

# 3. Configurar plugin JWT
Write-Host "Configurando plugin JWT..." -ForegroundColor Cyan

$jwtPluginBody = @{
    name = "jwt"
    service = @{ id = $service.id }
    config = @{
        header_names = @("Authorization")
        claims_to_verify = @("exp")
        key_claim_name = "iss"
        secret_is_base64 = $false
    }
} | ConvertTo-Json -Depth 4

try {
    $jwtPlugin = Invoke-RestMethod -Uri "$KONG_ADMIN_URL/plugins" -Method POST -Body $jwtPluginBody -ContentType "application/json"
    Write-Host "Plugin JWT configurado" -ForegroundColor Green
} catch {
    Write-Host "Erro ao configurar plugin JWT: $($_.Exception.Message)" -ForegroundColor Red
    if ($_.Exception.Response) {
        $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
        $responseBody = $reader.ReadToEnd()
        Write-Host "Detalhes do erro: $responseBody" -ForegroundColor Red
    }
    return
}

# 4. Criar consumer JWT
$consumerBody = @{
    username = "keycloak-client"
    custom_id = "keycloak-realm"
} | ConvertTo-Json

try {
    $consumer = Invoke-RestMethod -Uri "$KONG_ADMIN_URL/consumers" -Method POST -Body $consumerBody -ContentType "application/json"
    Write-Host "Consumer criado: $($consumer.username)" -ForegroundColor Green
} catch {
    Write-Host "Erro ao criar consumer: $($_.Exception.Message)" -ForegroundColor Yellow
    return
}

# 5. Obter chaves publicas do Keycloak e configurar JWT credential
try {
    Write-Host "Obtendo chaves publicas do Keycloak..." -ForegroundColor Cyan
    $keycloakCerts = Invoke-RestMethod -Uri "$KEYCLOAK_URL/realms/$REALM/protocol/openid-connect/certs"
    Write-Host "Chaves publicas do Keycloak obtidas" -ForegroundColor Green
    
    # Extrair a primeira chave RSA
    $rsaKey = $keycloakCerts.keys | Where-Object { $_.kty -eq "RSA" } | Select-Object -First 1
    
    if ($rsaKey -and $rsaKey.x5c) {
        $publicKeyPem = "-----BEGIN CERTIFICATE-----`n$($rsaKey.x5c[0])`n-----END CERTIFICATE-----"
        
        # Criar JWT credential
        $jwtCredentialBody = @{
            key = "$KEYCLOAK_URL/realms/$REALM"
            algorithm = "RS256"
            rsa_public_key = $publicKeyPem
        } | ConvertTo-Json
        
        $jwtCredential = Invoke-RestMethod -Uri "$KONG_ADMIN_URL/consumers/$($consumer.id)/jwt" -Method POST -Body $jwtCredentialBody -ContentType "application/json"
        Write-Host "Credential JWT criada para o consumer" -ForegroundColor Green
    } else {
        Write-Host "Chave RSA nao encontrada no Keycloak" -ForegroundColor Yellow
    }
    
} catch {
    Write-Host "Erro ao configurar credential JWT: $($_.Exception.Message)" -ForegroundColor Red
    if ($_.Exception.Response) {
        $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
        $responseBody = $reader.ReadToEnd()
        Write-Host "Detalhes do erro: $responseBody" -ForegroundColor Red
    }
}

Write-Host ""
Write-Host "Configuracao completa!" -ForegroundColor Green
Write-Host "Resumo da configuracao:" -ForegroundColor Cyan
Write-Host "- Servico: identity-api" -ForegroundColor White
Write-Host "- Rota: /api/identity" -ForegroundColor White
Write-Host "- Plugin: JWT ativado" -ForegroundColor White
Write-Host "- Consumer: keycloak-client" -ForegroundColor White
Write-Host ""
Write-Host "Como usar:" -ForegroundColor Cyan
Write-Host "1. Obtenha um token JWT do Keycloak" -ForegroundColor White
Write-Host "2. Faca requisicoes para: http://localhost:8000/api/identity" -ForegroundColor White
Write-Host "3. Inclua o header: Authorization: Bearer <seu-jwt-token>" -ForegroundColor White
Write-Host "4. Gerencie via Konga: http://localhost:1337" -ForegroundColor White