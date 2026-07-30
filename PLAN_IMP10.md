# Plan zur Lösung von IMP-10: Zwei inkompatible Aggregat-Programmiermodelle

**Priorität:** Hoch  
**Ziel:** Ein einheitliches Autorenmodell für beide Persistenzstrategien schaffen, bei dem Event Sourcing eine *additive* Fähigkeit ist, nicht ein Ersatz.

---

## Problem (Kurzfassung)

Derzeit gibt es zwei völlig verschiedene Aggregate-Modelle:

| Aspekt | `AggregateRoot<TKey>` | `EventSourcedAggregateRoot<TKey,TState>` |
|--------|-----|-----|
| Identität | Im Konstruktor, sofort validiert | Aus `State.Id`, erst nach erstem Event |
| Zustand | Freie Properties | `IState<TSelf,TKey>` Fold |
| Event auslösen | `AddDomainEvent(e)` | `RaiseEvent(e)` |
| Zeitstempel | Nicht gesetzt (Bug: IMP-01) | Über `IClock` gestempelt |
| Version | Nicht vorhanden | `long _version` |
| Repository | `IRepository<T,K>` | `IEventSourcedRepository<T,K>` |

**Konsequenz:** Ein Aggregat von state-stored zu event-sourced zu wechseln ist keine Refaktorierung, sondern eine komplette Neuimplementierung. Das widerspricht der Designthese „Event Sourcing selektiv, wo es Wert bringt".

---

## Lösung (Leitgedanke)

**Alle Aggregate verwenden den `IState` Fold.** Event Sourcing wird eine *additive* Erweiterung, nicht ein anderes Modell:

1. **Basis:** `AggregateRoot<TKey, TState>` — unified model, funktioniert mit state-stored oder event-sourced
2. **Erweiterung:** `EventSourcedAggregateRoot<TKey, TState>` — erbt von der Basis, fügt nur `Version` + `LoadFromHistory` hinzu

Ein Aggregat zwischen state-stored und event-sourced zu wechseln bedeutet dann nur: Basisklasse ändern + Repository-Interface tauschen.

---

## Implementierungsschritte

### Phase 1: Neue Unified Basis-Klasse (Breaking, aber saubere Migration)

#### 1.1 Neue `AggregateRoot<TKey, TState>` schreiben

**Datei:** `BuildingBlocks/src/BuildingBlocks.Domain/AggregateRootUnified.cs` (oder `AggregateRoot.cs` ersetzen)

```csharp
/// <summary>
/// Unified base class for aggregates, supporting both state-stored and event-sourced persistence.
/// The aggregate's state is managed via an IState<TSelf, TKey> fold, which applies domain events
/// immutably. The persistence strategy (state-stored or event-sourced) is a repository choice, not a class choice.
/// </summary>
/// <typeparam name="TKey">The type of the identity key.</typeparam>
/// <typeparam name="TState">The type of the aggregate's state, implementing IState.</typeparam>
public abstract class AggregateRoot<TKey, TState> : IAggregateRoot<TKey>, IDomainEventsManager, IEquatable<AggregateRoot<TKey, TState>>
    where TKey : struct, IEntityKey
    where TState : IState<TState, TKey>
{
    private readonly List<IDomainEvent> _domainEvents = [];

    /// <summary>
    /// Initializes a new instance with the provided initial state.
    /// </summary>
    /// <remarks>
    /// The state carries the identity; the aggregate's Id is derived from State.Id.
    /// </remarks>
    protected AggregateRoot(TState initialState)
    {
        State = initialState;
        EnsureValidIdentity();
    }

    /// <summary>
    /// Gets the current state of the aggregate.
    /// </summary>
    /// <remarks>
    /// The state is immutable: applying an event yields a new state instance.
    /// </remarks>
    protected TState State { get; private set; }

    /// <inheritdoc/>
    public TKey Id => State.Id;

    /// <inheritdoc/>
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    /// <summary>
    /// Raises (applies) a domain event to the aggregate's state.
    /// </summary>
    /// <remarks>
    /// This is the only way to change the aggregate's state: each state change is expressed
    /// as an event, applied to the current state to produce a new state, and recorded for later dispatch.
    /// </remarks>
    /// <param name="domainEvent">The domain event to apply.</param>
    /// <exception cref="ArgumentNullException">When domainEvent is null.</exception>
    protected void RaiseEvent(IDomainEvent domainEvent)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);
        State = State.Apply(domainEvent);
        EnsureValidIdentity();
        _domainEvents.Add(domainEvent);
    }

    /// <inheritdoc/>
    void IDomainEventsManager.ClearDomainEvents()
    {
        _domainEvents.Clear();
    }

    private void EnsureValidIdentity()
    {
        if (Id.IsEmpty)
        {
            throw new DomainValidationException(
                "The id of an aggregate cannot be empty.");
        }
    }

    /// <inheritdoc/>
    public bool Equals(AggregateRoot<TKey, TState>? other)
    {
        return other is not null
               && other.GetType() == GetType()
               && Id.Equals(other.Id);
    }

    /// <inheritdoc/>
    public sealed override bool Equals(object? obj) => Equals(obj as AggregateRoot<TKey, TState>);

    /// <inheritdoc/>
    public sealed override int GetHashCode() => Id.GetHashCode();
}
```

---

#### 1.2 `EventSourcedAggregateRoot<TKey, TState>` umschreiben (erbt jetzt von der Basis)

**Datei:** `BuildingBlocks/src/BuildingBlocks.Domain/EventSourcedAggregateRoot.cs`

```csharp
/// <summary>
/// Extension of AggregateRoot for event-sourced aggregates: adds version tracking and history replay.
/// </summary>
/// <remarks>
/// Derive from this when the aggregate's source of truth is its event stream. The base class
/// provides the unified state fold; this class adds optimistic concurrency (version) and history replay.
/// </remarks>
public abstract class EventSourcedAggregateRoot<TKey, TState> : AggregateRoot<TKey, TState>, IEventSourcedAggregateRoot<TKey>
    where TKey : struct, IEntityKey
    where TState : IState<TState, TKey>
{
    private long _version;

    /// <summary>
    /// Parameterless constructor for the event-sourced path: the state will be built via LoadFromHistory.
    /// </summary>
    /// <remarks>
    /// Called by MartenEventSourcedRepository when loading from an event stream.
    /// The state is not valid until LoadFromHistory is called.
    /// </remarks>
    protected EventSourcedAggregateRoot() : base(default!)
    {
    }

    /// <summary>
    /// Constructor for test/initialization scenarios where an initial state is provided.
    /// </summary>
    protected EventSourcedAggregateRoot(TState initialState) : base(initialState)
    {
    }

    /// <inheritdoc/>
    long IEventSourcedAggregateRoot<TKey>.Version => _version;

    /// <inheritdoc/>
    void IEventSourcedAggregateRoot<TKey>.LoadFromHistory(IEnumerable<IDomainEvent> history)
    {
        if (DomainEvents.Count > 0)
        {
            throw new InvalidOperationException(
                "LoadFromHistory cannot be called after events have been raised on the aggregate.");
        }

        foreach (var domainEvent in history)
        {
            // Apply the event via the base's RaiseEvent-like logic, but without recording it again
            State = State.Apply(domainEvent);
            _version++;
        }

        EnsureValidIdentity();
    }

    /// <summary>
    /// Raises an event and increments version (for optimistic concurrency).
    /// </summary>
    /// <remarks>
    /// Overrides the base to track version. The base applies the event to state and records it.
    /// </remarks>
    protected new void RaiseEvent(IDomainEvent domainEvent)
    {
        base.RaiseEvent(domainEvent);
        _version++;
    }

    private void EnsureValidIdentity()
    {
        if (Id.IsEmpty)
        {
            throw new DomainValidationException("The id of an aggregate cannot be empty.");
        }
    }
}
```

**Wichtig:** Der alte parameterlose Konstruktor in `EventSourcedAggregateRoot` wird entfernt (s. IMP-14).

---

### Phase 2: Repositories anpassen

#### 2.1 `IRepository<TAggregate, TKey>` (für state-stored) aktualisieren

**Datei:** `BuildingBlocks/src/BuildingBlocks.Application/IRepository.cs`

```csharp
public interface IRepository<TAggregate, in TKey>
    where TAggregate : AggregateRoot<TKey, ???>  // <- TState ist nicht im Interface nötig
    where TKey : struct, IEntityKey
{
    // gleich wie bisher
}
```

**Problem:** `AggregateRoot<TKey>` existiert nicht mehr; wir haben `AggregateRoot<TKey, TState>`.  
**Lösung:** Das Interface braucht ein drittes Generic:

```csharp
public interface IRepository<TAggregate, TState, in TKey>
    where TAggregate : AggregateRoot<TKey, TState>
    where TState : IState<TState, TKey>
    where TKey : struct, IEntityKey
{
    Task<TAggregate?> GetByIdAsync(TKey id, CancellationToken cancellationToken);
    Task AddAsync(TAggregate aggregate, CancellationToken cancellationToken);
    void Remove(TAggregate aggregate);
}
```

**Alternative (weniger Breaking):** Generic-Constraint mit `where TAggregateRoot : class` arbeiten, aber das ist weniger typsicher. **Empfehlung:** Das dritte Generic akzeptieren und den Code durchgehend anpassen.

#### 2.2 `EfCoreRepository<TAggregate, TKey>` anpassen

**Datei:** `BuildingBlocks/src/BuildingBlocks.Infrastructure/Persistence/EfCoreRepository.cs`

```csharp
public sealed class EfCoreRepository<TAggregate, TState, TKey>(DbContext context) 
    : IRepository<TAggregate, TState, TKey>
    where TAggregate : AggregateRoot<TKey, TState>
    where TState : IState<TState, TKey>
    where TKey : struct, IEntityKey
{
    public async Task<TAggregate?> GetByIdAsync(TKey id, CancellationToken cancellationToken) =>
        await context.Set<TAggregate>().FindAsync([id], cancellationToken).ConfigureAwait(false);

    public async Task AddAsync(TAggregate aggregate, CancellationToken cancellationToken) =>
        await context.Set<TAggregate>().AddAsync(aggregate, cancellationToken).ConfigureAwait(false);

    public void Remove(TAggregate aggregate) =>
        context.Set<TAggregate>().Remove(aggregate);
}
```

#### 2.3 `IEventSourcedRepository<TAggregate, TKey>` (bleibt unverändert)

Diese ist bereits typsicher und braucht keine Änderung. Der Wechsel von state-stored zu event-sourced besteht jetzt darin:
- Basis klasse: `AggregateRoot<TKey, TState>` → `EventSourcedAggregateRoot<TKey, TState>`
- Repository: `IRepository<TAggregate, TState, TKey>` → `IEventSourcedRepository<TAggregate, TKey>`

---

#### 2.4 `MartenEventSourcedRepository<TAggregate, TKey>` anpassen

**Datei:** `BuildingBlocks/src/BuildingBlocks.Infrastructure/Persistence/MartenEventSourcedRepository.cs`

Die Implementierung bleibt großteils gleich, aber der Konstruktor ändert sich:

```csharp
public sealed class MartenEventSourcedRepository<TAggregate, TState, TKey>(IDocumentSession session, MartenAggregateTracker tracker)
    : IEventSourcedRepository<TAggregate, TKey>
    where TAggregate : EventSourcedAggregateRoot<TKey, TState>, new()  // <- new() möglich wegen neuem parameterlosem Ctor
    where TState : IState<TState, TKey>
    where TKey : struct, IEntityKey
{
    // GetByIdAsync und SaveAsync bleiben gleich
}
```

**Hinweis:** Mit dem neuen Design kann `new()` erhalten bleiben, aber die Semantik ist jetzt sauberer: Es gibt einen parameterlosen Konstruktor speziell für ES-Aggregate, der vom Repository beim Laden aufgerufen wird.

---

### Phase 3: Vorhandene Aggregate migrieren

#### 3.1 State-Klassen definieren

Für jedes bestehendes state-stored Aggregate ein `IState<TSelf, TKey>`-implementierendes `record` schreiben.

**Beispiel (hypothetisch, falls in der Codebase Aggregates existieren):**

```csharp
// Vorher: AggregateRoot<WorkoutId>
public abstract class Workout : AggregateRoot<WorkoutId>
{
    public string Name { get; private set; }
    public DateTime StartTime { get; private set; }
    // ... weitere Properties
}

// Nachher:
public sealed record WorkoutState(WorkoutId Id, string Name, DateTime StartTime, /*...*/) 
    : IState<WorkoutState, WorkoutId>
{
    public WorkoutState Apply(IDomainEvent domainEvent) =>
        domainEvent switch
        {
            WorkoutStartedEvent e => this with { Name = e.Name, StartTime = e.StartTime },
            WorkoutCompletedEvent => this,
            _ => this
        };
}

public abstract class Workout : AggregateRoot<WorkoutId, WorkoutState>
{
    public Workout(WorkoutState initialState) : base(initialState) { }

    public string Name => State.Name;
    public DateTime StartTime => State.StartTime;

    public void Start(string name) =>
        RaiseEvent(new WorkoutStartedEvent { Name = name, StartTime = DateTime.Now });
}
```

---

### Phase 4: Tests schreiben (Neu + Migriert)

#### 4.1 Tests für den unified AggregateRoot (keine Infrastruktur nötig)

**Datei:** `BuildingBlocks/tests/BuildingBlocks.Domain.Tests/AggregateRootTests.cs` (neu)

```csharp
public class UnifiedAggregateRootTests
{
    /// <summary>
    /// Verifiziert, dass ein State-stored Aggregat über RaiseEvent Ereignisse sammelt.
    /// </summary>
    [Fact]
    public void RaiseEvent_AddsEventToCollection()
    {
        // Arrange
        var initialState = new TestState(TestId.Create(Guid.NewGuid()));
        var aggregate = new TestStateStoredAggregate(initialState);
        var testEvent = new TestDomainEvent();

        // Act
        aggregate.RaiseTestEvent(testEvent);

        // Assert
        Assert.Single(aggregate.DomainEvents);
        Assert.Equal(testEvent, aggregate.DomainEvents.First());
    }

    /// <summary>
    /// Verifiziert, dass RaiseEvent den State über Apply() mutiert.
    /// </summary>
    [Fact]
    public void RaiseEvent_AppliesEventToState()
    {
        // Arrange
        var initialState = new TestState(TestId.Create(Guid.NewGuid()), Value: 0);
        var aggregate = new TestStateStoredAggregate(initialState);
        var testEvent = new TestDomainEvent { ValueIncrement = 5 };

        // Act
        aggregate.RaiseTestEvent(testEvent);

        // Assert
        Assert.Equal(5, aggregate.State.Value);
    }

    /// <summary>
    /// Verifiziert, dass ein leeres Id den Aggregat invalidiert.
    /// </summary>
    [Fact]
    public void RaiseEvent_WithEmptyIdInState_ThrowsDomainValidationException()
    {
        // Arrange
        var invalidState = new TestState(default);  // TestId.IsEmpty == true
        var aggregate = new TestStateStoredAggregate(invalidState);
        var testEvent = new TestDomainEvent();

        // Act & Assert
        Assert.Throws<DomainValidationException>(() => aggregate.RaiseTestEvent(testEvent));
    }

    /// <summary>
    /// Verifiziert Gleichheit basierend auf Typ und Id.
    /// </summary>
    [Fact]
    public void Equals_SameTypeAndId_ReturnsTrue()
    {
        var id = TestId.Create(Guid.NewGuid());
        var state1 = new TestState(id);
        var state2 = new TestState(id);

        var agg1 = new TestStateStoredAggregate(state1);
        var agg2 = new TestStateStoredAggregate(state2);

        Assert.Equal(agg1, agg2);
    }

    /// <summary>
    /// Verifiziert, dass unterschiedliche Typen nicht gleich sind, auch mit gleichem Id.
    /// </summary>
    [Fact]
    public void Equals_DifferentType_ReturnsFalse()
    {
        var id = TestId.Create(Guid.NewGuid());
        var state1 = new TestState(id);
        var state2 = new TestState(id);

        var agg1 = new TestStateStoredAggregate(state1);
        var agg2 = new OtherTestAggregate(state2);

        Assert.NotEqual(agg1, agg2);
    }
}
```

#### 4.2 Tests für den Event-Sourced Aggregat

**Datei:** `BuildingBlocks/tests/BuildingBlocks.Domain.Tests/EventSourcedAggregateRootTests.cs` (neu)

```csharp
public class EventSourcedAggregateRootTests
{
    /// <summary>
    /// Verifiziert, dass LoadFromHistory den State korrekt rebuild.
    /// </summary>
    [Fact]
    public void LoadFromHistory_ReplaysEvents_RebuildStateAndVersion()
    {
        // Arrange
        var aggregate = new TestEventSourcedAggregate();
        var event1 = new TestDomainEvent { ValueIncrement = 10 };
        var event2 = new TestDomainEvent { ValueIncrement = 5 };

        // Act
        aggregate.LoadFromHistory(new[] { event1, event2 });

        // Assert
        Assert.Equal(15, aggregate.State.Value);
        Assert.Equal(2, aggregate.Version);
    }

    /// <summary>
    /// Verifiziert, dass RaiseEvent die Version inkrementiert (zusätzlich zum Base-Verhalten).
    /// </summary>
    [Fact]
    public void RaiseEvent_IncrementsVersion()
    {
        // Arrange
        var initialState = new TestState(TestId.Create(Guid.NewGuid()));
        var aggregate = new TestEventSourcedAggregate(initialState);

        // Act
        aggregate.RaiseTestEvent(new TestDomainEvent { ValueIncrement = 5 });

        // Assert
        Assert.Equal(1, aggregate.Version);
    }

    /// <summary>
    /// Verifiziert, dass LoadFromHistory nach RaiseEvent wirft (verhindert Vermischung).
    /// </summary>
    [Fact]
    public void LoadFromHistory_AfterRaiseEvent_Throws()
    {
        // Arrange
        var initialState = new TestState(TestId.Create(Guid.NewGuid()));
        var aggregate = new TestEventSourcedAggregate(initialState);
        aggregate.RaiseTestEvent(new TestDomainEvent());

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() =>
            aggregate.LoadFromHistory(new[] { new TestDomainEvent() }));
    }
}
```

#### 4.3 Integrationstests (Testcontainers, mit echter DB)

**Datei:** `BuildingBlocks/tests/BuildingBlocks.Infrastructure.Tests/Persistence/AggregateRootPersistenceTests.cs` (neu)

```csharp
public class AggregateRootPersistenceTests : IAsyncLifetime
{
    private PostgreSqlContainer _container;
    private DbContext _dbContext;
    private IDocumentSession _martenSession;

    public async Task InitializeAsync()
    {
        _container = new PostgreSqlBuilder()
            .WithCleanUp(true)
            .Build();
        await _container.StartAsync();

        // DbContext und Marten Session initialisieren
        // ...
    }

    public async Task DisposeAsync()
    {
        await _dbContext.DisposeAsync();
        await _martenSession.DisposeAsync();
        await _container.StopAsync();
    }

    /// <summary>
    /// Verifiziert, dass ein state-stored Aggregat persistiert und wieder geladen wird.
    /// </summary>
    [Fact]
    public async Task StateStoredAggregate_PersistsAndLoads()
    {
        // Arrange
        var initialState = new TestState(TestId.Create(Guid.NewGuid()), Value: 0);
        var aggregate = new TestStateStoredAggregate(initialState);
        aggregate.RaiseTestEvent(new TestDomainEvent { ValueIncrement = 10 });

        var repository = new EfCoreRepository<TestStateStoredAggregate, TestState, TestId>(_dbContext);
        var unitOfWork = new EfCoreUnitOfWork<TestDbContext>(_dbContext, new MockClock());

        // Act
        await repository.AddAsync(aggregate, CancellationToken.None);
        await unitOfWork.CommitAsync(CancellationToken.None);

        // Assert
        var loaded = await repository.GetByIdAsync(aggregate.Id, CancellationToken.None);
        Assert.NotNull(loaded);
        Assert.Equal(10, loaded!.State.Value);
    }

    /// <summary>
    /// Verifiziert, dass ein event-sourced Aggregat seine Events persistiert und State rebuildet.
    /// </summary>
    [Fact]
    public async Task EventSourcedAggregate_PersistsAndRebuildFromHistory()
    {
        // Arrange
        var initialState = new TestState(TestId.Create(Guid.NewGuid()), Value: 0);
        var aggregate = new TestEventSourcedAggregate(initialState);
        aggregate.RaiseTestEvent(new TestDomainEvent { ValueIncrement = 10 });
        aggregate.RaiseTestEvent(new TestDomainEvent { ValueIncrement = 5 });

        var repository = new MartenEventSourcedRepository<TestEventSourcedAggregate, TestState, TestId>(_martenSession, new MockTracker());
        var unitOfWork = new MartenUnitOfWork(_martenSession, new MockClock());

        // Act
        await repository.SaveAsync(aggregate, CancellationToken.None);
        await unitOfWork.CommitAsync(CancellationToken.None);

        // Assert: Lade den Aggregat erneut und prüfe, ob State rebuildet wurde
        var loaded = await repository.GetByIdAsync(aggregate.Id, CancellationToken.None);
        Assert.NotNull(loaded);
        Assert.Equal(15, loaded!.State.Value);  // 10 + 5
        Assert.Equal(2, loaded.Version);
    }

    /// <summary>
    /// Verifiziert optimistic concurrency auf event-sourced Aggregaten.
    /// </summary>
    [Fact]
    public async Task EventSourcedAggregate_ConcurrencyConflict_ThrowsException()
    {
        // Arrange
        var initialState = new TestState(TestId.Create(Guid.NewGuid()));
        var agg1 = new TestEventSourcedAggregate(initialState);
        var agg2 = new TestEventSourcedAggregate(initialState);

        var repository = new MartenEventSourcedRepository<TestEventSourcedAggregate, TestState, TestId>(_martenSession, new MockTracker());

        agg1.RaiseTestEvent(new TestDomainEvent { ValueIncrement = 5 });
        agg2.RaiseTestEvent(new TestDomainEvent { ValueIncrement = 10 });

        // Act: Erste Speicherung erfolgreich
        await repository.SaveAsync(agg1, CancellationToken.None);
        await _martenSession.SaveChangesAsync();

        // Act: Zweite Speicherung sollte wegen Version-Konflikt schlagen
        await repository.SaveAsync(agg2, CancellationToken.None);
        var ex = await Assert.ThrowsAsync<ConcurrencyException>(() => _martenSession.SaveChangesAsync());

        Assert.NotNull(ex);
    }
}
```

#### 4.4 TestDoubles aktualisieren

**Datei:** `BuildingBlocks/tests/BuildingBlocks.Domain.Tests/TestDoubles/TestAggregate.cs` (aktualisiert)

```csharp
namespace BuildingBlocks.Domain.Tests.TestDoubles;

public sealed record TestState(TestId Id, int Value = 0) : IState<TestState, TestId>
{
    public TestState Apply(IDomainEvent domainEvent) =>
        domainEvent switch
        {
            TestDomainEvent e => this with { Value = Value + (e.ValueIncrement ?? 0) },
            _ => this
        };
}

/// <summary>
/// Test aggregate using the unified AggregateRoot base with state-stored semantics.
/// </summary>
internal sealed class TestStateStoredAggregate(TestState initialState) : AggregateRoot<TestId, TestState>(initialState)
{
    public void RaiseTestEvent(IDomainEvent domainEvent) => RaiseEvent(domainEvent);
}

/// <summary>
/// Test aggregate using EventSourcedAggregateRoot for event-sourced semantics.
/// </summary>
internal sealed class TestEventSourcedAggregate : EventSourcedAggregateRoot<TestId, TestState>
{
    public TestEventSourcedAggregate() { }
    public TestEventSourcedAggregate(TestState initialState) : base(initialState) { }

    public void RaiseTestEvent(IDomainEvent domainEvent) => RaiseEvent(domainEvent);
}

/// <summary>
/// Another aggregate type for cross-type equality checks.
/// </summary>
internal sealed class OtherTestAggregate(TestState initialState) : AggregateRoot<TestId, TestState>(initialState)
{
}

public sealed record TestDomainEvent(int? ValueIncrement = null) : IDomainEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTimeOffset OccurredAt { get; init; } = DateTimeOffset.UtcNow;
}

public sealed record TestId(Guid Value) : IEntityKey
{
    public static TestId Create(Guid guid) => new(guid);
    public bool IsEmpty => Value == Guid.Empty;
}
```

---

### Phase 5: DI und Service-Registrierung anpassen

#### 5.1 `BuildingBlocksOptions` (ServiceCollectionExtensions)

**Datei:** `BuildingBlocks/src/BuildingBlocks.Infrastructure/DependencyInjection/ServiceCollectionExtensions.cs`

Im Bereich `UseEfCorePersistence`:

```csharp
public BuildingBlocksOptions UseEfCorePersistence<TContext>(this BuildingBlocksOptions options)
    where TContext : DbContext
{
    options.Services.TryAddScoped(typeof(IRepository<,,>), typeof(EfCoreRepository<,,>));
    // ... rest bleibt gleich
    return options;
}
```

**Hinweis:** Der generische Typ ist jetzt `IRepository<TAggregate, TState, TKey>` statt `IRepository<TAggregate, TKey>`.

---

### Phase 6: Existing Domain Tests anpassen

#### 6.1 `BuildingBlocks/tests/BuildingBlocks.Domain.Tests/AggregateRootEqualityTests.cs`

Existiert bereits, Test-Aggregates verwenden jetzt die neuen Klassen mit State.

#### 6.2 Alle anderen bestehenden Domain Tests

Müssen minimal angepasst werden — hauptsächlich um TestId und TestState zu verwenden.

---

## Migration-Checkliste

- [ ] **AggregateRoot<TKey, TState>** schreiben (neue Basis)
- [ ] **EventSourcedAggregateRoot<TKey, TState>** umschreiben (erbt von Basis)
- [ ] **IRepository** um `TState`-Generic erweitern
- [ ] **EfCoreRepository** anpassen
- [ ] **MartenEventSourcedRepository** anpassen (mit neuem Ctor)
- [ ] **Alle Aggregate** in der Domain auf neue Basis migrieren (State-Klassen schreiben)
- [ ] **TestDoubles** aktualisieren (TestState, TestAggregate*, TestEventSourcedAggregate)
- [ ] **Neue Domain Tests** schreiben (`AggregateRootTests.cs`, `EventSourcedAggregateRootTests.cs`)
- [ ] **Neue Integrationstests** mit Testcontainers schreiben (`AggregateRootPersistenceTests.cs`)
- [ ] **Alte Tests** anpassen / validieren
- [ ] **ServiceCollectionExtensions** anpassen (DI-Registrierung)
- [ ] **Bestehende Aggregate in anderen Projekten** (z. B. Fitness-Bounded-Context) migrieren

---

## Schrittweise Umsetzung (Empfohlen)

1. **Woche 1 — Neue Basis + Tests (schnell)**
   - AggregateRoot<TKey, TState> schreiben
   - EventSourcedAggregateRoot umschreiben
   - Domain-Unit-Tests für beide schreiben
   - Keine Änderung an bestehenden Aggregates

2. **Woche 2 — Repository-Anpassungen**
   - IRepository erweitern
   - EfCoreRepository + MartenEventSourcedRepository anpassen
   - DI anpassen
   - Integrationstests schreiben

3. **Woche 3 — Migration vorhandener Aggregate**
   - Jedes Aggregat-Paar (alt → neu) migrieren
   - Tests validieren / anpassen
   - Code-Review pro Aggregat

---

## Bekannte Abhängigkeiten (Breaking Changes!)

Alle Stellen, die `AggregateRoot<TKey>` oder `IRepository<TAggregate, TKey>` verwenden, müssen angepasst werden:

- **Domain:** Alle Aggregate-Klassen
- **Application:** IRepository-Injektionen, Handler
- **Infrastructure:** Repository-Implementierungen
- **Tests:** TestDoubles, Test-Setups
- **Bounded Contexts:** Alle Aggregates

**Auf der positiven Seite:** Die Migrationen sind **mechanisch und folgen einem festen Muster**, daher leicht automatisierbar / review-freundlich.

---

## Erfolgs-Kriterien

✅ Ein Aggregat kann mit einem einfachen Basisklasse-Wechsel von state-stored → event-sourced wechseln (nur Klasse + Repository ändern, keine Logik-Umschreibung)  
✅ Alle Domain Tests grün  
✅ Alle Integrationstests grün  
✅ Code-Coverage mindestens 80% für neue/modifizierte Klassen  
✅ Keine Regression in bestehenden Features  
✅ Dokumentation (ADR) aktualisiert mit den neuen Richtlinien

---

## Notizen

- **IMP-01 Synergien:** Nach der Unified Basis kann das Stamping in beiden Unit-of-Work-Implementierungen zentral gelöst werden (siehe IMP-01).
- **IMP-14 Synergien:** Das Problem mit dem parameterlosen Konstruktor ist damit gelöst — der neue Ctor existiert explizit nur für ES-Aggregate.
- **Gleichheits-Duplikate:** Die drei bestehenden Implementierungen (Entity, AggregateRoot, EventSourcedAggregateRoot) können nach dieser Umstrukturierung auf eine Basis-Implementierung reduziert werden.
