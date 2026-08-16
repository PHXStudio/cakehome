#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
# Parent of unity-mcp is the Unity project root (override with UNITY_PROJECT_ROOT).
export UNITY_PROJECT_ROOT="${UNITY_PROJECT_ROOT:-$(cd "$SCRIPT_DIR/.." && pwd)}"

if [[ ! -f "$SCRIPT_DIR/dist/index.js" ]]; then
  echo "unity-mcp: dist/index.js missing — run: cd \"$SCRIPT_DIR\" && npm install && npm run build" >&2
  exit 1
fi

exec node "$SCRIPT_DIR/dist/index.js"
