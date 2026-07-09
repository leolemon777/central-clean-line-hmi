param([switch]$Quick)

$ErrorActionPreference = "Stop"
$script = Join-Path $PSScriptRoot "..\agent-universal-harness\scripts\verify.ps1"
& $script -Quick:$Quick
exit $LASTEXITCODE
