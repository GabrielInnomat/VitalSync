# 0002. Use .NET Aspire 13 for orchestration

- **Status:** Accepted
- **Date:** 2026-06-23

## Context

VitalSync is a cloud-native distributed application composed of a Blazor frontend, a BFF, and multiple microservices with messaging infrastructure. We need a way to compose, run, and observe these components consistently during development, and to streamline service discovery and configuration.

## Decision

We will use **.NET Aspire 13** as the application orchestrator. Aspire is applied at the orchestration/application layer (AppHost and service defaults). The reusable **Building Blocks remain framework-agnostic** and do not depend on Aspire.

## Consequences

- Consistent local composition of frontend, BFF, services, and infrastructure.
- Built-in service discovery, configuration, and observability wiring.
- Aspire is an orchestration concern only; domain and building-block code stay portable.
- The project takes a dependency on the Aspire 13 toolchain and its prerequisites.

## Alternatives considered

- **Plain Docker Compose:** workable, but less integrated with the .NET developer experience and observability story.
- **Manual host wiring:** more boilerplate and weaker consistency across services.