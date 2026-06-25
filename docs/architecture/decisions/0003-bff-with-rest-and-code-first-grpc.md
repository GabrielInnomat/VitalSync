# 0003. BFF with REST externally and code-first gRPC internally

- **Status:** Accepted
- **Date:** 2026-06-23

## Context

The Blazor frontend must not depend on the internal service topology, and we want strongly typed, high-performance communication between the BFF and the microservices. We also prefer contracts defined close to the C# code rather than hand-authored `.proto` files.

## Decision

- The frontend communicates **exclusively** with a **Backend-for-Frontend (BFF)**.
- The BFF exposes **REST (HTTP/JSON)** endpoints to the frontend.
- The BFF communicates with microservices using **code-first gRPC** (contracts authored in C#).

## Consequences

- The frontend is decoupled from service topology; cross-cutting concerns are centralized in the BFF.
- Strongly typed, efficient BFF↔service communication.
- Contracts live close to the consuming code, reducing drift.
- The BFF becomes a critical component requiring its own testing and resilience considerations.

## Alternatives considered

- **Frontend calling services directly:** rejected — couples UI to topology and scatters cross-cutting concerns.
- **REST for BFF↔services:** workable but less efficient and weaker typing than gRPC.
- **`.proto`-first gRPC:** viable, but code-first keeps contracts closer to C# and reduces context switching for this team.