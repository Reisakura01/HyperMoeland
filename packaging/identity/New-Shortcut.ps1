<#
.SYNOPSIS
  创建「Islora（带身份）」快捷方式。

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

$packageName = if ($Channel -eq 'dev') { 'MoeOrigin.Islora.Dev' } else { 'MoeOrigin.Islora' }
$package = Get-AppxPackage -Name $packageName -ErrorAction SilentlyContinue
if ($null -eq $package) {
    throw "未找到已注册的包 $packageName。请先运行 Install-Identity.ps1。"
}

$aumid = "$($package.PackageFamilyName)!Islora"

$shortcutPath = switch ($Destination) {
    'Desktop'   { Join-Path ([Environment]::GetFolderPath('Desktop')) 'Islora.lnk' }
    'StartMenu' { Join-Path ([Environment]::GetFolderPath('Programs')) 'Islora.lnk' }
    default     { $Destination }
}

$iconPath = @(
    (Join-Path (Split-Path (Split-Path $PSScriptRoot -Parent) -Parent) 'Islora\App.ico'),  # 仓库布局
    (Join-Path $PSScriptRoot 'App.ico'),                                                        # 安装目录布局
    (Join-Path (Split-Path $PSScriptRoot -Parent) 'App.ico')
) | Where-Object { Test-Path $_ } | Select-Object -First 1

$shell = New-Object -ComObject WScript.Shell
$shortcut = $shell.CreateShortcut($shortcutPath)
$shortcut.TargetPath = 'explorer.exe'
$shortcut.Arguments = "shell:AppsFolder\$aumid"
$shortcut.Description = 'Islora（带包身份启动）'
if ($iconPath) {
    $shortcut.WorkingDirectory = (Split-Path $iconPath)
    $shortcut.IconLocation = $iconPath
}
$shortcut.Save()

Write-Host "已创建快捷方式：$shortcutPath" -ForegroundColor Green
Write-Host "目标 AUMID：$aumid"
