# syntax=docker/dockerfile:1.7

ARG DOTNET_VERSION=10.0

FROM mcr.microsoft.com/dotnet/sdk:${DOTNET_VERSION} AS restore
WORKDIR /src

COPY Directory.Build.props Directory.Packages.props ./
COPY .config/dotnet-tools.json .config/dotnet-tools.json
COPY src/Web/Web.csproj src/Web/
RUN dotnet restore src/Web/Web.csproj

FROM restore AS build
COPY src/Web/ src/Web/
RUN dotnet build src/Web/Web.csproj \
    --configuration Release \
    --no-restore

FROM build AS publish
RUN dotnet publish src/Web/Web.csproj \
    --configuration Release \
    --no-build \
    --output /app/publish \
    /p:UseAppHost=false

FROM build AS migration-build
RUN dotnet tool restore
RUN ConnectionStrings__DefaultConnection="Host=localhost;Database=publicpulse;Username=postgres;Password=not-used" \
    dotnet tool run dotnet-ef migrations bundle \
    --project src/Web/Web.csproj \
    --startup-project src/Web/Web.csproj \
    --configuration Release \
    --no-build \
    --output /app/efbundle

FROM mcr.microsoft.com/dotnet/aspnet:${DOTNET_VERSION} AS runtime-base
USER root
RUN apt-get update \
    && apt-get install --yes --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080 \
    DOTNET_EnableDiagnostics=0
EXPOSE 8080

FROM runtime-base AS runtime
COPY --from=publish --chown=app:app /app/publish/ ./
USER app
HEALTHCHECK --interval=30s --timeout=5s --start-period=10s --retries=3 \
    CMD curl --fail --silent --show-error http://127.0.0.1:8080/health/live || exit 1
ENTRYPOINT ["dotnet", "Web.dll"]

FROM runtime-base AS migrator
COPY --from=publish --chown=app:app /app/publish/appsettings*.json ./
COPY --from=migration-build --chown=app:app --chmod=0755 /app/efbundle ./efbundle
USER app
ENTRYPOINT ["./efbundle"]
