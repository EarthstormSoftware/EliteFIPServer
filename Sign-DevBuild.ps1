param(
    [Parameter(Mandatory = $true)]
    [string]$OutputDirectory,

    [switch]$ExportCertificate
)

$ErrorActionPreference = 'Stop'

$certSubject = 'CN=EliteFIPServer Local Dev Code Signing'
$cert = Get-ChildItem Cert:\CurrentUser\My |
    Where-Object { $_.Subject -eq $certSubject -and $_.NotAfter -gt (Get-Date) } |
    Sort-Object NotAfter -Descending |
    Select-Object -First 1

if (-not $cert) {
    $cert = New-SelfSignedCertificate `
        -Type CodeSigningCert `
        -Subject $certSubject `
        -CertStoreLocation Cert:\CurrentUser\My `
        -KeyAlgorithm RSA `
        -KeyLength 2048 `
        -HashAlgorithm SHA256 `
        -NotAfter (Get-Date).AddYears(5)
}

if ($ExportCertificate) {
    $certExportDirectory = Join-Path $env:LOCALAPPDATA 'EliteFIPServer\DevSigning'
    New-Item -ItemType Directory -Force -Path $certExportDirectory | Out-Null
    $certExportPath = Join-Path $certExportDirectory 'EliteFIPServer.LocalDevCodeSigning.cer'
    Export-Certificate -Cert $cert -FilePath $certExportPath -Force | Out-Null
    Write-Host "Exported local dev code-signing certificate to $certExportPath"
}

$resolvedOutputDirectory = Resolve-Path $OutputDirectory
$fileNamesToSign = @(
    'EliteFIPServer.UI.exe',
    'EliteFIPServer.UI.dll',
    'EliteFIPServer.Core.dll',
    'EliteAPI.dll',
    'EliteFIPProtocol.dll'
)

$filesToSign = foreach ($fileName in $fileNamesToSign) {
    $filePath = Join-Path $resolvedOutputDirectory $fileName
    if (Test-Path $filePath) {
        Get-Item $filePath
    }
}

foreach ($file in $filesToSign) {
    $signature = Get-AuthenticodeSignature $file.FullName
    if ($signature.Status -eq 'Valid' -and $signature.SignerCertificate.Thumbprint -eq $cert.Thumbprint) {
        continue
    }

    Write-Host "Signing $($file.Name)"

    Set-AuthenticodeSignature `
        -FilePath $file.FullName `
        -Certificate $cert `
        -HashAlgorithm SHA256 | Out-Null
}