# Hosting roadmap

This document records how VitalSync's hosting is meant to grow. How it is hosted today is described
in [Hosting](./hosting.md); this document describes the stages after it, what triggers each one, and
what each changes.

## The stages

**VitalSync starts at zero running cost and grows in three stages.** The project is at its
beginning, so hosting is kept free until the system has something worth exposing. Each stage is
entered only when its trigger is met, and each one keeps everything the previous stage established.

- **Stage 1 — private, through Tailscale.** Free. This is the current state, described in
  [Hosting](./hosting.md).
- **Stage 2 — public, through Cloudflare Tunnel behind Cloudflare Access.** The only cost is the
  project's own domain.
- **Stage 3 — VitalSync's own login.** The target state.

Throughout all three stages the system runs on the owner's own computer, and Tailscale stays in
place as the owner's private way into it.

## Stage 2 — public, through Cloudflare Tunnel and Cloudflare Access

**Trigger** — the first service is complete and tested.

**VitalSync becomes reachable from the internet, but only for people the owner lets in.** The
computer opens an outbound tunnel to Cloudflare, so the home router still exposes no port and the
home address stays hidden. Cloudflare Access sits in front of every public address and admits only
the email addresses the owner has approved.

- **The domain** — public reachability and Cloudflare Access require a domain managed by
  Cloudflare. Without one, Cloudflare offers only temporary addresses that change on every restart
  and cannot be protected by Access.
- **Two public addresses** — one for the application and one for the monitoring, each a subdomain
  of that domain with an Access application of its own. Any further application gets its own
  subdomain, routed through the same tunnel.
- **The tunnel connector** — runs as a container of the hosted system, next to the others. It
  reaches the web frontend and the dashboard over the container network, so neither needs another
  published port. Its token is a secret of the hosted system like the passwords.
- **Nothing else is routed** — pgAdmin, the RabbitMQ management interface and the health endpoints
  get no route through the tunnel.
- **The proxy in front** — the web frontend trusts the connector's forwarded headers, as it trusts
  Tailscale Serve's today, and accepts the public host name.
- **Blazor through the tunnel** — the interactive frontend holds a WebSocket connection, so the
  tunnel must pass WebSockets through and no caching rule may touch the application's responses.
- **Login** — VitalSync has no login of its own yet. Access stands in for it: it decides who may
  reach the application at all, but it cannot tell users apart inside it.
- **Cost** — the domain, nothing else.
- **Privacy** — Cloudflare terminates TLS, so traffic is readable on Cloudflare's side. This is
  acceptable while access is limited to approved people, and it is one of the questions the
  re-evaluation after Stage 3 has to answer.

### Revisited in Stage 2

Stage 1 accepts some limits because the owner is the only user. Once other people depend on the
system, each is decided again:

- **Restarts** — the system is offline after a Windows restart until the owner signs in.
- **The dashboard's telemetry endpoint** — it accepts telemetry without a key. It is still not
  published, but whether it should require an API key is decided here.
- **The dashboard's login** — its browser token changes whenever the container starts. A fixed
  token, kept as a secret of the hosted system, would make it a stable credential.
- **Allowed host names** — the web frontend accepts any host name today, since the loopback binding
  keeps every other caller out. With a public host name, narrowing this to the known names is
  decided again.

### Done when

- the application and the dashboard open from outside the Tailscale network only after the Access
  login;
- an email address that is not approved is turned away by Access, before VitalSync sees the request;
- the Blazor circuit stays connected through the tunnel;
- pgAdmin, the RabbitMQ management interface and the health endpoints are still reachable only
  through Tailscale.

## Stage 3 — VitalSync's own login

**Trigger** — either of these holds:

- someone other than the owner is to use VitalSync with their own real data;
- VitalSync has to keep one user's data apart from another's, which Access cannot do.

**VitalSync identifies its users itself.** Authentication runs through the
[BFF](./architecture.md#logical-tiers), the single entry point of the frontend, and every request
inside the system knows whose data it concerns.

- **The decision first** — which identity provider, which protocol and how users register is not
  yet decided. It is an architectural decision of its own and is recorded in
  [Architecture](./architecture.md) and [Technologies](./technologies.md) before any code is
  written.
- **Identity in every call** — the user's identity travels with every call from the BFF to the
  services, and every context that stores personal data scopes it to its user.
- **Access moves** — Cloudflare Access is not removed. It moves from guarding the application to
  guarding only the monitoring.

## Who can reach what in each stage

The rule from [Hosting](./hosting.md#who-can-reach-what) — the more a surface can do, the smaller
the circle that can reach it — applies in every stage:

- **The application** — private in Stage 1, behind Access in Stage 2, behind VitalSync's own login
  in Stage 3.
- **Administration** — pgAdmin and the RabbitMQ management interface are never public. They stay
  reachable only through Tailscale, each with a login of its own, in every stage.
- **Monitoring** — through Tailscale in Stage 1. From Stage 2 on also through its own subdomain
  behind Access, so it can be checked from a device outside the Tailscale network. It keeps its
  own login as well, so reaching it from outside takes two independent steps.
- **Health endpoints** — on the internal management port in every stage, never routed through
  Cloudflare.

## After Stage 3

**Hosting is evaluated again once Stage 3 is reached.** Running on the owner's own computer is the
starting point, not a commitment. One conceivable direction is to run production in the cloud — on
Azure, for example — while the Cloudflare setup on the own computer becomes the test environment.

That evaluation also covers monitoring, since a cloud platform brings its own tooling. Durable
telemetry is decided there, under the rule that telemetry is never sent to a third-party telemetry
service, because logs can carry personal health data.
