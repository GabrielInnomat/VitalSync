# Improvements — BuildingBlocks

Befunde aus der Code-Analyse von `BuildingBlocks/src/BuildingBlocks.{Domain,Application,Infrastructure}`.

**Dieses Dokument führt nur noch offene Befunde.** Gelöste wurden am 2026-08-09 entfernt; die
Begründungen leben in den ADRs unter `docs/architecture/decisions/`, in den Instruktionsdateien
und in den Tests weiter. Die Versionsgeschichte hat den vollen Wortlaut.

Jeder Punkt ist in [todo.md](todo.md) mit einer Priorität geführt; dort steht auch, was ihn
auslöst. Überschneidungen mit [hacky.md](hacky.md) sind beim jeweiligen Punkt vermerkt.

## Status

| Nr.    | Titel                                                          | Status    | TODO    |
| ------ | -------------------------------------------------------------- | --------- | ------- |
| IMP-12 | `IIntegrationEventMapper` ist untypisiert                      | offen     | TODO-30 |
| IMP-19 | Ein Assembly für EF Core, Marten, Wolverine und RabbitMQ       | offen     | TODO-33 |
| IMP-25 | `Sequential()` auf einer einzigen Queue für alle Domain Events | offen     | TODO-20 |
| IMP-32 | Keine Batch- oder Bulk-Fähigkeit                               | offen     | TODO-38 |
| IMP-33 | Keine Saga- oder Process-Manager-Abstraktion                   | offen     | TODO-39 |
| IMP-34 | `Result` hat keine Kombinatoren                                | offen     | TODO-31 |
| IMP-39 | `Result`-API: implizite Konvertierungen und werfendes `Value`  | teilweise | TODO-31 |
| IMP-43 | Wirkungslose Varianz-Modifikatoren                             | offen     | TODO-41 |
| IMP-47 | Keine zentrale Paketverwaltung                                 | offen     | TODO-34 |

---

# IMP-12, `IIntegrationEventMapper` ist untypisiert

```csharp
IReadOnlyCollection<IIntegrationEvent> Map(IDomainEvent domainEvent);
```

Jeder Mapper wird für **jedes** Domain Event aufgerufen und muss selbst per `switch` filtern — während
das benachbarte `IProjectionHandler<in TDomainEvent>` typisiert ist und vom `ProjectionRunner` gezielt
aufgelöst wird. Zwei funktional analoge Konzepte, gegensätzlich entworfen.

[IIntegrationEventMapper.cs](BuildingBlocks/src/BuildingBlocks.Application/IntegrationEvents/IIntegrationEventMapper.cs),
Aufruf in [DomainEventPublisher.cs:34-40](BuildingBlocks/src/BuildingBlocks.Infrastructure/Messaging/DomainEvents/DomainEventPublisher.cs:34).

## Lösungsvorschlag

Symmetrisch zum Projektionshandler typisieren und wie dort über einen gecachten Invoker auflösen:

```csharp
public interface IIntegrationEventMapper<in TDomainEvent>
    where TDomainEvent : IDomainEvent
{
    IReadOnlyCollection<IIntegrationEvent> Map(TDomainEvent domainEvent);
}
```

`AddHandlersFrom` registriert sie dann über denselben `MultiHandlerInterfaceDefinitions`-Pfad wie
`IProjectionHandler<>`; im `DomainEventPublisher` ersetzt ein `MapperRunner` (Zwilling des `ProjectionRunner`)
die Schleife über alle Mapper. Nebeneffekt: der `_ => []`-Default-Arm in jedem Mapper entfällt, und
„welche Events verlassen diesen Kontext" wird an der Typsignatur ablesbar statt im `switch` versteckt.

---

# IMP-19, Ein Assembly für EF Core, Marten, Wolverine und RabbitMQ

`BuildingBlocks.Infrastructure` referenziert unverändert 11 Pakete, darunter Marten, EF Core, Npgsql
und fünf WolverineFx-Pakete
([BuildingBlocks.Infrastructure.csproj](BuildingBlocks/src/BuildingBlocks.Infrastructure/BuildingBlocks.Infrastructure.csproj)).
Ein rein state-stored Service zieht Marten mit, ein event-sourced Service EF Core — und jedes
Major-Upgrade eines der Pakete betrifft alle Services gleichzeitig.

## Lösungsvorschlag

Aufteilen entlang der Abhängigkeitsgrenzen, sobald der zweite echte Service existiert:

```
BuildingBlocks.Infrastructure            → Dispatching, Events, DI-Kern (keine Store-Pakete)
BuildingBlocks.Infrastructure.EfCore     → EfCoreRepository/UnitOfWork/Tracker, EntityKey-Konverter
BuildingBlocks.Infrastructure.Marten     → Marten-Repository/UnitOfWork/Tracker, EntityKeyFormatter
BuildingBlocks.Infrastructure.Wolverine  → Envelope, Handler, Sink, Wolverine-Extensions
```

Der Schnitt ist heute schon durch die Ordnerstruktur vorgezeichnet und wäre eine reine
Projektverschiebung. Vorher lohnt er nicht — mit einem Sample-Paar ist der Aufwand höher als der
Gewinn. Der Auslöser für die Umsetzung: der erste Produktions-Service, der nur eine der beiden
Persistenzwelten braucht.

---

# IMP-25, `Sequential()` auf einer einzigen Queue für alle Domain Events

Unverändert offen — identisch mit [hacky.md Nr. 13](hacky.md).

```csharp
options.PublishMessage<DomainEventEnvelope>()
    .ToLocalQueue(DomainEventLocalQueueName).Sequential().UseDurableInbox();
```

([WolverineOptionsExtensions.cs:77-80](BuildingBlocks/src/BuildingBlocks.Infrastructure/DependencyInjection/Wiring/WolverineOptionsExtensions.cs:77))

Sämtliche Domain Events eines Service laufen durch eine strikt sequentielle Queue, um eine
**pro-Aggregat**-Ordnungsgarantie zu erkaufen. Global serialisieren für eine lokale Zusage: der
Durchsatz eines Service ist damit auf ein Event zur Zeit gedeckelt.

## Lösungsvorschlag

Nach Aggregat-Id partitionieren — gleiche Garantie, parallel über verschiedene Aggregate. Der
`DomainEventEnvelope` führt `AggregateName`/`AggregateId` seit ADR-0030 mit, die Voraussetzung
dafür ist also erfüllt.

Vor der ersten Lastmessung nicht anfassen. Hier festgehalten, damit die Entscheidung bewusst fällt und
nicht als Default stehen bleibt.

---

# IMP-32, Keine Batch- oder Bulk-Fähigkeit

# IMP-33, Keine Saga- oder Process-Manager-Abstraktion

Abgedeckt sind: eingehender Command, eingehende Query, eingetroffenes Event. Nicht abgedeckt: alles mit
Zustand über Zeit und Zeitsteuerung — „erinnere nach 3 Tagen ohne Eintrag", „warte, bis Nutrition
**und** Fitness gemeldet haben". Der letzte Fall ist für Analytics absehbar relevant.

## Lösungsvorschlag

Nicht selbst bauen — Wolverine bringt Sagas und `ScheduleAsync` bereits mit, und beides läuft über
dieselbe durable Message-Infrastruktur, die hier ohnehin schon konfiguriert ist:

```csharp
public sealed class DailyAnalyticsSaga : Saga
{
    public string Id { get; init; }
    public bool NutritionReported { get; set; }
    public bool FitnessReported { get; set; }

    public void Handle(NutritionDayClosed e) { NutritionReported = true; }
    public void Handle(FitnessDayClosed e)   { FitnessReported = true; }
}
```

Die Entscheidung, die eine ADR braucht: ob Wolverine damit vom reinen Transport (ADR-0015/0023) zum
Prozess-Host aufgewertet wird. Das ist eine bewusste Aufweichung der bisherigen Abgrenzung — vor der
ersten Saga klären, nicht danach.

---

# IMP-34, `Result` hat keine Kombinatoren

Kein `Map`, `Bind`, `Match`, `Tap`, `Ensure`. Jeder mehrstufige Handler schreibt dieselbe
`if (x is null) return Failure...`-Treppe.

## Lösungsvorschlag

Sparsam beginnen — drei Kombinatoren decken den Großteil ab, ohne den Handler-Code in eine
Fluent-Kette zu zwingen:

```csharp
public static Result<TOut> Map<TIn, TOut>(this Result<TIn> result, Func<TIn, TOut> map) =>
    result.IsSuccess ? Result.Success(map(result.Value)) : Result<TOut>.Failure(result.Failures);

public static async Task<Result<TOut>> Bind<TIn, TOut>(
    this Result<TIn> result, Func<TIn, Task<Result<TOut>>> bind) =>
    result.IsSuccess ? await bind(result.Value) : Result<TOut>.Failure(result.Failures);

public static TOut Match<TIn, TOut>(
    this Result<TIn> result, Func<TIn, TOut> onSuccess, Func<IReadOnlyList<Failure>, TOut> onFailure) =>
    result.IsSuccess ? onSuccess(result.Value) : onFailure(result.Failures);
```

`Match` ist der wertvollste — er ersetzt die `IsSuccess ? … : throw ToRpcException(…)`-Zeilen in jedem
gRPC-Adapter. Als Extensions in `BuildingBlocks.Application`, damit `Result` selbst schlank bleibt.

---

# IMP-39, `Result`-API: implizite Konvertierungen und werfendes `Value`

**Teilweise gelöst (2026-08-05).** Die Namenskollision ist weg: die Factory heißt `Failed(...)`
([ADR-0017-Amendment](docs/architecture/decisions/0017-application-error-handling-and-result.md)),
und die Reflection darüber ist mit ihr entfallen
([ADR-0015-Amendment](docs/architecture/decisions/0015-hand-rolled-cqrs-mediator.md)). Das
`static new` in `ResultOfT` besteht fort, ist jetzt aber folgenlos.

**Offen bleiben zwei Punkte:**

1. **Zwei implizite Konvertierungen** auf `Result<TResult>` (aus `TResult` und aus `Failure`) — für
   `TResult = Failure` wären sie mehrdeutig; heute nur theoretisch, aber unbewacht.
2. **`Value` wirft** bei einem fehlgeschlagenen Result, statt den Fehler im Typsystem sichtbar zu
   machen.

[Result.cs](BuildingBlocks/src/BuildingBlocks.Application/Results/Result.cs),
[ResultOfT.cs](BuildingBlocks/src/BuildingBlocks.Application/Results/ResultOfT.cs)

## Lösungsvorschlag

Punkt 1 mit einem Test absichern statt umbauen. Punkt 2 durch `Match` entschärfen, ohne `Value` zu
entfernen — das ist dieselbe Arbeit wie IMP-34, und beide gehören deshalb in TODO-31.

# IMP-43, Wirkungslose Varianz-Modifikatoren

Verifiziert unverändert: `IEntity<out TKey>`, `IAggregateRoot<out TKey>`,
`IEventSourcedAggregateRoot<out TKey>`, `IState<TSelf, out TKey>` und
`IRepository<TAggregate, in TKey>` — alle mit `where TKey : struct, IEntityKey`. Varianz gilt nur für
Referenztypen; bei einer `struct`-Constraint ist der Modifikator wirkungslos. Er suggeriert eine
Flexibilität, die es nicht gibt.

## Lösungsvorschlag

Ersatzlos streichen:

```csharp
public interface IEntity<TKey> where TKey : struct, IEntityKey
```

Rein mechanisch, kein Verhaltensunterschied, kein Breaking Change für Aufrufer — der Compiler
akzeptiert exakt dieselben Verwendungen. Gute Aufräumarbeit für den nächsten Durchgang durch die
Domain-Verträge.

---

# IMP-47, Keine zentrale Paketverwaltung

Verifiziert: kein `Directory.Packages.props` im Repository. Versionen stehen einzeln in den
`.csproj`-Dateien; `xunit.v3` etwa ist an sechs Stellen mit `3.2.2` gepflegt. Ein
Upgrade erfordert, jede Datei zu finden — und ein übersehenes Projekt erzeugt eine
Laufzeit-Bindungsdiskrepanz statt eines Build-Fehlers.

## Lösungsvorschlag

```xml
<Project>
  <PropertyGroup>
    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
  </PropertyGroup>
  <ItemGroup>
    <PackageVersion Include="Marten" Version="9.20.1" />
    <PackageVersion Include="WolverineFx.RabbitMQ" Version="6.23.0" />
  </ItemGroup>
</Project>
```

In den `.csproj` entfällt dann jedes `Version="…"`. Rein mechanisch, keine Verhaltensänderung, und mit
aktuell 34 Projekten schon spürbar. Guter Kandidat für den nächsten Aufräum-Commit.
