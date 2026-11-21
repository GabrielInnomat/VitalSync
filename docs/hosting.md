# Hosting

This document records how VitalSync is hosted and how that hosting is meant to grow. It is a
decision and a roadmap, not an operations manual: it states where the system runs, who can reach
it, and when the next step is taken.

How the system is composed at runtime is described in [Architecture](./architecture.md#runtime-composition).

## The decision

**VitalSync starts at zero running cost and grows in three stages.** The project is at its
beginning, so hosting is kept free until the system has something worth exposing. Each stage is
entered only when its trigger is met, and each one keeps everything the previous stage established.

- **Stage 1 — private, through Tailscale.** Free.
- **Stage 2 — public, through Cloudflare Tunnel behind Cloudflare Access.** The only cost is the
  project's own domain.
- **Stage 3 — VitalSync's own login.** The target state.

Throughout all three stages the system runs on the owner's own computer, as containers rather than
in the Aspire development mode, and comes back on its own once the owner signs in after a restart.
Until then it is offline. While the owner is the only user that is accepted; it is revisited in
Stage 2, when other people depend on the system.

## Stage 1 — private, through Tailscale

**VitalSync is reachable only from the owner's own devices, at home and on the road.** The computer
joins a private Tailscale network, and only devices in that network can reach it. Nothing is
published to the internet, and no port is opened on the home router.

- **Address** — the computer has a stable name inside the private network, so the address does not
  change between restarts. A fixed address is therefore not the reason for moving to Stage 2.
- **Login** — none is needed. Only the owner's devices get in, so the network itself is the access
  control.
- **Cost** — none.

**Moving on** — Stage 2 begins as soon as the first service is complete and tested.

## Stage 2 — public, through Cloudflare Tunnel and Cloudflare Access

**VitalSync becomes reachable from the internet, but only for people the owner lets in.** The
computer opens an outbound tunnel to Cloudflare, so the home router still exposes no port and the
home address stays hidden. Cloudflare Access sits in front of every public address and admits only
the email addresses the owner has approved.

- **Why the own domain** — public reachability and Cloudflare Access require a domain managed by
  Cloudflare. Without one, Cloudflare offers only temporary addresses that change on every restart
  and cannot be protected by Access.
- **Login** — VitalSync has no login of its own yet. Access stands in for it: it decides who may
  reach the application at all, but it cannot tell users apart inside it.
- **Further applications** — every application gets its own subdomain of the same domain, routed
  through the same tunnel.
- **Cost** — the domain, nothing else.
- **Privacy** — Cloudflare terminates TLS, so traffic is readable on Cloudflare's side. This is
  acceptable while access is limited to approved people, and it is one of the questions the
  re-evaluation after Stage 3 has to answer.

**Moving on** — Stage 3 begins as soon as either of these holds:

- someone other than the owner is to use VitalSync with their own real data;
- VitalSync has to keep one user's data apart from another's, which Access cannot do.

## Stage 3 — VitalSync's own login

**VitalSync identifies its users itself.** Authentication runs through the
[BFF](./architecture.md#logical-tiers), the single entry point of the frontend, and every request
inside the system knows whose data it concerns.

How authentication works — which identity provider, which protocol, how users register — is not yet
decided. It is an architectural decision of its own and will be recorded when it is made.

Cloudflare Access is not removed in this stage. It moves from guarding the application to guarding
the monitoring, as described under [Who can reach what](#who-can-reach-what).

## Who can reach what

**The more a surface can do, the smaller the circle that can reach it.**

- **The application** — the Blazor frontend is the only part of VitalSync that is ever public. It is
  private in Stage 1, behind Access in Stage 2, and behind VitalSync's own login in Stage 3.
- **Administration** — pgAdmin and the RabbitMQ management interface can change and delete data.
  They are never public and are reachable only through Tailscale, in every stage. Each asks for a
  login of its own on top of that.
- **Monitoring** — monitoring only reads. It is reachable through Tailscale in Stage 1, and from
  Stage 2 on also through its own subdomain behind Access, so it can be checked from a device that
  is not part of the private network.
- **Health endpoints** — the health, liveness and dead-letter endpoints stay on the internal
  management port. They are never routed through Cloudflare.

Tailscale stays in place in every stage as the owner's private way into the system.

## Monitoring

**The services already emit logs, traces and metrics through OpenTelemetry.** In development the
Aspire dashboard displays them. When hosted, the same dashboard runs as a standalone container and
receives that telemetry.

- **Protection** — from Stage 2 on the dashboard is reachable behind Access and additionally asks for
  its own login token, so reaching it takes two independent steps.
- **History** — the standalone dashboard keeps telemetry in memory only. A restart clears it. That
  is enough while the owner watches the system live. Durable storage is decided in the evaluation
  after Stage 3, and it stays self-hosted: logs can carry personal health data, so telemetry is not
  sent to a hosted service.
- **Outages** — the dashboard runs on the same computer as the system it watches, so it cannot report
  that the computer itself is down. There is deliberately no check from outside: it would bring in
  another provider for little gain while the owner is the main user.

## After Stage 3

**Hosting is evaluated again once Stage 3 is reached.** Running on the owner's own computer is the
starting point, not a commitment. One conceivable direction is to run production in the cloud — on
Azure, for example — while the Cloudflare setup on the own computer becomes the test environment.
That evaluation also covers monitoring, since a cloud platform brings its own tooling.
