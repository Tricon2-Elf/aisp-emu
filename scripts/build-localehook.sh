#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
SOURCE="$ROOT/AISpace.LocaleHook/aisp.localehook.cpp"
OUTPUT="${1:-$ROOT/AISpace.Launcher/bin/publish/win-x64/aisp.localehook.dll}"
DOCKER_IMAGE="${LOCALEHOOK_DOCKER_IMAGE:-debian:bookworm-slim}"

if [ ! -f "$SOURCE" ]; then
    echo "Locale hook source not found: $SOURCE" >&2
    exit 1
fi

if ! command -v docker >/dev/null 2>&1; then
    echo "Docker is required to build the locale hook DLL." >&2
    exit 1
fi

if [[ "$OUTPUT" != /* ]]; then
    OUTPUT="$ROOT/$OUTPUT"
fi

case "$OUTPUT" in
    "$ROOT"/*) ;;
    *)
        echo "Output path must be inside repository root: $ROOT" >&2
        exit 1
        ;;
esac

mkdir -p "$(dirname "$OUTPUT")"

SOURCE_IN_CONTAINER="${SOURCE/#$ROOT/\/workspace}"
OUTPUT_IN_CONTAINER="${OUTPUT/#$ROOT/\/workspace}"
HOST_UID="$(id -u)"
HOST_GID="$(id -g)"

# One-off containerized MinGW build (32-bit DLL for the x86 launcher/game).
docker run --rm \
    -v "$ROOT:/workspace" \
    -w /workspace \
    "$DOCKER_IMAGE" \
    bash -lc "set -euo pipefail \
        && apt-get update \
        && apt-get install -y --no-install-recommends g++-mingw-w64-i686 binutils-mingw-w64-i686 \
        && i686-w64-mingw32-g++ -shared -O2 -s -std=gnu++17 -fno-exceptions -fno-rtti -static-libgcc -static-libstdc++ \"$SOURCE_IN_CONTAINER\" -o \"$OUTPUT_IN_CONTAINER\" \
        && i686-w64-mingw32-objdump -p \"$OUTPUT_IN_CONTAINER\" \
        && chown \"$HOST_UID:$HOST_GID\" \"$OUTPUT_IN_CONTAINER\""

echo "Built locale hook DLL: $OUTPUT"
