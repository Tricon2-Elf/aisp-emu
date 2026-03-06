# Build context: repo root (docker build -f AISpace.Server/Dockerfile .)
# Alpine = smaller image; uses musl. No ICU/tzdata by default (add if you need globalization).

# Runtime stage
FROM mcr.microsoft.com/dotnet/runtime:10.0-alpine AS base
WORKDIR /app

# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY ["AISpace.Server/AISpace.Server.csproj", "AISpace.Server/"]
COPY ["AISpace.Common/AISpace.Common.csproj", "AISpace.Common/"]
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
ENTRYPOINT ["dotnet", "AISpace.Server.dll"]
