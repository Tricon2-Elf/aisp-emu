# Build context: repo root (docker build -f AISpace.Server/Dockerfile .)

# Runtime stage
FROM mcr.microsoft.com/dotnet/runtime:10.0 AS base
WORKDIR /app

# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
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
RUN dotnet publish "AISpace.Server.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Final image
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "AISpace.Server.dll"]
