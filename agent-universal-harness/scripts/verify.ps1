# ============================================================
# verify - 中央净软线统一验证入口（PowerShell 版）
#
# 用法：
#   .\agent-universal-harness\scripts\verify.ps1          全量检查
#   .\agent-universal-harness\scripts\verify.ps1 -Quick   快速检查
# ============================================================
param([switch]$Quick)
$ErrorActionPreference = "Stop"

$HarnessRoot = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)
$ProjectRoot = Split-Path -Parent $HarnessRoot
Set-Location -LiteralPath $ProjectRoot

function Step($Name, [scriptblock]$Block) {
    Write-Host "== $Name =="
    $global:LASTEXITCODE = 0
    & $Block
    if ($global:LASTEXITCODE -ne 0) {
        throw "$Name FAILED (exit $global:LASTEXITCODE)"
    }
}

function Require-Path($RelativePath) {
    if (-not (Test-Path -LiteralPath $RelativePath)) {
        throw "Required path missing: $RelativePath"
    }
}

Step "[1/4] build" {
    dotnet build .\CentralCleanLineHmi.sln --no-restore
}

Step "[2/4] config and structure guard" {
    Require-Path ".\AGENTS.md"
    Require-Path ".\CentralCleanLineHmi.sln"
    Require-Path ".\src\PipelineControl.UI\PipelineControl.UI.csproj"
    Require-Path ".\tests\PipelineControl.UI.Tests\PipelineControl.UI.Tests.csproj"
    Require-Path ".\src\PipelineControl.UI\Resources\io-points.json"
    Require-Path ".\src\PipelineControl.UI\Resources\servo-registers.json"
    Require-Path ".\docs\architecture.md"
    Require-Path ".\docs\project-structure.md"
    Get-Content -LiteralPath ".\src\PipelineControl.UI\appsettings.json" -Raw | ConvertFrom-Json | Out-Null
    Get-Content -LiteralPath ".\src\PipelineControl.UI\Resources\io-points.json" -Raw | ConvertFrom-Json | Out-Null
    Get-Content -LiteralPath ".\src\PipelineControl.UI\Resources\servo-registers.json" -Raw | ConvertFrom-Json | Out-Null
    Write-Host "Canonical source root and JSON configuration files are present."
}

Step "[3/4] test" {
    if ($Quick) {
        Write-Host "(quick 模式：编译测试项目，不运行测试 DLL；当前机器存在 WDAC 拦截测试 DLL 的历史风险)"
        dotnet build .\tests\PipelineControl.UI.Tests\PipelineControl.UI.Tests.csproj --no-restore
    } else {
        dotnet test .\tests\PipelineControl.UI.Tests\PipelineControl.UI.Tests.csproj --no-restore
    }
}

Step "[4/4] safety" {
    Write-Host "No app launch, no hardware connection, no IO output writes, no servo register writes."
}

Write-Host ""
Write-Host "VERIFY PASSED"
