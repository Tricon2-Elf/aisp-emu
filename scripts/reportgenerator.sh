#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
RESULTS_DIR="${COVERAGE_RESULTS_DIR:-$ROOT/TestResults}"
REPORT_DIR="${COVERAGE_REPORT_DIR:-$ROOT/coverage-report}"
REPORT_TYPES="${COVERAGE_REPORT_TYPES:-Html;TextSummary}"

usage() {
    cat <<EOF
Usage: $0 [reportgenerator args...]

Converts Coverlet Cobertura output into a readable report via ReportGenerator
(local tool from dotnet-tools.json).

Looks for: $RESULTS_DIR/**/coverage.cobertura.xml
Writes to: $REPORT_DIR

Environment:
  COVERAGE_RESULTS_DIR   Input directory (default: <repo>/TestResults)
  COVERAGE_REPORT_DIR    Output directory (default: <repo>/coverage-report)
  COVERAGE_REPORT_TYPES  ReportGenerator -reporttypes (default: Html;TextSummary)

Examples:
  ./scripts/run-coverage.sh
  $0
  COVERAGE_REPORT_TYPES=Html;Badges $0
EOF
}

if [[ "${1:-}" == "-h" || "${1:-}" == "--help" ]]; then
    usage
    exit 0
fi

if [[ ! -d "$RESULTS_DIR" ]] || [[ -z "$(find "$RESULTS_DIR" -type f -name 'coverage.cobertura.xml' -print -quit)" ]]; then
    echo "No coverage.cobertura.xml files found under $RESULTS_DIR" >&2
    echo "Run ./scripts/run-coverage.sh first." >&2
    exit 1
fi

dotnet tool restore --tool-manifest "$ROOT/dotnet-tools.json" >/dev/null

mkdir -p "$REPORT_DIR"

echo "Generating coverage report in $REPORT_DIR ..."
dotnet tool run reportgenerator \
    "-reports:$RESULTS_DIR/**/coverage.cobertura.xml" \
    "-targetdir:$REPORT_DIR" \
    "-reporttypes:$REPORT_TYPES" \
    "$@"

if [[ -f "$REPORT_DIR/Summary.txt" ]]; then
    echo
    cat "$REPORT_DIR/Summary.txt"
fi

if [[ -f "$REPORT_DIR/index.html" ]]; then
    echo
    echo "HTML report: $REPORT_DIR/index.html"
fi
