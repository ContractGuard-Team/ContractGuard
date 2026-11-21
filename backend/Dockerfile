# syntax=docker/dockerfile:1
# Build context is the REPOSITORY ROOT (not backend/), because the API embeds
# contracts/extraction.v2.schema.json. Build with:
#   docker build -f backend/Dockerfile -t contractguard-api .
# or through compose: docker compose --profile app up --build

# ---- build: restore, test-free publish, EF migrations bundle ---------------------------
# The SDK runs on the build machine's own architecture and cross-compiles for the target (ECS runs
# ARM64, SD-02): running the SDK under QEMU emulation is slow and unreliable.
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:10.0.403 AS build
ARG TARGETARCH
WORKDIR /src

# Restore first, with only the project files copied: this layer is cached until a .csproj
# changes, so editing C# code does not download every NuGet package again.
COPY backend/global.json backend/Directory.Build.props backend/dotnet-tools.json backend/
COPY backend/src/ContractGuard.Domain/ContractGuard.Domain.csproj backend/src/ContractGuard.Domain/
COPY backend/src/ContractGuard.Application/ContractGuard.Application.csproj backend/src/ContractGuard.Application/
COPY backend/src/ContractGuard.Infrastructure/ContractGuard.Infrastructure.csproj backend/src/ContractGuard.Infrastructure/
COPY backend/src/ContractGuard.Api/ContractGuard.Api.csproj backend/src/ContractGuard.Api/
WORKDIR /src/backend
RUN dotnet restore src/ContractGuard.Api/ContractGuard.Api.csproj -a $TARGETARCH && dotnet tool restore

# Then the sources.
COPY contracts/ /src/contracts/
COPY backend/src/ src/
RUN dotnet publish src/ContractGuard.Api/ContractGuard.Api.csproj -c Release -a $TARGETARCH --self-contained false --no-restore -o /out/api

# A migrations bundle is a single executable that applies pending migrations, so the
# runtime image needs neither the SDK nor dotnet-ef.
RUN dotnet ef migrations bundle \
      --project src/ContractGuard.Infrastructure \
      --startup-project src/ContractGuard.Api \
      --configuration Release \
      --self-contained -r linux-x64 \
      -o /out/efbundle

# ---- migrator: runs once, applies migrations, exits -----------------------------------
FROM mcr.microsoft.com/dotnet/runtime-deps:10.0.14 AS migrator
WORKDIR /app
COPY --from=build /out/efbundle .
USER $APP_UID
# Connection string: environment variable Database__ConnectionString (see compose.yaml).
ENTRYPOINT ["./efbundle"]

# ---- api: the runtime image ------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/aspnet:10.0.14 AS api
WORKDIR /app
COPY --from=build /out/api .
# Built-in non-root user of the .NET images: a compromised process cannot write to /app.
USER $APP_UID
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "ContractGuard.Api.dll"]
