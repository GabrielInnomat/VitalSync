# Operating the hosted system

How to set up, deploy, reach and maintain the hosted system described in
[Hosting](../../docs/hosting.md). Where hosting goes next is in the
[Hosting roadmap](../../docs/hosting-roadmap.md).

The scripts in this folder run in Windows PowerShell 5.1 and in PowerShell 7, need only Docker, and
find the Compose project that `aspire deploy` created on their own. If there is more than one, pass
`-Project`. Every command below runs from the root of a checkout. `aspire deploy` runs only in the
runner's checkout, as described under [First deployment](#first-deployment).

## Setting up a computer

### Windows and Docker

- **Docker Desktop** is installed and starts when the owner signs in to Windows.
- **The computer does not sleep.**
- **Windows Update active hours** are set, so Windows asks before restarting instead of restarting
  on its own. Automatic sign-in stays off; after a restart the system comes back once the owner
  signs in.

### Tools

- **The .NET SDK** pinned in `global.json`.
- **The Aspire CLI** at the Aspire version in `Directory.Packages.props`.
- **Git** and a checkout of the repository.
- **PowerShell scripts allowed** for the owner's account. Windows blocks them by default, and both
  the scripts in this folder and every step of the deployment job are PowerShell scripts.

```powershell
Set-ExecutionPolicy -Scope CurrentUser -ExecutionPolicy RemoteSigned
```

### Tailscale

1. Create a Tailscale account, signing in with an existing account of an identity provider. The
   account owns the whole tailnet, so it is one that is kept long-term and protected with a second
   factor.
2. Install Tailscale on the computer from tailscale.com and sign in. Check with `tailscale status`.
3. In the admin console:
   - **Name the computer** under Machines. The name becomes the address, and with the first
     certificate it appears in the public Certificate Transparency logs, so it is chosen before the
     first Serve mapping is created.
   - **Disable key expiry** for the computer, so it does not drop out of the tailnet after a few
     months.
   - **Enable MagicDNS and HTTPS certificates** under DNS.
4. In the Tailscale menu on the computer, under Preferences, turn on **Run unattended**, so the
   computer stays in the tailnet while nobody is signed in.
5. Install Tailscale on every device that is to reach the system and sign in with the same account.

### The pipeline

The pipeline in `.github/workflows/deploy.yml` first builds and tests on GitHub's own machines, then
runs its deployment job on a GitHub Actions runner on this computer. What it does and when is
described in [Hosting](../../docs/hosting.md#how-a-change-reaches-it).

1. In the repository's settings on GitHub, before the runner exists — every step below closes a way
   for someone else's code to reach this computer:
   - **Actions → General** — under the approval for fork pull request workflows, choose
     **Require approval for all external contributors**. A pull request can change a workflow to
     run on this runner, so it must never run without approval.
   - **Actions → General** — allow only actions created by GitHub, and keep the workflow
     permissions at reading the repository contents.
   - **Environments** — create the environment `production`. Under deployment branches choose
     **Selected branches and tags** and add `main`. Create it before the first run; a run that
     names a missing environment creates it without that restriction.
2. Under **Actions → Runners → New self-hosted runner**, choose Windows x64 and follow the download
   steps shown there into `C:\actions-runner`. Run them in a PowerShell window in which `dotnet`,
   `aspire`, `docker` and `git` are found, because the runner keeps that window's `PATH`.
3. Configure the runner with the token shown on that page, then answer its questions:

```powershell
.\config.cmd --url https://github.com/GabrielInnomat/VitalSync --token <token>
```

   - **Additional labels** — `vitalsync`. The pipeline runs only on a runner with this label.
   - **Work folder** — keep `_work`. The runner checks the repository out into
     `C:\actions-runner\_work\VitalSync\VitalSync`, and the deployment depends on that path.
   - **Run as service** — yes, under the owner's Windows account and its password, not the proposed
     `NETWORK SERVICE`. Docker Desktop and the kept parameters belong to that account.
4. Check that the runner appears as **Idle** under **Actions → Runners**, and that its service runs:

```powershell
Get-Service actions.runner.*
```

## First deployment

The first deployment on a computer runs by hand, because it asks for the parameters; every later one
runs in the pipeline. It runs in the runner's checkout, since the Compose project's name and the
kept parameters depend on the checkout's path.

1. Clone the repository into the runner's checkout. The runner reuses this clone for its jobs. If
   the runner has already run a job, the folder exists and only the `Set-Location` is needed.

```powershell
git clone https://github.com/GabrielInnomat/VitalSync.git C:\actions-runner\_work\VitalSync\VitalSync
```

```powershell
Set-Location C:\actions-runner\_work\VitalSync\VitalSync
```

2. Create the volumes. The script creates the missing ones and leaves existing ones alone, so it is
   safe to run before every deployment.

```powershell
.\tools\hosting\New-VitalSyncVolumes.ps1
```

3. Deploy. On the first run `aspire deploy` asks for the parameters of the hosted system and keeps
   them for every later run.

```powershell
aspire deploy
```

The parameters:

- **`hosted-postgres-password`** — the password of the PostgreSQL user `postgres`.
- **`hosted-messaging-password`** — the password of the RabbitMQ user `guest`.
- **`pgadmin-email`** and **`pgadmin-password`** — the login of pgAdmin.

Keep all four in a password manager as well. `aspire deploy` stores them in plain text in
`src/Aspire/VitalSync.AppHost/aspire-output/.env.Production`, which Git ignores, and below
`%USERPROFILE%\.aspire\deployments\`.

4. Check that the containers run. The three migration workers have exited, which is expected; every
   other container is `Up`. The Compose project defines no health checks, so Docker shows no health
   state.

```powershell
docker ps --format "table {{.Names}}\t{{.Status}}"
```

5. Check the health endpoints from inside the container network, since the management port is not
   published. The network is the one ending in `_aspire`.

```powershell
docker network ls --filter name=aspire-vitalsync
```

```powershell
foreach ($service in 'web-frontend', 'backend-for-frontend', 'nutrition-service', 'fitness-service', 'health-analytics-service') { docker run --rm --network <network> curlimages/curl:8.17.0 -sS "http://${service}:8081/health" }
```

Each answers `Healthy`.

6. Start the pipeline once by hand and check that both of its jobs pass: `ci` on GitHub's machines,
   then `deploy` on this computer. From then on every push to `main` that changes more than Markdown
   deploys on its own.

```powershell
gh workflow run deploy.yml --ref main
```

## Tailscale Serve

Serve maps the computer's tailnet address to the loopback ports. Its mappings belong to the
computer and survive restarts.

```powershell
tailscale serve --bg --https=443 127.0.0.1:8080
```

```powershell
tailscale serve --bg --https=8443 127.0.0.1:18888
```

```powershell
tailscale serve --bg --https=8444 127.0.0.1:5050
```

```powershell
tailscale serve --bg --https=8445 127.0.0.1:15672
```

`tailscale serve status` lists the mappings with their full addresses,
`tailscale serve --https=<port> off` removes one and `tailscale serve reset` removes all of them.
Only `serve` is used, never `funnel`, which would publish the system to the internet.

## Addresses

`<machine>.<tailnet>.ts.net` is the computer's name in the tailnet, as `tailscale serve status`
shows it.

- **Application** — `https://<machine>.<tailnet>.ts.net`, forwarded to `127.0.0.1:8080`.
- **Dashboard** — `https://<machine>.<tailnet>.ts.net:8443`, forwarded to `127.0.0.1:18888`.
- **pgAdmin** — `https://<machine>.<tailnet>.ts.net:8444`, forwarded to `127.0.0.1:5050`.
- **RabbitMQ management** — `https://<machine>.<tailnet>.ts.net:8445`, forwarded to
  `127.0.0.1:15672`.

On the computer itself the loopback addresses work directly, over plain HTTP.

### Signing in

- **Dashboard** — it asks for a browser token, which changes whenever its container starts. Find
  the container, then read the login link from its log:

```powershell
docker ps --filter name=dashboard --format "{{.Names}}"
```

```powershell
docker logs <container> 2>&1 | Select-String "login\?t="
```

  The link in the log names the container's own address. Keep only the part from `/login?t=` on and
  put it behind the dashboard's tailnet address, with `https`. Alternatively, paste the token into
  the field on the login page. The sign-in lasts three days.

- **pgAdmin** — sign in with `pgadmin-email` and `pgadmin-password`. After the first deployment,
  register the server once: host `postgres`, port `5432`, user `postgres`, password
  `hosted-postgres-password`. pgAdmin keeps the registration on its volume.
- **RabbitMQ management** — user `guest`, password `hosted-messaging-password`.

## Checking access

After setting up or changing anything around Tailscale:

- **From outside** — on a phone with Wi-Fi off and Tailscale on, all four addresses open. In the
  application, switching between a few pages shows no reconnection notice.
- **From the home network** — on a phone with Tailscale off, `http://<computer's LAN address>:8080`
  does not load, nor do the other ports.
- **After a restart** — once the owner has signed in, every container, every Serve mapping and the
  pgAdmin registration are back without further steps. The dashboard has a new token and an empty
  history.

## Redeploying

Every push to `main` that changes more than Markdown is deployed by the pipeline. The pipeline takes
no backup first; when a change is risky, take one by hand before pushing.

To deploy without a push, start the pipeline by hand, from the **Actions** tab or with:

```powershell
gh workflow run deploy.yml --ref main
```

`aspire deploy` never runs in a development checkout: from another path it would start a second
Compose project next to the first, on the same ports. If the pipeline cannot run, deploy by hand in
the runner's checkout instead, with the runner's service stopped meanwhile.

```powershell
Set-Location C:\actions-runner\_work\VitalSync\VitalSync
```

```powershell
git pull
```

```powershell
.\tools\hosting\New-VitalSyncVolumes.ps1
```

```powershell
aspire deploy
```

Afterwards check that the containers run and the application opens. The dashboard has a new token.

## Stopping and starting

`docker compose ls` lists the Compose project. A stopped project stays stopped across restarts.

```powershell
docker compose -p <project> stop
```

```powershell
docker compose -p <project> start
```

## Backup

`Backup-VitalSync.ps1` dumps every application database of the running PostgreSQL server into a new
folder `vitalsync-<timestamp>` below the destination: one custom-format dump per database, the
server's roles in `globals.sql`, and a `manifest.json` that lists the databases and their table
counts. Each dump is read back once before it is kept. The folder only receives its final name once
everything is complete, so an interrupted run never looks like a backup. The newest `-Keep`
backups stay, older ones are removed.

```powershell
.\tools\hosting\Backup-VitalSync.ps1 -Destination "$env:USERPROFILE\Documents\Backups\VitalSync" -Keep 14
```

## Restore

`Restore-VitalSync.ps1` takes the newest backup below the destination, or the one named with
`-Backup`.

Without further switches it only checks the backup. It restores into a throwaway PostgreSQL
container, checks that every role of the backup exists, compares every database with the manifest
and removes the container again. The running system is not touched, so this can run as often as
wanted.

Before it touches anything, it checks every database name in the manifest and stops if one holds
anything but lowercase letters, digits and hyphens. It then creates the roles from `globals.sql`
that the server does not have yet, before any database, so that every object finds its owner. Roles
the server already has, `postgres` always among them, stay exactly as they are: a restore never
replaces a password the running system uses, including the new ones chosen on another computer.

```powershell
.\tools\hosting\Restore-VitalSync.ps1 -Destination "$env:USERPROFILE\Documents\Backups\VitalSync"
```

With `-IntoRunningSystem` it replaces the databases of the running system. It asks for
confirmation first, stops every service that uses the databases, creates the missing roles,
recreates each database from the backup, checks it against the manifest and starts the services
again. Everything written since the backup is lost.

```powershell
.\tools\hosting\Restore-VitalSync.ps1 -Destination "$env:USERPROFILE\Documents\Backups\VitalSync" -IntoRunningSystem
```

## Moving to another computer

The system is not tied to one computer. Moving it means preparing the new one, deploying there and
restoring the newest backup into it.

1. Stop the runner's service on the old computer, so that from now on the pipeline runs only on the
   new one.

```powershell
Stop-Service actions.runner.*
```

2. Set up the new computer as described under [Setting up a computer](#setting-up-a-computer),
   signed in to the same tailnet and with a runner of its own.
3. Deploy as described under [First deployment](#first-deployment). New passwords may be chosen,
   since the old ones are kept only on the old computer. The system starts with empty databases.
4. Stop the old system, then take a last backup there. Nothing written afterwards is carried over.
5. Copy the backup to the new computer, check it with `Restore-VitalSync.ps1`, then restore it with
   `-IntoRunningSystem`.
6. Remove the old computer from the tailnet, give the new one the old name so the addresses stay
   the same, and create the [Serve mappings](#tailscale-serve) again.
7. [Check access](#checking-access) from outside and from the home network.
8. Move the scheduled backup to the new computer.
9. Only then remove the Compose project and the volumes on the old computer, and remove its runner
   with the removal command shown under **Actions → Runners**.

Only the databases are carried over. What is left behind is either recreated or not worth keeping:

- **Data Protection keys** — the web frontend creates new ones, so open browser sessions have to
  reload.
- **Broker** — messages not yet delivered are lost. After a clean stop the queues are normally
  empty.
- **pgAdmin** — the server `postgres` is registered again.

## Troubleshooting

- **`aspire deploy` stops because an external volume is missing** — run
  `New-VitalSyncVolumes.ps1`, then deploy again.
- **Nothing answers after a restart** — Docker starts only once the owner has signed in to Windows.
- **The dashboard rejects the token** — the container has restarted since the token was read; read
  it from the log again.
- **The browser warns that a dashboard address is not secure, or Tailscale answers 400** — the
  address uses `http`. Every tailnet address uses `https`.
- **The dashboard warns that its endpoint is unsecured** — this concerns the telemetry endpoint,
  which accepts telemetry without a key. It is not published and is reachable only inside the
  container network.
- **The deployment job waits for a runner** — the computer is off, nobody is signed in, or the runner's
  service is stopped. `Get-Service actions.runner.*` shows the service; the run starts once it is
  back.
- **A pipeline step fails because running scripts is disabled** — PowerShell scripts are not allowed
  for the owner's account; see [Tools](#tools).
- **The pipeline does not find `dotnet`, `aspire` or `docker`** — the runner keeps the `PATH` from
  when it was configured. Add the missing folder to the `.path` file in `C:\actions-runner`, then
  restart the service.
- **The runner's service does not start after a password change** — the service still has the old
  password. Enter the new one in the service's properties under **Log On**.
- **The deployment fails because a port is already allocated** — a second Compose project runs next
  to the hosted one, usually from `aspire deploy` in another checkout. `docker compose ls` shows it;
  remove it with `docker compose -p <project> down`. The volumes are external and stay.
