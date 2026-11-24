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

## Tailscale Serve

Every published port is bound to the loopback address, so the system is reachable from other
devices only through Tailscale Serve. Serve terminates HTTPS with a certificate for the computer's
tailnet name and forwards to the loopback ports. Its mappings belong to the computer and survive
restarts.

The tailnet needs MagicDNS and HTTPS certificates enabled. The computer's name becomes part of the
address and, with the first certificate, of the public Certificate Transparency logs, so it is
chosen before the first mapping is created. Key expiry is disabled for the computer and Tailscale
runs unattended, so the computer stays in the tailnet without anyone signed in.

```powershell
tailscale serve --bg --https=443 127.0.0.1:8080
tailscale serve --bg --https=8443 127.0.0.1:18888
tailscale serve --bg --https=8444 127.0.0.1:5050
tailscale serve --bg --https=8445 127.0.0.1:15672
```

- **Frontend** — `https://<machine>.<tailnet>.ts.net`
- **Dashboard** — `https://<machine>.<tailnet>.ts.net:8443`, with the browser token from the
  dashboard container's log
- **pgAdmin** — `https://<machine>.<tailnet>.ts.net:8444`
- **RabbitMQ management** — `https://<machine>.<tailnet>.ts.net:8445`

`tailscale serve status` lists the mappings, `tailscale serve --https=<port> off` removes one and
`tailscale serve reset` removes all of them. Only `serve` is used, never `funnel`, which would
publish the system to the internet.

## Moving to another computer

The system is not tied to one computer. Moving it means preparing the new one, deploying there and
restoring the newest backup into it.

1. Prepare the new computer: Docker, the SDK from `global.json`, the Aspire CLI at the AppHost's
   Aspire version, a checkout of the repository, and Tailscale signed in to the same tailnet, with
   key expiry disabled and unattended mode on.
2. Create the volumes with `New-VitalSyncVolumes.ps1` and run `aspire deploy`. It asks for the
   passwords again, since they are kept only on the old computer, and new ones may be chosen. The
   system starts with empty databases.
3. Stop the old system, then take a last backup there. Nothing written afterwards is carried over.
4. Copy the backup to the new computer, check it with `Restore-VitalSync.ps1`, then restore it with
   `-IntoRunningSystem`.
5. Remove the old computer from the tailnet, give the new one the old name so the addresses stay
   the same, and create the Serve mappings again.
6. Check every address from a device outside the home network.
7. Move the scheduled backup to the new computer.
8. Only then remove the Compose project and the volumes on the old computer.

Only the databases are carried over. What is left behind is either recreated or not worth keeping:

- **Data Protection keys** — the web frontend creates new ones, so open browser sessions have to
  reload.
- **Broker** — messages not yet delivered are lost. After a clean stop the queues are normally
  empty.
- **pgAdmin** — the server `postgres` is registered again.
