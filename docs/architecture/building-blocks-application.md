# BuildingBlocks.Application

`BuildingBlocks.Application` is the reusable, framework-agnostic building block that
defines the **CQRS abstractions** (commands, queries, handlers), the **pipeline
behavior** contract, the **dispatcher** contract, and the **`Result` / `Failure`**
model shared by every microservice. It depends only on `BuildingBlocks.Domain` and
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
  live here; their DI-based _implementations_ live in `BuildingBlocks.Infrastructure`.

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
`BuildingBlocks.Infrastructure` resolves the matching handler and the ordered
pipeline behaviors from the container.

## Pipeline behaviors

```csharp
public interface IPipelineBehavior<TRequest, TResponse>
{
    Task<TResponse> HandleAsync(TRequest request, RequestPipelineContinuation<TResponse> continuation, CancellationToken ct);
}
```

- Behaviors wrap handler execution to apply cross-cutting concerns (exception
  translation, logging, unit-of-work, etc.).
- **Ordering is an explicit numeric `order`** (ADR-0015): each behavior is
  registered with an order and the pipeline wraps them by ascending order (lower
  wraps further out). Registration and the ordering live in `Infrastructure`;
  hosts insert their own behaviors at a chosen position via
  `BuildingBlocksOptions.AddPipelineBehavior(type, order)`.
- The generic behaviors themselves (logging, unit-of-work, ExceptionToResult) live in
  `Infrastructure`; only the **contract** lives here.

## Infrastructure-implemented contracts

Per [ADR-0024](./decisions/0024-contract-placement-innermost-consumer.md), a
contract lives in the innermost layer that consumes it. The following contracts
therefore live in `Application` while their implementations reside in
`BuildingBlocks.Infrastructure` (see
[BuildingBlocks.Infrastructure](./building-blocks-infrastructure.md)):

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
    where TKey : struct, IEntityKey
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

The `TKey : struct, IEntityKey` constraint ties the repository to the strongly
typed identifier model of `BuildingBlocks.Domain` (ADR-0005): keys are value
types implementing `IEntityKey`, never primitives.

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

### Integration events

```csharp
public interface IIntegrationEvent
{
    Guid EventId { get; }
    DateTimeOffset OccurredAt { get; }
}

public interface IIntegrationEventMapper
{
    IReadOnlyCollection<IIntegrationEvent> Map(IDomainEvent domainEvent, DomainEventMetadata metadata);
}
```

- `IIntegrationEvent` marks the immutable, serializable contract types that are
  the **only** cross-context signal (ADR-0004/0023); domain events and
  aggregates never cross the broker. Unlike domain events, integration events
  **carry their identity on the event** (ADR-0029): there is no envelope a
  foreign consumer knows about, so `EventId`/`OccurredAt` are part of the
  published contract and give consumers a stable handle for deduplication.
- `IIntegrationEventMapper` is implemented **per service** (the translation maps
  themselves never live in the Building Blocks): it selects which domain events
  leave the context and what shape they take, returning an empty collection for
  events without cross-context significance. Mappers populate the integration
  event's identity from the supplied `DomainEventMetadata` — never a fresh Guid
  per invocation, or redeliveries break deduplication.

## Failure handling & the `Result` model

Per [ADR-0017](./decisions/0017-application-error-handling-and-result.md):

- The Domain **throws** `BusinessRuleViolationException` / `DomainValidationException`
  (ADR-0009). An **`ExceptionToResultBehavior`** (inside logging, outside the unit
  of work) translates these into `Result.Failure`.
- Handlers may also return `Result.Failure` directly for expected outcomes such as
  _not found_ or _conflict_.
- **Unexpected** exceptions are **not** turned into `Result`; they bubble to a thin
  global handler in the service host.

### `Result` / `Result<T>`

- `Result` — success, or failure carrying **one or more** `Failure`s.
- `Result<T>` — success carrying a value of `T`, or failure carrying `Failure`s.

### `Failure`

| Member     | Meaning                                                                                            |
| ---------- | -------------------------------------------------------------------------------------------------- |
| `Code`     | Stable, machine-readable string (e.g. `recipe.name_required`) for i18n / specific client handling. |
| `Message`  | Human-readable description.                                                                        |
| `Category` | An `FailureCategory` value (below).                                                                |

### `FailureCategory`

| Category       | Source                                                         |
| -------------- | -------------------------------------------------------------- |
| `Validation`   | `DomainValidationException` (translated)                       |
| `BusinessRule` | `BusinessRuleViolationException` (translated)                  |
| `NotFound`     | Returned directly by handlers for missing aggregates           |
| `Conflict`     | Returned directly by handlers for already-exists / concurrency |

There is deliberately **no** `Unexpected` category — unexpected failures remain
exceptions handled globally.

## Transport status mapping (not defined here)

`BuildingBlocks.Application` never mentions HTTP or gRPC status codes. Mapping
`FailureCategory` to a status code is a transport concern owned by the boundary:

- the **BFF** maps `FailureCategory` → HTTP status code;
- the **service host** maps `FailureCategory` → gRPC status.

Both consume the same semantic categories, mapping them independently.

## Testing

`BuildingBlocks.Application.Tests` mirrors this project. Tests use xUnit (built-in
asserts), NSubstitute, and EF Core InMemory where needed (ADR-0014). See
[Testing strategy](./testing-strategy.md).
