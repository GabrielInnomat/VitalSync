# Domain Model

The domain layer is the heart of each microservice. These rules are **mandatory** and apply across all services.

## Principles

- The **domain layer is independent** of infrastructure concerns and framework-specific details.
- **Domain Events represent purely business events.** They must not depend on third-party libraries or infrastructure components.
- **Aggregate Roots manage their own Domain Events.** Adding and removing domain events is **exclusively** the responsibility of the aggregate. Other layers may only access these events **read-only**.
- **Aggregate identifiers are strongly typed Value Objects.** Using an identifier of the wrong aggregate is a **compile-time** error.

## Tactical building blocks

### Entity
An object with an identity that persists over time. Equality is based on identity (same type + same id).

### Aggregate Root
The consistency boundary and entry point for a cluster of domain objects. It:
- exposes behavior (not setters) to enforce invariants,
- **raises domain events** to announce business-relevant changes,
- exposes those events **read-only** to the outside.

```text
        ┌───────────────────────────┐
        │      Aggregate Root       │
        │  - enforces invariants    │
        │  - raises domain events ──┼──► read-only to other layers
        └───────────────────────────┘
```

### Value Object
An immutable object defined by its attributes, with structural equality. Examples likely in VitalSync: a nutritional value, a quantity with unit, a calorie amount.

### Domain Event
A record of something business-relevant that happened in the domain. Pure business data, no infrastructure types. Internal to the service; may be translated into an **integration event** at the boundary.

### Strongly Typed Identifier
A Value Object wrapping the underlying primitive (e.g., a `Guid`). For example, a `RecipeId` and an `IngredientId` are distinct, incompatible types even though both wrap a `Guid`. See [ADR-0005](./decisions/0005-strongly-typed-aggregate-identifiers.md).

## Ownership of domain events (important)

The flow is deliberately constrained:

```text
Aggregate.RaiseDomainEvent(...)   // only the aggregate, internally
        │
        ▼
Aggregate.DomainEvents            // read-only view for other layers
        │
        ▼
Persistence collects on save  ──► Dispatcher / Outbox  ──► Messaging
        │
        ▼
Aggregate.ClearDomainEvents()     // after they have been handled
```

Other layers **cannot** add or remove events — they can only read and (after dispatch) trigger a clear through the aggregate's own method.

## Bounded Contexts (iterative)

The candidate contexts are **Nutrition**, **Fitness**, and **Analytics**, but the **final decomposition is part of the project** and will be refined. Domains remain clearly separated; no shared domain model crosses a context boundary.

## Related

- [CQRS & Event Sourcing](./cqrs-and-event-sourcing.md)
- [Building Blocks](./building-blocks.md)
- [Glossary](../glossary.md)