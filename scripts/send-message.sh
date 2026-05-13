#!/bin/bash
set -euo pipefail

API_KEY="${API_KEY:-}"
URL="${URL:-http://localhost:8080}"
MESSAGE=""

while getopts "k:u:m:h" opt; do
    case $opt in
        k) API_KEY="$OPTARG" ;;
        u) URL="$OPTARG" ;;
        m) MESSAGE="$OPTARG" ;;
        h)
            echo "Send a broadcast message to all connected players."
            echo ""
            echo "Usage: $0 -m MESSAGE [-k API_KEY] [-u URL]"
            echo ""
            echo "Options:"
            echo "  -m MESSAGE   Text to broadcast to all players"
            echo "  -k API_KEY   API key (or set API_KEY env var)"
            echo "  -u URL       Server URL (default: http://localhost:8080)"
            echo "  -h           Show this help"
            exit 0
            ;;
        *) exit 1 ;;
    esac
done

if [ -z "$API_KEY" ]; then
    echo "Error: API key is required. Set API_KEY env var or use -k." >&2
    exit 1
fi

if [ -z "$MESSAGE" ]; then
    echo "Error: Message is required. Use -m flag." >&2
    exit 1
fi

echo "Sending broadcast to $URL/api/broadcast ..."
RESPONSE=$(curl -s -w "\n%{http_code}" \
    -H "X-Api-Key: $API_KEY" \
    -H "Content-Type: application/json" \
    -d "$(printf '{"message":"%s"}' "$MESSAGE")" \
    "$URL/api/broadcast")

HTTP_CODE=$(echo "$RESPONSE" | tail -1)
BODY=$(echo "$RESPONSE" | sed '$d')

if [ "$HTTP_CODE" = "200" ]; then
    echo "OK: $BODY"
else
    echo "Error ($HTTP_CODE): $BODY" >&2
    exit 1
fi
