#Requires -RunAsAdministrator

$ErrorActionPreference = 'Stop'

$certPath = Join-Path $env:LOCALAPPDATA 'EliteFIPServer\DevSigning\EliteFIPServer.LocalDevCodeSigning.cer'

if (-not (Test-Path $certPath)) {
    throw "Dev signing certificate not found at $certPath. Build EliteFIPServer.UI first, or run Sign-DevBuild.ps1 with -ExportCertificate."
}

foreach ($storeName in @('Root', 'TrustedPublisher')) {
    $store = [System.Security.Cryptography.X509Certificates.X509Store]::new($storeName, 'LocalMachine')
    $store.Open([System.Security.Cryptography.X509Certificates.OpenFlags]::ReadWrite)
    try {
        $cert = [System.Security.Cryptography.X509Certificates.X509Certificate2]::new($certPath)
        $existing = $store.Certificates | Where-Object { $_.Thumbprint -eq $cert.Thumbprint } | Select-Object -First 1
        if (-not $existing) {
            $store.Add($cert)
            Write-Host "Installed $($cert.Subject) into LocalMachine\$storeName"
        } else {
            Write-Host "$($cert.Subject) already exists in LocalMachine\$storeName"
        }
    } finally {
        $store.Close()
    }
}