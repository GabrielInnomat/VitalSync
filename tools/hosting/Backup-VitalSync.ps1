[CmdletBinding()]
param(
    [Parameter(Mandatory)] [string] $Destination,
    [ValidateRange(1, 1000)] [int] $Keep = 14,
    [string] $Project
)

$ErrorActionPreference = 'Stop'
Import-Module (Join-Path $PSScriptRoot 'VitalSyncHosting.psm1') -Force

$Project = Resolve-ComposeProject -Project $Project
$postgres = Get-ServiceContainer -Project $Project -Service 'postgres'
$databases = Get-ApplicationDatabases -Container $postgres
if ($databases.Count -eq 0) {
    throw "The PostgreSQL server of '$Project' holds no application database, so there is nothing to back up."
}

$Destination = (New-Item -ItemType Directory -Force -Path $Destination).FullName
$name = 'vitalsync-' + (Get-Date -Format 'yyyyMMdd-HHmmss')
$staging = Join-Path $Destination ".$name.partial"
$final = Join-Path $Destination $name
$inContainer = "/tmp/$name"

New-Item -ItemType Directory -Path $staging | Out-Null
try {
    Invoke-Docker -Arguments @('exec', $postgres, 'mkdir', '-p', $inContainer) | Out-Null
    Invoke-InPostgres -Container $postgres -Command "pg_dumpall -U postgres --globals-only --file '$inContainer/globals.sql'" | Out-Null

    foreach ($database in $databases) {
        Write-Host "Dumping $database"
        $file = "$inContainer/$database.dump"
        Invoke-InPostgres -Container $postgres -Command "pg_dump -U postgres --format=custom --file '$file' --dbname '$database'" | Out-Null
        Invoke-InPostgres -Container $postgres -Command "pg_restore --list '$file' > /dev/null" | Out-Null
    }

    Invoke-Docker -Arguments @('cp', "${postgres}:$inContainer/.", $staging) | Out-Null

    $missing = @($databases | Where-Object { -not (Test-Path -LiteralPath (Join-Path $staging "$_.dump")) })
    if ($missing.Count -gt 0) {
        throw "The backup is missing the dumps of: $($missing -join ', ')."
    }

    $manifest = [ordered]@{
        project   = $Project
        createdAt = (Get-Date).ToString('o')
        databases = @($databases | ForEach-Object { [ordered]@{ name = $_; tables = Get-TableCount -Container $postgres -Database $_ } })
    }
    $manifest | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath (Join-Path $staging 'manifest.json') -Encoding utf8
    Rename-Item -LiteralPath $staging -NewName $name
}
catch {
    $failure = $_
    try {
        Remove-Item -LiteralPath $staging -Recurse -Force
    }
    catch {
        Write-Warning "The incomplete backup '$staging' could not be removed: $($_.Exception.Message)"
    }
    throw $failure
}
finally {
    Invoke-Docker -Arguments @('exec', $postgres, 'rm', '-rf', $inContainer) -AllowFailure | Out-Null
}

Get-ChildItem -LiteralPath $Destination -Directory |
    Where-Object { $_.Name -match '^vitalsync-\d{8}-\d{6}$' } |
    Sort-Object Name -Descending |
    Select-Object -Skip $Keep |
    ForEach-Object {
        Write-Host "Removing the old backup $($_.Name)"
        Remove-Item -LiteralPath $_.FullName -Recurse -Force
    }

Write-Host "Backed up $($databases.Count) databases to $final"
