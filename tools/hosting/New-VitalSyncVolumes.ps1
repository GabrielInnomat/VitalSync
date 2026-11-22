[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
Import-Module (Join-Path $PSScriptRoot 'VitalSyncHosting.psm1') -Force

$volumes = @(
    'vitalsync-messaging-data'
    'vitalsync-pgadmin-data'
    'vitalsync-postgres-data'
    'vitalsync-web-keys'
)

foreach ($volume in $volumes) {
    if ((Invoke-Docker -Arguments @('volume', 'inspect', $volume) -AllowFailure).ExitCode -eq 0) {
        Write-Host "$volume already exists"
        continue
    }

    Invoke-Docker -Arguments @('volume', 'create', '--label', 'vitalsync.hosted=true', $volume) | Out-Null
    Write-Host "$volume created"
}
