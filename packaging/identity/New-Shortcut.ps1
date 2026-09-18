<#
.SYNOPSIS
  创建「HyperMoeland（带身份）」快捷方式。

.DESCRIPTION
  快捷方式指向 explorer.exe shell:AppsFolder\<AUMID>，
  因此从该快捷方式启动时进程带包身份（通知走官方事件订阅）。

.PARAMETER Destination
  'Desktop'（默认）、'StartMenu'，或自定义的 .lnk 完整路径。

.PARAMETER Channel
  stable（默认）或 dev。
#>
param(
    [string]$Destination = 'Desktop',

    [ValidateSet('dev', 'stable')]
    [string]$Channel = 'stable'
)

$ErrorActionPreference = 'Stop'

$packageName = if ($Channel -eq 'dev') { 'MoeOrigin.HyperMoeland.Dev' } else { 'MoeOrigin.HyperMoeland' }
$package = Get-AppxPackage -Name $packageName -ErrorAction SilentlyContinue
if ($null -eq $package) {
    throw "未找到已注册的包 $packageName。请先运行 Install-Identity.ps1。"
}

$aumid = "$($package.PackageFamilyName)!HyperMoeland"

$shortcutPath = switch ($Destination) {
    'Desktop'   { Join-Path ([Environment]::GetFolderPath('Desktop')) 'HyperMoeland.lnk' }
    'StartMenu' { Join-Path ([Environment]::GetFolderPath('Programs')) 'HyperMoeland.lnk' }
    default     { $Destination }
}

$iconPath = Join-Path (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path 'HyperMoeland\App.ico'

$shell = New-Object -ComObject WScript.Shell
$shortcut = $shell.CreateShortcut($shortcutPath)
$shortcut.TargetPath = 'explorer.exe'
$shortcut.Arguments = "shell:AppsFolder\$aumid"
$shortcut.WorkingDirectory = (Split-Path $iconPath)
if (Test-Path $iconPath) { $shortcut.IconLocation = $iconPath }
$shortcut.Description = 'HyperMoeland（带包身份启动）'
$shortcut.Save()

Write-Host "已创建快捷方式：$shortcutPath" -ForegroundColor Green
Write-Host "目标 AUMID：$aumid"
