# Testing Strategy

Automated tests are implemented for **both the Building Blocks and the individual microservices**. The strategy includes, but is not limited to, the categories below.

## Test categories

| Category | Purpose | Typical scope |
|---|---|---|
| **Unit tests** | Verify individual units in isolation | A class or method |
| **Domain tests** | Verify domain rules, invariants, and event-raising | Aggregates, value objects, domain events |
| **Application-layer tests** | Verify command/query handlers and pipeline behaviors | Handlers, validation, CQRS flow |
| **Persistence tests** | Verify mapping, persistence, and event collection on save | EF Core `DbContext`, converters |
| **Integration tests** | Verify components working together with real-ish infrastructure | Service + database / messaging |
| **Component communication tests** | Verify messaging and contracts between components | gRPC contracts, message publish/consume |

## Tooling

| Tool | Use |
|---|---|
| **xUnit** | Test framework and assertions (`Assert.*`; see ADR-0014) |
| **NSubstitute** | Mocking/substitutes |
| **EF Core InMemory** | Fast persistence-layer tests |

> Integration and component-communication tests may additionally use containerized infrastructure (e.g., via Testcontainers) once the messaging platform is selected.

> NSubstitute is used in the application/persistence/messaging tests; domain tests use lightweight hand-written test doubles instead.

## Principles

- **The domain is highly testable** because it has no infrastructure dependencies — domain tests need no mocks for frameworks.
- **Behavior over implementation** — assert observable behavior (e.g., "creating a recipe raises a `RecipeCreated` event") rather than internal details.
- **Read-only event access is enforced and tested** — outside layers must not be able to mutate an aggregate's domain events.
- **Fast feedback first** — unit/domain/application/persistence tests run quickly; heavier integration tests run as needed.

## What the current Building Blocks tests cover

- **Domain**: strongly typed id equality and compile-time distinctness, aggregate event raising/clearing, read-only exposure of domain events, entity identity equality, value object structural equality.
- **Application**: validation pipeline behavior (pass/fail/no-validators), `Result` success/failure semantics.
- **Persistence**: domain events are dispatched and cleared on `SaveChanges`; dispatcher is not invoked when there are no events.
- **Event Processing**: dispatcher invokes the correct handler and tolerates missing handlers.

## Related

- [Building Blocks](./building-blocks.md)
- [Domain model](./domain-model.md)
