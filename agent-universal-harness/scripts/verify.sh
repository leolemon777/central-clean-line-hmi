#!/usr/bin/env bash
# ============================================================
# verify - 中央净软线统一验证入口
#
# 用法：
#   ./agent-universal-harness/scripts/verify.sh          全量检查
#   ./agent-universal-harness/scripts/verify.sh --quick  快速检查
# ============================================================
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
QUICK="${1:-}"

if [ "$QUICK" = "--quick" ]; then
  powershell -NoProfile -ExecutionPolicy Bypass -File "$SCRIPT_DIR/verify.ps1" -Quick
else
  powershell -NoProfile -ExecutionPolicy Bypass -File "$SCRIPT_DIR/verify.ps1"
fi
