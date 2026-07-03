#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
PROJECT="$ROOT/AISpace.Launcher/AISpace.Launcher.csproj"
HOOK_BUILDER="$ROOT/scripts/build-localehook.sh"
WIN_PUBLISH_DIR="$ROOT/AISpace.Launcher/bin/publish/win-x86"

dotnet publish "$PROJECT" -c Release -p:PublishProfile="win-x86"
"$HOOK_BUILDER" "$WIN_PUBLISH_DIR/aisp.localehook.dll"
dotnet publish "$PROJECT" -c Release -p:PublishProfile="linux-x86" -p:PublishSingleFile=true

echo "Published to AISpace.Launcher/bin/publish/win-x86/"
echo "Published to AISpace.Launcher/bin/publish/linux-x86/"
