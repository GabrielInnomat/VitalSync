# Hosting scripts

Operational scripts for the hosted system described in [Hosting](../../docs/hosting.md). They run in
Windows PowerShell 5.1 and in PowerShell 7, need only Docker, and find the Compose project that
`aspire deploy` created on their own. If there is more than one, pass `-Project`.

## Volumes

The hosted system keeps its data on external volumes with fixed names, which Compose does not
create on its own. `New-VitalSyncVolumes.ps1` creates the missing ones and leaves existing ones
alone, so it is safe to run before every deployment.

```powershell
.\tools\hosting\New-VitalSyncVolumes.ps1
```

## Backup

`Backup-VitalSync.ps1` dumps every application database of the running PostgreSQL server into a new
folder `vitalsync-<timestamp>` below the destination: one custom-format dump per database, the
server's roles in `globals.sql`, and a `manifest.json` that lists the databases and their table
counts. Each dump is read back once before it is kept. The folder only receives its final name once
everything is complete, so an interrupted run never looks like a backup. The newest `-Keep`
backups stay, older ones are removed.

```powershell
.\tools\hosting\Backup-VitalSync.ps1 -Destination D:\Backups\VitalSync -Keep 14
```

## Restore

`Restore-VitalSync.ps1` takes the newest backup below the destination, or the one named with
`-Backup`.

Without further switches it only checks the backup. It restores into a throwaway PostgreSQL
container, compares every database with the manifest and removes the container again. The running
system is not touched, so this can run as often as wanted.

```powershell
.\tools\hosting\Restore-VitalSync.ps1 -Destination D:\Backups\VitalSync
```

With `-IntoRunningSystem` it replaces the databases of the running system. It asks for
confirmation first, stops every service that uses the databases, recreates each database from the
backup, checks it against the manifest and starts the services again. Everything written since the
backup is lost.

```powershell
.\tools\hosting\Restore-VitalSync.ps1 -Destination D:\Backups\VitalSync -IntoRunningSystem
```
