# Testing Strategy

Automated tests are implemented for **both the Building Blocks and the individual microservices**. The strategy includes, but is not limited to, the categories below.

## Test categories

| Category                          | Purpose                                                         | Typical scope                                                 |
| --------------------------------- | --------------------------------------------------------------- | ------------------------------------------------------------- |
| **Unit tests**                    | Verify individual units in isolation                            | A class or method                                             |
| **Domain tests**                  | Verify domain rules, invariants, and event-raising              | Aggregates, value objects, domain events                      |
| **Application-layer tests**       | Verify command/query handlers and pipeline behaviors            | Handlers, dispatcher, pipeline, CQRS flow, `Result` semantics |
| **Persistence tests**             | Verify mapping, persistence, and event collection on save       | EF Core `DbContext`, converters                               |
| **Integration tests**             | Verify components working together with real-ish infrastructure | Service + database / messaging                                |
| **Component communication tests** | Verify messaging and contracts between components               | gRPC contracts, message publish/consume                       |

## Tooling

| Tool                 | Use                                                      |
| -------------------- | -------------------------------------------------------- |
| **xUnit**            | Test framework and assertions (`Assert.*`; see ADR-0014) |
| **NSubstitute**      | Mocking/substitutes                                      |
| **EF Core InMemory** | Fast persistence-layer tests                             |
| **Testcontainers (PostgreSQL)** | Integration tests against a real PostgreSQL for Marten optimistic concurrency and strongly-typed key persistence; skipped automatically when Docker is unavailable |

> Integration and component-communication tests may additionally use containerized infrastructure (e.g., via Testcontainers) once the messaging platform is selected.

> NSubstitute is used in the application/persistence/messaging tests; domain tests use lightweight hand-written test doubles instead.

## Principles

- **The domain is highly testable** because it has no infrastructure dependencies — domain tests need no mocks for frameworks.
- **Behavior over implementation** — assert observable behavior (e.g., "creating a recipe raises a `RecipeCreated` event") rather than internal details.
- **Read-only event access is enforced and tested** — outside layers must not be able to mutate an aggregate's domain events.
- **Fast feedback first** — unit/domain/application/persistence tests run quickly; heavier integration tests run as needed.

## External fixture assemblies

Some tests need types that live in a **separate compiled assembly** from the test project
itself — most notably assembly-scanning tests (`AddHandlersFrom(assembly)`, startup handler
validation), where scanning the test assembly would pick up unrelated test types and pollute
the results. For these cases:

- **Where:** always place the fixture project under
  `BuildingBlocks/tests/ExternalAssemblies/<FixtureName>/` — one small project per fixture
  scenario. Reference it from the consuming test project via `ProjectReference` and add it
  to `VitalSync.slnx` (under the `ExternalAssemblies` solution folder).
- **Naming:** keep folder and project names **short** (e.g. `ValidHandlersFixture`, *not*
  `BuildingBlocks.Infrastructure.Tests.ValidHandlersFixture`). Long duplicated names have
  broken the Windows 260-character `MAX_PATH` limit before (build failure on checkout and
  compile). The root namespace equals the project name.
- **Rules:** fixture projects set `IsPackable=false`, reference only what the scenario needs
  (typically `BuildingBlocks.Domain` / `BuildingBlocks.Application`), contain **no tests**
  themselves, and are test code — the XML-documentation requirement (ADR-0013) does **not**
  apply.

Existing examples: `ValidHandlersFixture`, `ConflictingHandlersFixture`,
`OrphanRequestsFixture`, `AmbiguousRequestsFixture` (used by
`BuildingBlocks.Infrastructure.Tests` for handler registration and
startup-validation tests).

## What the current Building Blocks tests cover

- **Domain**: strongly typed id equality and compile-time distinctness, aggregate event raising/clearing, read-only exposure of domain events, entity identity equality, value object structural equality.
- **Application**: dispatcher routing to the correct handler, pipeline behavior ordering and execution, exception-to-`Result` translation (business-rule / domain-validation), `Result` / `Result<T>` success/failure semantics.
- **Infrastructure**: the real `Sender` with a DI container (failure translation, unit-of-work commit/suppression and concurrency-conflict mapping, dispatcher and handler registration, `FailureResults` runtime types), serialization/mapping (`DomainEventEnvelopeSerializer` round-trip with typed ids, `EntityKeyFormatter`, entity-key converter registration), persistence against a real PostgreSQL via Testcontainers (`MartenEventSourcedRepository` version arithmetic / optimistic concurrency and strongly-typed key persistence — skipped when Docker is unavailable), and architecture tests enforcing the layer-dependency rules.

## Related

- [Building Blocks](./building-blocks.md)
