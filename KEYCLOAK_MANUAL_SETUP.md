# 🔐 Guia Manual de Configuração do Keycloak

Este guia fornece instruções passo a passo para configurar manualmente o Keycloak após o deploy no Kubernetes.

## 📋 Pré-requisitos

- Sistema deployado no Kubernetes (executar `.\k8s\production\deploy.ps1`)
- Keycloak acessível via port-forward ou Ingress
- Credenciais de admin do Keycloak: `admin / admin`

## 🚀 Passo a Passo

### 1. Acessar o Keycloak Admin Console

```bash
# Fazer port-forward se necessário
kubectl port-forward -n identity-system svc/keycloak-service 8080:8080
```

Acessar: http://localhost:8080/admin

**Credenciais:**
- Usuário: `admin`
- Senha: `admin`

### 2. Criar Realm `fiap-cloud-games`

1. No console admin, clique em **"Master"** (canto superior esquerdo)
2. Clique em **"Create Realm"**
3. Preencha:
   - **Realm name**: `fiap-cloud-games`
   - **Display name**: `FIAP Cloud Games`
   - **Enabled**: ✅
4. Clique em **"Create"**

### 3. Configurar Settings do Realm

1. Vá em **Realm Settings**
2. Na aba **General**:
   - **User registration**: ✅ Enabled
   - **Edit username**: ✅ Enabled
   - **Forgot password**: ✅ Enabled
   - **Remember me**: ✅ Enabled
   - **Login with email**: ✅ Enabled

3. Na aba **Login**:
   - **User registration**: ✅ Enabled
   - **Forgot password**: ✅ Enabled
   - **Remember me**: ✅ Enabled
   - **Login with email**: ✅ Enabled

4. Clique em **"Save"**

### 4. Criar Roles

1. Vá em **Realm Roles**
2. Clique em **"Create Role"**
3. Criar as seguintes roles:

#### Role: admin
- **Role name**: `admin`
- **Description**: `Administrator role for FIAP Cloud Games`
- Clique em **"Save"**

#### Role: user
- **Role name**: `user`
- **Description**: `Regular user role for FIAP Cloud Games`
- Clique em **"Save"**

#### Role: manager (opcional)
- **Role name**: `manager`
- **Description**: `Manager role for FIAP Cloud Games`
- Clique em **"Save"**

### 5. Criar Client `fcg-api`

1. Vá em **Clients**
2. Clique em **"Create Client"**
3. **General Settings**:
   - **Client type**: `OpenID Connect`
   - **Client ID**: `fcg-api`
   - Clique em **"Next"**

4. **Capability config**:
   - **Client authentication**: ✅ ON
   - **Authorization**: ❌ OFF
   - **Standard flow**: ✅ ON
   - **Direct access grants**: ✅ ON
   - **Service accounts roles**: ✅ ON
   - Clique em **"Next"**

5. **Login settings**:
   - **Valid redirect URIs**: `*`
   - **Valid post logout redirect URIs**: `*`
   - **Web origins**: `*`
   - Clique em **"Save"**

### 6. Configurar Client Secret

1. Na aba **Credentials** do client `fcg-api`
2. **Client Authenticator**: `Client Id and Secret`
3. Anotar o **Client Secret** gerado (ou regenerar se necessário)

**Exemplo de secret**: `2HUm0LauNhHn5Swn7yS0brWmamRohCYK`

### 7. Configurar Client Scopes

1. Vá em **Client Scopes**
2. Clique em **"Create Client Scope"**

#### Scope: users:manage
- **Name**: `users:manage`
- **Description**: `Manage users permission`
- **Type**: `Optional`
- **Include in token scope**: ✅ ON
- Clique em **"Save"**

#### Scope: users:read
- **Name**: `users:read`
- **Description**: `Read users permission`
- **Type**: `Optional`
- **Include in token scope**: ✅ ON
- Clique em **"Save"**

#### Scope: profiles:manage
- **Name**: `profiles:manage`
- **Description**: `Manage profiles permission`
- **Type**: `Optional`
- **Include in token scope**: ✅ ON
- Clique em **"Save"**

### 8. Associar Scopes ao Client

1. Vá em **Clients** → **fcg-api**
2. Na aba **Client Scopes**
3. Clique em **"Add client scope"**
4. Adicionar como **Default**:
   - `users:manage`
   - `users:read`
   - `profiles:manage`

### 9. Configurar Audience Mapper

1. Em **Clients** → **fcg-api** → aba **Client scopes**
2. Clique no scope **fcg-api-dedicated**
3. Clique em **"Add mapper"** → **"By configuration"**
4. Selecione **"Audience"**
5. Configure:
   - **Name**: `fcg-api-audience`
   - **Included Client Audience**: `fcg-api`
   - **Add to ID token**: ❌ OFF
   - **Add to access token**: ✅ ON
   - **Add to token introspection**: ✅ ON
6. Clique em **"Save"**

### 10. Criar Service Account no Master Realm

1. Troque para o realm **Master** (canto superior esquerdo)
2. Vá em **Clients** → **"Create Client"**
3. Configure:
   - **Client ID**: `fcg-api-service-account`
   - **Name**: `FCG Identity API Service Account`
   - **Client authentication**: ✅ ON
   - **Service accounts roles**: ✅ ON
   - **Standard flow**: ❌ OFF
   - **Direct access grants**: ❌ OFF

4. Em **Credentials**, anotar o **Client Secret**

### 11. Criar Usuário de Teste (Opcional)

1. Volte para o realm **fiap-cloud-games**
2. Vá em **Users** → **"Add user"**
3. Configure:
   - **Username**: `testuser`
   - **Email**: `test@fiap.com.br`
   - **First name**: `Test`
   - **Last name**: `User`
   - **Email verified**: ✅ ON
   - **Enabled**: ✅ ON

4. Clique em **"Create"**
5. Na aba **Credentials**:
   - **Password**: `Test@123`
   - **Temporary**: ❌ OFF
   - Clique em **"Set password"**

6. Na aba **Role mapping**:
   - Clique em **"Assign role"**
   - Selecionar **"user"**
   - Clique em **"Assign"**

## 🧪 Testando a Configuração

### 1. Obter Token via Client Credentials

```bash
curl -X POST "http://localhost:8080/realms/fiap-cloud-games/protocol/openid-connect/token" \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -d "grant_type=client_credentials&client_id=fcg-api&client_secret=txCC3nj6snXNYWsIVPNJV2zeeoTyMSBO"
```

### 2. Obter Token via Password Grant (usuário)

```bash
curl -X POST "http://localhost:8080/realms/fiap-cloud-games/protocol/openid-connect/token" \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -d "grant_type=password&client_id=fcg-api&client_secret=txCC3nj6snXNYWsIVPNJV2zeeoTyMSBO&username=testuser&password=Test@123&scope=openid profile users:read"
```

### 3. Validar Token

```bash
curl -X POST "http://localhost:8080/realms/fiap-cloud-games/protocol/openid-connect/token/introspect" \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -d "token=SEU_ACCESS_TOKEN&client_id=fcg-api&client_secret=txCC3nj6snXNYWsIVPNJV2zeeoTyMSBO"
```

## 📝 Credenciais Importantes

Anote estas informações após a configuração:

```yaml
# Keycloak Configuration
KEYCLOAK_URL: http://localhost:8080
REALM: fiap-cloud-games

# Client fcg-api
CLIENT_ID: fcg-api
CLIENT_SECRET: txCC3nj6snXNYWsIVPNJV2zeeoTyMSBO

# Service Account (Master Realm)
SERVICE_ACCOUNT_CLIENT_ID: fcg-api-service-account  
SERVICE_ACCOUNT_CLIENT_SECRET: MtgWaDXicoDcGpIQzIYUf2ulMQT1nkrV

# Test User
USERNAME: testuser
PASSWORD: Test@123
```

## 🔧 Integração com a API

Após a configuração, atualize as configurações da sua API Identity:

```json
{
  "Keycloak": {
    "BaseUrl": "http://keycloak-service:8080",
    "Realm": "fiap-cloud-games",
    "ClientId": "fcg-api",
    "ClientSecret": "txCC3nj6snXNYWsIVPNJV2zeeoTyMSBO"
  }
}
```

## ✅ Verificação Final

- [ ] Realm `fiap-cloud-games` criado
- [ ] Roles `admin`, `user` criadas
- [ ] Client `fcg-api` configurado com secret
- [ ] Client scopes `users:manage`, `users:read`, `profiles:manage` criados
- [ ] Audience mapper configurado
- [ ] Service account no master realm criado
- [ ] Usuário de teste criado (opcional)
- [ ] Tokens sendo gerados com sucesso
- [ ] API consegue validar tokens

## 🚨 Troubleshooting

### Token inválido ou expirado
- Verificar se o client secret está correto
- Conferir se o realm está correto
- Validar se o usuário tem as roles necessárias

### Erro de audience
- Verificar se o audience mapper foi configurado
- Conferir se o client ID está correto no mapper

### Erro de scope
- Verificar se os client scopes foram criados
- Conferir se foram associados ao client como "Optional"
- Validar se estão sendo solicitados na requisição de token

---

**📚 Documentação Oficial:** https://www.keycloak.org/documentation