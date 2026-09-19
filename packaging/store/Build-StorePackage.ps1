<#
.SYNOPSIS
  把 Islora 打成「完整 MSIX 包」——用于 Microsoft Store 提交，或本地安装测试。

.DESCRIPTION
  流程：
    1. dotnet publish 发布应用（默认框架依赖；-SelfContained 可发布自包含版）
    2. 从 Islora.csproj 读版本号，换算成清单要求的四段式
    3. 用 App.ico 生成清单所需的全套图标（44/50/71/150/310/310x150）
    4. 由 AppxManifest.xml.template 生成 AppxManifest.xml
    5. MakeAppx 打包；默认用自签证书签名（仅本地测试用）

  提交 Store 时用 -NoSign 产出未签名包（Store 会免费代为签名），
  并把 -PackageName / -Publisher / -PublisherDisplayName 换成 Partner Center 给出的值。

.PARAMETER SelfContained
  发布自包含版（用户无需安装 .NET 运行时）。需要能访问 nuget.org 拉运行时包。

.PARAMETER NoSign
  不打签名（用于提交 Store）。

.EXAMPLE
  pwsh -File packaging/store/Build-StorePackage.ps1
  pwsh -File packaging/store/Build-StorePackage.ps1 -NoSign -PackageName "12345Reisakura01.Islora" -Publisher "CN=1A2B3C4D-..."
#>
[CmdletBinding()]
param(
    [string]$Version,
    [string]$Configuration = 'Release',
    [string]$StagingDirectory,
    [string]$OutputDirectory,
    [string]$PackageName = 'MoeOrigin.Islora.StoreTest',
    [string]$Publisher = 'CN=MoeOrigin Team',
    [string]$PublisherDisplayName = 'MoeOrigin Team',
    [string]$DisplayName = 'Islora',
    [string]$Description = 'Islora —— Windows 11 顶部胶囊式状态岛：时钟 / 媒体 / 通知 / 电量 / 系统小组件',
    [string]$CertificatePath,
    [string]$CertificatePassword = 'IsloraDevelopment',
    [switch]$SelfContained,
    [switch]$SkipPublish,
    [switch]$NoSign
)

$ErrorActionPreference = 'Stop'

function Write-Step([string]$Text) {
    Write-Host ""
    Write-Host "== $Text ==" -ForegroundColor Cyan
}

# ---- 路径与工具 ----
$storeDirectory = $PSScriptRoot
$repositoryRoot = Split-Path (Split-Path $storeDirectory -Parent) -Parent
$projectPath = Join-Path $repositoryRoot 'Islora\Islora.csproj'
$iconPath = Join-Path $repositoryRoot 'Islora\App.ico'
$templatePath = Join-Path $storeDirectory 'AppxManifest.xml.template'

# 注意：Store 包用的是自包含发布，与安装包（框架依赖）的产物不能混在同一个目录里——
# 混放会出现 runtimeconfig.json 指向共享框架、而目录里又是自包含文件的四不像状态，
# 应用启动会卡住且不报错。因此这里默认用独立的暂存目录。
if ([string]::IsNullOrWhiteSpace($StagingDirectory)) { $StagingDirectory = Join-Path $repositoryRoot 'artifacts\app-store' }
if ([string]::IsNullOrWhiteSpace($OutputDirectory))  { $OutputDirectory  = Join-Path $repositoryRoot 'artifacts\store' }

function Find-SdkTool([string]$Name) {
    $candidates = Get-ChildItem 'C:\Program Files (x86)\Windows Kits\10\bin' -Directory -ErrorAction SilentlyContinue |
        Sort-Object Name -Descending |
        ForEach-Object { Join-Path $_.FullName "x64\$Name" }
    $hit = $candidates | Where-Object { Test-Path $_ } | Select-Object -First 1
    if (-not $hit) { throw "找不到 $Name，请安装 Windows SDK（含 10.0.26100 的 x64 工具）" }
    return $hit
}
$makeAppx = Find-SdkTool 'makeappx.exe'
$signTool = Find-SdkTool 'signtool.exe'

# ---- 版本号 ----
if ([string]::IsNullOrWhiteSpace($Version)) {
    $projectXml = Get-Content -LiteralPath $projectPath -Raw -Encoding UTF8
    $match = [regex]::Match($projectXml, '<Version>\s*([^<\s]+)\s*</Version>')
    if (-not $match.Success) { throw "无法从 $projectPath 读取 <Version>" }
    $Version = $match.Groups[1].Value
}
# 清单版本号必须是纯数字四段式：1.3.0-beta.1 → 1.3.0.1
$packageVersion = ([regex]::Matches($Version, '\d+') | ForEach-Object { $_.Value }) -join '.'
while (($packageVersion -split '\.').Count -lt 4) { $packageVersion += '.0' }

Write-Step 'Islora 完整 MSIX 打包'
Write-Host "应用版本  : $Version"
Write-Host "清单版本  : $packageVersion"
Write-Host "包名      : $PackageName"
Write-Host "发布者    : $Publisher"
Write-Host "暂存目录  : $StagingDirectory"
Write-Host "产物目录  : $OutputDirectory"
Write-Host "自包含    : $([bool]$SelfContained)"

# ---- 1. 发布 ----
if (-not $SkipPublish) {
    Write-Step "发布应用（dotnet publish -c $Configuration$(if($SelfContained){' --self-contained'}))"
    if (Test-Path $StagingDirectory) { Remove-Item $StagingDirectory -Recurse -Force }
    New-Item -ItemType Directory -Path $StagingDirectory -Force | Out-Null

    $publishArgs = @($projectPath, '-c', $Configuration, '-o', $StagingDirectory)
    if ($SelfContained) {
        $publishArgs += @('-r', 'win-x64', '--self-contained', 'true')
        $publishArgs += @('-p:PublishSingleFile=false')
    }
    & dotnet publish @publishArgs
    if ($LASTEXITCODE -ne 0) { throw "dotnet publish 失败（退出码 $LASTEXITCODE）" }
}

$appExe = Join-Path $StagingDirectory 'Islora.exe'
if (-not (Test-Path $appExe)) { throw "暂存目录里没有 Islora.exe：$StagingDirectory" }

# ---- 2. 组装包目录 ----
Write-Step '组装包内容'
$packageRoot = Join-Path $OutputDirectory 'package'
if (Test-Path $packageRoot) { Remove-Item $packageRoot -Recurse -Force }
New-Item -ItemType Directory -Path $packageRoot -Force | Out-Null
$assetsDirectory = Join-Path $packageRoot 'Assets'
New-Item -ItemType Directory -Path $assetsDirectory -Force | Out-Null

# 应用文件：排除调试符号、包身份脚本与文档（Store 包不需要）
Get-ChildItem -Path $StagingDirectory -File | Where-Object {
    $_.Extension -ne '.pdb' -and $_.Name -notlike '*.log'
} | ForEach-Object { Copy-Item $_.FullName -Destination $packageRoot -Force }

Get-ChildItem -Path $StagingDirectory -Directory -ErrorAction SilentlyContinue |
    Where-Object { $_.Name -ne 'identity' } |
    ForEach-Object { Copy-Item $_.FullName -Destination $packageRoot -Recurse -Force }

# ---- 3. 图标 ----
Write-Step '生成包图标（源自 App.ico）'
Add-Type -AssemblyName System.Drawing

function Export-IconPng {
    param([string]$IcoPath, [int]$Width, [int]$Height, [string]$Destination, [int]$IconSize = 0)

    if ($IconSize -le 0) { $IconSize = [Math]::Min($Width, $Height) }
    $icon = New-Object System.Drawing.Icon($IcoPath, $IconSize, $IconSize)
    $source = $icon.ToBitmap()

    $canvas = New-Object System.Drawing.Bitmap($Width, $Height)
    $graphics = [System.Drawing.Graphics]::FromImage($canvas)
    $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $graphics.Clear([System.Drawing.Color]::Transparent)
    $x = [int](($Width - $IconSize) / 2)
    $y = [int](($Height - $IconSize) / 2)
    $graphics.DrawImage($source, $x, $y, $IconSize, $IconSize)
    $graphics.Dispose()
    $canvas.Save($Destination, [System.Drawing.Imaging.ImageFormat]::Png)
    $canvas.Dispose(); $source.Dispose(); $icon.Dispose()
}

$iconSpecs = @(
    @{ Name = 'Square44x44Logo.png';  W = 44;  H = 44 },
    @{ Name = 'Square71x71Logo.png';  W = 71;  H = 71 },
    @{ Name = 'Square150x150Logo.png'; W = 150; H = 150 },
    @{ Name = 'Square310x310Logo.png'; W = 310; H = 310 },
    @{ Name = 'Wide310x150Logo.png';  W = 310; H = 150 },
    @{ Name = 'StoreLogo.png';        W = 50;  H = 50 }
)
foreach ($spec in $iconSpecs) {
    $destination = Join-Path $assetsDirectory $spec.Name
    Export-IconPng -IcoPath $iconPath -Width $spec.W -Height $spec.H -Destination $destination
    Write-Host ("  {0,-24} {1}x{2}" -f $spec.Name, $spec.W, $spec.H)
}

# ---- 4. 清单 ----
Write-Step '生成 AppxManifest.xml'
$manifest = Get-Content -LiteralPath $templatePath -Raw -Encoding UTF8
$manifest = $manifest.Replace('{{PACKAGE_NAME}}', $PackageName).
    Replace('{{PUBLISHER_DISPLAY_NAME}}', $PublisherDisplayName).
    Replace('{{PUBLISHER}}', $Publisher).
    Replace('{{PACKAGE_VERSION}}', $packageVersion).
    Replace('{{DISPLAY_NAME}}', $DisplayName).
    Replace('{{DESCRIPTION}}', $Description).
    Replace('{{EXECUTABLE}}', 'Islora.exe')
[System.IO.File]::WriteAllText((Join-Path $packageRoot 'AppxManifest.xml'), $manifest, (New-Object System.Text.UTF8Encoding($true)))
Write-Host "  已写入 AppxManifest.xml"

# ---- 5. 打包 ----
Write-Step 'MakeAppx 打包'
New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null
$msixPath = Join-Path $OutputDirectory "$PackageName-$packageVersion.msix"
if (Test-Path $msixPath) { Remove-Item $msixPath -Force }
& $makeAppx pack /d $packageRoot /p $msixPath /o
if ($LASTEXITCODE -ne 0) { throw "MakeAppx 打包失败（退出码 $LASTEXITCODE）" }

# ---- 6. 签名 ----
if (-not $NoSign) {
    Write-Step '签名（本机自签证书，仅用于本地安装测试）'
    if ([string]::IsNullOrWhiteSpace($CertificatePath)) {
        $CertificatePath = Join-Path $repositoryRoot 'dist\identity\cert\Islora.Dev.pfx'
    }
    if (-not (Test-Path $CertificatePath)) {
        throw "找不到证书：$CertificatePath`n请先用 packaging/identity/New-Certificate.ps1 生成，或传 -CertificatePath。"
    }
    & $signTool sign /fd SHA256 /f $CertificatePath /p $CertificatePassword $msixPath
    if ($LASTEXITCODE -ne 0) { throw "signtool 签名失败（退出码 $LASTEXITCODE）" }
}
else {
    Write-Host ""
    Write-Host "已跳过签名（-NoSign）：上传 Partner Center 用这个未签名包即可。" -ForegroundColor Yellow
}

# ---- 完成 ----
$size = [math]::Round((Get-Item $msixPath).Length / 1MB, 2)
Write-Host ""
Write-Host "打包成功！" -ForegroundColor Green
Write-Host "  安装包 : $msixPath"
Write-Host "  大小   : $size MB"
Write-Host "  包目录 : $packageRoot"
Write-Host ""
Write-Host "本地安装测试：" -ForegroundColor Yellow
Write-Host "  pwsh -File packaging/store/Install-StorePackage.ps1 -MsixPath `"$msixPath`""

return $msixPath
