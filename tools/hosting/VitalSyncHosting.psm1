Set-StrictMode -Version Latest

function Invoke-Docker {
    param([Parameter(Mandatory)] [string[]] $Arguments, [switch] $AllowFailure)

    $previous = $ErrorActionPreference
    $ErrorActionPreference = 'Continue'
    try {
        $output = & docker @Arguments 2>&1
        $exitCode = $LASTEXITCODE
    }
    finally {
        $ErrorActionPreference = $previous
    }

    $text = ($output | ForEach-Object { "$_" }) -join [Environment]::NewLine
    if ($exitCode -ne 0 -and -not $AllowFailure) {
        throw "docker $($Arguments[0..1] -join ' ') failed with exit code ${exitCode}:$([Environment]::NewLine)$text"
    }

    [pscustomobject]@{ ExitCode = $exitCode; Output = $text }
}

function Resolve-ComposeProject {
    param([string] $Project)

    if ($Project) {
        return $Project
    }

    $names = @((Invoke-Docker -Arguments @('compose', 'ls', '--all', '--format', 'json')).Output |
            ConvertFrom-Json |
            ForEach-Object { $_.Name } |
            Where-Object { $_ -like 'aspire-vitalsync-*' })

    if ($names.Count -ne 1) {
        throw "Expected exactly one VitalSync Compose project, found $($names.Count): $($names -join ', '). Pass -Project explicitly."
    }

    $names[0]
}

function Get-ServiceContainer {
    param([Parameter(Mandatory)] [string] $Project, [Parameter(Mandatory)] [string] $Service)

    $id = (Invoke-Docker -Arguments @('compose', '-p', $Project, 'ps', '-q', $Service)).Output.Trim()
    if (-not $id) {
        throw "The Compose project '$Project' has no running '$Service' container."
    }

    $id
}

function Invoke-InPostgres {
    param([Parameter(Mandatory)] [string] $Container, [Parameter(Mandatory)] [string] $Command)

    if ($Command.Contains('"')) {
        throw 'Commands run inside PostgreSQL must not contain double quotes; Windows PowerShell does not pass them through to docker intact.'
    }

    (Invoke-Docker -Arguments @('exec', $Container, 'sh', '-c', ('PGPASSWORD=$POSTGRES_PASSWORD ' + $Command))).Output
}

function Get-ApplicationDatabases {
    param([Parameter(Mandatory)] [string] $Container)

    $query = 'psql -U postgres -At -c ''SELECT datname FROM pg_database WHERE NOT datistemplate AND datname <> $$postgres$$ ORDER BY datname'''
    @((Invoke-InPostgres -Container $Container -Command $query) -split "`r?`n" | Where-Object { $_ })
}

function Get-TableCount {
    param([Parameter(Mandatory)] [string] $Container, [Parameter(Mandatory)] [string] $Database)

    $query = "psql -U postgres -At --dbname '$Database' -c " +
        '''SELECT count(*) FROM pg_tables WHERE schemaname NOT IN ($$pg_catalog$$, $$information_schema$$)'''
    [int](Invoke-InPostgres -Container $Container -Command $query).Trim()
}

function Get-LatestBackup {
    param([Parameter(Mandatory)] [string] $Destination)

    $latest = Get-ChildItem -LiteralPath $Destination -Directory |
        Where-Object { $_.Name -match '^vitalsync-\d{8}-\d{6}$' } |
        Sort-Object Name -Descending |
        Select-Object -First 1

    if (-not $latest) {
        throw "No backup was found in '$Destination'."
    }

    $latest.FullName
}

Export-ModuleMember -Function Invoke-Docker, Resolve-ComposeProject, Get-ServiceContainer, Invoke-InPostgres, Get-ApplicationDatabases, Get-TableCount, Get-LatestBackup
