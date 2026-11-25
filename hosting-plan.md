# Hosting — what is left to do

**This file is throwaway.** It only lists the work that is still open. How the system is hosted
today is in [docs/hosting.md](docs/hosting.md), the stages ahead in
[docs/hosting-roadmap.md](docs/hosting-roadmap.md), and how to operate it in
[tools/hosting/README.md](tools/hosting/README.md). Anything found here that stays true afterwards
belongs there. Delete this file once Stage 3 is reached.

## Stage 1 — private, through Tailscale

Everything is in place and checked, except:

1. **Prepare the gRPC endpoints for plain HTTP** — gRPC without TLS needs an endpoint that speaks
   HTTP/2 only. Do this with the first gRPC service, not before.
2. **Confirm the broker needs no backup** — once real messages flow, check that the outbox covers
   what the broker's volume holds.

## Independent of the stages — moving the repository to `gaweCodes`

At some point the repository moves from `GabrielInnomat` to the owner's private account `gaweCodes`.
Its name stays `VitalSync`, so the runner's checkout path, the Compose project and the kept
parameters stay as they are.

1. **Transfer it** — under **Settings → Danger Zone → Transfer**, to `gaweCodes`, keeping the name
   `VitalSync`.
2. **Check the runner** — under **Actions → Runners** of `gaweCodes/VitalSync`. If it is not there,
   register it again as described under "The pipeline" in `tools/hosting/README.md`, with the new
   URL, the label `vitalsync`, the work folder `_work`, and as a service under the owner's account.
3. **Check the settings** — the approval for fork pull request workflows, only actions created by
   GitHub, the workflow permissions at reading the contents, and the environment `production`
   restricted to `main`.
4. **Replace the URLs** — `GabrielInnomat` with `gaweCodes` in `README.md`,
   `tools/hosting/README.md` and the gallery pages of the design system, and
   `git remote set-url origin https://github.com/gaweCodes/VitalSync.git` in the development
   checkout. Not before the transfer: GitHub redirects only from the old address to the new one.
5. **Deploy once** — start the pipeline by hand. Both jobs pass, and `docker compose ls` still
   shows exactly `aspire-vitalsync-ba398fcb`.

## Stage 2 — public, through Cloudflare Tunnel and Cloudflare Access

Starts as soon as the first service is complete and tested. The design is in
[docs/hosting-roadmap.md](docs/hosting-roadmap.md#stage-2--public-through-cloudflare-tunnel-and-cloudflare-access).

### In the repository

1. **Add the tunnel connector as a container** to the AppHost, present only when publishing, with
   its token as a secret parameter.
2. **Trust the connector as a proxy** in the web frontend's forwarded-headers handling, and accept
   the public host name.
3. **Make sure nothing else is routed** — no tunnel route for the health endpoints, pgAdmin or the
   RabbitMQ management interface — and cover the published settings with tests.
4. **Decide the points revisited in Stage 2** — restarts, the dashboard's telemetry key, a fixed
   dashboard token, the allowed host names.

### At Cloudflare and the registrar

1. **Buy the domain** and switch its name servers to Cloudflare.
2. **Create the tunnel** and the two public host names, routed to the containers by their Compose
   service names.
3. **Create the two Access applications**, each admitting only the approved email addresses.
4. **Check WebSockets and caching** for the application's host name.

### Afterwards

Check the "Done when" list of Stage 2 in the roadmap, and write any deviation back into the docs.

## Stage 3 — VitalSync's own login

1. **Make the authentication decision** and record it in `docs/` before any code.
2. **Authenticate in the BFF and the web frontend.**
3. **Carry the user's identity into the services** with every gRPC call.
4. **Scope data to its user** in every context that stores personal data, with tests proving that one
   user cannot read another's.
5. **Remove Access from the application's host name** and keep it on the monitoring host name.
6. **Evaluate the hosting again**, monitoring included.
