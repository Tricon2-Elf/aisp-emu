#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
PROJECT="$ROOT/AISpace.Launcher/AISpace.Launcher.csproj"

dotnet publish "$PROJECT" -c Release -p:PublishProfile="win-x64"
dotnet publish "$PROJECT" -c Release -p:PublishProfile="linux-x64" -p:PublishSingleFile=true

echo "Published to AISpace.Launcher/bin/publish/win-x64/"
echo "Published to AISpace.Launcher/bin/publish/linux-x64/"
