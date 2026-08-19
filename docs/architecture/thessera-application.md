# GaWeCodes.Thessera.Application

`GaWeCodes.Thessera.Application` is the reusable, framework-agnostic package that
defines the **CQRS abstractions** (commands, queries, handlers), the **pipeline
behavior** contract, the **dispatcher** contract, and the **`Result` / `Failure`**
model shared by every microservice. It depends only on `GaWeCodes.Thessera.Domain` and
is independent of VitalSync.

> Related decisions:
> [ADR-0015](./decisions/0015-hand-rolled-cqrs-mediator.md) (hand-rolled mediator),
> [ADR-0016](./decisions/0016-remove-common-result-in-application.md) (`Result` lives here),
> [ADR-0017](./decisions/0017-application-error-handling-and-result.md) (error handling).

## Design rules

- **Framework-agnostic & reusable.** No references to HTTP, gRPC, ASP.NET Core,
  MediatR, or any third-party mediator. No VitalSync-specific concepts.
- **Async-only.** Every handler and dispatch method returns a `Task<...>` and
  accepts a `CancellationToken`. There are **no** synchronous overloads.
- **Depends on `Domain` only.** Needed for the domain exception types translated by
  the pipeline; nothing else is required.
- **Contracts here, DI wiring elsewhere.** The dispatcher and behavior _contracts_
  live here; their DI-based _implementations_ live in `GaWeCodes.Thessera.Core`.

## Folder layout — folder is namespace, and it is a contract

```
GaWeCodes.Thessera.Application/
├── Cqrs/               ICommand, IQuery, ICommandHandler, IQueryHandler, ISender,
│                       IPipelineBehavior, RequestPipeline, RequestPipelineContinuation
├── Results/            Result, Result<T>, Failure, FailureCategory
├── Persistence/        IRepository, IUnitOfWork
├── DomainEvents/       DomainEventMetadata, IProjectionHandler, IDomainEventPublisher
├── IntegrationEvents/  IIntegrationEvent, IIntegrationEventMapper<>, IIntegrationEventSink,
│                       IntegrationEventTopicAttribute
└── ReadModels/         IReadModelRebuilder
```

The root namespace is **empty on purpose**. Domain events and integration events are split into
two folders rather than one `Events/`, because the line between them is the bounded-context
boundary — the single most important distinction this block makes.

Every type here is `public`, so **the namespaces are part of the published API**: moving a file
changes each exported type's `FullName` and breaks every consumer. `PublicSurfaceTests` pins the
full list of exported type names, so a move fails a test instead of a downstream build.

A service pays for this once, in its `.csproj`, not per file:

```xml
<ItemGroup>
  <Using Include="GaWeCodes.Thessera.Application.Cqrs" />
  <Using Include="GaWeCodes.Thessera.Application.Persistence" />
  <Using Include="GaWeCodes.Thessera.Application.Results" />
</ItemGroup>
```

## CQRS contracts

| Concept              | Marker              | Handler                              | Returns                 |
| -------------------- | ------------------- | ------------------------------------ | ----------------------- |
| Command (no value)   | `ICommand`          | `ICommandHandler<TCommand>`          | `Task<Result>`          |
| Command (with value) | `ICommand<TResult>` | `ICommandHandler<TCommand, TResult>` | `Task<Result<TResult>>` |
| Query                | `IQuery<TResult>`   | `IQueryHandler<TQuery, TResult>`     | `Task<Result<TResult>>` |

- **Commands** express intent and change state.
- **Queries** read state and never mutate it.
- Each command/query is handled by exactly one dedicated handler.

### Create vs. delete conventions

- **Create** returns the new aggregate's **strongly typed identifier** (ADR-0005)
  so the frontend can navigate to the created item:

    ```csharp
    public sealed record CreateRecipeCommand(string Name) : ICommand<RecipeId>;

    public sealed class CreateRecipeHandler : ICommandHandler<CreateRecipeCommand, RecipeId>
    {
        public async Task<Result<RecipeId>> HandleAsync(CreateRecipeCommand command, CancellationToken ct)
        {
            var recipe = Recipe.Create(command.Name);
            await _repository.AddAsync(recipe, ct);
            return Result.Success(recipe.Id);
        }
    }
    ```

- **Delete / update / void** return a plain `Result` — success or failure is enough:

    ```csharp
    public sealed record DeleteRecipeCommand(RecipeId Id) : ICommand;
    ```

## Dispatcher

```csharp
public interface ISender
{
    Task<Result> SendAsync(ICommand command, CancellationToken ct);
    Task<Result<TResult>> SendAsync<TResult>(ICommand<TResult> command, CancellationToken ct);
    Task<Result<TResult>> SendAsync<TResult>(IQuery<TResult> query, CancellationToken ct);
}
```

`ISender` is the single entry point callers use. Its DI-based implementation in
`GaWeCodes.Thessera.Core` resolves the matching handler and the ordered
pipeline behaviors from the container.

## Pipeline behaviors

```csharp
public interface IPipelineBehavior<TRequest, TResponse>
{
    Task<TResponse> HandleAsync(TRequest request, RequestPipeline<TResponse> pipeline, CancellationToken ct);
}

public sealed class RequestPipeline<TResponse>
{
    public Task<TResponse> NextAsync(CancellationToken ct);
    public TResponse Failed(Failure failure);
}
```

- Behaviors wrap handler execution to apply cross-cutting concerns (exception
  translation, logging, unit-of-work, etc.).
- **`RequestPipeline<TResponse>` is what a behavior gets instead of a bare
  continuation.** `NextAsync` runs the rest of the pipeline; `Failed` builds a
  failed response **of the behavior's own `TResponse`**. The failure factory is
  supplied by the dispatcher, which knows the concrete result type (`Result` for a
  void command, `Result<T>` for a query or a value-returning command) — so a
  behavior that must short-circuit needs neither a generic constraint nor
  reflection. Without it, `TResponse` is an opaque type parameter and there is no
  way to name `Result<T>.Failed` (ADR-0015 amendment 2026-08-05).
- **Ordering is an explicit numeric `order`** (ADR-0015): each behavior is
  registered with an order and the pipeline wraps them by ascending order (lower
  wraps further out). Registration and the ordering live in `Infrastructure`;
  hosts insert their own behaviors at a chosen position via
  `ThesseraOptions.AddPipelineBehavior(type, order)`.
- The generic behaviors themselves (logging, unit-of-work, ExceptionToResult) live in
  `Infrastructure`; only the **contract** lives here.

## Infrastructure-implemented contracts

Per [ADR-0024](./decisions/0024-contract-placement-innermost-consumer.md), a
contract lives in the innermost layer that consumes it. The following contracts
therefore live in `Application` while their implementations reside in
`GaWeCodes.Thessera.Core` (see
[GaWeCodes.Thessera.Core](./thessera-core.md)):

### Unit of work

```csharp
public interface IUnitOfWork
{
    Task CommitAsync(CancellationToken ct);
}
```

One unit of work spans each command dispatch; it is owned by the unit-of-work
pipeline behavior in `Infrastructure` — handlers never commit themselves.

```csharp
public interface IRepository<TAggregate, in TKey>
    where TAggregate : class, IAggregateRoot<TKey>
    where TKey : struct, IEntityKey, IEquatable<TKey>
{
    Task<TAggregate?> GetByIdAsync(TKey id, CancellationToken ct);
    Task AddAsync(TAggregate aggregate, CancellationToken ct);
}
```

This is the **single** repository contract for all aggregates, regardless of how
they are persisted ([ADR-0026](./decisions/0026-single-repository-contract.md)).
The surface is deliberately minimal:

- **No `Remove`** — VitalSync never hard-deletes; removal is a domain state
  change (a soft delete) and therefore an ordinary update.
- **No `Update`/`Save`** — aggregates returned by `GetByIdAsync` are tracked;
  their changes flow through the `IUnitOfWork` at commit. This holds for both
  implementations: EF Core relies on change tracking, and the event-store
  implementation appends tracked aggregates' uncommitted events at commit.

Both implementations rebuild a stored aggregate through the same convention: the
aggregate's **private parameterless constructor**, resolved and cached by an
internal factory in `Infrastructure` and validated at host startup
([ADR-0025](./decisions/0025-unified-state-fold-aggregate-model.md)
reconstitution amendment 2026-08-04) — no public constructor is ever demanded.

The `TKey : struct, IEntityKey, IEquatable<TKey>` constraint ties the repository to the strongly
typed identifier model of `GaWeCodes.Thessera.Domain` (ADR-0005): keys are value
types implementing `IEntityKey`, never primitives, and they must declare value
equality — a `readonly record struct` does so automatically (ADR-0008 amendment
2026-08-05).

Because the contract is persistence-agnostic, handlers do not change when a
context switches between state-stored (EF Core) and event-sourced (Marten)
persistence — only the composition-layer registration does
(ADR-0025/0026).

### Domain-event publishing & projections

```csharp
public sealed record DomainEventMetadata(Guid EventId, DateTimeOffset OccurredAt);

public interface IDomainEventPublisher
{
    Task PublishAsync(IDomainEvent domainEvent, DomainEventMetadata metadata, IIntegrationEventSink integrationEventSink, CancellationToken ct);
}

public interface IProjectionHandler<in TDomainEvent>
    where TDomainEvent : IDomainEvent
{
    Task HandleAsync(TDomainEvent domainEvent, DomainEventMetadata metadata, CancellationToken ct);
}
```

- `IDomainEventPublisher` is the contract of the outbox-backed publisher
  (ADR-0022/0023): it is invoked once per committed domain event and fans it out
  to the in-context projection handlers and the integration-event path.
- `DomainEventMetadata` carries the identity minted at commit — domain events
  themselves are pure value records without identity fields (ADR-0029).
- `IProjectionHandler<TDomainEvent>` is implemented per service to update read
  models. Delivery is at-least-once, so idempotency is the handler's
  responsibility. The handler receives the same `DomainEventMetadata` as the
  publisher, so it can keep the last processed `Version` per aggregate on its
  read model and ignore anything at or below that watermark (ADR-0030) — that is
  how ADR-0022's "per-aggregate order-aware" requirement is met.

### Read-model rebuilders

```csharp
public interface IReadModelRebuilder<in TAggregate, TKey>
    where TAggregate : class, IAggregateRoot<TKey>
    where TKey : struct, IEntityKey, IEquatable<TKey>
{
    Task ClearAsync(CancellationToken ct);

    Task RebuildAsync(TAggregate aggregate, CancellationToken ct);
}
```

- Implemented **per service**, next to that service's projection handlers, and only for
  **state-stored** contexts: an event-sourced context still has its stream (ADR-0036).
- It is a **multi-handler** contract — a context may register one rebuilder per read model
  for the same aggregate, and `AddHandlersFrom` registers them all.
- `RebuildAsync` receives the aggregate, not an event. **Every field it writes must be a
  function of the current aggregate state** — absolute values (`PartCount = parts.Count`),
  never increments. A field that needs history does not belong in a state-stored read
  model.
- Writing the aggregate's current `Version` onto the read model is what lets live traffic
  continue incrementally afterwards, through the same watermark check the projection
  handlers already use.
- Infrastructure supplies the driver — `StateStoredReadModelRebuildRunner<TContext>` for an
  EF Core context, `EventSourcedReadModelRebuildRunner` for a Marten one; the service
  supplies only the two methods above, unchanged either way.

### Integration events

```csharp
public interface IIntegrationEvent
{
    Guid EventId { get; }
    DateTimeOffset OccurredAt { get; }
}

public interface IIntegrationEventMapper<in TDomainEvent>
    where TDomainEvent : IDomainEvent
{
    IReadOnlyCollection<IIntegrationEvent> Map(TDomainEvent domainEvent, DomainEventMetadata metadata);
}
```

- `IIntegrationEvent` marks the immutable, serializable contract types that are
  the **only** cross-context signal (ADR-0004/0023); domain events and
  aggregates never cross the broker. Unlike domain events, integration events
  **carry their identity on the event** (ADR-0029): there is no envelope a
  foreign consumer knows about, so `EventId`/`OccurredAt` are part of the
  published contract and give consumers a stable handle for deduplication.
- `IIntegrationEventMapper<TDomainEvent>` is implemented **per service** (the
  translation maps themselves never live in the Thessera) and is
  **typed**, symmetric to `IProjectionHandler<TDomainEvent>`: which domain events
  leave the context is readable from the type signature instead of hidden in a
  `switch`, and the `MapperRunner` resolves only the mappers for the event at
  hand. Mappers populate the integration
  event's identity from the supplied `DomainEventMetadata` — never a fresh Guid
  per invocation, or redeliveries break deduplication.

## Failure handling & the `Result` model

Per [ADR-0017](./decisions/0017-application-error-handling-and-result.md):

- The Domain **throws** `BusinessRuleViolationException` / `DomainValidationException`
  (ADR-0009). An **`ExceptionToResultBehavior`** (inside logging, outside the unit
  of work) translates these into `Result.Failed`, producing **one `Failure` per
  `RuleViolation`** the exception carries — so a domain call that collected several
  broken rules reports every one of them in a single response.
- Handlers may also return `Result.Failed` directly for expected outcomes such as
  _not found_ or _conflict_.
- **Unexpected** exceptions are **not** turned into `Result`; they bubble to a thin
  global handler in the service host.

### `Result` / `Result<T>`

- `Result` — success, or failure carrying **one or more** `Failure`s.
- `Result<T>` — success carrying a value of `T`, or failure carrying `Failure`s.
- Both are built through `Success(...)` and **`Failed(...)`**. The factory is
  deliberately *not* called `Failure`: that name belongs to the error **value**, and
  the surrounding members `Failures` / `IsFailure` already use the noun. `Failed` is
  the verb — what you do with a `Failure` (ADR-0017 amendment 2026-08-05).

### `Failure`

| Member     | Meaning                                                                                            |
| ---------- | -------------------------------------------------------------------------------------------------- |
| `Code`     | Stable, machine-readable string (e.g. `recipe.name_required`) for i18n / specific client handling. Comes from the rule that failed, for business rules as well as validation rules. |
| `Message`  | Human-readable description.                                                                        |
| `Category` | An `FailureCategory` value (below).                                                                |
| `Target`   | Optional name of the field the failure is about; `null` for an invariant or a rule spanning several fields. |

Because the two rule kinds are never mixed in one `RuleChecker` call (ADR-0009), a handler
invocation raises at most one exception, so **every `Failure` in a failed `Result` shares one
category** and the transport status stays unambiguous. A behavior returns several failures at
once through `RequestPipeline<TResponse>.Failed(IReadOnlyList<Failure>)`.

### `FailureCategory`

| Category       | Source                                                         |
| -------------- | -------------------------------------------------------------- |
| `Validation`   | `DomainValidationException` (translated)                       |
| `BusinessRule` | `BusinessRuleViolationException` (translated)                  |
| `NotFound`     | Returned directly by handlers for missing aggregates           |
| `Conflict`     | Returned directly by handlers for already-exists / concurrency |
| `Forbidden`    | Returned directly by handlers for a denied authorization       |

There is deliberately **no** `Unexpected` category — unexpected failures remain
exceptions handled globally (ADR-0017). There is also no `Unauthorized`:
authentication is the host's business and never reaches this layer.

The set is **not** compiler-checked at the transport boundary, and cannot be: a
`switch` expression over an enum always needs a discard arm (CS8509), which swallows
any value added later. Each adapter is therefore covered by a test that walks
`Enum.GetValues<FailureCategory>()` and fails on anything falling through to the
adapter's fallback status, and `FailureTests` asserts that every declared value has
a factory of its own name on `Failure`. Add a category, and both guards tell you
where to go.

## Transport status mapping (not defined here)

`GaWeCodes.Thessera.Application` never mentions HTTP or gRPC status codes. Mapping
`FailureCategory` to a status code is a transport concern owned by the boundary:

- the **BFF** maps `FailureCategory` → HTTP status code;
- the **service host** maps `FailureCategory` → gRPC status.

Both consume the same semantic categories, mapping them independently.

## Testing

`GaWeCodes.Thessera.Application.Tests` mirrors this project. Tests use xUnit (built-in
asserts), NSubstitute, and EF Core InMemory where needed (ADR-0014). See
[Testing strategy](./testing-strategy.md).
