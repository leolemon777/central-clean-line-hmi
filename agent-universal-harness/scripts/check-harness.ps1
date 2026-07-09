# ============================================================
# check-harness - Harness 配置自检（PowerShell 版）
#
# 用法：
#   ./scripts/check-harness.ps1
#   ./scripts/check-harness.ps1 -Mode Template
#   ./scripts/check-harness.ps1 -Strict
#
# Project 模式用于真实项目：占位符、未配置 verify、空任务卡会失败。
# Template 模式用于维护本模板：允许占位符，但检查关键文件完整性。
# ============================================================
param(
    [ValidateSet("Project", "Template")]
    [string]$Mode = "Project",
    [switch]$Strict
)

$ErrorActionPreference = "Stop"
$Root = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)
$Issues = New-Object System.Collections.Generic.List[object]

function Add-Issue($Severity, $Code, $Message) {
    $Issues.Add([pscustomobject]@{
        Severity = $Severity
        Code = $Code
        Message = $Message
    }) | Out-Null
}

function Read-Text($RelativePath) {
    $path = Join-Path $Root $RelativePath
    if (-not (Test-Path -LiteralPath $path)) {
        return $null
    }
    return Get-Content -LiteralPath $path -Raw
}

function Has-Placeholder($Text) {
    if ($null -eq $Text) { return $false }
    return $Text -match "<填写>|<例如|<这个|<谁用|<本任务|<标题>|TODO|___"
}

function Section-HasCommand($Text, $SectionName) {
    if ($null -eq $Text) { return $false }
    $inSection = $false
    $inCode = $false
    foreach ($line in ($Text -split "`r?`n")) {
        if ($line -match [regex]::Escape($SectionName)) {
            $inSection = $true
            $inCode = $false
            continue
        }
        if ($inSection -and $line -match "^## ") {
            break
        }
        if ($inSection -and $line -match '^```') {
            $inCode = -not $inCode
            continue
        }
        if ($inSection -and $inCode -and $line -match '\S') {
            return $true
        }
    }
    return $false
}

$RequiredFiles = @(
    "AGENTS.md",
    "PROJECT.md",
    "scripts/verify.ps1",
    "scripts/verify.sh",
    "scripts/check-harness.ps1",
    "scripts/check-harness.sh",
    "tasks/task-template.md",
    "tasks/current-task.md",
    "reports/progress.md",
    "harness/loop.md",
    "harness/quality-gates.md",
    "harness/stop-rules.md",
    "harness/rules-ledger.md",
    "harness/review-checklist.md",
    "harness/prompt-template.md",
    "harness/tool-permissions.md",
    "docs/context-index.md",
    "docs/lessons-learned.md",
    "docs/verify-recipes.md",
    "docs/adoption-playbook.md"
)

foreach ($file in $RequiredFiles) {
    if (-not (Test-Path -LiteralPath (Join-Path $Root $file))) {
        Add-Issue "ERROR" "MISSING_FILE" "缺少必需文件：$file"
    }
}

$agents = Read-Text "AGENTS.md"
$currentTask = Read-Text "tasks/current-task.md"
$project = Read-Text "PROJECT.md"
$progress = Read-Text "reports/progress.md"
$verifyPs1 = Read-Text "scripts/verify.ps1"
$verifySh = Read-Text "scripts/verify.sh"
$rulesLedger = Read-Text "harness/rules-ledger.md"
$stopRules = Read-Text "harness/stop-rules.md"

if ($Mode -eq "Template") {
    if ($verifyPs1 -notmatch "VERIFY NOT CONFIGURED") {
        Add-Issue "ERROR" "VERIFY_TEMPLATE" "Template 模式要求 scripts/verify.ps1 默认未配置并明确失败。"
    }
    if ($verifySh -notmatch "VERIFY NOT CONFIGURED") {
        Add-Issue "ERROR" "VERIFY_TEMPLATE" "Template 模式要求 scripts/verify.sh 默认未配置并明确失败。"
    }
} else {
    if (Has-Placeholder $agents) {
        Add-Issue "ERROR" "AGENTS_PLACEHOLDER" "AGENTS.md 仍含占位符；请填写技术栈、基准分支和 verify 命令。"
    }
    if (Has-Placeholder $currentTask) {
        Add-Issue "ERROR" "TASK_PLACEHOLDER" "tasks/current-task.md 仍含占位符；请填写真实任务卡。"
    }
    if ($verifyPs1 -match "Fail-NotConfigured|VERIFY NOT CONFIGURED|TODO" -and $verifySh -match "fail_not_configured|VERIFY NOT CONFIGURED|TODO") {
        Add-Issue "ERROR" "VERIFY_NOT_CONFIGURED" "两个 verify 入口都仍是模板状态；至少配置当前平台使用的 verify。"
    }
    if (-not (Section-HasCommand $currentTask "Quick")) {
        Add-Issue "ERROR" "TASK_NO_QUICK_VERIFY" "任务卡缺少 quick verify 命令。"
    }
    if (-not (Section-HasCommand $currentTask "Full")) {
        Add-Issue "ERROR" "TASK_NO_FULL_VERIFY" "任务卡缺少 full verify 命令。"
    }
    if ($project -and (Has-Placeholder $project)) {
        Add-Issue "WARN" "PROJECT_PLACEHOLDER" "PROJECT.md 仍含占位符；跨会话项目应补齐项目宪章。"
    }
    if ($progress -and (Has-Placeholder $progress)) {
        Add-Issue "WARN" "PROGRESS_PLACEHOLDER" "reports/progress.md 仍含占位符；完成一轮后应更新真实状态。"
    }
}

foreach ($id in @("S1", "S2", "S3", "S4", "S5", "S6", "S7", "S8", "S9")) {
    if ($stopRules -and $stopRules -match "\b$id\b" -and $rulesLedger -notmatch "\b$id\b|S1-S5|S6-S9") {
        Add-Issue "WARN" "RULE_NOT_REGISTERED" "$id 出现在 stop-rules.md，但未在 rules-ledger.md 登记。"
    }
}

if ($Strict) {
    foreach ($issue in $Issues) {
        if ($issue.Severity -eq "WARN") {
            $issue.Severity = "ERROR"
        }
    }
}

if ($Issues.Count -eq 0) {
    Write-Host "HARNESS CHECK PASSED ($Mode)"
    exit 0
}

foreach ($issue in $Issues) {
    $line = "[{0}] {1}: {2}" -f $issue.Severity, $issue.Code, $issue.Message
    if ($issue.Severity -eq "ERROR") {
        [Console]::Error.WriteLine($line)
    } else {
        Write-Host $line
    }
}

$hasError = $Issues | Where-Object { $_.Severity -eq "ERROR" } | Select-Object -First 1
if ($hasError) {
    exit 1
}

Write-Host "HARNESS CHECK PASSED WITH WARNINGS ($Mode)"
exit 0
