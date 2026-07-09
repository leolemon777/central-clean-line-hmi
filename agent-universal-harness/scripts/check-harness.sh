#!/usr/bin/env bash
# ============================================================
# check-harness - Harness 配置自检
#
# 用法：
#   ./scripts/check-harness.sh
#   ./scripts/check-harness.sh --mode template
#   ./scripts/check-harness.sh --strict
#
# Project 模式用于真实项目：占位符、未配置 verify、空任务卡会失败。
# Template 模式用于维护本模板：允许占位符，但检查关键文件完整性。
# ============================================================
set -euo pipefail

MODE="project"
STRICT="false"

while [ "$#" -gt 0 ]; do
  case "$1" in
    --mode)
      MODE="${2:-project}"
      shift 2
      ;;
    --strict)
      STRICT="true"
      shift
      ;;
    *)
      echo "Unknown argument: $1" >&2
      exit 2
      ;;
  esac
done

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
ISSUES=()

add_issue() {
  ISSUES+=("$1|$2|$3")
}

read_text() {
  local file="$ROOT/$1"
  if [ -f "$file" ]; then
    cat "$file"
  fi
}

has_placeholder() {
  grep -Eq '<填写>|<例如|<这个|<谁用|<本任务|<标题>|TODO|___'
}

section_has_command() {
  local section="$1"
  local file="$ROOT/tasks/current-task.md"
  awk -v section="$section" '
    $0 ~ section { in_section=1; in_code=0; next }
    in_section && /^## / { exit }
    in_section && /^```/ { in_code = !in_code; next }
    in_section && in_code && $0 ~ /[[:alnum:]_.\/-]/ { found=1 }
    END { exit found ? 0 : 1 }
  ' "$file"
}

required_files=(
  "AGENTS.md"
  "PROJECT.md"
  "scripts/verify.ps1"
  "scripts/verify.sh"
  "scripts/check-harness.ps1"
  "scripts/check-harness.sh"
  "tasks/task-template.md"
  "tasks/current-task.md"
  "reports/progress.md"
  "harness/loop.md"
  "harness/quality-gates.md"
  "harness/stop-rules.md"
  "harness/rules-ledger.md"
  "harness/review-checklist.md"
  "harness/prompt-template.md"
  "harness/tool-permissions.md"
  "docs/context-index.md"
  "docs/lessons-learned.md"
  "docs/verify-recipes.md"
  "docs/adoption-playbook.md"
)

for file in "${required_files[@]}"; do
  if [ ! -f "$ROOT/$file" ]; then
    add_issue "ERROR" "MISSING_FILE" "缺少必需文件：$file"
  fi
done

agents="$(read_text "AGENTS.md")"
current_task="$(read_text "tasks/current-task.md")"
project="$(read_text "PROJECT.md")"
progress="$(read_text "reports/progress.md")"
verify_ps1="$(read_text "scripts/verify.ps1")"
verify_sh="$(read_text "scripts/verify.sh")"
rules_ledger="$(read_text "harness/rules-ledger.md")"
stop_rules="$(read_text "harness/stop-rules.md")"

if [ "$MODE" = "template" ]; then
  if ! printf '%s' "$verify_ps1" | grep -q "VERIFY NOT CONFIGURED"; then
    add_issue "ERROR" "VERIFY_TEMPLATE" "Template 模式要求 scripts/verify.ps1 默认未配置并明确失败。"
  fi
  if ! printf '%s' "$verify_sh" | grep -q "VERIFY NOT CONFIGURED"; then
    add_issue "ERROR" "VERIFY_TEMPLATE" "Template 模式要求 scripts/verify.sh 默认未配置并明确失败。"
  fi
elif [ "$MODE" = "project" ]; then
  if printf '%s' "$agents" | has_placeholder; then
    add_issue "ERROR" "AGENTS_PLACEHOLDER" "AGENTS.md 仍含占位符；请填写技术栈、基准分支和 verify 命令。"
  fi
  if printf '%s' "$current_task" | has_placeholder; then
    add_issue "ERROR" "TASK_PLACEHOLDER" "tasks/current-task.md 仍含占位符；请填写真实任务卡。"
  fi
  if printf '%s' "$verify_ps1" | grep -Eq 'Fail-NotConfigured|VERIFY NOT CONFIGURED|TODO' &&
     printf '%s' "$verify_sh" | grep -Eq 'fail_not_configured|VERIFY NOT CONFIGURED|TODO'; then
    add_issue "ERROR" "VERIFY_NOT_CONFIGURED" "两个 verify 入口都仍是模板状态；至少配置当前平台使用的 verify。"
  fi
  if ! section_has_command "Quick"; then
    add_issue "ERROR" "TASK_NO_QUICK_VERIFY" "任务卡缺少 quick verify 命令。"
  fi
  if ! section_has_command "Full"; then
    add_issue "ERROR" "TASK_NO_FULL_VERIFY" "任务卡缺少 full verify 命令。"
  fi
  if printf '%s' "$project" | has_placeholder; then
    add_issue "WARN" "PROJECT_PLACEHOLDER" "PROJECT.md 仍含占位符；跨会话项目应补齐项目宪章。"
  fi
  if printf '%s' "$progress" | has_placeholder; then
    add_issue "WARN" "PROGRESS_PLACEHOLDER" "reports/progress.md 仍含占位符；完成一轮后应更新真实状态。"
  fi
else
  echo "Invalid mode: $MODE" >&2
  exit 2
fi

for id in S1 S2 S3 S4 S5 S6 S7 S8 S9; do
  if printf '%s' "$stop_rules" | grep -Eq "\\b$id\\b" &&
     ! printf '%s' "$rules_ledger" | grep -Eq "\\b$id\\b|S1-S5|S6-S9"; then
    add_issue "WARN" "RULE_NOT_REGISTERED" "$id 出现在 stop-rules.md，但未在 rules-ledger.md 登记。"
  fi
done

has_error="false"
if [ "${#ISSUES[@]}" -eq 0 ]; then
  echo "HARNESS CHECK PASSED ($MODE)"
  exit 0
fi

for issue in "${ISSUES[@]}"; do
  severity="${issue%%|*}"
  rest="${issue#*|}"
  code="${rest%%|*}"
  message="${rest#*|}"
  if [ "$STRICT" = "true" ] && [ "$severity" = "WARN" ]; then
    severity="ERROR"
  fi
  line="[$severity] $code: $message"
  if [ "$severity" = "ERROR" ]; then
    echo "$line" >&2
    has_error="true"
  else
    echo "$line"
  fi
done

if [ "$has_error" = "true" ]; then
  exit 1
fi

echo "HARNESS CHECK PASSED WITH WARNINGS ($MODE)"
