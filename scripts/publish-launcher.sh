#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
PROJECT="$ROOT/AISpace.Launcher/AISpace.Launcher.csproj"

runtime="${1:-}"
if [[ -z "$runtime" ]]; then
  echo "Usage: $0 <win-x64|linux-x64>"
  exit 1
fi

dotnet publish "$PROJECT" -c Release -p:PublishProfile="$runtime"

echo "Published to AISpace.Launcher/bin/publish/$runtime/"
