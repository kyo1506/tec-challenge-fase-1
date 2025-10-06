ARG DOTNET_VERSION=9.0
FROM mcr.microsoft.com/dotnet/sdk:${DOTNET_VERSION}-alpine AS build
WORKDIR /src

ENV HOME=/app
ENV PATH="${PATH}:${HOME}/.dotnet/tools"
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

# This installs the necessary ICU libraries that provide culture data (like pt-BR) for Alpine Linux.
RUN apk add --no-cache icu-libs

# Copy the solution file
COPY *.sln .

# Copy the source code (this creates /src/Fcg.Identity.Api, etc.)
COPY src/ ./src/

# Restore dependencies for the entire solution
RUN dotnet restore "Fcg.Identity.sln"

# Publish the application (New Relic packages already in .csproj)
RUN dotnet publish src/Fcg.Identity.Api/Fcg.Identity.Api.csproj -c Release -o /app/publish --no-restore

# --- Final Stage ---
FROM mcr.microsoft.com/dotnet/aspnet:${DOTNET_VERSION}-alpine AS final
WORKDIR /app

ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false
ENV HOME=/app

# Install basic packages for New Relic
RUN apk add --no-cache icu-libs

# New Relic will be configured via environment variables in Kubernetes
# The NewRelic.Agent NuGet package (already in .csproj) provides the agent

COPY --from=build /app/publish .

# This is good practice for security if you need it

RUN chown -R 0:0 /app && \
    chmod -R g+w /app

EXPOSE 80

# The entrypoint should now correctly point to your application's DLL
ENTRYPOINT ["dotnet", "Fcg.Identity.Api.dll"]