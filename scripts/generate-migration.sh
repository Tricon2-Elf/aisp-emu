#!/usr/bin/env bash
set -euo pipefail

if [[ $# -ne 1 ]]; then
    echo "Usage: $0 <MigrationName>" >&2
    exit 1
fi

script_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
repo_root="$(cd "$script_dir/.." && pwd)"
migration_name="$1"

dotnet tool run dotnet-ef migrations add "$migration_name" \
    --project "$repo_root/aisp.Common/aisp.Common.csproj" \
    --startup-project "$repo_root/aisp.Server/aisp.Server.csproj" \
    --context MainContext \
    --output-dir "DAL/Migrations"
