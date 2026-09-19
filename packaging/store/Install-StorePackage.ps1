<#
.SYNOPSIS
  本地安装 / 卸载 HyperMoeland 完整 MSIX 包（测试用）。

.DESCRIPTION
  自签证书签名的包要能安装，证书必须位于「本地计算机 → 受信任人」存储，
  这一步需要管理员权限（脚本会检测并在需要时提示）。
  证书已受信任时，安装本身是每用户操作，不需要管理员。

.PARAMETER MsixPath
  要安装的 .msix 路径。

.PARAMETER CertificatePath
  随包附带的 .cer（用于导入受信任人）。不给则尝试用包内签名证书的指纹查找。

.PARAMETER Uninstall
  卸载已安装的包（按包家族名）。

.PARAMETER PackageFamilyName
  按包家族名卸载（优先级高于 -PackageName）。

.PARAMETER PackageName
  卸载目标包名，默认只卸载本脚本构建的测试包。
  注意：不要用通配符匹配 MoeOrigin.HyperMoeland*，
  那会把 packaging/identity 注册的稀疏包一起删掉。
#>
[CmdletBinding()]
param(
    [string]$MsixPath,
    [string]$CertificatePath,
    [switch]$Uninstall,
    [string]$PackageName = 'MoeOrigin.HyperMoeland.StoreTest',
    [string]$PackageFamilyName
)

$ErrorActionPreference = 'Stop'

$repositoryRoot = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent

function Test-IsElevated {
    $identity = [Security.Principal.WindowsIdentity]::GetCurrent()
    ([Security.Principal.WindowsPrincipal]$identity).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
}

function Test-CertTrusted([string]$Thumbprint) {
    if ([string]::IsNullOrWhiteSpace($Thumbprint)) { return $false }
    $null -ne (Get-ChildItem Cert:\LocalMachine\TrustedPeople -ErrorAction SilentlyContinue |
        Where-Object { $_.Thumbprint -eq $Thumbprint })
}

# ---- 卸载 ----
if ($Uninstall) {
    # 精确匹配：只删指定的包，避免误伤 packaging/identity 注册的稀疏包
    if (-not [string]::IsNullOrWhiteSpace($PackageFamilyName)) {
        $packages = Get-AppxPackage -FamilyName $PackageFamilyName
    }
    else {
        $packages = Get-AppxPackage -Name $PackageName
    }
    if (-not $packages) { Write-Host "没有找到已安装的包（PackageName=$PackageName）。"; return }
    foreach ($p in @($packages)) {
        Write-Host "卸载 $($p.PackageFullName) ..." -ForegroundColor Cyan
        Remove-AppxPackage -Package $p.PackageFullName
    }
    Write-Host "已卸载。" -ForegroundColor Green
    return
}

# ---- 安装 ----
if ([string]::IsNullOrWhiteSpace($MsixPath)) {
    $candidate = Get-ChildItem (Join-Path $repositoryRoot 'artifacts\store') -Filter *.msix -ErrorAction SilentlyContinue |
        Sort-Object LastWriteTime -Descending | Select-Object -First 1
    if (-not $candidate) { throw "没找到 .msix，请先运行 Build-StorePackage.ps1，或用 -MsixPath 指定。" }
    $MsixPath = $candidate.FullName
}
$MsixPath = (Resolve-Path $MsixPath).Path
Write-Host "安装包: $MsixPath" -ForegroundColor Cyan

# 证书信任检查（自签证书必须进「受信任人」才能安装）
$signature = Get-AuthenticodeSignature $MsixPath
$thumbprint = $null
if ($signature -and $signature.SignerCertificate) { $thumbprint = $signature.SignerCertificate.Thumbprint }
Write-Host "签名状态: $($signature.Status)  指纹: $thumbprint"

if (-not (Test-CertTrusted $thumbprint)) {
    if ([string]::IsNullOrWhiteSpace($CertificatePath)) {
        $CertificatePath = Join-Path $repositoryRoot 'dist\identity\MoeOrigin.HyperMoeland.cer'
    }
    if (-not (Test-Path $CertificatePath)) {
        throw "证书未受信任且找不到 .cer（$CertificatePath）。请先运行 packaging/identity/New-Certificate.ps1，或用 -CertificatePath 指定。"
    }
    if (-not (Test-IsElevated)) {
        throw "证书尚未导入「本地计算机 → 受信任人」，该操作需要管理员权限。`n请以管理员身份运行：`n  Import-Certificate -FilePath `"$CertificatePath`" -CertStoreLocation Cert:\LocalMachine\TrustedPeople"
    }
    Write-Host "导入证书到受信任人..." -ForegroundColor Yellow
    Import-Certificate -FilePath $CertificatePath -CertStoreLocation 'Cert:\LocalMachine\TrustedPeople' | Out-Null
}

# 覆盖安装：先移除同名旧包
$existing = Get-AppxPackage -Name 'MoeOrigin.HyperMoeland.StoreTest' -ErrorAction SilentlyContinue
if ($existing) {
    Write-Host "移除旧的测试包 $($existing.PackageFullName) ..." -ForegroundColor Yellow
    $existing | Remove-AppxPackage
}

Write-Host "注册包..." -ForegroundColor Cyan
Add-AppxPackage -Path $MsixPath

$installed = Get-AppxPackage -Name 'MoeOrigin.HyperMoeland.StoreTest' |
    Sort-Object Version -Descending | Select-Object -First 1
if (-not $installed) { throw "安装后未能查询到包" }

Write-Host ""
Write-Host "安装成功！" -ForegroundColor Green
Write-Host "  包全名    : $($installed.PackageFullName)"
Write-Host "  包家族名  : $($installed.PackageFamilyName)"
Write-Host "  安装位置  : $($installed.InstallLocation)"
Write-Host "  启动命令  : explorer.exe shell:AppsFolder\$($installed.PackageFamilyName)!HyperMoeland"
