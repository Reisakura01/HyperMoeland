param(
    [Parameter(Mandatory)]
    [string]$CertificateDirectory
)

$ErrorActionPreference = 'Stop'

# 发布者名称必须与 AppxManifest.xml.template 里的 Publisher 完全一致
$subject = 'CN=MoeOrigin Team'
$friendlyName = 'Islora Development Package Signing'
$pfxPassword = 'IsloraDevelopment'
$pfxName = 'Islora.Dev.pfx'

New-Item -ItemType Directory -Force -Path $CertificateDirectory | Out-Null

$certificate = Get-ChildItem Cert:\CurrentUser\My |
    Where-Object {
        $_.Subject -eq $subject -and
        $_.FriendlyName -eq $friendlyName -and
        $_.HasPrivateKey
    } |
    Select-Object -First 1

if ($null -eq $certificate) {
    Write-Host "生成自签名证书：$subject"
    $certificate = New-SelfSignedCertificate `
        -Type Custom `
        -KeyUsage DigitalSignature `
        -CertStoreLocation 'Cert:\CurrentUser\My' `
        -TextExtension @('2.5.29.37={text}1.3.6.1.5.5.7.3.3', '2.5.29.19={text}') `
        -Subject $subject `
        -FriendlyName $friendlyName
}
else {
    Write-Host "复用已有证书：$subject"
}

$password = ConvertTo-SecureString $pfxPassword -AsPlainText -Force
$pfxPath = Join-Path $CertificateDirectory $pfxName
Export-PfxCertificate -Cert $certificate -FilePath $pfxPath -Password $password -Force | Out-Null
Write-Host "已导出证书：$pfxPath"
