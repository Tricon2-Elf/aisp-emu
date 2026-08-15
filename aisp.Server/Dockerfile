# Build context: repo root (docker build -f aisp.Server/Dockerfile .)
# Debian/glibc runtime — more reliable .NET threading/timers than Alpine/musl on small VPS hosts.

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app

# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY ["aisp.Server/aisp.Server.csproj", "aisp.Server/"]
COPY ["aisp.Common/aisp.Common.csproj", "aisp.Common/"]
COPY ["aisp.Network/aisp.Network.csproj", "aisp.Network/"]
COPY ["aisp.Portal/aisp.Portal.csproj", "aisp.Portal/"]
RUN dotnet restore "aisp.Server/aisp.Server.csproj"

COPY . .
WORKDIR "/src/aisp.Server"
RUN dotnet build "aisp.Server.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Publish stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "aisp.Server.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false -r linux-x64 --self-contained false -p:PublishReadyToRun=true

# Final image
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

EXPOSE 8080 50050 50052 50054

ENTRYPOINT ["dotnet", "aisp.Server.dll"]
