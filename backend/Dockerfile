# ── Stage 1: Build ────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy solution and project files first for layer-cached restore
COPY TaskManager.sln ./
COPY src/TaskManager.Domain/TaskManager.Domain.csproj             src/TaskManager.Domain/
COPY src/TaskManager.Application/TaskManager.Application.csproj   src/TaskManager.Application/
COPY src/TaskManager.Infrastructure/TaskManager.Infrastructure.csproj src/TaskManager.Infrastructure/
COPY src/TaskManager.WebApi/TaskManager.WebApi.csproj             src/TaskManager.WebApi/

RUN dotnet restore src/TaskManager.WebApi/TaskManager.WebApi.csproj

# Copy all source and publish
COPY src/ src/
RUN dotnet publish src/TaskManager.WebApi/TaskManager.WebApi.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

# ── Stage 2: Runtime ──────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish .

EXPOSE 5000

ENTRYPOINT ["dotnet", "TaskManager.WebApi.dll"]
