$ErrorActionPreference = 'Continue'
$baseDir = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location -LiteralPath $baseDir

Write-Host "中央净软线 DLL 环境检查"
Write-Host "目录: $baseDir"
Write-Host "64位系统: $([Environment]::Is64BitOperatingSystem)"
Write-Host "64位进程: $([Environment]::Is64BitProcess)"
Write-Host ""

Add-Type -TypeDefinition @"
using System;
using System.Runtime.InteropServices;

public static class NativeDllCheck
{
    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern IntPtr LoadLibrary(string lpFileName);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool FreeLibrary(IntPtr hModule);

    public static string TryLoad(string path)
    {
        IntPtr module = LoadLibrary(path);
        if (module == IntPtr.Zero)
        {
            return "FAIL Win32Error=" + Marshal.GetLastWin32Error();
        }

        FreeLibrary(module);
        return "OK";
    }
}
"@

function Get-PeMachine {
    param([string]$Path)
    if (-not (Test-Path -LiteralPath $Path)) {
        return "missing"
    }

    try {
        $bytes = [IO.File]::ReadAllBytes($Path)
        if ($bytes.Length -lt 0x40 -or $bytes[0] -ne 0x4D -or $bytes[1] -ne 0x5A) {
            return "not-pe"
        }

        $peOffset = [BitConverter]::ToInt32($bytes, 0x3c)
        $machine = [BitConverter]::ToUInt16($bytes, $peOffset + 4)
        switch ($machine) {
            0x8664 { "x64" }
            0x014c { "x86" }
            default { "0x{0:x4}" -f $machine }
        }
    }
    catch {
        "read-failed: $($_.Exception.Message)"
    }
}

$required = @(
    "PipelineControl.UI.exe",
    "MultiCard.dll",
    "MultiCardCLR.dll",
    "MultiCardCS.dll",
    "msvcr100.dll",
    "Resources\io-points.json",
    "appsettings.json"
)

Write-Host "文件检查:"
foreach ($name in $required) {
    $path = Join-Path $baseDir $name
    $exists = Test-Path -LiteralPath $path
    $length = if ($exists) { (Get-Item -LiteralPath $path).Length } else { 0 }
    $machine = if ($name -like "*.dll" -or $name -like "*.exe") { Get-PeMachine $path } else { "-" }
    Write-Host ("  {0,-26} exists={1,-5} machine={2,-5} size={3}" -f $name, $exists, $machine, $length)
}

Write-Host ""
Write-Host "Native LoadLibrary 检查:"
foreach ($name in @("msvcr100.dll", "MultiCard.dll", "MultiCardCLR.dll")) {
    $path = Join-Path $baseDir $name
    if (Test-Path -LiteralPath $path) {
        Write-Host ("  {0,-18} {1}" -f $name, [NativeDllCheck]::TryLoad($path))
    }
    else {
        Write-Host ("  {0,-18} MISSING" -f $name)
    }
}

Write-Host ""
Write-Host "Managed MultiCardCS 加载检查:"
try {
    $managed = Join-Path $baseDir "MultiCardCS.dll"
    [Reflection.Assembly]::LoadFrom($managed) | Out-Null
    Write-Host "  MultiCardCS.dll OK"
}
catch {
    Write-Host "  MultiCardCS.dll FAIL"
    Write-Host "  $($_.Exception.GetType().FullName): $($_.Exception.Message)"
    if ($_.Exception.LoaderExceptions) {
        foreach ($loaderException in $_.Exception.LoaderExceptions) {
            Write-Host "  LoaderException: $($loaderException.Message)"
        }
    }
}

Write-Host ""
Write-Host "结论提示:"
Write-Host "  如果 msvcr100.dll 缺失或不是 x64，请复制完整部署包，或安装 VC++ 2010 x64 运行库/厂家驱动。"
Write-Host "  如果 MultiCardCLR.dll LoadLibrary 失败，优先看错误码里缺的依赖名称。"
