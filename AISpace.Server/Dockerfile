# Build context: repo root (docker build -f AISpace.Server/Dockerfile .)
# Debian/glibc runtime — more reliable .NET threading/timers than Alpine/musl on small VPS hosts.

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app

# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
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
RUN dotnet publish "AISpace.Server.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false -r linux-x64 --self-contained false -p:PublishReadyToRun=true

# Final image
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

EXPOSE 8080 50050 50052 50054

ENTRYPOINT ["dotnet", "AISpace.Server.dll"]
