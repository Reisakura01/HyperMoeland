<#
.SYNOPSIS
  卸载 Islora 的稀疏包身份注册。

.DESCRIPTION
  移除包注册；可选（-RemoveCertificate）同时从「本地计算机 → 受信任人」
  中删除自签名证书。需要管理员权限（脚本会自动提权）。
#>
param(
    [ValidateSet('dev', 'stable', 'both')]
    [string]$Channel = 'both',

    [switch]$RemoveCertificate
)

$ErrorActionPreference = 'Stop'

function Test-IsElevated {
    $identity = [Security.Principal.WindowsIdentity]::GetCurrent()
    $principal = [Security.Principal.WindowsPrincipal]::new($identity)
    $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
}

if (-not (Test-IsElevated)) {
    $arguments = @(
        '-NoLogo', '-NoProfile', '-NonInteractive', '-ExecutionPolicy', 'Bypass',
        '-File', ('"{0}"' -f $PSCommandPath),
        '-Channel', $Channel
    )
    if ($RemoveCertificate) { $arguments += '-RemoveCertificate' }
    $process = Start-Process powershell.exe -ArgumentList ($arguments -join ' ') -Verb RunAs -Wait -PassThru
    if ($process.ExitCode -ne 0) {
        throw "提权卸载身份失败，退出码 $($process.ExitCode)。"
    }
    return
}

$names = switch ($Channel) {
    'dev'    { @('MoeOrigin.Islora.Dev') }
    'stable' { @('MoeOrigin.Islora') }
    default  { @('MoeOrigin.Islora', 'MoeOrigin.Islora.Dev') }
}

foreach ($name in $names) {
    $package = Get-AppxPackage -Name $name -ErrorAction SilentlyContinue
    if ($null -ne $package) {
        Write-Host "移除包注册：$name"
        $package | Remove-AppxPackage
    }
}

if ($RemoveCertificate) {
    # 只删「本次安装所用证书」——即随包产出的 .cer（其指纹就是签名用的那张）。
    # 原来按 Subject 全库扫描删除，会把 stable / dev / store 自签测试包共用的信任证书
    # 一起删掉，导致原有包再也无法安装或更新（-Channel 对证书毫无过滤作用）。
    $cerCandidates = @(
        (Join-Path (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path "dist\identity\$packageName.cer"),
        (Join-Path $PSScriptRoot "build\$packageName.cer")
    ) | Where-Object { Test-Path $_ } | Select-Object -First 1

    $thumbprints = @()
    if ($cerCandidates) {
        $thumbprints = @((Get-PfxCertificate -FilePath $cerCandidates -ErrorAction SilentlyContinue).Thumbprint |
            Where-Object { $_ })
    }

    if ($thumbprints.Count -eq 0) {
        Write-Host "找不到本次安装的证书文件（$packageName.cer），跳过证书删除。" -ForegroundColor Yellow
        Write-Host "如确需清理，请手动确认指纹后删除，避免误删其它包所依赖的信任证书。" -ForegroundColor Yellow
    }
    foreach ($tp in $thumbprints) {
        $cert = Get-ChildItem 'Cert:\LocalMachine\TrustedPeople' -ErrorAction SilentlyContinue |
            Where-Object { $_.Thumbprint -eq $tp }
        if ($cert) {
            Write-Host "移除受信任证书：$tp"
            Remove-Item -Path $cert.PSPath -Force
        }
    }
}

Write-Host "完成。" -ForegroundColor Green
