param(
    [Parameter(Mandatory)]
    [ValidateSet('dev', 'stable')]
    [string]$Channel,

    [Parameter(Mandatory)]
    [string]$Version,

    [Parameter(Mandatory)]
    [string]$CertificatePath,

    [Parameter(Mandatory)]
    [string]$CertificatePassword,

    [Parameter(Mandatory)]
    [string]$OutputDirectory
)

$ErrorActionPreference = 'Stop'

function Get-SdkTool([string]$Name) {
    $roots = @(
        "${env:ProgramFiles(x86)}\Windows Kits\10\bin",
        "$env:ProgramFiles\Windows Kits\10\bin"
    )
    foreach ($root in $roots) {
        $tool = Get-ChildItem -Path $root -Filter $Name -Recurse -ErrorAction SilentlyContinue |
            Where-Object { $_.FullName -match '\\x64\\' } |
            Sort-Object FullName -Descending |
            Select-Object -First 1
        if ($null -ne $tool) { return $tool.FullName }
    }
    throw "$Name 未找到，请先安装 Windows SDK（10 或 11）。"
}

$publisher = 'CN=MoeOrigin Team'

$identity = switch ($Channel) {
    'stable' { @{ Name = 'MoeOrigin.Islora';     DisplayName = 'Islora' } }
    'dev'    { @{ Name = 'MoeOrigin.Islora.Dev'; DisplayName = 'Islora Development' } }
}

# 兼容两种布局：仓库内（<repo>\packaging\identity）与安装目录内（<app>\identity）
$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$iconPath = @(
    (Join-Path $repositoryRoot 'Islora\App.ico'),   # 仓库布局
    (Join-Path $PSScriptRoot 'App.ico')                   # 安装目录布局（随安装包附带）
) | Where-Object { Test-Path $_ } | Select-Object -First 1
if (-not $iconPath) { throw '找不到应用图标 App.ico（仓库：Islora\App.ico；安装目录：identity\App.ico）' }

$stagingDirectory = Join-Path $OutputDirectory 'identity-staging'
$assetsDirectory = Join-Path $stagingDirectory 'assets'
$manifestPath = Join-Path $stagingDirectory 'AppxManifest.xml'
$packagePath = Join-Path $OutputDirectory "$($identity.Name).msix"

Remove-Item -Recurse -Force -ErrorAction SilentlyContinue $stagingDirectory
New-Item -ItemType Directory -Force -Path $stagingDirectory | Out-Null
New-Item -ItemType Directory -Force -Path $assetsDirectory | Out-Null
New-Item -ItemType Directory -Force -Path $OutputDirectory | Out-Null

# ---- 从 App.ico 生成清单所需的方形图标 ----
Add-Type -AssemblyName System.Drawing

function Export-IconPng([string]$icoPath, [int]$size, [string]$destination) {
    $icon = New-Object System.Drawing.Icon($icoPath, $size, $size)
    $bitmap = $icon.ToBitmap()
    $canvas = New-Object System.Drawing.Bitmap($size, $size)
    $graphics = [System.Drawing.Graphics]::FromImage($canvas)
    $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $graphics.Clear([System.Drawing.Color]::Transparent)
    $graphics.DrawImage($bitmap, 0, 0, $size, $size)
    $graphics.Dispose()
    $canvas.Save($destination, [System.Drawing.Imaging.ImageFormat]::Png)
    $canvas.Dispose()
    $bitmap.Dispose()
    $icon.Dispose()
}

Export-IconPng $iconPath 44  (Join-Path $assetsDirectory 'Square44x44Logo.png')
Export-IconPng $iconPath 150 (Join-Path $assetsDirectory 'Square150x150Logo.png')
Export-IconPng $iconPath 50  (Join-Path $assetsDirectory 'StoreLogo.png')

# ---- 生成清单 ----
# 必须显式指定编码：模板含中文注释且没有 BOM，
# Windows PowerShell 5.1 下 Get-Content 不指定编码会按系统 ANSI 代码页解码 → 生成物中文乱码。
$manifest = Get-Content -Raw -Encoding UTF8 (Join-Path $PSScriptRoot 'AppxManifest.xml.template')
$manifest = $manifest.Replace('{{PACKAGE_NAME}}', $identity.Name)
$manifest = $manifest.Replace('{{PUBLISHER}}', $publisher)
$manifest = $manifest.Replace('{{PACKAGE_VERSION}}', $Version)
$manifest = $manifest.Replace('{{DISPLAY_NAME}}', $identity.DisplayName)
# 同样显式写 UTF-8 with BOM（PowerShell 5.1 的 -Encoding utf8 就是带 BOM 的 UTF-8）
[System.IO.File]::WriteAllText($manifestPath, $manifest, (New-Object System.Text.UTF8Encoding($true)))

# ---- 打包 + 签名 ----
$makeAppx = Get-SdkTool 'MakeAppx.exe'
$signTool = Get-SdkTool 'SignTool.exe'

Remove-Item -Force -ErrorAction SilentlyContinue $packagePath
& $makeAppx pack /o /d $stagingDirectory /nv /p $packagePath
if ($LASTEXITCODE -ne 0) { throw 'MakeAppx 打包失败。' }

& $signTool sign /fd SHA256 /f $CertificatePath /p $CertificatePassword $packagePath
if ($LASTEXITCODE -ne 0) { throw 'SignTool 签名失败。' }

# ---- 导出公钥证书（用于导入受信任存储） ----
$certificate = New-Object System.Security.Cryptography.X509Certificates.X509Certificate2($CertificatePath, $CertificatePassword)
$cerPath = Join-Path $OutputDirectory "$($identity.Name).cer"
[IO.File]::WriteAllBytes($cerPath, $certificate.Export([System.Security.Cryptography.X509Certificates.X509ContentType]::Cert))
$certificate.Dispose()

Write-Host "已生成身份包：$packagePath"
Write-Host "已生成公钥证书：$cerPath"
