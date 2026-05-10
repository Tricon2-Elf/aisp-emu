#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$repo_root"

dotnet tool restore
python3 -m pip install --user pre-commit
export PATH="${HOME}/.local/bin:${PATH}"

if git rev-parse --git-dir >/dev/null 2>&1; then
  pre-commit install
fi
