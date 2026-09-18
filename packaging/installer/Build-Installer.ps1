<#
.SYNOPSIS
  一键构建 HyperMoeland 的 Inno Setup 安装包。

.DESCRIPTION
  流程：
    1. dotnet publish 发布应用（框架依赖，输出到 artifacts\app）
    2. 从 HyperMoeland.csproj 读取版本号
    3. 调用 Inno Setup 编译器 ISCC.exe 生成安装包到 artifacts\

  自动查找 ISCC.exe（注册表 → 常见安装路径 → PATH）。
  未安装 Inno Setup 时会给出安装提示并退出。

.PARAMETER Version
  安装包版本号，默认取 HyperMoeland.csproj 里的 <Version>。

.PARAMETER StagingDirectory
  publish 输出目录，默认 <仓库>\artifacts\app。

.PARAMETER OutputDirectory
  安装包输出目录，默认 <仓库>\artifacts。

.PARAMETER SkipPublish
  跳过 dotnet publish，直接用已有的暂存目录打包。

.PARAMETER OpenOutput
  构建完成后在资源管理器中打开产物目录。

.EXAMPLE
  pwsh -File packaging/installer/Build-Installer.ps1
  pwsh -File packaging/installer/Build-Installer.ps1 -Version 1.3.0 -OpenOutput
#>
[CmdletBinding()]
param(
    [string]$Version,
    [string]$Configuration = 'Release',
    [string]$StagingDirectory,
    [string]$OutputDirectory,
    [switch]$SkipPublish,
    [switch]$OpenOutput
)

$ErrorActionPreference = 'Stop'

function Write-Step([string]$Text) {
    Write-Host ""
    Write-Host "== $Text ==" -ForegroundColor Cyan
}

# ---- 路径 ----
$installerDirectory = $PSScriptRoot
$repositoryRoot = Split-Path (Split-Path $installerDirectory -Parent) -Parent
$projectPath = Join-Path $repositoryRoot 'HyperMoeland\HyperMoeland.csproj'
$scriptPath = Join-Path $installerDirectory 'HyperMoeland.iss'

if (-not (Test-Path $projectPath)) { throw "找不到项目文件：$projectPath" }
if (-not (Test-Path $scriptPath)) { throw "找不到 Inno 脚本：$scriptPath" }

if ([string]::IsNullOrWhiteSpace($StagingDirectory)) {
    $StagingDirectory = Join-Path $repositoryRoot 'artifacts\app'
}
if ([string]::IsNullOrWhiteSpace($OutputDirectory)) {
    $OutputDirectory = Join-Path $repositoryRoot 'artifacts'
}

# ---- 版本号 ----
if ([string]::IsNullOrWhiteSpace($Version)) {
    $projectXml = Get-Content -LiteralPath $projectPath -Raw -Encoding UTF8
    $match = [regex]::Match($projectXml, '<Version>\s*([^<\s]+)\s*</Version>')
    if (-not $match.Success) { throw "无法从 $projectPath 读取 <Version>" }
    $Version = $match.Groups[1].Value
}

# Inno 的 VersionInfoVersion 必须是纯数字四段式
$numeric = ([regex]::Matches($Version, '\d+') | ForEach-Object { $_.Value }) -join '.'
while (($numeric -split '\.').Count -lt 4) { $numeric += '.0' }

Write-Step 'HyperMoeland 安装包构建'
Write-Host "版本号    : $Version  (VersionInfoVersion=$numeric)"
Write-Host "暂存目录  : $StagingDirectory"
Write-Host "产物目录  : $OutputDirectory"

# ---- 发布应用 ----
if (-not $SkipPublish) {
    Write-Step "发布应用（dotnet publish -c $Configuration）"
    if (Test-Path $StagingDirectory) { Remove-Item $StagingDirectory -Recurse -Force }
    New-Item -ItemType Directory -Path $StagingDirectory -Force | Out-Null

    & dotnet publish $projectPath -c $Configuration -o $StagingDirectory
    if ($LASTEXITCODE -ne 0) { throw "dotnet publish 失败（退出码 $LASTEXITCODE）" }

    # 调试符号不进安装包；包身份的开发产物（证书/私钥/msix）绝不随包分发
    Get-ChildItem -Path $StagingDirectory -Recurse -Include *.pdb -File -ErrorAction SilentlyContinue |
        Remove-Item -Force -ErrorAction SilentlyContinue
    $identityJunk = Join-Path $StagingDirectory 'identity'
    if (Test-Path $identityJunk) { Remove-Item $identityJunk -Recurse -Force }
}

$exePath = Join-Path $StagingDirectory 'HyperMoeland.exe'
if (-not (Test-Path $exePath)) { throw "暂存目录里没有 HyperMoeland.exe：$StagingDirectory" }

# ---- 查找 ISCC ----
Write-Step '查找 Inno Setup 编译器'
$candidates = @()
foreach ($key in @(
    'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Inno Setup 6_is1',
    'HKLM:\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\Inno Setup 6_is1',
    'HKCU:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Inno Setup 6_is1')) {
    $item = Get-ItemProperty -Path $key -ErrorAction SilentlyContinue
    if ($item -and $item.InstallLocation) {
        $candidates += (Join-Path $item.InstallLocation 'ISCC.exe')
    }
}
$candidates += @(
    (Join-Path $env:LOCALAPPDATA 'Programs\Inno Setup 6\ISCC.exe'),
    'C:\Program Files (x86)\Inno Setup 6\ISCC.exe',
    'C:\Program Files\Inno Setup 6\ISCC.exe'
)
$onPath = Get-Command ISCC.exe -ErrorAction SilentlyContinue
if ($onPath) { $candidates += $onPath.Source }

$iscc = $candidates | Where-Object { $_ -and (Test-Path $_) } | Select-Object -First 1
if (-not $iscc) {
    throw @'
未找到 Inno Setup 6 编译器 ISCC.exe。
请先安装 Inno Setup 6：https://jrsoftware.org/isdl.php
（或执行：winget install --id JRSoftware.InnoSetup）
安装后重新运行本脚本即可。
'@
}
Write-Host "ISCC      : $iscc"

# ---- 编译 ----
Write-Step '编译安装包'
New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null

& $iscc "/DAppVersion=$Version" "/DNumericVersion=$numeric" `
    "/DStagingDir=$StagingDirectory" "/DOutputDir=$OutputDirectory" $scriptPath
if ($LASTEXITCODE -ne 0) { throw "ISCC 编译失败（退出码 $LASTEXITCODE）" }

$setup = Get-ChildItem -Path $OutputDirectory -Filter "HyperMoeland-$Version-setup.exe" -File -ErrorAction SilentlyContinue |
    Select-Object -First 1
if (-not $setup) { throw "编译结束但没有找到安装包，请检查 $OutputDirectory" }

Write-Host ""
Write-Host "构建成功！" -ForegroundColor Green
Write-Host ("  安装包 : {0}" -f $setup.FullName)
Write-Host ("  大小   : {0:N1} MB" -f ($setup.Length / 1MB))

if ($OpenOutput) { Start-Process explorer.exe -ArgumentList "/select,`"$($setup.FullName)`"" }

return $setup.FullName
