#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
if [ "${1:-}" = "--quick" ]; then
  powershell -NoProfile -ExecutionPolicy Bypass -File "$SCRIPT_DIR/../agent-universal-harness/scripts/verify.ps1" -Quick
else
  powershell -NoProfile -ExecutionPolicy Bypass -File "$SCRIPT_DIR/../agent-universal-harness/scripts/verify.ps1"
fi
