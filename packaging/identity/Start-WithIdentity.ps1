<#
.SYNOPSIS
  以「包身份」启动 Islora。

.DESCRIPTION
  通过包 AUMID 激活应用（shell:AppsFolder\<PFN>!Islora），
  这样进程会带上 Windows 包身份，通知走官方事件订阅（实时、无轮询）。

  直接双击 exe 也能运行，但不会带包身份（此时程序自动退化为轮询模式）。

.PARAMETER Channel
  stable（默认）或 dev。
#>
param(
    [ValidateSet('dev', 'stable')]
    [string]$Channel = 'stable'
)

$ErrorActionPreference = 'Stop'

$packageName = if ($Channel -eq 'dev') { 'MoeOrigin.Islora.Dev' } else { 'MoeOrigin.Islora' }

$package = Get-AppxPackage -Name $packageName -ErrorAction SilentlyContinue
if ($null -eq $package) {
    throw "未找到已注册的包 $packageName。请先运行 Install-Identity.ps1 注册包身份。"
}

$appId = 'Islora'
$aumid = "$($package.PackageFamilyName)!$appId"

Write-Host "以包身份启动：$aumid" -ForegroundColor Cyan
Start-Process 'explorer.exe' -ArgumentList "shell:AppsFolder\$aumid"
