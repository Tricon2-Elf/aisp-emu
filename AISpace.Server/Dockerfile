# Build context: repo root (docker build -f AISpace.Server/Dockerfile .)
# Alpine = smaller image; uses musl. No ICU/tzdata by default (add if you need globalization).

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS base
WORKDIR /app

# wget is used by the HEALTHCHECK below (curl is not in the alpine aspnet image)
RUN apk add --no-cache wget

# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY ["AISpace.Server/AISpace.Server.csproj", "AISpace.Server/"]
COPY ["AISpace.Common/AISpace.Common.csproj", "AISpace.Common/"]
COPY ["AISpace.Network/AISpace.Network.csproj", "AISpace.Network/"]
RUN dotnet restore "AISpace.Server/AISpace.Server.csproj"

COPY . .
WORKDIR "/src/AISpace.Server"
RUN dotnet build "AISpace.Server.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Publish stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "AISpace.Server.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false -r linux-musl-x64 --self-contained false

# Final image
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

EXPOSE 8080 50050 50052 50054

# /healthz returns 200 only when every game server is "healthy" (503 otherwise),
# so a non-zero exit from wget marks the container unhealthy.
HEALTHCHECK --interval=30s --timeout=5s --start-period=30s --retries=3 \
    CMD wget --quiet --tries=1 --spider http://localhost:8080/healthz || exit 1

ENTRYPOINT ["dotnet", "AISpace.Server.dll"]
