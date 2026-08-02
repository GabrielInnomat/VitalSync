# Architecture Overview

VitalSync is a **cloud-native, distributed application**. This document gives the high-level picture; deeper topics live in their own documents linked below.

## Logical layers

VitalSync is composed of four logical tiers:

1. **Frontend (Blazor)** — user interface only. Holds no business logic and talks exclusively to the BFF.
2. **Backend-for-Frontend (BFF)** — exposes REST to the frontend and orchestrates calls to microservices using code-first gRPC.
3. **Microservices** — one per business area (Nutrition, Fitness, Analytics). Each is independent and owns its data.
4. **Messaging backbone** — the only channel for inter-service communication; strictly asynchronous.

```text
Blazor ──REST──> BFF ──gRPC──> Microservices ──async messaging──> Microservices
```

## Runtime composition

The system is composed and run by the .NET Aspire AppHost
(`src/Aspire/VitalSync.AppHost`, see [ADR-0002](./decisions/0002-use-dotnet-aspire-13-for-orchestration.md)):

| Resource                                                        | Role                                                                                    |
| --------------------------------------------------------------- | ----------------------------------------------------------------------------------------- |
| `messaging` (RabbitMQ)                                          | the asynchronous backbone between services                                              |
| `postgres`                                                      | the single relational engine, one shared server for now                                 |
| `<context>-write` / `<context>-read`                            | the database pair each bounded context owns ([ADR-0021](./decisions/0021-write-read-database-pair-per-context.md)) |
| `<context>-migration-service`                                   | a worker that migrates that context's databases and exits; the service waits for it     |
| `nutrition-service`, `fitness-service`, `analytics-service`     | the microservices, each gated on a `/health` check                                      |
| `backend-for-frontend`                                          | fans out to the three services                                                          |
| `web-frontend`                                                  | the Blazor client — the only externally reachable endpoint                               |

Adding a bounded context means adding this same set: two databases, one migration
worker, one service. Services and migration workers currently exist as skeletons
without domain code.

## Architectural principles

- **Domain-Driven Design (DDD)** in every microservice.
- **CQRS** to separate write and read concerns in every service.
- **Event Sourcing** _where it provides business value_; otherwise traditional persistence with **Entity Framework Core**.
- **Clear separation of business domains** — no shared domain models across contexts.
- **Independent deployability** — services do not share databases and avoid synchronous coupling.

## Non-functional requirements

The architecture must be:

- modular
- extensible
- maintainable
- testable
- loosely coupled
- cloud-native
- reusable in the long term

## The Building Blocks platform

A reusable set of shared Building Blocks underpins the services without coupling them to VitalSync. It covers Domain, Application, and Infrastructure. See [Building Blocks](./building-blocks.md).

## Related documents

- [Communication](./communication.md)
- [Building Blocks](./building-blocks.md)
- [Domain model](./domain-model.md)
- [CQRS & Event Sourcing](./cqrs-and-event-sourcing.md)
- [Testing strategy](./testing-strategy.md)
- [Architecture Decision Records](./decisions/README.md)
