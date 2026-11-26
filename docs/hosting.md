# Hosting

This document describes how VitalSync is hosted today: where it runs, what runs there, how the
owner reaches it and how its data is kept safe. It states what exists and why, not how to operate
it.

- **Where hosting goes next** — the later stages are described in the
  [Hosting roadmap](./hosting-roadmap.md).
- **How to operate it** — setting up a computer and its pipeline runner, deploying, the addresses,
  backups and moving to another computer are in the [operations guide](../tools/hosting/README.md).
- **How the system is composed** — see [Architecture](./architecture.md#runtime-composition), and
  [Technologies](./technologies.md#net-aspire) for how the hosted composition differs from
  development.

## Where it runs

**VitalSync runs on the owner's own, always-on Windows computer, at no running cost.** It is the
first of the stages described in the roadmap: private, reachable only through Tailscale.

- **As containers** — `aspire deploy` turns the same AppHost that runs the development system into a
  Docker Compose project and starts it. The images are built on the computer itself, so no
  container registry is involved.
- **After a restart** — every long-running container restarts on its own once Docker is running,
  and Docker starts when the owner signs in to Windows. Until then the system is offline. That is
  accepted while the owner is the only user. Windows asks before restarting for updates instead of
  restarting on its own, and it does not sign in automatically.
- **Not tied to the computer** — moving the system to another computer means deploying it there and
  restoring the newest backup. Nothing in the repository names the computer.

## How a change reaches it

**Every push to `main` that changes more than Markdown is built and tested on GitHub's own machines
and then deployed on the computer itself.** Only the deployment runs on the computer. A GitHub
Actions runner is installed there as a Windows service under the owner's account. GitHub announces
the deployment, the runner fetches the job and runs it locally. The runner only connects out to
GitHub, so this opens nothing on the computer.

- **What triggers it** — a push to `main` that changes at least one file that is not Markdown. A push
  that changes only `*.md` files, wherever they are, starts nothing. The pipeline can also be started
  by hand.
- **Tests first, on GitHub** — the same Release build and tests as for a pull request,
  on GitHub's own machines. The system is deployed only once all of them have passed; otherwise the
  running system stays as it is.
- **The deployment** — creates any volume a change has added, runs `aspire deploy` without asking
  anything, and then checks each hosted project's health endpoint from inside the container network.
  Nothing else runs on the computer: no tests, no test containers next to the hosted system.
- **One at a time** — a deployment is never cancelled midway. Pushes that arrive meanwhile wait, and
  of those only the newest is deployed.
- **From one checkout** — the Compose project's name and the kept parameters follow the path of the
  AppHost, as described in [Technologies](./technologies.md#net-aspire). The system is therefore
  deployed only from the runner's checkout, never from a development checkout. That path contains
  the repository's name but not its owner, so the repository can move to another account but is
  never renamed.
- **Only while signed in** — the runner needs Docker, which starts when the owner signs in. Until
  then the deployment waits, just as the system itself is offline. The build and tests run
  regardless.
- **Pull requests** — are built and tested on GitHub's own machines and are never deployed.

## What runs

- **The application** — the three services, the BFF and the web frontend. Each restarts unless it
  was stopped deliberately.
- **The migration workers** — one per bounded context. Each runs once per deployment, creates its
  context's two databases if they are missing, and exits.
- **The infrastructure** — PostgreSQL and RabbitMQ.
- **The tools** — pgAdmin and the Aspire dashboard, each as a container of its own.

Inside the container network the hosted projects speak plain HTTP. Each listens on an application
port and on a separate management port for its health endpoints. The management port is never
published, so the health endpoints answer only inside that network.

### Data

The data lives on four Docker volumes with fixed names, which survive tearing the Compose project
down and do not depend on the name `aspire deploy` gives the project:

- **`vitalsync-postgres-data`** — every database of every bounded context.
- **`vitalsync-messaging-data`** — the broker's queues.
- **`vitalsync-pgadmin-data`** — pgAdmin's settings and its server registration.
- **`vitalsync-web-keys`** — the web frontend's Data Protection keys, so browser sessions survive a
  restart of its container.

### Secrets

The hosted system has passwords of its own, separate from those of the development run. They are
chosen when deploying for the first time. `aspire deploy` keeps them on the computer, readable only
by the owner's Windows account, because every redeployment needs them, and the owner keeps them in
a password manager as well. They are never committed and never copied into a backup.

## How the owner reaches it

**Every published port is bound to the computer's loopback address, and Tailscale is the only way
in from another device.** The computer and the owner's devices form a private Tailscale network.
Tailscale Serve terminates HTTPS on the computer with a certificate for its name in that network and
forwards each address to a loopback port.

- **Nothing is public** — no port is opened on the home router, and Tailscale Funnel, which would
  publish an address to the internet, is not used.
- **At home as well** — a device in the home network that is not part of the Tailscale network
  cannot reach the system. On the computer itself the loopback ports work directly.
- **A stable address** — the address is the computer's name in the Tailscale network, which does not
  change between restarts.
- **A public name** — the certificate makes the computer's full name in the Tailscale network
  visible in the public Certificate Transparency logs, so that name is chosen with that in mind,
  before the first certificate is issued.

The application has the default HTTPS port; the dashboard, pgAdmin and the RabbitMQ management
interface each have a port of their own. The addresses are listed in the
[operations guide](../tools/hosting/README.md#addresses).

## Who can reach what

**The more a surface can do, the smaller the circle that can reach it.** This rule holds in every
stage; how each surface is reached in the later stages is described in the
[roadmap](./hosting-roadmap.md#who-can-reach-what-in-each-stage).

- **The application** — the Blazor frontend is the only part of VitalSync that is ever meant to be
  public. Today it is private: the Tailscale network is its access control, so it has no login.
- **Administration** — pgAdmin and the RabbitMQ management interface can change and delete data.
  They are reachable only through Tailscale and each asks for a login of its own on top.
- **Monitoring** — the dashboard only reads. It is reachable through Tailscale and asks for its
  login token.
- **Health endpoints** — the health, liveness and dead-letter endpoints stay on the internal
  management port.

## Backups

**The databases are backed up as dumps, outside Docker, and every backup is proven restorable
before it counts.**

- **What** — one dump per database, the server's roles, and a manifest listing every database and
  its table counts. Each dump is read back once before it is kept, and an interrupted run never
  leaves something that looks like a backup.
- **Where** — in a folder outside the Docker volumes, so losing a volume does not lose its backups.
  A fixed number of the newest backups is kept.
- **Checking** — a backup can be restored into a throwaway PostgreSQL container and compared with
  its manifest, without touching the running system.
- **Restoring** — a restore into the running system stops every service that uses the databases,
  recreates each database from the backup, checks it against the manifest and starts the services
  again.
- **The broker** — its volume is not backed up. Integration events leave a service through its
  outbox, which lives in that service's database, so the databases are what has to survive. This
  is to be confirmed once real messages flow.
- **Schedule** — backups are meant to run as a scheduled task on the computer.

## Monitoring

**The services emit logs, traces and metrics through OpenTelemetry, and the Aspire dashboard
displays them.** When hosted, the dashboard runs as a standalone container at the released version
that matches the AppHost's Aspire packages.

- **Login** — the dashboard asks for a browser token, which it creates anew whenever its container
  starts.
- **Telemetry endpoint** — the endpoint the services send their telemetry to accepts it without a
  key. It is not published and is reachable only inside the container network. The dashboard
  displays a warning about it.
- **History** — the dashboard keeps telemetry in memory only, so a restart clears it. That is enough
  while the owner watches the system live. Telemetry stays self-hosted: logs can carry personal
  health data, so it is never sent to a hosted service, and the dashboard's own usage telemetry is
  switched off.
- **Outages** — the dashboard runs on the computer it watches, so it cannot report that the computer
  itself is down. There is deliberately no check from outside: it would bring in another provider
  for little gain while the owner is the only user.

## Decisions taken

- **Deployment through `aspire deploy`** — Aspire's Docker Compose deployment is still labelled a
  preview. That is acceptable while the owner is the only user, and `aspire publish` followed by
  `docker compose up` remains a fallback that needs no change to the AppHost.
- **Plain HTTP inside the container network** — HTTPS ends at the entry point in front of the
  system, and the containers talk to each other only inside the container network.
- **Volumes with fixed names** — the reason is described in
  [Technologies](./technologies.md#net-aspire).
- **No container registry** — the images are built where they run.
- **A runner on the computer itself, for the deployment only** — GitHub's own machines could only
  deploy if the computer were reachable from the internet. A runner on the computer deploys without
  that, and it is free. Building and testing stay on GitHub's machines: the tests start containers
  of their own, which would otherwise compete with the hosted system for the computer's Docker and
  ports, and the build and tests keep running while the computer is off.
- **A public repository** — a pull request from a fork could otherwise change a workflow so that it
  runs on the computer. Workflows of outside contributors therefore wait for the owner's approval,
  and the `production` environment accepts deployments from `main` only. The pipeline's logs are
  public as well, so it never prints anything of the hosted system: no container logs and no
  parameters.
- **No external uptime check** and **no durable telemetry** yet — see [Monitoring](#monitoring).
  When durable storage becomes necessary, a self-hosted, OpenTelemetry-native stack is the
  candidate, for example the `grafana/otel-lgtm` container.
