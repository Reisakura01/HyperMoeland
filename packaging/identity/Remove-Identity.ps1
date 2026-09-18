<#
.SYNOPSIS
  卸载 HyperMoeland 的稀疏包身份注册。

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
    'dev'    { @('MoeOrigin.HyperMoeland.Dev') }
    'stable' { @('MoeOrigin.HyperMoeland') }
    default  { @('MoeOrigin.HyperMoeland', 'MoeOrigin.HyperMoeland.Dev') }
}

foreach ($name in $names) {
    $package = Get-AppxPackage -Name $name -ErrorAction SilentlyContinue
    if ($null -ne $package) {
        Write-Host "移除包注册：$name"
        $package | Remove-AppxPackage
    }
}

if ($RemoveCertificate) {
    $certs = Get-ChildItem 'Cert:\LocalMachine\TrustedPeople' -ErrorAction SilentlyContinue |
        Where-Object { $_.Subject -eq 'CN=MoeOrigin Team' }
    foreach ($cert in $certs) {
        Write-Host "移除受信任证书：$($cert.Thumbprint)"
        Remove-Item -Path $cert.PSPath -Force
    }
}

Write-Host "完成。" -ForegroundColor Green
