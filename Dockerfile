ARG DOTNET_VERSION=10.0

# Build stage
FROM mcr.microsoft.com/dotnet/sdk:${DOTNET_VERSION}-alpine AS build

# Definir argumentos de build para metadata
ARG BUILD_DATE
ARG VERSION
ARG REVISION

WORKDIR /src

# Copiar solution e código fonte
COPY *.sln .
COPY src/ ./src/

# Restore e publish em etapas separadas
RUN dotnet restore "Fcg.Identity.sln" && \
    dotnet publish src/Fcg.Identity.Api/Fcg.Identity.Api.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:${DOTNET_VERSION}-alpine AS final

# Adicionar labels para metadata
LABEL maintainer="FIAP Cloud Games Team" \
      org.opencontainers.image.title="FCG Identity API" \
      org.opencontainers.image.description="Identity and authentication service for FIAP Cloud Games" \
      org.opencontainers.image.version="${VERSION}" \
      org.opencontainers.image.created="${BUILD_DATE}" \
      org.opencontainers.image.revision="${REVISION}"

# Instalar dependências de segurança e runtime
RUN apk add --no-cache \
    icu-libs \
    ca-certificates \
    tzdata \
    && update-ca-certificates

# Criar usuário não-root
RUN addgroup -g 1001 -S appgroup && \
    adduser -u 1001 -S appuser -G appgroup

WORKDIR /app

# Copiar arquivos publicados
COPY --from=build --chown=appuser:appgroup /app/publish .

# Configurar variáveis de ambiente
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false \
    DOTNET_RUNNING_IN_CONTAINER=true \
    DOTNET_EnableDiagnostics=0 \
    ASPNETCORE_URLS=http://+:8080 \
    TZ=America/Sao_Paulo

# Trocar para usuário não-root
USER appuser

# Expor porta não-privilegiada
EXPOSE 8080

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=40s --retries=3 \
    CMD wget --no-verbose --tries=1 --spider http://localhost:8080/health || exit 1

# Usar exec form para sinais corretos
ENTRYPOINT ["dotnet", "Fcg.Identity.Api.dll"]