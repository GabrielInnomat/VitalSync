# BuildingBlocks.Application

`BuildingBlocks.Application` is the reusable, framework-agnostic building block that
defines the **CQRS abstractions** (commands, queries, handlers), the **pipeline
behavior** contract, the **dispatcher** contract, and the **`Result` / `Error`**
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
- **Contracts here, DI wiring elsewhere.** The dispatcher and behavior *contracts*
  live here; their DI-based *implementations* live in `BuildingBlocks.Infrastructure`.

## CQRS contracts

| Concept | Marker | Handler | Returns |
|---|---|---|---|
| Command (no value) | `ICommand` | `ICommandHandler<TCommand>` | `Task<Result>` |
| Command (with value) | `ICommand<TResult>` | `ICommandHandler<TCommand, TResult>` | `Task<Result<TResult>>` |
| Query | `IQuery<TResult>` | `IQueryHandler<TQuery, TResult>` | `Task<Result<TResult>>` |

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
      public async Task<Result<RecipeId>> Handle(CreateRecipeCommand command, CancellationToken ct)
      {
          var recipe = Recipe.Create(command.Name);   // may throw domain exceptions
          await _repository.AddAsync(recipe, ct);
          return Result.Success(recipe.Id);
      }
  }
  ```

- **Delete / update / void** return a plain `Result` — success or failure is enough:

  ```csharp
  public sealed record DeleteRecipeCommand(RecipeId Id) : ICommand;
  // handler returns Task<Result>
  ```

## Dispatcher

```csharp
public interface ISender
{
    Task<Result> Send(ICommand command, CancellationToken ct);
    Task<Result<TResult>> Send<TResult>(ICommand<TResult> command, CancellationToken ct);
    Task<Result<TResult>> Send<TResult>(IQuery<TResult> query, CancellationToken ct);
}
```

`ISender` is the single entry point callers use. Its DI-based implementation in
`BuildingBlocks.Infrastructure` resolves the matching handler and the ordered
pipeline behaviors from the container.

## Pipeline behaviors

```csharp
public interface IPipelineBehavior<TRequest, TResponse>
{
    Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct);
}
```

- Behaviors wrap handler execution to apply cross-cutting concerns (exception
  translation, logging, unit-of-work, etc.).
- **Ordering is explicit registration order** (ADR-0015): behaviors run in the
  order they are registered in DI. Registration lives in `Infrastructure`.
- The generic behaviors themselves (logging, unit-of-work, validation) live in
  `Infrastructure` / `Persistence`; only the **contract** lives here.

## Error handling & the `Result` model

Per [ADR-0017](./decisions/0017-application-error-handling-and-result.md):

- The Domain **throws** `BusinessRuleViolationException` / `DomainValidationException`
  (ADR-0009). An **`ExceptionToResultBehavior`** (registered first) translates these
  into `Result.Failure`.
- Handlers may also return `Result.Failure` directly for expected outcomes such as
  *not found* or *conflict*.
- **Unexpected** exceptions are **not** turned into `Result`; they bubble to a thin
  global handler in the service host.

### `Result` / `Result<T>`

- `Result` — success, or failure carrying **one or more** `Error`s.
- `Result<T>` — success carrying a value of `T`, or failure carrying `Error`s.

### `Error`

| Member | Meaning |
|---|---|
| `Code` | Stable, machine-readable string (e.g. `recipe.name_required`) for i18n / specific client handling. |
| `Message` | Human-readable description. |
| `Category` | An `ErrorCategory` value (below). |

### `ErrorCategory`

| Category | Source |
|---|---|
| `Validation` | `DomainValidationException` (translated) |
| `BusinessRule` | `BusinessRuleViolationException` (translated) |
| `NotFound` | Returned directly by handlers for missing aggregates |
| `Conflict` | Returned directly by handlers for already-exists / concurrency |

There is deliberately **no** `Unexpected` category — unexpected errors remain
exceptions handled globally.

## Transport status mapping (not defined here)

`BuildingBlocks.Application` never mentions HTTP or gRPC status codes. Mapping
`ErrorCategory` to a status code is a transport concern owned by the boundary:

- the **BFF** maps `ErrorCategory` → HTTP status code;
- the **service host** maps `ErrorCategory` → gRPC status.

Both consume the same semantic categories, mapping them independently.

## Testing

`BuildingBlocks.Application.Tests` mirrors this project. Tests use xUnit (built-in
asserts), NSubstitute, and EF Core InMemory where needed (ADR-0014). See
[Testing strategy](./testing-strategy.md).
