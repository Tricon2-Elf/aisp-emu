#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
RESULTS_DIR="${COVERAGE_RESULTS_DIR:-$ROOT/TestResults}"
CONFIGURATION="${CONFIGURATION:-Release}"

usage() {
    cat <<EOF
Usage: $0 [dotnet test args...]

Runs AISpace.sln tests with Coverlet (XPlat Code Coverage).
Coverage files land under: $RESULTS_DIR/**/coverage.cobertura.xml

Environment:
  CONFIGURATION          Build config (default: Release)
  COVERAGE_RESULTS_DIR   Results directory (default: <repo>/TestResults)

Examples:
  $0
  $0 --filter FullyQualifiedName~AvatarCreate
  CONFIGURATION=Debug $0 --project AISpace.Common.Tests

Generate an HTML report afterwards with:
  ./scripts/reportgenerator.sh
EOF
}

if [[ "${1:-}" == "-h" || "${1:-}" == "--help" ]]; then
    usage
    exit 0
fi

mkdir -p "$RESULTS_DIR"

echo "Collecting coverage into $RESULTS_DIR ..."
dotnet test "$ROOT/AISpace.sln" \
    --configuration "$CONFIGURATION" \
    --collect:"XPlat Code Coverage" \
    --results-directory "$RESULTS_DIR" \
    "$@"

mapfile -t cobertura_files < <(find "$RESULTS_DIR" -type f -name 'coverage.cobertura.xml' | sort)
if [[ ${#cobertura_files[@]} -eq 0 ]]; then
    echo "No coverage.cobertura.xml files were produced under $RESULTS_DIR" >&2
    exit 1
fi

echo "Wrote ${#cobertura_files[@]} coverage file(s):"
printf '  %s\n' "${cobertura_files[@]}"
echo
echo "Generate HTML report with: ./scripts/reportgenerator.sh"
