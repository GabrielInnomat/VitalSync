[CmdletBinding(SupportsShouldProcess, ConfirmImpact = 'High', DefaultParameterSetName = 'Verify')]
param(
    [Parameter(Mandatory)] [string] $Destination,
    [string] $Backup,
    [Parameter(ParameterSetName = 'Live', Mandatory)] [switch] $IntoRunningSystem,
    [string] $Project,
    [Parameter(ParameterSetName = 'Verify')] [string] $Image = 'postgres:18.3'
)

$ErrorActionPreference = 'Stop'
Import-Module (Join-Path $PSScriptRoot 'VitalSyncHosting.psm1') -Force

if (-not $Backup) {
    $Backup = Get-LatestBackup -Destination $Destination
}

$manifestPath = Join-Path $Backup 'manifest.json'
if (-not (Test-Path -LiteralPath $manifestPath)) {
    throw "'$Backup' is not a VitalSync backup: it has no manifest.json."
}

$manifest = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json
$inContainer = '/tmp/vitalsync-restore'

function Restore-Databases {
    param([Parameter(Mandatory)] [string] $Container)

    Invoke-Docker -Arguments @('exec', $Container, 'rm', '-rf', $inContainer) | Out-Null
    Invoke-Docker -Arguments @('cp', $Backup, "${Container}:$inContainer") | Out-Null

    foreach ($database in $manifest.databases) {
        Write-Host "Restoring $($database.name)"
        $name = $database.name
        Invoke-InPostgres -Container $Container -Command "dropdb -U postgres --if-exists --force '$name'" | Out-Null
        Invoke-InPostgres -Container $Container -Command "createdb -U postgres '$name'" | Out-Null
        Invoke-InPostgres -Container $Container -Command "pg_restore -U postgres --exit-on-error --dbname '$name' '$inContainer/$name.dump'" | Out-Null
    }

    Invoke-Docker -Arguments @('exec', $Container, 'rm', '-rf', $inContainer) -AllowFailure | Out-Null
}

function Assert-Restored {
    param([Parameter(Mandatory)] [string] $Container)

    $present = Get-ApplicationDatabases -Container $Container
    $failures = @()
    foreach ($database in $manifest.databases) {
        if ($present -notcontains $database.name) {
            $failures += "$($database.name) is missing"
            continue
        }

        $tables = Get-TableCount -Container $Container -Database $database.name
        if ($tables -ne $database.tables) {
            $failures += "$($database.name) has $tables tables instead of $($database.tables)"
        }
        else {
            Write-Host "  $($database.name): present, $tables tables as in the backup"
        }
    }

    if ($failures.Count -gt 0) {
        throw "The restore does not match the backup: $($failures -join '; ')."
    }
}

if ($PSCmdlet.ParameterSetName -eq 'Verify') {
    $container = 'vitalsync-restore-check-' + [guid]::NewGuid().ToString('N').Substring(0, 8)
    $password = [guid]::NewGuid().ToString('N')

    Write-Host "Checking $Backup in a throwaway container; the running system is not touched"
    Invoke-Docker -Arguments @('run', '-d', '--name', $container, '-e', "POSTGRES_PASSWORD=$password", $Image) | Out-Null
    try {
        $deadline = (Get-Date).AddSeconds(90)
        while ((Invoke-Docker -Arguments @('exec', $container, 'pg_isready', '-h', '127.0.0.1', '-U', 'postgres') -AllowFailure).ExitCode -ne 0) {
            if ((Get-Date) -gt $deadline) {
                throw 'The throwaway PostgreSQL did not become ready within 90 seconds.'
            }
            Start-Sleep -Seconds 1
        }

        Restore-Databases -Container $container
        Assert-Restored -Container $container
        Write-Host "The backup $Backup restores completely."
    }
    finally {
        Invoke-Docker -Arguments @('rm', '-f', '-v', $container) -AllowFailure | Out-Null
    }

    return
}

$Project = Resolve-ComposeProject -Project $Project
$postgres = Get-ServiceContainer -Project $Project -Service 'postgres'
$names = ($manifest.databases | ForEach-Object { $_.name }) -join ', '

if (-not $PSCmdlet.ShouldProcess("$names in '$Project'", "Replace with the backup $Backup")) {
    return
}

$services = @((Invoke-Docker -Arguments @('compose', '-p', $Project, 'ps', '--services', '--status', 'running')).Output -split "`r?`n" |
        Where-Object { $_ -and $_ -notin @('postgres', 'messaging', 'vitalsync-dashboard') })

if ($services.Count -gt 0) {
    Write-Host "Stopping $($services -join ', ')"
    Invoke-Docker -Arguments (@('compose', '-p', $Project, 'stop') + $services) | Out-Null
}

try {
    Restore-Databases -Container $postgres
    Assert-Restored -Container $postgres
}
finally {
    if ($services.Count -gt 0) {
        Write-Host "Starting $($services -join ', ')"
        Invoke-Docker -Arguments (@('compose', '-p', $Project, 'start') + $services) | Out-Null
    }
}

Write-Host "Restored $names in '$Project' from $Backup"
