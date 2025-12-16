# 1) Create folders in Documents\hollow-IM\certificates
$docsPath = [Environment]::GetFolderPath("MyDocuments")
$certDir  = Join-Path $docsPath "hollow-IM\certificates"

if (!(Test-Path $certDir)) {
    New-Item -ItemType Directory -Path $certDir -Force | Out-Null
    Write-Host "Created folder: $certDir"
} else {
    Write-Host "Folder already exists: $certDir"
}

# 2) Generate self-signed certificate with SAN entries
$cert = New-SelfSignedCertificate `
    -DnsName "localhost","127.0.0.1" `
    -CertStoreLocation "Cert:\CurrentUser\My" `
    -KeyExportPolicy Exportable `
    -NotAfter (Get-Date).AddYears(1)

Write-Host "Created certificate with thumbprint: $($cert.Thumbprint)"

# 3) Export to PKCS#12 (.pfx)
$pfxPath = Join-Path $certDir "server.pfx"
$pwd     = ConvertTo-SecureString -String "password" -Force -AsPlainText

Export-PfxCertificate -Cert $cert -FilePath $pfxPath -Password $pwd

Write-Host "Exported certificate to: $pfxPath"
