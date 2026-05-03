# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["K8SMonitor.csproj", "./"]
RUN dotnet restore "K8SMonitor.csproj"

COPY . .
RUN dotnet build "K8SMonitor.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "K8SMonitor.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/runtime:8.0
WORKDIR /app

RUN apt-get update && \
    apt-get install -y curl && \
    rm -rf /var/lib/apt/lists/*

COPY --from=publish /app/publish .

# Set default environment variable
ENV DRY_RUN=false

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=5s --retries=3 \
    CMD test -f /app/health || exit 1

ENTRYPOINT ["dotnet", "K8SMonitor.dll"]
