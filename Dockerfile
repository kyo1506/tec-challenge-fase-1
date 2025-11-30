ARG DOTNET_VERSION=10.0
FROM mcr.microsoft.com/dotnet/sdk:${DOTNET_VERSION}-alpine AS build
WORKDIR /src

ENV HOME=/root
ENV DOTNET_CLI_HOME=/root/.dotnet

RUN mkdir -p $DOTNET_CLI_HOME && \
    mkdir -p $HOME && \
    chmod -R 777 $HOME

ENV PATH="${PATH}:${HOME}/.dotnet/tools"
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

RUN apk add --no-cache icu-libs

COPY *.sln .
COPY src/ ./src/

RUN dotnet restore "Fcg.Identity.sln"

RUN dotnet publish src/Fcg.Identity.Api/Fcg.Identity.Api.csproj -c Release -o /app/publish --no-restore --verbosity diag

FROM mcr.microsoft.com/dotnet/aspnet:${DOTNET_VERSION}-alpine AS final
WORKDIR /app

ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

RUN apk add --no-cache icu-libs

COPY --from=build /app/publish .

RUN chown -R 0:0 /app && chmod -R g+w /app

EXPOSE 80

ENTRYPOINT ["dotnet", "Fcg.Identity.Api.dll"]