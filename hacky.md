# Hacky — Fundstellen aus dem Codebase-Scan

## Status

| Nr. | Titel                                                          | Status |
| --- | -------------------------------------------------------------- | ------ |
| 1   | AssemblyQualifiedName als Persistenz-Contract                  | gelöst |
| 2   | CLR-Typname im Event-Stream-Key                                | gelöst |
| 3   | `FailureResults` sucht statische Methode per Name              | offen  |
| 4   | `ApplyEntityKeyConversions` scannt CLR- statt Model-Properties | offen  |
| 5   | Kein `Id.IsEmpty`-Guard in `AddAsync`                          | gelöst    |
| 6   | `CurrentValues.SetValues` kopiert nur Skalare                  | offen  |
| 7   | `DomainEventStamper` erkennt „unstamped" über Sentinel         | gelöst |
| 8   | Connection String zweimal, ohne Abgleich                       | gelöst |
| 9   | `AddBuildingBlocks` ist nicht idempotent                       | offen  |
| 10  | `RuleChecker` schluckt `null`                                  | offen  |
| 11  | Optionale Constructor-Injection für `IUnitOfWork`              | offen  |
| 12  | Nur `Failures[0]` erreicht den Client                          | offen  |
| 13  | Global sequentielle Domain-Event-Queue                         | offen  |

---

# 1, AssemblyQualifiedName als Persistenz-Contract — **gelöst (2026-08-03)**

Siehe [TODO-03](todo.md) und [ADR-0030](docs/architecture/decisions/0030-persisted-names-and-aggregate-version.md).
`[EventName]` ist Pflicht, eine `DomainEventTypeRegistry` löst Name↔Typ auf und `Type.GetType`
ist weg. Der Befund war halbiert: Marten leitete `mt_events.type` ebenfalls aus dem CLR-Namen ab —
dieselbe Registry speist jetzt `MapEventType`, sonst hätte der Event Store weiter am Klassennamen
gehangen.


In jeder Outbox-Zeile steht der assembly-qualifizierte Typname des Domain Events
(`"Foo.WidgetCreated, MyAsm, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null"`),
zurückgeholt via `Type.GetType(..., throwOnError: true)`. Ein Version-Bump, eine
Assembly-Umbenennung oder ein Typ-Umzug lässt jede noch nicht zugestellte Nachricht beim
Redelivery werfen — und das ist Crash-Recovery-Datenbestand. Zusätzlich ist `Type.GetType`
auf persistierten Daten eine unbegrenzte Typ-Aktivierungsfläche.

`BuildingBlocks/src/BuildingBlocks.Infrastructure/Messaging/DomainEventEnvelopeSerializer.cs:23,33`

## Lösungsvorschlag

Stabiler logischer Eventname statt CLR-Identität; AQN höchstens als Fallback für Altbestand.

```csharp
[AttributeUsage(AttributeTargets.Class)]
public sealed class EventNameAttribute(string name) : Attribute
{
    public string Name { get; } = name;
}
```

Nebeneffekt: Event-Versionierung wird überhaupt erst ausdrückbar (`-v2` neben `-v1`).

---

# 2, CLR-Typname im Event-Stream-Key — **gelöst (2026-08-03)**

Siehe [TODO-04](todo.md) und [ADR-0030](docs/architecture/decisions/0030-persisted-names-and-aggregate-version.md).
`[AggregateName]` ist Pflicht und wirft bei Fehlen; der Name dient zugleich als Stream-Präfix und
als `AggregateName` auf dem Envelope.


Der Stream-Key wird als `$"{aggregateType.Name}/{keyValue}"` gebildet. Ein Rename von
`Gadget` nach `Device` verwaist sämtliche existierenden Streams — im Event Store, wo es per
Definition kein „einfach neu aufbauen" gibt. Gleiche Klasse wie Nr. 1, mit schlimmerer
Konsequenz.

`BuildingBlocks/src/BuildingBlocks.Infrastructure/Persistence/EntityKeyFormatter.cs:20`

## Lösungsvorschlag

Explizites, vom Klassennamen entkoppeltes Stream-Präfix am Aggregat deklarieren.

```csharp
[AttributeUsage(AttributeTargets.Class)]
public sealed class StreamPrefixAttribute(string prefix) : Attribute
{
    public string Prefix { get; } = prefix;
}

private static string PrefixOf(Type aggregateType) =>
    aggregateType.GetCustomAttribute<StreamPrefixAttribute>()?.Prefix
    ?? throw new InvalidOperationException(
        $"'{aggregateType}' braucht ein [StreamPrefix]; der Klassenname ist kein Persistenz-Contract.");
```

Prefix pro Aggregattyp cachen wie heute die Value-Accessors. Das Werfen ist Absicht: der
Contract soll bewusst gesetzt werden, nicht aus dem Refactoring-Zufall entstehen.

---

# 3, `FailureResults` sucht statische Methode per Name

`responseType.GetMethod("Failure", [typeof(Failure)])` plus Expression-Compile und Cache,
abgesichert durch eine Runtime-`InvalidOperationException`. Reiner Workaround dafür, dass
`Result` und `Result<T>` keine gemeinsame Factory-Abstraktion haben.

`BuildingBlocks/src/BuildingBlocks.Infrastructure/Dispatching/FailureResults.cs:39-45`

## Lösungsvorschlag

Static abstract interface member (C# 11) — der Compiler erledigt es, die Klasse entfällt
komplett.

```csharp
public interface IFailureResult<out TSelf>
{
    static abstract TSelf Failure(Failure failure);
}

public class Result : IFailureResult<Result> { }
public sealed class Result<TResult> : Result, IFailureResult<Result<TResult>> { }

return TResponse.Failure(Failure.Validation(ValidationFailureCode, exception.Message));
```

Betrifft `ExceptionToResultBehavior` und `UnitOfWorkBehavior` (Constraint erweitern), sonst
nichts. Aus einem Laufzeitfehler wird ein Compile-Fehler.

---

# 4, `ApplyEntityKeyConversions` scannt CLR- statt Model-Properties

Der Scan läuft über `clrType.GetProperties(...)` und ruft dann
`modelBuilder.Entity(clrType).Property(name)`. Dieser Aufruf _legt die Property im Modell an_,
falls sie dort noch nicht existiert. Jede berechnete, get-only oder explizit `Ignore()`-te
Property vom Key-Typ landet damit still als Spalte in der Tabelle. Der CLR-Scan ist mit dem
Primärschlüssel begründet — der Nebeneffekt trifft aber alles.

`BuildingBlocks/src/BuildingBlocks.Infrastructure/Persistence/EntityKeyValueConverter.cs:74-103`

## Lösungsvorschlag

Nur konvertieren, was EF bereits kennt — plus gezielt die Key-Properties, wegen derer der
CLR-Scan überhaupt existiert.

```csharp
var mutable = entityType as IConventionEntityType ?? (IConventionEntityType)entityType;

var candidates = entityType.GetProperties()
    .Concat(entityType.GetKeys().SelectMany(key => key.Properties))
    .DistinctBy(property => property.Name);

foreach (var property in candidates)
{
    if (KeyInterfaceOf(property.ClrType) is not { } keyInterface) continue;
    if (property.GetValueConverter() is not null) continue;
    modelBuilder.Entity(clrType).Property(property.Name).HasConversion(ConverterFor(keyInterface));
}
```

Zusätzlich ein Test, der eine `Ignore()`-te Key-Property im Modell nachweislich ignoriert
lässt.

---

# 5, Kein `Id.IsEmpty`-Guard in `AddAsync`

**Gelöst (2026-08-03): der Guard steht jetzt in beiden `AddAsync`-Implementierungen** und wirft
bei leerer Identität eine `InvalidOperationException`, abgesichert durch
`RepositoryEmptyIdentityGuardTests`. Die erste Teillösung (2026-08-03, Rekonstitutions-Amendment)
hatte bereits den bequemen Weg geschlossen; siehe unten.

Die Domäne bewacht Leer-Identität an zwei Stellen (`RaiseEvent`, `IStateOwner.Restore`) — das
Repository ist die einzige Tür ohne Schloss. Ein Aggregat mit leerer Identität schreibt eine
Zeile mit `Guid.Empty` bzw. öffnet Stream `Gadget/00000000-...`.

Der bequeme Weg dorthin ist zu: `repository.AddAsync(new Widget())` kompiliert nicht mehr. Beide
Sample-Aggregate hatten einen **öffentlichen** parameterlosen Konstruktor, bei Marten durch die
`new()`-Constraint erzwungen — ADR-0025s „darf non-public sein" stimmte für event-sourced
Aggregate schlicht nicht. Seit dem Rekonstitutions-Amendment ist der Konstruktor überall privat
und die Hülle nur über `IReconstitutable<T>.CreateEmpty()` erreichbar, also ausschließlich aus
generischem, constraint-gebundenem Code heraus.

Der Guard bleibt trotzdem nötig — er schließt genau diese Restlücke, und die Kosten sind null.

`BuildingBlocks/src/BuildingBlocks.Infrastructure/Persistence/EfCoreRepository.cs:55`,
`BuildingBlocks/src/BuildingBlocks.Infrastructure/Persistence/MartenEventSourcedRepository.cs:48`,
`samples/StateStored/VitalSync.Sample.StateStored.Domain/Widget.cs:5`

## Lösungsvorschlag

Guard in beiden `AddAsync`-Implementierungen; die Kosten sind null, der Fehler wäre sonst
eine Datenkorruption.

```csharp
public Task AddAsync(TAggregate aggregate, CancellationToken cancellationToken)
{
    ArgumentNullException.ThrowIfNull(aggregate);

    if (aggregate.Id.IsEmpty)
    {
        throw new InvalidOperationException(
            $"'{typeof(TAggregate)}' hat keine Identität. Ein Aggregat erhält sie durch sein erstes " +
            "Event — der parameterlose Konstruktor dient nur der Rehydrierung.");
    }
}
```

Der zweite Teil — der erzwungen-öffentliche Konstruktor — ist erledigt, aber anders als hier
vorgeschlagen: nicht `Activator` überall, sondern `IReconstitutable<TSelf>` mit
`static abstract CreateEmpty()`, explizit implementiert. Weder Reflection noch `new()`, und die
Anforderung ist zur Compile-Zeit ausdrückbar. Siehe **TODO-10** und das
Rekonstitutions-Amendment von ADR-0025.

**Nachtrag (2026-08-04):** Kehrtwende — dieser Punkt hatte doch recht. `IReconstitutable` ist
gelöscht; die Hülle kommt jetzt aus einer internen, pro Typ gecachten `AggregateFactory`
(Reflection auf den privaten parameterlosen Konstruktor), validiert beim Startup über die
`AddDomainEventsFrom`-Assemblies. Die hier beschriebene „Restlücke" (constraint-gebundener
generischer Code) ist damit ebenfalls weg; der Guard bleibt. Siehe ADR-0025-Amendment 2026-08-04.

---

# 6, `CurrentValues.SetValues` kopiert nur Skalare

`SetValues` traversiert keine Navigationen und keine Owned Types. Sobald ein State-Record eine
Kindkollektion bekommt (Recipe mit Ingredients — also das erste echte Aggregat), verschwinden
Änderungen daran spurlos: kein Fehler, kein Log, die Zeile wird nicht geschrieben. Die Samples
sind flach, also fängt es kein Test. ADR-0025/0026 sagen „State mappt als gewöhnlicher
Entity-Typ" — diese Zusage hält der Commit-Pfad heute nur für flache States.

Das ist die Fundstelle mit dem größten Schaden pro Wahrscheinlichkeit, sobald die Domänenarbeit
beginnt.

`BuildingBlocks/src/BuildingBlocks.Infrastructure/Persistence/EfCoreUnitOfWork.cs:47`

## Lösungsvorschlag

Kurzfristig ehrlich abriegeln, statt still falsch zu speichern:

```csharp
var entry = outbox.DbContext.Entry(entry.PersistedState);

if (entry.Navigations.Any() || entry.References.Any(r => r.TargetEntry?.Metadata.IsOwned() == true))
{
    throw new NotSupportedException(
        $"Der State '{entry.Metadata.ClrType}' hat Navigationen/Owned Types. SetValues kopiert nur " +
        "Skalare, Änderungen daran gingen verloren.");
}

entry.CurrentValues.SetValues(entry.StateOwner.State);
```

Mittelfristig der eigentliche Fix: den geladenen State-Graph ersetzen statt patchen — die alte
Instanz detachen und den neuen State als geänderten Graph attachen, oder den Fold direkt auf
der getrackten Instanz materialisieren. Verdient eine eigene ADR (Amendment zu 0025/0026), weil
es die Kernzusage der State-Mapping-Entscheidung betrifft.

---

# 7, `DomainEventStamper` erkennt „unstamped" über Sentinel — **gelöst (2026-08-03)**

> Gelöst durch die erste Variante des Vorschlags unten, per
> [ADR-0029](docs/architecture/decisions/0029-event-identity-placement.md) (TODO-13):
> `EventId`/`OccurredAt` liegen jetzt auf dem `DomainEventEnvelope`, die Events selbst sind
> reine Wert-Records, und der `DomainEventStamper` ist gelöscht — es gibt kein „unstamped" mehr.

`domainEvent is DomainEvent { OccurredAt.Ticks: 0 }`. Zwei Annahmen in einem Ausdruck: dass
`Ticks == 0` nie ein echter Wert ist, und dass jedes Event vom `DomainEvent`-Record erbt. Wer
`IDomainEvent` direkt implementiert, bekommt still für immer `default(DateTimeOffset)`.

`BuildingBlocks/src/BuildingBlocks.Infrastructure/Events/DomainEventStamper.cs:18`

## Lösungsvorschlag

Zeitstempel aus dem Event heraus in den Envelope ziehen — dann gibt es kein „unstamped" mehr:

```csharp
public sealed record DomainEventEnvelope(string EventTypeName, string Payload, DateTimeOffset OccurredAt);
```

Falls `OccurredAt` am Event bleiben soll, wenigstens die zweite Annahme schließen: `IDomainEvent`
um `IDomainEvent WithOccurredAt(DateTimeOffset)` erweitern, in `DomainEvent` einmal
implementieren. Dann ist der Vertrag sichtbar statt durch einen Typtest geraten.

---

# 8, Connection String zweimal, ohne Abgleich — **gelöst (2026-08-03)**

> Nicht durch den Abgleich unten, sondern durch Wegfall der zweiten Nennung: Building Blocks setzt
> den `UseWolverine`-Aufruf seit dem zweiten ADR-0027-Amendment selbst ab
> (`builder.AddBuildingBlocks(…)` auf `IHostApplicationBuilder`) und nimmt den Connection String aus
> der bereits getroffenen `UseEfCorePersistence`-Auswahl. Details in [todo.md](todo.md), TODO-06.
> Der ursprüngliche Befund bleibt zur Nachvollziehbarkeit stehen.

`UseEfCorePersistence(cs)` legt `EfCoreMessageStoreConnectionString` ab und nutzt es
ausschließlich für ein `RequiresWolverine`-Bool. Der Host muss denselben String ein zweites Mal
an `UseBuildingBlocksEfCorePersistence(cs)` reichen. Dass die Duplizierung strukturell erzwungen
ist (Wolverine 3.0 verbietet der Extension den Zugriff auf die ServiceCollection), ist
nachvollziehbar — dass sie ungeprüft bleibt, nicht: zwei Tippfehler auseinander und die Outbox
sitzt in einer anderen Datenbank als die Aggregate. Die ADR-0022-Atomaritätsgarantie ist dann
still weg.

`samples/StateStored/VitalSync.Sample.StateStored.Api/Program.cs:10-19`,
`BuildingBlocks/src/BuildingBlocks.Infrastructure/DependencyInjection/BuildingBlocksOptions.cs:316`

## Lösungsvorschlag

Der Wert liegt in DI bereit — beim Start vergleichen. Billigster großer Gewinn der Liste.

```csharp
if (settings.EfCoreMessageStoreConnectionString is { } declared
    && appliedStore is { } applied
    && !string.Equals(Normalize(declared), Normalize(applied), StringComparison.Ordinal))
{
    throw new InvalidOperationException(
        "UseEfCorePersistence und UseBuildingBlocksEfCorePersistence haben unterschiedliche " +
        "Write-Datenbanken. Outbox und Aggregate müssen in einer Transaktion liegen (ADR-0022).");
}
```

`Normalize` über `NpgsqlConnectionStringBuilder`, damit Formatierungsunterschiede nicht
fälschlich anschlagen.

---

# 9, `AddBuildingBlocks` ist nicht idempotent

`services.TryAddSingleton(behaviorRegistry)` behält beim zweiten Aufruf die _erste_ Registry —
`options` bekommt aber die _zweite_. Host-eigene Behaviors aus dem zweiten Aufruf schreiben ihre
Order in eine verwaiste Instanz und laufen zur Laufzeit auf Order 0. Dazu passt: `GetOrder` gibt
für Unbekanntes `0` zurück, exakt `LoggingBehaviorOrder` — ein direkt auf der
`IServiceCollection` registriertes Behavior kollidiert lautlos mit dem Logging.

`BuildingBlocks/src/BuildingBlocks.Infrastructure/DependencyInjection/ServiceCollectionExtensions.cs:54-58`,
`BuildingBlocks/src/BuildingBlocks.Infrastructure/Dispatching/PipelineBehaviorRegistry.cs:40`

## Lösungsvorschlag

Immer die Registry benutzen, die tatsächlich registriert ist, und Unbekanntes nicht auf einen
belegten Wert fallen lassen.

```csharp
var behaviorRegistry = (PipelineBehaviorRegistry?)services
    .FirstOrDefault(d => d.ServiceType == typeof(PipelineBehaviorRegistry))?.ImplementationInstance
    ?? new PipelineBehaviorRegistry();
services.TryAddSingleton(behaviorRegistry);

public int GetOrder(Type closedBehaviorType) =>
    _orders.TryGetValue(Definition(closedBehaviorType), out var order)
        ? order
        : throw new InvalidOperationException(
            $"Behavior '{closedBehaviorType}' hat keine Order. Über BuildingBlocksOptions." +
            "AddPipelineBehavior registrieren, nicht direkt auf der IServiceCollection.");
```

Dazu ein Test, der `AddBuildingBlocks` zweimal aufruft und die Order des zweiten Behaviors prüft.

---

# 10, `RuleChecker` schluckt `null`

`rule?.IsBroken() == true` und `foreach (var rule in rules ?? [])`. Eine Factory, die
versehentlich `null` liefert, bedeutet „Regel bestanden". Begründet ist das mit „damit
Guard-Klauseln knapp bleiben" — der Preis ist, dass die Validierung genau im Fehlerfall schweigt.

`BuildingBlocks/src/BuildingBlocks.Domain/RuleChecker.cs:18-63`

## Lösungsvorschlag

Null ist kein Zustand einer Regel, sondern ein Bug.

```csharp
public static void Check(IBusinessRule rule)
{
    ArgumentNullException.ThrowIfNull(rule);

    if (rule.IsBroken())
    {
        throw new BusinessRuleViolationException(rule.Message);
    }
}

public static void Check(params IBusinessRule[] rules)
{
    ArgumentNullException.ThrowIfNull(rules);

    foreach (var rule in rules)
    {
        Check(rule);
    }
}
```

Analog für `IDomainValidationRule`. Bestehende Tests, die auf die Null-Toleranz zeigen,
mitziehen.

---

# 11, Optionale Constructor-Injection für `IUnitOfWork`

`UnitOfWorkBehavior<TRequest, TResponse>(IUnitOfWork? unitOfWork = null)` — funktioniert mit
MS.DI, aber „kein UoW registriert ⇒ Command committet still nicht" ist nur durch ein
`Information`-Log beim Start abgesichert. Der Default entscheidet, nicht der Entwickler.

`BuildingBlocks/src/BuildingBlocks.Infrastructure/Dispatching/UnitOfWorkBehavior.cs:27,49`

## Lösungsvorschlag

Aus dem stillen Default eine ausdrückliche Wahl machen: immer ein `IUnitOfWork` registrieren,
im persistenzlosen Fall eben ein leeres.

```csharp
internal sealed class NullUnitOfWork : IUnitOfWork
{
    public Task CommitAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}

if (!services.Any(d => d.ServiceType == typeof(IUnitOfWork)))
{
    services.AddScoped<IUnitOfWork, NullUnitOfWork>();
}
```

Das Behavior nimmt dann `IUnitOfWork` ohne `?` und ohne Default; der Null-Check im `Handle`
entfällt.

---

# 12, Nur `Failures[0]` erreicht den Client

Der gRPC-Adapter mappt ausschließlich den ersten Fehler auf einen Status-Code, obwohl das ganze
`Result`-Modell auf mehrere Failures ausgelegt ist (`Result.Failure(IReadOnlyList<Failure>)`).
Alles Weitere fällt still weg.

`samples/StateStored/VitalSync.Sample.StateStored.Api/WidgetGrpcService.cs:62`

## Lösungsvorschlag

Status-Code aus dem ersten Failure ableiten (eine Antwort hat einen Code), aber alle Failures
im Trailer transportieren.

```csharp
private static RpcException ToRpcException(Result result)
{
    var status = StatusCodeFor(result.Failures[0].Category);
    var metadata = new Metadata();

    foreach (var failure in result.Failures)
    {
        metadata.Add("failure-code", failure.Code);
        metadata.Add("failure-message", failure.Message);
    }

    return new RpcException(
        new Status(status, string.Join("; ", result.Failures.Select(f => $"{f.Code}: {f.Message}"))),
        metadata);
}
```

Alternativ die Mehrfach-Failures aus `Result` streichen, falls sie ohnehin nie entstehen — dann
ist die API ehrlich. Entscheidung gehört in eine ADR, sobald der BFF echte Fehler durchreicht.

---

# 13, Global sequentielle Domain-Event-Queue

`options.PublishMessage<DomainEventEnvelope>().ToLocalQueue(...).Sequential()` serialisiert
_sämtliche_ Domain Events des gesamten Service über eine Queue, um eine _pro-Aggregat_-Garantie
zu kaufen (ADR-0022). Global serialisieren für eine lokale Zusage — kein Fehler, aber eine
Durchsatzobergrenze von einem Event zur Zeit pro Service.

`BuildingBlocks/src/BuildingBlocks.Infrastructure/Messaging/WolverineOptionsExtensions.cs:77-80`

## Lösungsvorschlag

Nach Aggregat-Id partitionieren statt global zu serialisieren: gleiche Ordnungsgarantie, aber
parallel über verschiedene Aggregate. Voraussetzung ist, dass der Envelope die Aggregat-Identität
mitführt (heute tut er das nicht — siehe Nr. 1/7, dieselbe Stelle):

```csharp
public sealed record DomainEventEnvelope(string EventTypeName, string Payload, string AggregateKey);
```

Vor der ersten Lastmessung nicht anfassen — steht sinngemäß als offener Punkt in
`WalkingSkeleton.md`. Hier festgehalten, damit die Entscheidung bewusst getroffen wird und
nicht als Default stehen bleibt.
