<#
.SYNOPSIS
  为 HyperMoeland 注册「稀疏包身份」（Sparse Package Identity）。

.DESCRIPTION
  做三件事（需要管理员权限，脚本会自动请求提权）：
    1. 生成/复用自签名证书，并导入到「本地计算机 → 受信任人」证书存储
    2. 用 makeappx + signtool 打包并签名一个只含清单的 .msix
    3. Add-AppxPackage 注册该包，并通过 -ExternalLocation 指向 exe 所在目录
  注册后，从该目录启动 HyperMoeland.exe 即可获得 Windows 包身份，
  UserNotificationListener 事件订阅等功能随之可用。

.PARAMETER ExternalLocation
  存放 HyperMoeland.exe 的目录（例如仓库的 dist 目录）。

.PARAMETER Version
  清单版本号，必须是四段式，如 1.2.0.0。

.PARAMETER Channel
  stable（正式）或 dev（开发，包名带 .Dev 后缀，可与正式版共存）。
#>
param(
    [string]$ExternalLocation,

    [string]$Version = '1.2.0.0',

    [ValidateSet('dev', 'stable')]
    [string]$Channel = 'stable'
)

$ErrorActionPreference = 'Stop'

function Test-IsElevated {
    $identity = [Security.Principal.WindowsIdentity]::GetCurrent()
    $principal = [Security.Principal.WindowsPrincipal]::new($identity)
    $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
}

function Quote-Argument([string]$Value) { '"{0}"' -f $Value }

# ---- 自动提权 ----
if (-not (Test-IsElevated)) {
    $arguments = @(
        '-NoLogo', '-NoProfile', '-NonInteractive', '-ExecutionPolicy', 'Bypass',
        '-File', (Quote-Argument $PSCommandPath),
        '-Version', (Quote-Argument $Version),
        '-Channel', (Quote-Argument $Channel)
    )
    if (-not [string]::IsNullOrWhiteSpace($ExternalLocation)) {
        $arguments += @('-ExternalLocation', (Quote-Argument $ExternalLocation))
    }
    $process = Start-Process powershell.exe -ArgumentList ($arguments -join ' ') -Verb RunAs -Wait -PassThru
    if ($process.ExitCode -ne 0) {
        throw "提权安装身份失败，退出码 $($process.ExitCode)。"
    }
    return
}

$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path

if ([string]::IsNullOrWhiteSpace($ExternalLocation)) {
    $ExternalLocation = Join-Path $repositoryRoot 'dist'
}
$ExternalLocation = (Resolve-Path $ExternalLocation).Path

$exePath = Join-Path $ExternalLocation 'HyperMoeland.exe'
if (-not (Test-Path $exePath)) {
    throw "在 $ExternalLocation 中找不到 HyperMoeland.exe，请先执行构建/发布。"
}

$packageName = if ($Channel -eq 'dev') { 'MoeOrigin.HyperMoeland.Dev' } else { 'MoeOrigin.HyperMoeland' }
$workDirectory = Join-Path $repositoryRoot 'dist\identity'
$certificateDirectory = Join-Path $workDirectory 'cert'

Write-Host "== HyperMoeland 身份注册 ==" -ForegroundColor Cyan
Write-Host "包名      : $packageName"
Write-Host "版本      : $Version"
Write-Host "外部位置  : $ExternalLocation"

# ---- 1. 证书 ----
& (Join-Path $PSScriptRoot 'New-Certificate.ps1') -CertificateDirectory $certificateDirectory

# ---- 2. 打包 + 签名 ----
& (Join-Path $PSScriptRoot 'Build-IdentityPackage.ps1') `
    -Channel $Channel `
    -Version $Version `
    -CertificatePath (Join-Path $certificateDirectory 'HyperMoeland.Dev.pfx') `
    -CertificatePassword 'HyperMoelandDevelopment' `
    -OutputDirectory $workDirectory

# ---- 3. 信任证书 + 注册包 ----
Import-Certificate `
    -FilePath (Join-Path $workDirectory "$packageName.cer") `
    -CertStoreLocation 'Cert:\LocalMachine\TrustedPeople' | Out-Null

$previous = Get-AppxPackage -Name $packageName -ErrorAction SilentlyContinue
if ($null -ne $previous) {
    Write-Host "移除旧的包注册..."
    $previous | Remove-AppxPackage
}

Add-AppxPackage `
    -Path (Join-Path $workDirectory "$packageName.msix") `
    -ExternalLocation $ExternalLocation

$installed = Get-AppxPackage -Name $packageName
Write-Host ""
Write-Host "注册成功！" -ForegroundColor Green
Write-Host "  包全名    : $($installed.PackageFullName)"
Write-Host "  包家族名  : $($installed.PackageFamilyName)"
Write-Host "  AUMID     : $($installed.PackageFamilyName)!HyperMoeland"
Write-Host ""
Write-Host "现在从 $ExternalLocation 启动 HyperMoeland.exe 即带包身份。" -ForegroundColor Yellow
