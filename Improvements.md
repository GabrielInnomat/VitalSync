# Improvements — BuildingBlocks

Ergebnis der Code-Analyse von `BuildingBlocks/src/BuildingBlocks.{Domain,Application,Infrastructure}`.
Basis: ausschließlich der Implementierungscode (ohne XML-Doku, ohne `/docs`, ohne ADRs).

> **Kontext:** Zum Analysezeitpunkt referenziert kein Projekt unter `src/` die Building Blocks.
> Der Code wurde nie end-to-end ausgeführt. Alle Befunde sind statisch hergeleitet;
> die als _(zu verifizieren)_ markierten Punkte brauchen einen Integrationstest zur Bestätigung.

## Index

| Nr.                                                                                          | Titel                                                                      | Prio     |
| -------------------------------------------------------------------------------------------- | -------------------------------------------------------------------------- | -------- |
| [IMP-01](#imp-01---occurredat-wird-im-state-stored-pfad-nie-gesetzt)                         | `OccurredAt` wird im state-stored Pfad nie gesetzt                         | Kritisch |
| [IMP-02](#imp-02---kein-iunitofwork-bei-gemischtem-efmarten-kontext)                         | Kein `IUnitOfWork` bei gemischtem EF/Marten-Kontext                        | Kritisch |
| [IMP-03](#imp-03---erwartete-domänenfehler-werden-als-error-geloggt)                         | Erwartete Domänenfehler werden als Error geloggt                           | Kritisch |
| [IMP-04](#imp-04---integrations-event-transport-nutzt-imessagebus-statt-imessagecontext)     | Integrations-Event-Transport nutzt `IMessageBus` statt `IMessageContext`   | Kritisch |
| [IMP-05](#imp-05---addhandlersfrom-erzeugt-duplikate-und-überschreibt-still)                 | `AddHandlersFrom` erzeugt Duplikate und überschreibt still                 | Kritisch |
| [IMP-06](#imp-06---sender-cache-ist-nur-nach-request-typ-gekeyed)                            | `Sender`-Cache ist nur nach Request-Typ gekeyed                            | Kritisch |
| [IMP-07](#imp-07---unitofworkbehavior-wirft-ohne-konfigurierte-persistenz)                   | `UnitOfWorkBehavior` wirft ohne konfigurierte Persistenz                   | Kritisch |
| [IMP-08](#imp-08---martenunitofwork-flusht-die-outbox-nicht)                                 | `MartenUnitOfWork` flusht die Outbox nicht                                 | Kritisch |
| [IMP-09](#imp-09---kein-testprojekt-für-buildingblocksinfrastructure)                        | Kein Testprojekt für `BuildingBlocks.Infrastructure`                       | Kritisch |
| [IMP-10](#imp-10---zwei-inkompatible-aggregat-programmiermodelle)                            | Zwei inkompatible Aggregat-Programmiermodelle                              | Hoch     |
| [IMP-11](#imp-11---iintegrationevent-ist-ein-leerer-marker)                                  | `IIntegrationEvent` ist ein leerer Marker                                  | Hoch     |
| [IMP-12](#imp-12---iintegrationeventmapper-ist-untypisiert)                                  | `IIntegrationEventMapper` ist untypisiert                                  | Hoch     |
| [IMP-13](#imp-13---messaging-konfiguration-ohne-guard-rails-stiller-datenverlust)            | Messaging-Konfiguration ohne Guard-Rails, stiller Datenverlust             | Hoch     |
| [IMP-14](#imp-14---constraint-mismatch-zwischen-ieventsourcedrepository-und-implementierung) | Constraint-Mismatch zwischen `IEventSourcedRepository` und Implementierung | Hoch     |
| [IMP-15](#imp-15---efcorerepository-lädt-aggregate-unvollständig-und-ist-sealed)             | `EfCoreRepository` lädt Aggregate unvollständig und ist `sealed`           | Hoch     |
| [IMP-16](#imp-16---kein-validierungs-behavior-mehrfachfehler-nicht-erzeugbar)                | Kein Validierungs-Behavior, Mehrfachfehler nicht erzeugbar                 | Hoch     |
| [IMP-17](#imp-17---failure-ohne-zielfeld-und-ohne-fachliche-fehlercodes)                     | `Failure` ohne Zielfeld und ohne fachliche Fehlercodes                     | Hoch     |
| [IMP-18](#imp-18---failurecategory-fehlen-autorisierung-und-unerwartet)                      | `FailureCategory` fehlen Autorisierung und Unerwartet                      | Hoch     |
| [IMP-19](#imp-19---ein-assembly-für-ef-core-marten-wolverine-und-rabbitmq)                   | Ein Assembly für EF Core, Marten, Wolverine und RabbitMQ                   | Hoch     |
| [IMP-20](#imp-20---dbcontext-als-di-schlüssel-kollidiert-mit-dem-readwrite-paar)             | `DbContext` als DI-Schlüssel kollidiert mit dem Read/Write-Paar            | Hoch     |
| [IMP-21](#imp-21---irepository-koppelt-an-die-konkrete-domain-basisklasse)                   | `IRepository` koppelt an die konkrete Domain-Basisklasse                   | Hoch     |
| [IMP-22](#imp-22---assemblyqualifiedname-als-event-typ-token)                                | `AssemblyQualifiedName` als Event-Typ-Token                                | Mittel   |
| [IMP-23](#imp-23---marten-stream-key-hängt-am-klassennamen)                                  | Marten-Stream-Key hängt am Klassennamen                                    | Mittel   |
| [IMP-24](#imp-24---domaineventenvelope-trägt-zu-wenig-metadaten)                             | `DomainEventEnvelope` trägt zu wenig Metadaten                             | Mittel   |
| [IMP-25](#imp-25---sequential-auf-einer-einzigen-queue-für-alle-domain-events)               | `Sequential()` auf einer einzigen Queue für alle Domain Events             | Mittel   |
| [IMP-26](#imp-26---publisher-koppelt-projektion-und-integration-event-publikation)           | `Publisher` koppelt Projektion und Integration-Event-Publikation           | Mittel   |
| [IMP-27](#imp-27---failureresults-reflection-ist-vermeidbar)                                 | `FailureResults`-Reflection ist vermeidbar                                 | Mittel   |
| [IMP-28](#imp-28---kein-iclock-im-container)                                                 | Kein `IClock` im Container                                                 | Mittel   |
| [IMP-29](#imp-29---unique-constraint-verletzungen-werden-nicht-übersetzt)                    | Unique-Constraint-Verletzungen werden nicht übersetzt                      | Mittel   |
| [IMP-30](#imp-30---keine-tracing-instrumentierung-der-cqrs-pipeline)                         | Keine Tracing-Instrumentierung der CQRS-Pipeline                           | Mittel   |
| [IMP-31](#imp-31---read-modelle-im-state-stored-pfad-sind-nicht-wiederaufbaubar)             | Read-Modelle im state-stored Pfad sind nicht wiederaufbaubar               | Mittel   |
| [IMP-32](#imp-32---keine-batch--oder-bulk-fähigkeit)                                         | Keine Batch- oder Bulk-Fähigkeit                                           | Mittel   |
| [IMP-33](#imp-33---keine-saga--oder-process-manager-abstraktion)                             | Keine Saga- oder Process-Manager-Abstraktion                               | Mittel   |
| [IMP-34](#imp-34---result-hat-keine-kombinatoren)                                            | `Result` hat keine Kombinatoren                                            | Mittel   |
| [IMP-35](#imp-35---statische-caches-über-container-grenzen-hinweg)                           | Statische Caches über Container-Grenzen hinweg                             | Mittel   |
| [IMP-36](#imp-36---rulechecker-schluckt-null-still)                                          | `RuleChecker` schluckt `null` still                                        | Mittel   |
| [IMP-37](#imp-37---async-suffix-ist-inkonsistent)                                            | Async-Suffix ist inkonsistent                                              | Niedrig  |
| [IMP-38](#imp-38---sichtbarkeits-disziplin-ist-uneinheitlich)                                | Sichtbarkeits-Disziplin ist uneinheitlich                                  | Niedrig  |
| [IMP-39](#imp-39---result-api-namenskollision-und-implizite-konvertierungen)                 | `Result`-API: Namenskollision und implizite Konvertierungen                | Niedrig  |
| [IMP-40](#imp-40---state-ist-public-und-bricht-die-kapselung)                                | `State` ist `public` und bricht die Kapselung                              | Niedrig  |
| [IMP-41](#imp-41---domainevent-als-record-mit-garantiert-ungleicher-wertgleichheit)          | `DomainEvent` als `record` mit garantiert ungleicher Wertgleichheit        | Niedrig  |
| [IMP-42](#imp-42---irepository-api-ist-asymmetrisch-und-irreführend-benannt)                 | `IRepository`-API ist asymmetrisch und irreführend benannt                 | Niedrig  |
| [IMP-43](#imp-43---wirkungslose-varianz-modifikatoren)                                       | Wirkungslose Varianz-Modifikatoren                                         | Niedrig  |
| [IMP-44](#imp-44---uneinheitliche-projektstruktur)                                           | Uneinheitliche Projektstruktur                                             | Niedrig  |
| [IMP-45](#imp-45---sendercontracttests-testet-nsubstitute-statt-produktionscode)             | `SenderContractTests` testet NSubstitute statt Produktionscode             | Niedrig  |
| [IMP-46](#imp-46---behaviors-nutzen-service-locator-statt-optionaler-abhängigkeiten)         | Behaviors nutzen Service Locator statt optionaler Abhängigkeiten           | Niedrig  |
| [IMP-47](#imp-47---keine-zentrale-paketverwaltung)                                           | Keine zentrale Paketverwaltung                                             | Niedrig  |
| [IMP-48](#imp-48---uneinheitliche-benennung-der-wolverine-extensions)                        | Uneinheitliche Benennung der Wolverine-Extensions                          | Niedrig  |

---

# IMP-01 - `OccurredAt` wird im state-stored Pfad nie gesetzt

- **Kritisch**
-   - **Status**: gelöst. Der `DomainEventStamper` wird jetzt in beiden Unit-of-Work-Implementierungen (`EfCoreUnitOfWork`, `MartenUnitOfWork`) aufgerufen: pro Transaktion wird ein einziger `IClock.Now`-Wert an alle Domain Events gestempelt, bevor sie in die Outbox geschrieben werden. Im Marten-Pfad werden die **gestempelten** Events an den Stream angehängt, sodass der Replay die echten Zeitstempel rehydriert. Schritt 3 (`RaiseEvent` ohne `IClock`) ist durch das vereinheitlichte Aggregatmodell (ADR-0025) bereits erfüllt. Abgesichert durch `DomainEventStamperTests`.

## Beschreibung

`DomainEvent` setzt im Konstruktor ausschließlich `EventId`. `OccurredAt` bleibt auf `default(DateTimeOffset)`, also `0001-01-01T00:00:00+00:00`.

Gestempelt wird der Zeitstempel nur an einer einzigen Stelle:

```csharp
// EventSourcedAggregateRoot.RaiseEvent
var stamped = Stamp(domainEvent, clock);
```

`AggregateRoot.AddDomainEvent` stempelt **nicht**. Damit trägt **jedes Domain Event eines state-stored Aggregats** einen leeren Zeitstempel.

Der Wert bleibt nicht im Aggregat: Er wird von `EfCoreUnitOfWork` über `DomainEventEnvelopeSerializer.Wrap` in die Outbox serialisiert, von `DomainEventEnvelopeHandler` wieder ausgepackt und an `Publisher` gegeben — und landet damit in Projektionen (Read-Modelle mit falschen Zeitstempeln) und potenziell in Integration Events, die andere Bounded Contexts konsumieren.

Der Domain-Test `OccurredAt_DefaultsToUnsetSoStampingCanDetectIt` zementiert das Default sogar explizit, ohne dass für den state-stored Pfad je ein Stempler existiert hätte. Das Problem ist damit im Test-Setup unsichtbar und fällt erst bei der ersten produktiven Auswertung auf („alle Einträge vom Jahr 1").

## Lösungsvorschlag

Das Stempeln gehört nicht ins Aggregat, sondern an die Transaktionsgrenze. Dort ist der fachlich korrekte Zeitpunkt bekannt (Commit-Zeitpunkt), er ist für alle Events einer Transaktion identisch, und beide Persistenzpfade können ihn teilen.

**Schritt 1 — Stempler in Infrastructure zentralisieren:**

```csharp
namespace BuildingBlocks.Infrastructure.Events;

internal static class DomainEventStamper
{
    public static IDomainEvent Stamp(IDomainEvent domainEvent, DateTimeOffset occurredAt) =>
        domainEvent is DomainEvent { OccurredAt.Ticks: 0 } record
            ? record with { OccurredAt = occurredAt }
            : domainEvent;
}
```

**Schritt 2 — in beiden Unit-of-Work-Implementierungen anwenden:**

```csharp
// EfCoreUnitOfWork / MartenUnitOfWork
var occurredAt = clock.Now;   // IClock injiziert, ein Zeitpunkt pro Transaktion

foreach (var domainEvent in aggregate.DomainEvents)
{
    var stamped = DomainEventStamper.Stamp(domainEvent, occurredAt);
    await outbox.PublishAsync(DomainEventEnvelopeSerializer.Wrap(stamped)).ConfigureAwait(false);
}
```

**Schritt 3 — `IClock` aus dem Domänen-API entfernen:**

```csharp
protected void RaiseEvent(IDomainEvent domainEvent)   // statt (IDomainEvent, IClock)
```

Das ist der eigentliche Gewinn: Aktuell muss jede Aggregat-Methode einen `IClock` durchreichen (`workout.Complete(clock)`), was das Domänen-API mit einem technischen Belang verschmutzt. Nach dieser Änderung braucht die Domäne `IClock` nur noch dort, wo Zeit **fachlich** relevant ist (z. B. „Trainingsdatum darf nicht in der Zukunft liegen") — und das ist eine bewusste, sichtbare Entscheidung statt Boilerplate.

**Achtung bei ES:** In `EventSourcedAggregateRoot` wird das Event beim `RaiseEvent` sofort auf den State angewendet. Wenn der State `OccurredAt` liest, verschiebt diese Änderung das Stempeln hinter den Fold. Das ist unkritisch, solange `IState.Apply` den Zeitstempel nicht auswertet — und das sollte es ohnehin nicht, weil dieselbe Logik beim Replay dann von persistierten Werten abhinge. Beim Replay aus dem Event Store ist `OccurredAt` korrekt gesetzt, weil Marten die gestempelte Fassung speichert. Diese Invariante ("`Apply` liest `OccurredAt` nicht") sollte im Test abgesichert werden.

**Abgrenzung:** Die Alternative — `AddDomainEvent(IDomainEvent, IClock)` symmetrisch zu `RaiseEvent` — löst den Bug ebenfalls, verschlimmert aber die API-Verschmutzung und lässt zwei Stempelstellen bestehen. Nicht empfohlen.

---

# IMP-02 - Kein `IUnitOfWork` bei gemischtem EF/Marten-Kontext

- **Kritisch**
-   - **Status**: gelöst.

## Beschreibung

`BuildingBlocksOptions` registriert die Unit of Work in beiden Persistenzvarianten mit `TryAddScoped`:

```csharp
// UseEfCorePersistence<TContext>
_services.TryAddScoped<IUnitOfWork, EfCoreUnitOfWork<TContext>>();

// UseMartenEventSourcing
_services.TryAddScoped<IUnitOfWork, MartenUnitOfWork>();
```

`TryAdd*` ist ein No-Op, wenn der Service-Typ bereits registriert ist. Ruft ein Service **beide** Methoden auf, gewinnt die erste — die zweite verschwindet **ohne Fehler, ohne Warnung, ohne Log**.

Die Folge: In einem Bounded Context mit einem event-sourced Aggregat _und_ state-stored Aggregaten committet `UnitOfWorkBehavior` nur eine der beiden Seiten. Die andere Seite verliert **alle** Änderungen und **alle** Domain Events — die Änderungen wurden nie geschrieben, aber der Command gibt `Result.Success()` zurück. Stiller Datenverlust bei erfolgreicher Antwort ist der schlechtestmögliche Fehlermodus.

Das wiegt besonders schwer, weil „Event Sourcing selektiv, nur wo es fachlichen Wert bringt" die zentrale Designthese des Projekts ist. Genau die dabei entstehende Mischung wird von der Infrastruktur nicht unterstützt — das Problem wird nicht gelöst, sondern versteckt.

## Lösungsvorschlag

**Entscheidung:** Ein Microservice hostet genau einen Bounded Context, und ein Bounded Context nutzt **genau eine** Persistenzstrategie — entweder state-stored (EF Core) oder event-sourced (Marten), niemals beides. Da beide Stores in getrennten Datenbanken liegen (ADR-0020), kann ein Commit sie ohnehin nicht atomar überspannen. Ein Context, der beide Welten zu brauchen scheint, ist ein **Smell für einen falschen Schnitt** und gehört in zwei Contexts aufgeteilt. Die ADRs (0019/0020/0021) bleiben damit vollständig gültig.

Statt eine nicht-atomare Mischung (`CompositeUnitOfWork`) zu ermöglichen, wird die Mischung deshalb **verboten und laut abgelehnt**: `BuildingBlocksOptions` verfolgt die gewählte Strategie und wirft, sobald sowohl `UseEfCorePersistence<TContext>()` als auch `UseMartenEventSourcing(...)` für denselben Host registriert werden.

```csharp
private PersistenceStyle _persistenceStyle;

private void SelectPersistenceStyle(PersistenceStyle style)
{
    if (_persistenceStyle != PersistenceStyle.None && _persistenceStyle != style)
    {
        throw new InvalidOperationException(
            "Two persistence strategies were configured for the same host (EF Core and Marten). " +
            "A microservice hosts exactly one bounded context, and a bounded context uses exactly one " +
            "persistence strategy (ADR-0019/0020/0021) ...");
    }

    _persistenceStyle = style;
}
```

`UseEfCorePersistence<TContext>()` ruft `SelectPersistenceStyle(PersistenceStyle.EfCore)` auf, `UseMartenEventSourcing(...)` ruft `SelectPersistenceStyle(PersistenceStyle.Marten)` auf. Damit wird aus dem bisherigen stillen Datenverlust (`TryAdd*`-No-Op) ein lauter Startfehler mit Handlungsanweisung. Die Dokumentation (ADR-0020, `docs/architecture/cqrs-and-event-sourcing.md`) hält die Ein-Strategie-pro-Context-Regel explizit fest.

**Umgesetzt.**

---

# IMP-03 - Erwartete Domänenfehler werden als Error geloggt

- **Kritisch**
-   - **Status**: gelöst. Logging ist jetzt die äußerste Behavior-Schicht (explizite numerische Reihenfolge: `Logging(0) → ExceptionToResult(100) → UnitOfWork(300) → Handler`); erwartete Domänenfehler werden als `Warning` mit Kategorie geloggt, nur unerwartete Ausnahmen als `Error`. Die Reihenfolge ist über `BuildingBlocksOptions.AddPipelineBehavior(type, order)` ein expliziter Vertrag (löst zugleich IMP-16 Schritt 4).

## Beschreibung

`AddBuildingBlocks` registriert die Behaviors in dieser Reihenfolge:

```csharp
services.TryAddEnumerable(... typeof(ExceptionToResultBehavior<,>));
services.TryAddEnumerable(... typeof(LoggingBehavior<,>));
services.TryAddEnumerable(... typeof(UnitOfWorkBehavior<,>));
```

`Sender.BuildPipeline` wickelt die Behaviors in umgekehrter Reihenfolge um den Handler, sodass die Registrierungsreihenfolge der Ausführungsreihenfolge von außen nach innen entspricht:

```
ExceptionToResultBehavior → LoggingBehavior → UnitOfWorkBehavior → Handler
```

`ExceptionToResultBehavior` liegt damit **außerhalb** von `LoggingBehavior`. Eine `DomainValidationException` aus dem Aggregat durchläuft also zuerst den `catch (Exception)`-Block des Loggers:

```csharp
catch (Exception)
{
    Log.RequestFaulted(logger, requestName, ...);   // LogLevel.Error
    throw;
}
```

…und wird erst danach in ein sauberes `Failure` übersetzt.

Konsequenz: **Jede fehlgeschlagene Eingabevalidierung und jede verletzte Geschäftsregel erzeugt einen Error-Log** mit der Meldung „Handling {RequestName} threw an unexpected exception". Das sind die häufigsten Ereignisse im normalen Betrieb.

Warum das kritisch ist: In einem Aspire-/OpenTelemetry-Setup wird die Error-Rate zur primären Alarmierungs- und SLO-Metrik. Wenn ein leeres Pflichtfeld einen Error erzeugt, ist die Metrik unbrauchbar — echte Störungen verschwinden im Rauschen. Der `LogLevel.Warning`-Pfad in `Log.RequestFailed`, der genau für diesen Fall gebaut wurde (inklusive Ausgabe der Failure-Kategorien), wird für Domänenfehler **nie erreicht**.

## Lösungsvorschlag

Die Reihenfolge in `ServiceCollectionExtensions` tauschen, sodass Logging die äußerste Schicht wird:

```csharp
services.TryAddEnumerable(ServiceDescriptor.Transient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>)));
services.TryAddEnumerable(ServiceDescriptor.Transient(typeof(IPipelineBehavior<,>), typeof(ExceptionToResultBehavior<,>)));
services.TryAddEnumerable(ServiceDescriptor.Transient(typeof(IPipelineBehavior<,>), typeof(UnitOfWorkBehavior<,>)));
```

Danach gilt:

| Ereignis                         | Ergebnis                                                                                                       |
| -------------------------------- | -------------------------------------------------------------------------------------------------------------- |
| Erfolg                           | `Information` — „Handled … successfully"                                                                       |
| `DomainValidationException`      | von `ExceptionToResultBehavior` in `Failure` übersetzt → Logger sieht ein `Result` → `Warning` inkl. Kategorie |
| `BusinessRuleViolationException` | dito                                                                                                           |
| Concurrency-Konflikt             | von `UnitOfWorkBehavior` übersetzt → `Warning`                                                                 |
| Unerwartete Exception            | durchläuft `ExceptionToResultBehavior` ungefangen → `Error` mit „faulted"                                      |

Das ist genau die gewünschte Semantik: Error bedeutet „hier ist etwas kaputt", Warning bedeutet „der Aufrufer hat etwas falsch gemacht".

**Ergänzend — Reihenfolge explizit machen statt implizit.** Die aktuelle Kopplung zwischen Registrierungsreihenfolge und Ausführungsreihenfolge ist korrekt, aber unsichtbar: Sie ergibt sich aus `.Reverse()` in `BuildPipeline` und der Aufrufreihenfolge in `AddBuildingBlocks`. Ein Service, der ein eigenes Behavior registriert, kann die Position nicht steuern (siehe auch IMP-16). Empfehlung:

```csharp
public BuildingBlocksOptions AddPipelineBehavior(Type openGenericBehavior, int order = 0);
```

…und in `BuildPipeline` nach `order` sortieren. Damit wird die Reihenfolge zu einem expliziten, testbaren Vertrag statt zu einer Eigenschaft der Aufrufreihenfolge.

**Test, der das dauerhaft absichert:**

```csharp
[Fact]
public async Task DomainValidationException_IsLoggedAsWarning_NotError()
```

mit einem `FakeLogger<T>` (`Microsoft.Extensions.Diagnostics.Testing`) und einem Handler, der wirft. Der Test ist Teil von IMP-09.

---

# IMP-04 - Integrations-Event-Transport nutzt `IMessageBus` statt `IMessageContext`

- **Kritisch** _(zu verifizieren)_

## Beschreibung

`WolverineIntegrationEventTransport` injiziert `IMessageBus`:

```csharp
public sealed class WolverineIntegrationEventTransport(IMessageBus messageBus) : IIntegrationEventTransport
{
    public Task PublishAsync(IIntegrationEvent integrationEvent, CancellationToken cancellationToken)
        => messageBus.PublishAsync(integrationEvent).AsTask();
}
```

Der einzige Aufrufpfad dorthin führt über `Publisher`, und `Publisher` wird ausschließlich von `DomainEventEnvelopeHandler` aufgerufen — also **innerhalb eines Wolverine-Message-Handlers**, der auf einer Queue mit `UseDurableInbox()` läuft.

Wolverines Kontrakt dafür lautet: Innerhalb eines Handlers ist `IMessageContext` zu injizieren, nicht `IMessageBus`. Nur der Context ist in die Transaktion und die Outbox der gerade verarbeiteten Nachricht eingeschrieben und propagiert Correlation- und Causation-Id.

Zwei konkrete Auswirkungen:

1. **Duplikate über die Servicegrenze.** Das Integration Event geht sofort und außerhalb der Inbox-Transaktion raus. Stürzt der Prozess nach dem Publish, aber vor dem Inbox-Ack, liefert Wolverine den `DomainEventEnvelope` erneut zu — und das Integration Event wird ein zweites Mal publiziert. Da `IIntegrationEvent` keine Id trägt (IMP-11), kann der konsumierende Service das nicht erkennen. Im Beispiel „Training abgeschlossen erhöht das Kalorienbudget" wird das Budget doppelt erhöht.
2. **Zerrissenes Tracing.** Ohne Correlation-Propagation bricht die Trace-Kette exakt an der Servicegrenze ab — also genau dort, wo verteiltes Tracing seinen Wert hat. In einem Aspire-Setup ist das der Unterschied zwischen einem durchgehenden Trace und N unverbundenen Fragmenten.

_Diese Analyse ist statisch hergeleitet; Wolverines DI-Verhalten wurde nicht ausgeführt. Vor der Änderung mit einem Integrationstest bestätigen._

## Lösungsvorschlag

**Schritt 1 — Verifikation.** Integrationstest mit einem In-Memory-Wolverine-Host: Command absetzen, den Envelope-Handler laufen lassen, und im publizierten Integration Event `Envelope.CorrelationId` gegen die Ursprungsnachricht prüfen. Zusätzlich einen Testfall mit einem nach dem Publish werfenden Projection-Handler, um zu sehen, ob das Integration Event beim Retry erneut rausgeht.

**Schritt 2 — Transport auf den Handler-Kontext umstellen.** Der Transport darf seine Nachrichtenquelle nicht mehr selbst aus DI ziehen, sondern muss den Kontext des aktuellen Handlers verwenden. Sauberste Variante: den Kontext explizit durchreichen, statt ihn implizit zu injizieren.

```csharp
// Messaging/DomainEventEnvelopeHandler.cs
public sealed class DomainEventEnvelopeHandler(IDomainEventPublisher publisher)
{
    public Task Handle(DomainEventEnvelope envelope, IMessageContext context, CancellationToken cancellationToken)
    {
        var domainEvent = DomainEventEnvelopeSerializer.Unwrap(envelope);
        return publisher.PublishAsync(domainEvent, new WolverineOutboxSink(context), cancellationToken);
    }
}
```

Wolverine injiziert `IMessageContext` als Handler-Parameter — das ist der dokumentierte Weg und garantiert den Kontext der aktuellen Nachricht.

**Schritt 3 — `IDomainEventPublisher` um die Senke erweitern.** Der Publisher bekommt den Transport nicht mehr aus DI, sondern als Parameter. Damit ist im Typsystem sichtbar, dass die Publikation an einen konkreten Transaktionskontext gebunden ist:

```csharp
public interface IIntegrationEventSink
{
    Task PublishAsync(IIntegrationEvent integrationEvent, CancellationToken cancellationToken);
}

internal sealed class WolverineOutboxSink(IMessageContext context) : IIntegrationEventSink
{
    public Task PublishAsync(IIntegrationEvent e, CancellationToken ct) => context.PublishAsync(e).AsTask();
}
```

`NullIntegrationEventTransport` wird zur `NullIntegrationEventSink` und bleibt der Default für Hosts ohne Messaging.

**Alternative (kleinerer Eingriff):** `IMessageContext` direkt in `WolverineIntegrationEventTransport` injizieren statt `IMessageBus`. Wolverine registriert `IMessageContext` scoped und setzt ihn für den Handler-Scope. Das ist eine Ein-Zeilen-Änderung, koppelt die Korrektheit aber an ein DI-Lifetime-Detail statt an eine sichtbare Signatur — und bricht still, sobald der Publisher je außerhalb eines Handlers aufgerufen wird. Nur wählen, wenn Schritt 2 zu invasiv ist, und dann mit einem Test absichern, der genau diesen Fall prüft.

---

# IMP-05 - `AddHandlersFrom` erzeugt Duplikate und überschreibt still

- **Kritisch**
-   - **Status**: gelöst (Schritte 1–4). `AddHandlersFrom` registriert Mehrfach-Handler (`IProjectionHandler<>`, `IIntegrationEventMapper`) jetzt über `TryAddEnumerable`, sodass ein doppelter Scan derselben Assembly keine doppelten Projektionen/Mapper mehr erzeugt (Schritt 1). Einzel-Handler (`ICommandHandler<>`, `ICommandHandler<,>`, `IQueryHandler<,>`) laufen über `RegisterSingleHandler`, das denselben Typ erneut ignoriert, bei zwei **verschiedenen** Handlern für denselben Command/Query aber mit beiden Typnamen sofort `InvalidOperationException` wirft, statt still einen zu wählen (Schritt 2). `assembly.GetTypes()` wird in einen `try/catch` gefasst, der `ReflectionTypeLoadException` in eine verständliche `InvalidOperationException` übersetzt (Schritt 3). Schritt 4 ist als **Opt-out** umgesetzt statt als Opt-in-`ValidateOnStart()`: `AddBuildingBlocks` registriert standardmäßig den `HandlerRegistrationStartupValidator` (ein `IHostedService`), der beim Host-Start für jede `ICommand`/`ICommand<>`/`IQuery<>`-Implementierung in den gescannten Assemblies den Handler auflöst und bei fehlenden Handlern mit allen betroffenen Request-Typen im Fehlertext den Start abbricht; ein Host, der Handler bewusst außerhalb des Assembly-Scans registriert, deaktiviert die Prüfung über `options.ValidateHandlersOnStart = false`. Abgesichert durch `HandlerRegistrationTests` und `HandlerStartupValidationTests` (u. a. der zuvor als `Skip` hinterlegte Dedup-Test ist aktiviert; die Fixtures liegen in eigenen Assemblies `BuildingBlocks.Infrastructure.Tests.{Fixtures,ConflictingHandlers,OrphanRequests}`).

## Beschreibung

`BuildingBlocksOptions.AddHandlersFrom` registriert alle gefundenen Handler mit `AddScoped`:

```csharp
if (contract == typeof(IIntegrationEventMapper))
{
    _services.AddScoped(typeof(IIntegrationEventMapper), type);
}
else if (contract.IsGenericType && Array.IndexOf(HandlerInterfaceDefinitions, contract.GetGenericTypeDefinition()) >= 0)
{
    _services.AddScoped(contract, type);
}
```

`AddScoped` (im Gegensatz zu `TryAddEnumerable`) fügt bei jedem Aufruf einen weiteren Descriptor hinzu. Zwei Fehlerbilder folgen daraus:

**1. Mehrfachausführung von Projektionen.** Wird `AddHandlersFrom(assembly)` versehentlich zweimal mit derselben Assembly aufgerufen — etwa weil zwei Aufrufe unterschiedliche Assemblies meinen, die sich überschneiden, oder weil ein Test-Setup die Registrierung wiederholt — liefert `GetServices<IProjectionHandler<T>>()` in `ProjectionRunner` **jeden Handler doppelt**. Eine idempotente Projektion (`UPSERT` auf den Zielzustand) übersteht das; eine nicht-idempotente (`counter += 1`, `total += amount`, `INSERT`) schreibt falsche Daten. Da Idempotenz ohnehin nirgends erzwungen oder unterstützt wird (siehe IMP-24), ist das ein realistisches Szenario.

**2. Stilles Überschreiben von Command-Handlern.** `Sender` löst über `GetRequiredService<ICommandHandler<TCommand>>()` auf — das liefert bei mehreren Registrierungen die **letzte**. Existieren aus Versehen zwei Handler für denselben Command (Copy-Paste, unvollständiges Refactoring, ein Handler in einem Test-Assembly), gewinnt einer davon abhängig von der Reflection-Reihenfolge von `assembly.GetTypes()`. Das ist nicht deterministisch garantiert und der Fehler äußert sich als „mein Code wird nicht ausgeführt" ohne jeden Hinweis.

Ein Command mit zwei Handlern ist per Definition ein Modellierungsfehler — CQRS-Commands haben genau einen Handler. Er sollte laut scheitern, nicht still eine Variante wählen.

Nebenaspekt: `assembly.GetTypes()` wirft `ReflectionTypeLoadException`, sobald eine Abhängigkeit der Assembly nicht ladbar ist. Die Meldung nennt dann nicht die eigentliche Ursache.

## Lösungsvorschlag

**Schritt 1 — Mehrfach-Handler (Projektionen, Mapper) idempotent registrieren:**

```csharp
_services.TryAddEnumerable(ServiceDescriptor.Scoped(contract, type));
```

`TryAddEnumerable` vergleicht Service- **und** Implementierungstyp und ignoriert exakte Duplikate. Damit ist ein doppelter `AddHandlersFrom`-Aufruf folgenlos, während zwei _unterschiedliche_ Projektionen für dasselbe Event weiterhin beide laufen — was gewollt ist.

**Schritt 2 — Einzel-Handler (Command, Query) beim Duplikat hart abbrechen:**

```csharp
private readonly Dictionary<Type, Type> _singleHandlers = [];

private void RegisterSingleHandler(Type contract, Type implementation)
{
    if (_singleHandlers.TryGetValue(contract, out var existing))
    {
        if (existing == implementation)
        {
            return;   // derselbe Typ erneut entdeckt: unkritisch
        }

        throw new InvalidOperationException(
            $"Für '{contract}' wurden zwei Handler gefunden: '{existing}' und '{implementation}'. " +
            "Ein Command bzw. eine Query darf genau einen Handler haben.");
    }

    _singleHandlers.Add(contract, implementation);
    _services.AddScoped(contract, implementation);
}
```

Der Fehler tritt damit beim Start auf, mit beiden Typnamen in der Meldung — statt zur Laufzeit als schwer auffindbares Fehlverhalten.

**Schritt 3 — Typ-Ladefehler verständlich machen:**

```csharp
Type[] types;
try
{
    types = assembly.GetTypes();
}
catch (ReflectionTypeLoadException exception)
{
    throw new InvalidOperationException(
        $"Die Typen der Assembly '{assembly.FullName}' konnten nicht geladen werden. " +
        "Häufigste Ursache ist eine fehlende Paketreferenz.",
        exception);
}
```

**Schritt 4 — Registrierung verifizierbar machen.** `AddHandlersFrom` ist reine Konvention; ein Tippfehler im Interface oder eine vergessene Assembly führt zu „no service registered" erst beim ersten Request. Eine Diagnose-Methode für Startup-Checks und Tests schließt die Lücke:

```csharp
public BuildingBlocksOptions ValidateOnStart();   // prüft: jeder ICommand-Typ in den
                                                   // registrierten Assemblies hat genau
                                                   // einen passenden Handler
```

Umsetzbar als `IHostedService`, der beim Start alle `ICommand`/`ICommand<>`/`IQuery<>`-Implementierungen in den registrierten Assemblies sucht und für jede den zugehörigen Handler aus dem Container auflöst. Das verwandelt eine ganze Fehlerklasse von „Produktionsfehler" in „Startfehler".

---

# IMP-06 - `Sender`-Cache ist nur nach Request-Typ gekeyed

- **Kritisch**

## Beschreibung

`Sender` cached die generierten Dispatcher in statischen Dictionaries. Für Commands mit Ergebnis und für Queries ist der Schlüssel jedoch nur der Request-Typ, während der gecachte Wert zusätzlich vom Ergebnistyp abhängt:

```csharp
var dispatcher = (CommandWithResultDispatcher<TResult>)CommandWithResultDispatchers.GetOrAdd(
    command.GetType(),                                     // Schlüssel: nur der Command-Typ
    static type => Activator.CreateInstance(
        typeof(CommandWithResultDispatcher<,>).MakeGenericType(type, typeof(TResult)))!);
```

Implementiert ein Typ sowohl `ICommand<int>` als auch `ICommand<string>` (analog `IQuery<A>`/`IQuery<B>`), landet beim ersten Aufruf der Dispatcher für den einen Ergebnistyp im Cache. Der zweite Aufruf holt denselben Eintrag und castet ihn auf `CommandWithResultDispatcher<string>` → **`InvalidCastException`** zur Laufzeit, mit einer Meldung über generische interne Typen, die nichts über die Ursache aussagt.

Das ist ein Randfall, aber ein realistischer: Eine Query, die je nach Kontext eine Id oder ein DTO liefern soll, ist eine naheliegende (wenn auch fragwürdige) Modellierung. Der Fehler ist zudem **zustandsabhängig** — er tritt nur auf, wenn die Aufrufreihenfolge stimmt, und verschwindet in einem Prozess, der zufällig anders startet. Das macht ihn extrem schwer zu diagnostizieren.

Der Cache ist zusätzlich `static` und damit prozessglobal (siehe IMP-35), was den Effekt über Container- und Testgrenzen hinweg trägt.

## Lösungsvorschlag

**Schritt 1 — Schlüssel vervollständigen.** Der Cache-Schlüssel muss alle Typparameter umfassen, die in den erzeugten Typ eingehen:

```csharp
private readonly record struct DispatcherKey(Type Request, Type Result);

private static readonly ConcurrentDictionary<DispatcherKey, object> CommandWithResultDispatchers = new();

var dispatcher = (CommandWithResultDispatcher<TResult>)CommandWithResultDispatchers.GetOrAdd(
    new DispatcherKey(command.GetType(), typeof(TResult)),
    static key => Activator.CreateInstance(
        typeof(CommandWithResultDispatcher<,>).MakeGenericType(key.Request, key.Result))!);
```

Analog für `QueryDispatchers`. `CommandDispatchers` (ohne Ergebnistyp) ist korrekt und bleibt unverändert.

Ein `readonly record struct` als Schlüssel ist hier die richtige Wahl: strukturelle Gleichheit ohne Allokation, korrektes `GetHashCode` ohne eigenen Code.

**Schritt 2 — die Mehrdeutigkeit ausschließen, statt sie nur zu überleben.** Ein Typ, der mehrere `ICommand<>`/`IQuery<>` implementiert, ist ein Modellierungsfehler — ein Command hat ein Ergebnis. Nach Schritt 1 funktioniert er zwar, aber die Absicht bleibt unklar. Deshalb in der Registrierungsprüfung aus IMP-05 zusätzlich prüfen:

```csharp
var resultContracts = type.GetInterfaces()
    .Where(i => i.IsGenericType &&
        (i.GetGenericTypeDefinition() == typeof(ICommand<>) ||
         i.GetGenericTypeDefinition() == typeof(IQuery<>)))
    .ToArray();

if (resultContracts.Length > 1)
{
    throw new InvalidOperationException(
        $"'{type}' implementiert mehrere Request-Verträge ({string.Join(", ", resultContracts.Select(c => c.Name))}). " +
        "Ein Command bzw. eine Query hat genau einen Ergebnistyp.");
}
```

**Schritt 3 — Regressionstest:**

```csharp
private sealed record AmbiguousRequest : IQuery<int>, IQuery<string>;

[Fact]
public async Task Send_SameRequestTypeWithDifferentResultTypes_ResolvesCorrectDispatcher()
```

Der Test muss beide Varianten **in derselben Testmethode nacheinander** aufrufen, sonst greift der Cache nicht. Teil von IMP-09.

---

# IMP-07 - `UnitOfWorkBehavior` wirft ohne konfigurierte Persistenz

- **Kritisch**

## Beschreibung

`UnitOfWorkBehavior` löst die Unit of Work zwingend auf:

```csharp
var unitOfWork = serviceProvider.GetRequiredService<IUnitOfWork>();
```

`IUnitOfWork` wird aber nur registriert, wenn `UseEfCorePersistence` oder `UseMartenEventSourcing` aufgerufen wurde. Jeder Host, der die Building Blocks ohne Persistenz nutzt, stürzt damit bei **jedem Command** mit einer `InvalidOperationException` aus dem DI-Container ab — nach erfolgreicher Ausführung des Handlers, und mit einer Meldung, die auf einen fehlenden Service verweist statt auf eine fehlende Konfiguration.

Betroffen sind reale Szenarien:

- **Unit-Tests von Handlern** über den echten `Sender` mit `AddBuildingBlocks(o => o.AddHandlersFrom(asm))` und Fake-Repositories — die naheliegendste Testform für Application-Code.
- **Gateway-/Facade-Services**, die Commands entgegennehmen und nur weiterleiten (z. B. der BFF, wenn er je selbst Commands dispatcht).
- **Services mit eigener, nicht-EF/Marten-Persistenz** (Dapper, externes API).

Die Abhängigkeit ist optional — der Code behandelt sie als erforderlich. Zusätzlich ist der Service-Locator-Zugriff hier funktional unnötig: Die einzige Begründung für `IServiceProvider` statt direkter Injektion wäre genau die Optionalität, die dann mit `GetRequiredService` wieder aufgegeben wird.

## Lösungsvorschlag

**Schritt 1 — Abhängigkeit als optional deklarieren und direkt injizieren:**

```csharp
public sealed class UnitOfWorkBehavior<TRequest, TResponse>(IUnitOfWork? unitOfWork)
    : IPipelineBehavior<TRequest, TResponse>
    where TResponse : Result
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestPipelineContinuation<TResponse> continuation,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(continuation);

        var response = await continuation(cancellationToken).ConfigureAwait(false);

        if (unitOfWork is null || !IsCommand || response.IsFailure)
        {
            return response;
        }

        // … commit
    }
}
```

Ein nullable Konstruktorparameter wird von `Microsoft.Extensions.DependencyInjection` korrekt mit `null` befüllt, wenn kein Service registriert ist. Die Optionalität steht damit in der Signatur — sichtbar, dokumentiert, testbar — statt in einem Kommentar.

Das löst gleichzeitig IMP-46: Das Behavior lässt sich jetzt mit `new UnitOfWorkBehavior<C, Result>(Substitute.For<IUnitOfWork>())` direkt instanziieren, ohne einen Container aufzubauen.

**Schritt 2 — den stillen No-Op sichtbar machen.** Ein Command, der keine Unit of Work findet, ist in Produktion fast immer ein Konfigurationsfehler. Ein einmaliger Log beim Start ist besser als Schweigen:

```csharp
// in AddBuildingBlocks bzw. einem Startup-Check
if (!services.Any(d => d.ServiceType == typeof(IUnitOfWork)))
{
    logger.LogInformation(
        "Keine Persistenz konfiguriert — Commands werden nicht committet. " +
        "Das ist nur für Tests und reine Gateway-Services vorgesehen.");
}
```

**Schritt 3 — Verhalten festschreiben:**

```csharp
[Fact]
public async Task Handle_WithoutUnitOfWork_ReturnsHandlerResult()

[Fact]
public async Task Handle_FailedCommand_DoesNotCommit()

[Fact]
public async Task Handle_Query_DoesNotCommit()
```

Der zweite und dritte Test sichern Verhalten ab, das heute korrekt implementiert, aber ungetestet ist — und dessen Bruch stiller Datenverlust bzw. unnötige Transaktionen wäre.

---

# IMP-08 - `MartenUnitOfWork` flusht die Outbox nicht

- **Kritisch**

## Beschreibung

Die beiden Unit-of-Work-Implementierungen schließen die Transaktion unterschiedlich ab:

```csharp
// EfCoreUnitOfWork — committet und sendet sofort
await outbox.SaveChangesAndFlushMessagesAsync(cancellationToken).ConfigureAwait(false);

// MartenUnitOfWork — committet, sendet nicht
await session.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
```

Bei `IMartenOutbox` ist nach `SaveChangesAsync` ein `FlushOutgoingMessagesAsync()` erforderlich, um die persistierten Nachrichten sofort an die Queue zu übergeben. Fehlt der Aufruf, bleiben sie in der Outbox-Tabelle liegen, bis der Wolverine Durability Agent sie beim nächsten Poll-Durchlauf einsammelt.

Die Nachrichten gehen also nicht verloren — das ist genau der Zweck der durablen Outbox. Aber:

- **Asymmetrische Latenz ohne fachlichen Grund.** Projektionen im event-sourced Kontext hinken denen im state-stored Kontext um das Poll-Intervall hinterher. Zwei Read-Modelle, die dieselbe Oberfläche versorgen, aktualisieren sich unterschiedlich schnell — ein Verhalten, das in der Fehlersuche viel Zeit kostet, weil es nach einem Projektionsfehler aussieht.
- **Fehlende Symmetrie als Wartungsrisiko.** Zwei Implementierungen desselben Interfaces mit unterschiedlichem Abschlussverhalten laden dazu ein, beim nächsten Refactoring die falsche als Vorlage zu nehmen.
- **Verstärkt IMP-25.** Wenn die Zustellung ohnehin verzögert und die Verarbeitung strikt sequenziell ist, addieren sich beide Effekte.

## Lösungsvorschlag

**Schritt 1 — Flush ergänzen:**

```csharp
public async Task CommitAsync(CancellationToken cancellationToken)
{
    outbox.Enroll(session);

    foreach (var aggregate in tracker.Aggregates)
    {
        foreach (var domainEvent in aggregate.DomainEvents)
        {
            await outbox.PublishAsync(DomainEventEnvelopeSerializer.Wrap(domainEvent)).ConfigureAwait(false);
        }
    }

    await session.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    await outbox.FlushOutgoingMessagesAsync().ConfigureAwait(false);

    tracker.ClearDomainEvents();
}
```

Die Reihenfolge ist entscheidend und sollte im Test festgehalten werden: **erst** committen, **dann** flushen. Umgekehrt würden Nachrichten für eine Transaktion rausgehen, die noch scheitern kann.

**Schritt 2 — Symmetrie erzwingen statt hoffen.** Beide Implementierungen teilen dieselbe Ablauflogik (Events einsammeln → wrappen → in die Outbox → committen → flushen → aufräumen) und unterscheiden sich nur in Store-spezifischen Details. Eine Template-Methode macht Abweichungen unmöglich:

```csharp
internal abstract class OutboxUnitOfWorkBase(IClock clock) : IUnitOfWorkParticipant
{
    public async Task CommitAsync(CancellationToken cancellationToken)
    {
        var aggregates = CollectAggregates();
        var occurredAt = clock.Now;

        foreach (var aggregate in aggregates)
        {
            foreach (var domainEvent in aggregate.DomainEvents)
            {
                await PublishToOutboxAsync(
                    DomainEventEnvelopeSerializer.Wrap(
                        DomainEventStamper.Stamp(domainEvent, occurredAt))).ConfigureAwait(false);
            }
        }

        await SaveAndFlushAsync(cancellationToken).ConfigureAwait(false);

        foreach (var aggregate in aggregates)
        {
            aggregate.ClearDomainEvents();
        }
    }

    protected abstract IReadOnlyList<IDomainEventsManager> CollectAggregates();
    protected abstract Task PublishToOutboxAsync(DomainEventEnvelope envelope);
    protected abstract Task SaveAndFlushAsync(CancellationToken cancellationToken);
}
```

Damit sind IMP-01 (Stamping) und IMP-08 (Flush) strukturell für beide Pfade gelöst, und ein dritter Store (etwa Dapper) muss nur drei kleine Methoden liefern.

**Schritt 3 — Test.** Mit Testcontainers-PostgreSQL: Command absetzen, unmittelbar danach die Wolverine-Outbox-Tabelle prüfen — sie muss leer sein. Ohne Flush enthält sie den Eintrag.

---

# IMP-09 - Kein Testprojekt für `BuildingBlocks.Infrastructure`

- **Kritisch**
-   - **Status**: gelöst. Das Testprojekt `BuildingBlocks/tests/BuildingBlocks.Infrastructure.Tests` existiert und ist in allen drei Stufen aufgebaut. **Stufe 1** (Dispatching/DI gegen den realen `Sender`): Failure-Übersetzung, `UnitOfWorkBehavior` (Commit bei Erfolg, kein Commit bei Fehler/Query, `DbUpdateConcurrencyException` → `Failure.Conflict`), Dispatcher-Auflösung, `AddHandlersFrom`-Registrierung und `FailureResults`-Laufzeittypen. **Stufe 2** (ohne Infrastruktur): `DomainEventEnvelopeSerializer`-Round-Trip mit typisierter ID, `decimal` und `DateTimeOffset`, Typauflösung, `EntityKeyFormatter` und `ApplyEntityKeyConversions`. **Stufe 3** (Testcontainers PostgreSQL): `MartenEventSourcedRepository`-Versionsarithmetik/Optimistic Concurrency und Entity-Key-Persistenz — skippt automatisch, wenn kein Docker verfügbar ist. Dazu **Architekturtests**, die die Schichtregeln per Reflection durchsetzen. Regressionstests für noch offene Punkte (IMP-06/07) sind als `Skip` mit Verweis auf das jeweilige IMP hinterlegt und werden aktiviert, sobald diese gelöst sind; der IMP-05-Dedup-Test ist inzwischen aktiviert (siehe IMP-05). Der Persistenztest gegen echtes PostgreSQL deckte zudem eine reale Lücke in `ApplyEntityKeyConversions` auf: ein stark typisierter **Primärschlüssel** wird von relationalen Providern nicht automatisch als Property erkannt, sodass der Konverter nie griff (unter EF InMemory fiel das nicht auf). `ApplyEntityKeyConversions` scannt jetzt die CLR-Properties der Entität und konfiguriert stark typisierte Schlüssel explizit — behoben und durch den Testcontainers-Test abgesichert.

## Beschreibung

Es existieren genau zwei Testprojekte: `BuildingBlocks.Domain.Tests` (47 Tests) und `BuildingBlocks.Application.Tests` (26 Tests). Für `BuildingBlocks.Infrastructure` gibt es **keines**.

Die 73 vorhandenen Tests decken damit ausschließlich den einfachsten Code ab: Gleichheitslogik, Guard-Clauses und das `Result`-Wertmodell. Ungetestet bleibt der gesamte Code, der Reflection, Nebenläufigkeit, Transaktionen und Versionsarithmetik enthält:

| Komponente                                     | Ungetestetes Risiko                                       |
| ---------------------------------------------- | --------------------------------------------------------- |
| `Sender`                                       | Pipeline-Aufbau, Reihenfolge, Dispatcher-Caching          |
| `UnitOfWorkBehavior`                           | Command-Erkennung, Conflict-Mapping, Commit-Unterdrückung |
| `ExceptionToResultBehavior` + `FailureResults` | Reflection-Pfad, Typkonvertierung                         |
| `EfCoreUnitOfWork` / `MartenUnitOfWork`        | Event-Einsammlung, Outbox-Atomarität, Clear               |
| `MartenEventSourcedRepository`                 | **Versionsarithmetik / Optimistic Concurrency**           |
| `DomainEventEnvelopeSerializer`                | Round-Trip, typisierte IDs, Typauflösung                  |
| `EntityKeyValueConverter`                      | Konverter-Erzeugung, Modell-Scan                          |
| `Publisher` / `ProjectionRunner`               | Handler-Auflösung, Mapper-Iteration                       |
| `AddHandlersFrom`                              | Registrierungssemantik                                    |

Der Befund ist nicht abstrakt: **Praktisch jeder Punkt aus der Kategorie „Kritisch" in diesem Dokument wäre von einem Test dieser Komponenten gefunden worden.** IMP-03 von einem Behavior-Reihenfolgetest, IMP-05 von einem Registrierungstest, IMP-06 von einem Cache-Test, IMP-07 von einem Test ohne Persistenz, IMP-08 von einem Outbox-Test.

Besonders exponiert ist die Versionsarithmetik in `MartenEventSourcedRepository`:

```csharp
session.Events.Append(streamKey, eventSourced.Version, uncommittedEvents);
```

`Version` wird sowohl in `LoadFromHistory` als auch in `RaiseEvent` inkrementiert und entspricht damit der erwarteten **End**-Version — was Martens Semantik genau trifft. Das ist korrekt, aber extrem subtil. Eine Off-by-one an dieser Stelle führt entweder dazu, dass **jeder** Schreibvorgang mit einem Konflikt scheitert, oder — deutlich schlimmer — dass Optimistic Concurrency vollständig wirkungslos wird und konkurrierende Schreibvorgänge sich gegenseitig überschreiben. Beides ohne Testabdeckung.

## Lösungsvorschlag

Ein Testprojekt `BuildingBlocks/tests/BuildingBlocks.Infrastructure.Tests`, in drei Stufen aufgebaut — schnellste und wertvollste zuerst.

**Stufe 1 — Dispatching und DI (keine Infrastruktur nötig, Laufzeit < 1 s).**

Ein echter `ServiceCollection` mit Fake-Handlern; kein Mock von `ISender`, sondern der reale `Sender`:

```csharp
private static ServiceProvider BuildProvider(Action<BuildingBlocksOptions>? configure = null)
{
    var services = new ServiceCollection();
    services.AddLogging();
    services.AddBuildingBlocks(configure ?? (_ => { }));
    return services.BuildServiceProvider();
}
```

Abzudecken:

- Pipeline-Reihenfolge: ein Recording-Behavior, das seinen Ein- und Austritt in eine Liste schreibt → erwartete Sequenz assertieren _(IMP-03)_
- `DomainValidationException` → `Failure.Validation`, `BusinessRuleViolationException` → `Failure.BusinessRule`
- Log-Level bei Domänenfehler ist `Warning`, nicht `Error` — mit `FakeLogger<T>` aus `Microsoft.Extensions.Diagnostics.Testing` _(IMP-03)_
- Command ohne registrierte `IUnitOfWork` läuft durch _(IMP-07)_
- Failed Command committet nicht; Query committet nicht
- Ein Request-Typ mit zwei Ergebnistypen liefert den richtigen Dispatcher _(IMP-06)_
- Doppeltes `AddHandlersFrom` erzeugt keine doppelten Projektionen _(IMP-05)_
- `FailureResults.Create<Result>` und `Create<Result<T>>` liefern den korrekten Laufzeittyp

**Stufe 2 — Serialisierung und Mapping (ebenfalls ohne Infrastruktur).**

- `DomainEventEnvelopeSerializer` Round-Trip mit einem Event, das eine typisierte ID (`readonly record struct RecipeId(Guid Value)`), ein `decimal` und ein `DateTimeOffset` trägt — deckt IMP-22 und die offene Frage nach STJ-Konvertern für Entity Keys ab
- Unbekannter Typname führt zu einer verständlichen Exception, nicht zu `NullReferenceException`
- `ApplyEntityKeyConversions` erzeugt für ein Testmodell die erwarteten Konverter und lässt bereits konfigurierte Properties unangetastet
- `EntityKeyFormatter.GetStreamKey` liefert einen stabilen, kulturunabhängigen Schlüssel

**Stufe 3 — Persistenz gegen echte Datenbank (Testcontainers, langsam, aber unverzichtbar).**

In-Memory-Provider können weder Transaktionsatomarität noch Optimistic Concurrency abbilden — genau die zwei Eigenschaften, um die es hier geht. Deshalb `Testcontainers.PostgreSql` mit einer nach Kollektion geteilten Instanz:

- `MartenEventSourcedRepository`: Laden → ändern → speichern → erneut laden liefert den erwarteten State und die erwartete Version
- **Konfliktfall:** dieselbe Version zweimal appenden → `ConcurrencyException` → `UnitOfWorkBehavior` liefert `Failure.Conflict`
- `EfCoreUnitOfWork`: Aggregat und Outbox-Eintrag liegen nach dem Commit vor; wirft der Commit, ist **keins von beidem** persistiert
- Nach dem Commit ist die Outbox-Tabelle leer (Flush) _(IMP-08)_
- Domain Events sind nach dem Commit am Aggregat geleert

**Ergänzend — Architekturtests.** Die Schichtregeln sind derzeit nur durch `.csproj`-Referenzen gesichert. Ein kleines Set an Reflection-Tests (oder `NetArchTest`) hält sie dauerhaft:

```csharp
[Fact] public void Domain_HasNoPackageReferences()
[Fact] public void Application_DependsOnlyOnDomain()
[Fact] public void Domain_DoesNotReferenceApplicationOrInfrastructure()
```

Diese Tests sind billig und fangen genau die Erosion ab, die in einem wachsenden Projekt am ehesten passiert.

---

# IMP-10 - Zwei inkompatible Aggregat-Programmiermodelle

- **Hoch**
-   - **Status**: Gelöst (ADR-0025 / ADR-0026 — vereinheitlichtes State-Fold-Modell, ein Repository-Vertrag)

## Beschreibung

`AggregateRoot<TKey>` und `EventSourcedAggregateRoot<TKey, TState>` teilen außer `IAggregateRoot<TKey>` und der Gleichheitslogik **nichts**:

| Aspekt         | `AggregateRoot<TKey>`                 | `EventSourcedAggregateRoot<TKey,TState>`          |
| -------------- | ------------------------------------- | ------------------------------------------------- |
| Identität      | Konstruktorparameter, eager validiert | aus `State.Id`, erst nach dem ersten Event gültig |
| Zustand        | freie Properties                      | `IState<TSelf,TKey>`-Fold                         |
| Event auslösen | `AddDomainEvent(e)`                   | `RaiseEvent(e, clock)`                            |
| Zeitstempel    | wird nicht gesetzt _(IMP-01)_         | via `IClock` gestempelt                           |
| Version        | nicht vorhanden                       | `long _version`                                   |
| Konstruktion   | `protected AggregateRoot(TKey id)`    | faktisch **public parameterlos** _(IMP-14)_       |
| Repository     | `IRepository<TAggregate,TKey>`        | `IEventSourcedRepository<TAggregate,TKey>`        |

Der Wechsel eines Aggregats von state-stored zu event-sourced ist damit kein Refactoring, sondern eine Neuimplementierung: neue Basisklasse, neuer State-Typ, alle Methoden umgeschrieben, anderes Repository-Interface, andere Handler-Signaturen, andere Tests.

Warum das ein Architekturproblem ist: Die erklärte Strategie lautet „Event Sourcing selektiv, dort wo es fachlichen Wert bringt". Diese Entscheidung fällt man selten am Anfang richtig — der Wert von Event Sourcing zeigt sich meist erst, wenn eine fachliche Anforderung nach Historie auftaucht („warum wurde dieser Wert geändert?", „zeig mir den Stand von letzter Woche"). Ein Framework, das diese Strategie unterstützt, muss den nachträglichen Wechsel **billig** machen. Aktuell ist er maximal teuer, was in der Praxis dazu führt, dass er nicht stattfindet und die Strategie zur Fiktion wird.

Zweiter Effekt: Zwei Programmiermodelle bedeuten doppelte Einarbeitung, doppelte Konventionen, doppelte Review-Regeln — und im Zweifel wählt jeder Entwickler das, das er zuletzt benutzt hat.

## Lösungsvorschlag

Ziel ist ein **einheitliches Autorenmodell**, bei dem die Persistenzstrategie eine Konfigurations- und Repository-Entscheidung ist, keine Klassenhierarchie-Entscheidung.

**Ansatz — `IState`-Fold für beide Varianten.** Der State-Fold ist auch ohne Event Sourcing wertvoll: Er zwingt dazu, jede Zustandsänderung über ein Event auszudrücken, statt sie an einer Property vorbei zu machen. Genau das ist der Grund, warum die Aggregate überhaupt Events erzeugen.

```csharp
public abstract class AggregateRoot<TKey, TState> : IAggregateRoot<TKey>, IDomainEventsManager
    where TKey : struct, IEntityKey
    where TState : IState<TState, TKey>
{
    private readonly List<IDomainEvent> _domainEvents = [];

    protected TState State { get; private set; }
    public TKey Id => State.Id;

    protected void RaiseEvent(IDomainEvent domainEvent)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);
        State = State.Apply(domainEvent);
        EnsureValidIdentity();
        _domainEvents.Add(domainEvent);
    }
}

public abstract class EventSourcedAggregateRoot<TKey, TState> : AggregateRoot<TKey, TState>,
                                                                IEventSourcedAggregateRoot<TKey>
{
    private long _version;
    // ergänzt ausschließlich Version und LoadFromHistory
}
```

Damit ist ES eine **additive** Fähigkeit: `Version` + `LoadFromHistory`. Der Wechsel eines Aggregats besteht aus dem Ändern der Basisklasse und dem Tausch des Repository-Interfaces — der gesamte fachliche Code bleibt unverändert.

Vorteile über den Wechsel hinaus:

- `IClock` verschwindet aus dem Domänen-API (kombiniert mit IMP-01)
- Der Zeitstempel-Bug kann strukturell nicht mehr auftreten, weil es nur einen Pfad gibt
- State-stored Aggregate bekommen den immutable State-Fold — deutlich besser testbar als frei mutierte Properties
- Das EF-Core-Mapping erfolgt gegen den State (ein `record` mit klaren Properties) statt gegen ein Objekt mit privaten Feldern

**Migrationskosten und Trade-off, offen benannt:**

- EF Core muss auf den State mappen. Bei einem `record`-State geht das über `ComplexProperty` bzw. eine Owned Entity, oder — pragmatischer — über eine Mapping-Konfiguration, die die State-Properties direkt auf Spalten legt. Das ist mehr Konfigurationsarbeit als das direkte Mappen einer Klasse mit Auto-Properties.
- Für einfache CRUD-nahe Aggregate ist der Fold Overhead. Das ist ein realer Preis. Er ist gerechtfertigt, wenn die Konsistenz zwischen beiden Modellen als Ziel akzeptiert wird — sonst nicht.

**Wenn dieser Preis nicht akzeptabel ist**, sollte die Konsequenz explizit gezogen werden: Dann ist der Wechsel state-stored ↔ event-sourced **kein unterstütztes Szenario** und gehört so in einen ADR. Der schlechteste Zustand ist der aktuelle — ein beworbenes Ziel ohne strukturelle Unterstützung.

**Unabhängig vom gewählten Weg** sollten die drei duplizierten Gleichheitsimplementierungen (`Entity<TKey>`, `AggregateRoot<TKey>`, `EventSourcedAggregateRoot<TKey,TState>`) auf eine gemeinsame Basis reduziert werden. Dreimal identischer `Equals`/`GetHashCode`/`==`/`!=`-Code ist Wartungslast ohne Gegenwert.

---

# IMP-11 - `IIntegrationEvent` ist ein leerer Marker

- **Hoch**

## Beschreibung

Die beiden Event-Verträge sind gegenläufig ausgestattet:

```csharp
public interface IDomainEvent
{
    Guid EventId { get; }
    DateTimeOffset OccurredAt { get; }
}

public interface IIntegrationEvent;   // leer
```

Das ist genau verkehrt herum. Das **kontextinterne** Event, das im selben Prozess und derselben Transaktion verarbeitet wird, trägt Identität und Zeitstempel. Das Event, das die **Servicegrenze verlässt**, über RabbitMQ läuft und beim Empfänger in einem anderen Prozess, einer anderen Datenbank und einem anderen Deployment-Zyklus ankommt, trägt nichts.

Die Zustellung ist at-least-once — das ist eine Eigenschaft des Transports und nicht abwählbar. Ohne stabile Event-Id kann ein konsumierender Service eine Doppelzustellung **nicht erkennen**. Im Beispiel „`WorkoutCompleted` erhöht das Kalorienbudget" bedeutet das: Das Budget wird bei jeder Redelivery erneut erhöht, mit dauerhaft falschen Daten als Ergebnis. IMP-04 macht Redeliveries dabei nicht nur möglich, sondern wahrscheinlich.

Weiter fehlen:

- **Zeitstempel** — der Empfänger kann verspätete oder außer der Reihe eingetroffene Nachrichten nicht erkennen
- **Versionierung** — ein Integration Event ist ein _öffentlicher Vertrag_ zwischen unabhängig deploybaren Services. Ohne Versionsangabe ist jede Änderung eine Breaking Change ohne Migrationspfad
- **Correlation-Id** — die Ursachenkette vom auslösenden Command bis zur Reaktion im Zielkontext ist nicht rekonstruierbar

## Lösungsvorschlag

**Schritt 1 — Vertrag ausstatten:**

```csharp
namespace BuildingBlocks.Application;

public interface IIntegrationEvent
{
    Guid EventId { get; }

    DateTimeOffset OccurredAt { get; }

    Guid CorrelationId { get; }
}

public abstract record IntegrationEvent : IIntegrationEvent
{
    public Guid EventId { get; init; } = Guid.CreateVersion7();
    public DateTimeOffset OccurredAt { get; init; }
    public Guid CorrelationId { get; init; }
}
```

`Guid.CreateVersion7()` statt `Guid.NewGuid()`: zeitlich sortierbar und damit indexfreundlich in der Deduplizierungstabelle des Empfängers. Für `DomainEvent.EventId` gilt dasselbe Argument.

**Schritt 2 — Felder an der richtigen Stelle befüllen.** Der Mapper soll sich um Fachlichkeit kümmern, nicht um Metadaten. `Publisher` füllt sie zentral, analog zum Domain-Event-Stamping aus IMP-01:

```csharp
foreach (var integrationEvent in mapper.Map(domainEvent))
{
    var enriched = IntegrationEventStamper.Stamp(
        integrationEvent,
        occurredAt: clock.Now,
        correlationId: domainEvent.EventId);   // Ursachenkette: Domain Event → Integration Event
}
```

Die `CorrelationId` aus der `EventId` des auslösenden Domain Events abzuleiten, macht die Kausalkette über die Servicegrenze hinweg nachvollziehbar — auch dann noch, wenn das Wolverine-Tracing aus irgendeinem Grund nicht durchgängig ist.

**Schritt 3 — Versionierung im Vertragsnamen, nicht im Typ.** Nicht als Property (die wird ignoriert), sondern im logischen Nachrichtennamen, wie in IMP-22 vorgeschlagen:

```csharp
[IntegrationEventName("fitness.workout-completed.v1")]
public sealed record WorkoutCompletedIntegrationEvent(...) : IntegrationEvent;
```

Eine neue Version ist dann ein neuer Typ mit neuem Namen; beide können parallel publiziert werden, bis alle Konsumenten migriert sind. Das ist der einzige Ansatz, der unabhängige Deployments tatsächlich erlaubt.

**Schritt 4 — Empfängerseitige Idempotenz unterstützen.** Eine Event-Id nützt nur, wenn es einen Ort gibt, sie zu speichern. Wolverine bringt mit `UseDurableInbox()` bereits eine Deduplizierung pro Endpoint mit — das sollte in den Messaging-Defaults für **eingehende** Endpoints verbindlich gesetzt und getestet sein, nicht dem Service überlassen:

```csharp
options.ListenToRabbitQueue(queueName).UseDurableInbox();
```

Ergänzend eine Guard-Rail: Ein Integration Event ohne gesetzte `EventId` (also `Guid.Empty`) sollte im Transport hart scheitern statt still zu passieren.

---

# IMP-12 - `IIntegrationEventMapper` ist untypisiert

- **Hoch**

## Beschreibung

Zwei benachbarte, funktional analoge Konzepte sind gegensätzlich entworfen:

```csharp
public interface IProjectionHandler<in TDomainEvent> where TDomainEvent : IDomainEvent
{
    Task Handle(TDomainEvent domainEvent, CancellationToken cancellationToken);
}

public interface IIntegrationEventMapper
{
    IReadOnlyCollection<IIntegrationEvent> Map(IDomainEvent domainEvent);
}
```

Der Projektions-Handler ist typisiert und wird von `ProjectionRunner` gezielt pro Event-Typ aufgelöst. Der Mapper ist untypisiert und wird von `Publisher` für **jedes** Event aufgerufen:

```csharp
foreach (var mapper in _mappers)
{
    foreach (var integrationEvent in mapper.Map(domainEvent))
```

Konsequenzen:

- **Jede Implementierung braucht einen `switch`** über alle Event-Typen und muss für alle nicht behandelten Fälle eine leere Collection liefern. Das ist Boilerplate, das der Compiler nicht prüfen kann — ein vergessener Fall ist ein stiller Fehler.
- **Zentralisierung erzwungen.** Statt „ein Mapper pro fachlichem Übergang" entsteht in der Praxis ein großer Mapper pro Service, der zum Sammelpunkt wird.
- **Aufwand `mappers × events`** mit fast immer leerem Ergebnis, plus Allokation einer leeren Collection pro Kombination. Zusammen mit IMP-25 (sequenzielle Verarbeitung) ist das messbar.
- **Inkonsistenz als Signal.** Wer den einen Vertrag verstanden hat, erwartet den anderen analog. Zwei Muster für dasselbe Problem im selben Namespace kosten bei jedem Lesen Aufmerksamkeit.

## Lösungsvorschlag

**Schritt 1 — Vertrag typisieren, analog zum Projektions-Handler:**

```csharp
namespace BuildingBlocks.Application;

public interface IIntegrationEventMapper<in TDomainEvent>
    where TDomainEvent : IDomainEvent
{
    IReadOnlyCollection<IIntegrationEvent> Map(TDomainEvent domainEvent);
}
```

Anwendung im Service:

```csharp
internal sealed class WorkoutCompletedMapper : IIntegrationEventMapper<WorkoutCompleted>
{
    public IReadOnlyCollection<IIntegrationEvent> Map(WorkoutCompleted e) =>
        [new WorkoutCompletedIntegrationEvent(e.UserId, e.CompletedOn, e.EnergyBurnedKcal)];
}
```

Kein `switch`, kein Cast, kein Leerfall. Die Existenz der Klasse _ist_ die Aussage „dieses Domain Event verlässt den Kontext" — und damit im Code auffindbar (`Find All References` auf den Event-Typ zeigt Projektionen und Mapper).

**Schritt 2 — Auflösung über denselben Mechanismus wie `ProjectionRunner`.** Die Invoker-Logik (statischer Cache `Type → Invoker`, abstrakte Basisklasse, geschlossenes Generic per `Activator`) ist bereits vorhanden und erprobt. Sie sollte einmal extrahiert und zweimal verwendet werden.

Damit entfallen `Publisher._mappers` und die doppelte Schleife; `Publisher` wird zu:

```csharp
await projectionRunner.RunAsync(domainEvent, cancellationToken).ConfigureAwait(false);
await integrationEventRunner.RunAsync(domainEvent, sink, cancellationToken).ConfigureAwait(false);
```

**Schritt 3 — Registrierung anpassen.** In `BuildingBlocksOptions.HandlerInterfaceDefinitions` wird `typeof(IIntegrationEventMapper<>)` aufgenommen, und der Sonderfall

```csharp
if (contract == typeof(IIntegrationEventMapper))
```

entfällt ersatzlos. Die Registrierung wird dadurch einheitlich — alle vier Handler-Arten laufen über denselben Zweig.

**Migrationshinweis:** Da es noch keine Implementierung gibt, ist die Änderung derzeit **kostenlos**. Mit dem ersten Service, der einen zentralen Mapper geschrieben hat, wird sie zu einem Refactoring über alle Event-Typen hinweg.

---

# IMP-13 - Messaging-Konfiguration ohne Guard-Rails, stiller Datenverlust

- **Hoch**

## Beschreibung

Die Messaging- und Outbox-Konfiguration verteilt sich auf zwei getrennte Oberflächen mit insgesamt sechs voneinander unabhängigen Aufrufen:

| Aufruf                                     | Ort                          | Zweck                                  |
| ------------------------------------------ | ---------------------------- | -------------------------------------- |
| `UseEfCorePersistence<T>()`                | `BuildingBlocksOptions` (DI) | EF-Repository + UoW                    |
| `UseMartenEventSourcing(cs)`               | `BuildingBlocksOptions` (DI) | Marten + `IntegrateWithWolverine()`    |
| `UseWolverineMessaging()`                  | `BuildingBlocksOptions` (DI) | ersetzt den Null-Transport             |
| `ApplyBuildingBlockEfCoreOutbox()`         | `WolverineOptions`           | `UseEntityFrameworkCoreTransactions()` |
| `ApplyBuildingBlockDomainEventRouting()`   | `WolverineOptions`           | lokale Queue + durable Inbox           |
| `ApplyBuildingBlockMessagingDefaults(uri)` | `WolverineOptions`           | RabbitMQ + Retry + Error-Queue         |

Es gibt **keine Prüfung**, ob eine sinnvolle Kombination vorliegt. Jede Auslassung führt zu stillem Fehlverhalten statt zu einem Fehler:

- **`UseWolverineMessaging()` vergessen** → `NullIntegrationEventTransport` bleibt aktiv → **alle Integration Events werden verworfen**, nur mit einem `Warning`-Log pro Event. Ein Bounded Context reagiert schlicht nicht mehr auf einen anderen. Da das Warning im normalen Log-Rauschen untergeht und nichts abstürzt, kann dieser Zustand sehr lange unbemerkt bleiben.
- **`ApplyBuildingBlockMessagingDefaults` vergessen** → keine Retry-Policy, keine Error-Queue. Ein vorübergehender DB-Fehler in einer Projektion führt zum Nachrichtenverlust statt zum Retry.
- **`ApplyBuildingBlockEfCoreOutbox` vergessen** bei EF-Persistenz → `IDbContextOutbox<TContext>` ist nicht auflösbar → Laufzeitfehler beim ersten Commit, mit einer DI-Meldung, die nicht auf den fehlenden Aufruf zeigt.
- **`ApplyBuildingBlockDomainEventRouting` vergessen** → Wolverine routet den `DomainEventEnvelope` per Discovery trotzdem an den lokalen Handler, aber **ohne** durable Inbox und ohne Sequenzierung. Das Verhalten degradiert also still zu „Events gehen bei einem Absturz verloren".

Zusätzlich ist `ApplyBuildingBlockEfCoreOutbox()` ein reiner Ein-Zeilen-Wrapper um `UseEntityFrameworkCoreTransactions()` — er fügt keinen Wert hinzu, sondern nur einen weiteren Namen, den man kennen und aufrufen muss.

Der `NullIntegrationEventTransport` als Null-Objekt ist für Tests und lokale Entwicklung richtig. Als **Produktions-Default** ist er die gefährlichste Konfiguration im gesamten Repository: Datenverlust ohne Fehler.

## Lösungsvorschlag

**Schritt 1 — Eine Konfigurationsoberfläche.** Die `WolverineOptions`-Extensions sollten von `BuildingBlocksOptions` aus erreichbar sein, damit alles an einer Stelle steht:

```csharp
builder.Services.AddBuildingBlocks(options => options
    .AddHandlersFrom(typeof(CreateRecipeCommand).Assembly)
    .UseEfCorePersistence<NutritionWriteDbContext>()
    .UseWolverineMessaging(rabbitMqUri));   // Transport, Routing, Outbox, Defaults in einem
```

`UseWolverineMessaging(Uri)` registriert intern einen `IConfigureOptions<WolverineOptions>`-Callback, der alle Schritte ausführt — inklusive `UseEntityFrameworkCoreTransactions()` bzw. `IntegrateWithWolverine()`, je nachdem welche Persistenz zuvor gewählt wurde. Die Reihenfolge-Abhängigkeit wird damit vom Aufrufer in die Bibliothek verlagert, wo sie hingehört.

Die einzelnen `Apply*`-Methoden bleiben `public` als Escape-Hatch für Hosts mit Sonderanforderungen, sind aber nicht mehr der Standardweg.

**Schritt 2 — Fehlkonfiguration beim Start erkennen.** Ein `IHostedService`, der vor dem ersten Request prüft:

```csharp
internal sealed class BuildingBlocksConfigurationValidator(...) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        var hasMappers   = _mappers.Length > 0;
        var hasTransport = _transport is not NullIntegrationEventTransport;

        if (hasMappers && !hasTransport)
        {
            throw new InvalidOperationException(
                $"Es sind {_mappers.Length} Integration-Event-Mapper registriert, aber kein Transport. " +
                "Alle Integration Events würden verworfen. " +
                "Rufe UseWolverineMessaging(...) auf oder entferne die Mapper.");
        }

        // weitere Prüfungen: UoW vorhanden wenn Repositories registriert,
        // DomainEventEnvelope-Routing konfiguriert, …
    }
}
```

Die Regel „Mapper vorhanden, aber kein Transport" ist die entscheidende: Sie unterscheidet zuverlässig zwischen „Service braucht kein Messaging" (kein Mapper, kein Problem) und „Service braucht Messaging, hat es aber nicht konfiguriert" (Datenverlust).

**Schritt 3 — Null-Transport explizit machen.** Er sollte nicht mehr der stille Default sein, sondern eine bewusste Wahl:

```csharp
public BuildingBlocksOptions UseNoMessaging();   // registriert den Null-Transport explizit
```

Ohne `UseWolverineMessaging` **und** ohne `UseNoMessaging` scheitert der Startup-Check aus Schritt 2. Damit ist die gefährlichste Konfiguration nicht mehr diejenige, die man durch Nichtstun bekommt.

**Schritt 4 — `AutoProvision()` umgebungsabhängig machen.**

```csharp
options.UseRabbitMq(rabbitMqUri).AutoProvision();
```

Automatisches Anlegen von Exchanges und Queues ist in Entwicklung und Test genau richtig und in Produktion meist unerwünscht — dort werden Topologien deklarativ verwaltet, und ein Tippfehler im Queue-Namen legt still eine neue Queue an, statt zu scheitern. Empfehlung: Parameter mit sicherem Default.

```csharp
public static WolverineOptions ApplyBuildingBlockMessagingDefaults(
    this WolverineOptions options, Uri rabbitMqUri, bool autoProvision = false)
```

**Schritt 5 — Retry-Policy präzisieren.** Aktuell:

```csharp
options.Policies.OnException<Exception>()
    .RetryWithCooldown(100ms, 500ms, 2s)
    .Then.MoveToErrorQueue();
```

`OnException<Exception>` retried auch das, was garantiert nie gelingen wird — `JsonException` bei kaputtem Payload, `TypeLoadException` bei unbekanntem Event-Typ, `ArgumentNullException` durch einen Programmierfehler. Drei Retries plus Cooldown auf einer `Sequential()`-Queue (IMP-25) blockieren dabei **alle** nachfolgenden Events des Service für rund 2,6 Sekunden. Besser explizit trennen:

```csharp
options.Policies.OnException<JsonException>().MoveToErrorQueue();
options.Policies.OnException<InvalidOperationException>().MoveToErrorQueue();
options.Policies.OnException<DbException>().RetryWithCooldown(...).Then.MoveToErrorQueue();
options.Policies.OnException<TimeoutException>().RetryWithCooldown(...).Then.MoveToErrorQueue();
```

Transiente Fehler werden wiederholt, deterministische landen sofort in der Error-Queue.

---

# IMP-14 - Constraint-Mismatch zwischen `IEventSourcedRepository` und Implementierung

- **Hoch**

## Beschreibung

Abstraktion und Implementierung haben unterschiedliche generische Constraints:

```csharp
// BuildingBlocks.Application
public interface IEventSourcedRepository<TAggregate, in TKey>
    where TAggregate : class, IEventSourcedAggregateRoot<TKey>

// BuildingBlocks.Infrastructure
public sealed class MartenEventSourcedRepository<TAggregate, TKey>
    where TAggregate : class, IEventSourcedAggregateRoot<TKey>, new()
```

Das `new()` in der Implementierung stammt aus dem Rehydrieren:

```csharp
var aggregate = new TAggregate();
((IEventSourcedAggregateRoot<TKey>)aggregate).LoadFromHistory(...);
```

Die offene generische Registrierung `AddScoped(typeof(IEventSourcedRepository<,>), typeof(MartenEventSourcedRepository<,>))` akzeptiert das beim Start. Der Container prüft Constraints jedoch erst beim Schließen des Generics — also beim ersten `GetRequiredService<IEventSourcedRepository<Workout, WorkoutId>>()`. Hat `Workout` keinen public parameterlosen Konstruktor, meldet der Container „No service for type … has been registered". Die Meldung nennt weder das `new()`-Constraint noch den fehlenden Konstruktor. Bei einem korrekt registrierten offenen Generic ist das eine der irreführendsten Fehlermeldungen, die .NET DI produziert.

Der zweite, schwerwiegendere Teil: Das `new()`-Constraint erzwingt, dass **jedes event-sourced Aggregat einen public Konstruktor hat, der ein leeres Objekt ohne gültige Identität erzeugt**. Das steht in direktem Widerspruch zur Invariante, die `AggregateRoot` durchsetzt („ein Aggregat existiert nie ohne gültige Id") und öffnet ein Loch in der Kapselung: Jeder Anwendungscode kann `new Workout()` schreiben und bekommt ein Objekt in einem Zustand, den die Domäne eigentlich verbietet.

## Lösungsvorschlag

**Schritt 1 — Fabrik statt `new()`.** Das Erzeugen der leeren Hülle ist ein Rehydrierungs-Detail und gehört nicht in die öffentliche Oberfläche des Aggregats. Ein statisches Interface-Member (C# 11) drückt das typsicher aus, ohne einen public Konstruktor zu erzwingen:

```csharp
namespace BuildingBlocks.Domain;

public interface IEventSourcedAggregateFactory<out TSelf>
{
    /// Erzeugt eine leere Hülle für die Rehydrierung. Nur von der Persistenz aufzurufen.
    static abstract TSelf CreateEmpty();
}
```

Im Aggregat:

```csharp
public sealed class Workout : EventSourcedAggregateRoot<WorkoutId, WorkoutState>,
                              IEventSourcedAggregateFactory<Workout>
{
    private Workout() : base(WorkoutState.Empty) { }          // privat!

    static Workout IEventSourcedAggregateFactory<Workout>.CreateEmpty() => new();
}
```

Der Konstruktor bleibt privat, die Fabrik ist explizit implementiert und damit nur über das Interface erreichbar — dieselbe Kapselungstechnik, die im Repo bereits erfolgreich für `ClearDomainEvents` und `LoadFromHistory` verwendet wird.

**Schritt 2 — Constraint in beide Verträge ziehen.** Entscheidend ist, dass Abstraktion und Implementierung dasselbe fordern:

```csharp
// Application
public interface IEventSourcedRepository<TAggregate, in TKey>
    where TAggregate : class, IEventSourcedAggregateRoot<TKey>, IEventSourcedAggregateFactory<TAggregate>
    where TKey : struct, IEntityKey

// Infrastructure — identische Constraints
public sealed class MartenEventSourcedRepository<TAggregate, TKey> : IEventSourcedRepository<TAggregate, TKey>
    where TAggregate : class, IEventSourcedAggregateRoot<TKey>, IEventSourcedAggregateFactory<TAggregate>
```

Damit wird aus einem Laufzeitfehler mit unbrauchbarer Meldung ein **Compile-Fehler an der Verwendungsstelle**: Wer `IEventSourcedRepository<Workout, WorkoutId>` injizieren will, ohne dass `Workout` die Fabrik implementiert, bekommt vom Compiler gesagt, was fehlt.

Das ist die generelle Regel, die hier verletzt wurde: **Constraints an der Abstraktion dürfen nie schwächer sein als an der Implementierung.** Jede Abweichung verschiebt einen Compile-Fehler in die Laufzeit.

**Schritt 3 — Doppel-Save absichern.** `SaveAsync` staged die Events und trackt das Aggregat, setzt aber nichts zurück:

```csharp
session.Events.Append(streamKey, eventSourced.Version, uncommittedEvents);
tracker.Track((IDomainEventsManager)aggregate);
```

Ein zweiter `SaveAsync`-Aufruf im selben Scope appendet dieselben Events erneut und trackt das Aggregat doppelt. Das ist ein Bedienfehler, aber einer ohne jede Rückmeldung. `MartenAggregateTracker.Track` sollte bereits verfolgte Aggregate erkennen (`HashSet` mit Referenzvergleich) und `SaveAsync` bei einem bereits gestagten Aggregat entweder no-oppen oder werfen.

---

# IMP-15 - `EfCoreRepository` lädt Aggregate unvollständig und ist `sealed`

- **Hoch**

## Beschreibung

```csharp
public sealed class EfCoreRepository<TAggregate, TKey>(DbContext context) : IRepository<TAggregate, TKey>
{
    public async Task<TAggregate?> GetByIdAsync(TKey id, CancellationToken cancellationToken) =>
        await context.Set<TAggregate>().FindAsync([id], cancellationToken).ConfigureAwait(false);
}
```

`FindAsync` lädt genau die Root-Entität, **ohne Navigationseigenschaften**. Ein `Recipe` mit `IReadOnlyCollection<Ingredient> Ingredients` kommt mit leerer Zutatenliste aus dem Repository.

Warum das kein Performance-, sondern ein Korrektheitsproblem ist: In DDD ist das Aggregat die Konsistenzgrenze, und seine Invarianten werden gegen den **vollständigen** Zustand geprüft. Konkret:

```csharp
recipe.AddIngredient("Hafer", 80m);
// → RuleChecker.Check(new MaxIngredientsRule(_ingredients));
// → _ingredients ist leer, weil nie geladen
// → Regel greift nie, Rezept bekommt 200 Zutaten
```

Die Invariante existiert im Code, ist aber wirkungslos. Schlimmer: Beim `SaveChanges` sieht EF Core die Collection als leer bzw. unverändert an — je nach Mapping-Konfiguration können bestehende Kindzeilen dabei verwaisen oder gelöscht werden.

Die Klasse ist `sealed` und wird als **offenes Generic für alle Aggregate** registriert:

```csharp
_services.TryAddScoped(typeof(IRepository<,>), typeof(EfCoreRepository<,>));
```

Ein Service kann das Ladeverhalten für ein einzelnes Aggregat also nicht anpassen. Der einzige Ausweg wäre, `IRepository<Recipe, RecipeId>` gezielt zu überschreiben und eine komplett eigene Implementierung zu registrieren — womit der Building Block für dieses Aggregat nutzlos wird.

## Lösungsvorschlag

Drei Optionen, kombinierbar. Empfohlen ist die Kombination aus 1 und 3.

**Option 1 — Ladestrategie als Erweiterungspunkt (empfohlen).** `sealed` entfernen und den Query-Einstiegspunkt überschreibbar machen:

```csharp
public class EfCoreRepository<TAggregate, TKey>(DbContext context) : IRepository<TAggregate, TKey>
    where TAggregate : AggregateRoot<TKey>
    where TKey : struct, IEntityKey
{
    protected DbContext Context => context;

    protected virtual IQueryable<TAggregate> Query() => context.Set<TAggregate>();

    public virtual Task<TAggregate?> GetByIdAsync(TKey id, CancellationToken cancellationToken) =>
        Query().FirstOrDefaultAsync(aggregate => aggregate.Id.Equals(id), cancellationToken);
}
```

Ein Service leitet nur dort ab, wo es nötig ist:

```csharp
internal sealed class RecipeRepository(NutritionWriteDbContext context)
    : EfCoreRepository<Recipe, RecipeId>(context)
{
    protected override IQueryable<Recipe> Query() =>
        base.Query().Include(recipe => recipe.Ingredients);
}
```

Und registriert sie gezielt — die offene generische Registrierung bleibt der Default für einfache Aggregate:

```csharp
_services.AddScoped<IRepository<Recipe, RecipeId>, RecipeRepository>();
```

**Wichtiger Nebeneffekt:** Der Wechsel von `FindAsync` zu `FirstOrDefaultAsync` gibt den Identity-Map-Vorteil von `Find` auf (Find liefert ein bereits getracktes Objekt ohne DB-Roundtrip). Wenn das relevant ist, lässt es sich kombinieren: erst `Context.Set<TAggregate>().Local` prüfen, sonst die Query ausführen.

**Option 2 — EF-Auto-Includes.** In der Entity-Konfiguration:

```csharp
builder.Navigation(recipe => recipe.Ingredients).AutoInclude();
```

Vorteil: keine Änderung an den Building Blocks, das Wissen liegt beim Aggregat-Mapping — konzeptionell der richtige Ort. Nachteil: `AutoInclude` gilt auch für Read-Queries auf denselben Typ, was dort unnötige Joins erzeugt (mit `IgnoreAutoIncludes()` abschaltbar). Da Read-Modelle hier ohnehin getrennt sind, ist das verkraftbar.

**Option 3 — Vollständigkeit prüfbar machen (empfohlen als Ergänzung).** Beide Optionen bleiben Konvention: Wer eine neue Collection ans Aggregat hängt und den Include vergisst, bekommt den Fehler still zurück. Ein Test schließt die Lücke systematisch:

```csharp
[Fact]
public void EveryAggregateNavigation_IsEitherAutoIncludedOrCoveredByCustomRepository()
```

Der Test iteriert über `Model.GetEntityTypes()`, filtert auf Aggregate-Roots und prüft für jede Collection-Navigation, dass entweder `AutoInclude` gesetzt ist oder ein spezifisches Repository registriert wurde. Das ist die einzige Variante, die auch in zwei Jahren noch hält.

**Schritt 4 — `AddAsync` vereinfachen.** `DbSet.AddAsync` ist nur bei serverseitigen Wertgeneratoren (HiLo-Sequenzen) erforderlich. Bei client-generierten typisierten IDs — dem hier vorgesehenen Modell — ist es nie nötig und erzeugt nur eine überflüssige `Task`-Allokation pro Aufruf. Siehe IMP-42.

---

# IMP-16 - Kein Validierungs-Behavior, Mehrfachfehler nicht erzeugbar

- **Hoch**
-   - **Status**: teilweise gelöst. Schritt 4 (explizite Behavior-Reihenfolge via `AddPipelineBehavior(type, order)`) ist umgesetzt; Schritt 1-3 (`IRequestValidator` + `ValidationBehavior`, Mehrfach-/Feldvalidierung) stehen noch aus. Slot `200` ist dafür reserviert.

## Beschreibung

Das Fehlermodell ist auf mehrere Fehler ausgelegt:

```csharp
public IReadOnlyList<Failure> Failures => _failures;
public static Result Failure(IReadOnlyList<Failure> failures)
```

Erzeugt werden Mehrfachfehler jedoch **nirgends**. Der einzige Produzent von `Failure` ist `ExceptionToResultBehavior`, und der erzeugt aus einer Exception genau einen:

```csharp
catch (DomainValidationException exception)
{
    return FailureResults.Create<TResponse>(Failure.Validation(ValidationFailureCode, exception.Message));
}
```

Es existiert kein Validierungs-Behavior, kein Validator-Vertrag und keine Möglichkeit, ein Command vor Erreichen der Domäne zu prüfen. Daraus folgen zwei Probleme:

**1. Feld-für-Feld-Validierung ist unmöglich.** Ein Formular mit fünf ungültigen Feldern liefert genau einen Fehler — den ersten, auf den die Domäne stößt. Der Nutzer korrigiert ihn, sendet erneut, bekommt den nächsten. Das ist bei jeder Web-Oberfläche inakzeptabel und lässt sich mit dem aktuellen Modell nicht umgehen.

**2. Technische Validierung landet in der Domäne.** „Name darf nicht leer sein", „Datum nicht in der Zukunft", „Seitenzahl > 0" gehören nicht ins Aggregat — sie sind Eingabeprüfungen, keine Geschäftsregeln. Weil es keinen anderen Ort gibt, wandern sie in `RuleChecker.Check(...)`-Aufrufe in den Aggregat-Konstruktoren und vermischen sich dort mit echten Invarianten. Die Unterscheidung zwischen `IDomainValidationRule` und `IBusinessRule`, die die Domain sauber anlegt, verwässert dadurch.

Ergänzend fehlt eine Möglichkeit, eigene Behaviors mit definierter Position zu registrieren. Ein in `configure(...)` registriertes Behavior landet **vor** allen eingebauten und wird damit die äußerste Schicht — außerhalb von Logging und Exception-Übersetzung. Für ein Validierungs-Behavior ist das die falsche Position, und es gibt keinen Weg, das zu korrigieren.

## Lösungsvorschlag

**Schritt 1 — Validator-Vertrag in Application:**

```csharp
namespace BuildingBlocks.Application;

public interface IRequestValidator<in TRequest>
{
    ValueTask<IReadOnlyList<Failure>> ValidateAsync(TRequest request, CancellationToken cancellationToken);
}
```

`ValueTask` weil die überwiegende Mehrheit synchron validiert; `IReadOnlyList<Failure>` statt `bool` weil das Ergebnis direkt in `Result.Failure(...)` fließt. Kein eigener `ValidationResult`-Typ — das vorhandene `Failure` ist bereits das richtige Vokabular.

**Schritt 2 — Behavior in Infrastructure:**

```csharp
public sealed class ValidationBehavior<TRequest, TResponse>(IEnumerable<IRequestValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse>
    where TResponse : Result
{
    private readonly IRequestValidator<TRequest>[] _validators = [.. validators];

    public async Task<TResponse> Handle(
        TRequest request,
        RequestPipelineContinuation<TResponse> continuation,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(continuation);

        if (_validators.Length == 0)
        {
            return await continuation(cancellationToken).ConfigureAwait(false);
        }

        List<Failure>? failures = null;

        foreach (var validator in _validators)
        {
            var result = await validator.ValidateAsync(request, cancellationToken).ConfigureAwait(false);
            if (result.Count > 0)
            {
                (failures ??= []).AddRange(result);
            }
        }

        return failures is null
            ? await continuation(cancellationToken).ConfigureAwait(false)
            : FailureResults.Create<TResponse>(failures);
    }
}
```

Der Fast-Path ohne Validatoren ist wichtig: Das Behavior läuft für **jeden** Request, und die überwiegende Mehrheit hat keinen Validator.

`FailureResults.Create` muss dafür eine Überladung für `IReadOnlyList<Failure>` bekommen (bzw. entfällt komplett, siehe IMP-27).

**Schritt 3 — Position in der Pipeline.** Das Behavior gehört zwischen Logging und Exception-Übersetzung, jedenfalls **außerhalb** der Unit of Work — ein ungültiger Request soll gar nicht erst eine Transaktion eröffnen:

```
LoggingBehavior → ValidationBehavior → ExceptionToResultBehavior → UnitOfWorkBehavior → Handler
```

**Schritt 4 — Behavior-Registrierung mit expliziter Ordnung.** Damit Services eigene Behaviors an definierter Stelle einhängen können (Autorisierung, Multi-Tenancy, Idempotenz, Caching):

```csharp
public BuildingBlocksOptions AddPipelineBehavior(Type openGenericBehavior, int order);
```

Konvention: negative Werte laufen vor den eingebauten, positive danach; die eingebauten belegen 0, 100, 200, 300. `Sender.BuildPipeline` sortiert entsprechend. Das macht aus einer impliziten Eigenschaft der Aufrufreihenfolge einen expliziten, testbaren Vertrag — und löst gemeinsam mit IMP-03 die gesamte Reihenfolgeproblematik.

**Bewusst nicht vorgeschlagen:** FluentValidation. Es passt nicht zur „alles handgeschrieben"-Linie des Projekts (kein MediatR, kein AutoMapper), bringt eine große Abhängigkeit für wenig Gegenwert, und einfache Validatoren als kleine Klassen sind lesbarer als Fluent-Ketten. Wenn die Anzahl der Validatoren später Boilerplate erzeugt, ist ein Source Generator die konsistentere Antwort.

---

# IMP-17 - `Failure` ohne Zielfeld und ohne fachliche Fehlercodes

- **Hoch**

## Beschreibung

```csharp
public sealed record Failure(string Code, string Message, FailureCategory Category);
```

Zwei getrennte Lücken:

**1. Kein Zielfeld.** Für eine Validierung braucht der Client zwingend die Information, **welches Feld** betroffen ist. `Failure` hat weder `Target`/`PropertyName` noch ein Metadaten-Dictionary. Ein BFF kann daraus keine `ProblemDetails` mit `errors`-Objekt bauen — das Standardformat, das jedes Frontend-Framework erwartet. Zusammen mit IMP-16 (keine Mehrfachfehler) bedeutet das: Feld-Validierung im UI ist mit dem aktuellen Modell nicht umsetzbar.

**2. Fehlercodes sind technische Konstanten, keine fachlichen Verträge.** `ExceptionToResultBehavior` setzt für **alle** Domänenfehler denselben Code:

```csharp
public const string ValidationFailureCode = "domain.validation";
public const string BusinessRuleFailureCode = "domain.business_rule";
```

Ein Client kann „Rezept hat zu viele Zutaten" nicht von „Rezept ist bereits archiviert" unterscheiden — beide kommen als `domain.business_rule` mit unterschiedlichem `Message`-String. Die Folge in der Praxis: Clients matchen auf Meldungstexte. Das bricht bei der ersten Umformulierung und macht Lokalisierung unmöglich.

Der Grund liegt eine Ebene tiefer: `IBusinessRule` und `IDomainValidationRule` haben nur `Message`, und `BusinessRuleViolationException` transportiert nur einen String. Die Information, **welche Regel** gebrochen wurde, geht beim Werfen verloren — obwohl sie als Typ vorliegt.

Nebeneffekt: `exception.Message` wird ungefiltert in `Failure.Message` und damit potenziell bis zum Endnutzer durchgereicht. Bei einer selbst formulierten Regelmeldung ist das gewollt; sobald eine Meldung interne Details enthält, ist es ein Informationsleck.

## Lösungsvorschlag

**Schritt 1 — `Failure` um Ziel und Metadaten erweitern:**

```csharp
public sealed record Failure
{
    public string Code { get; }
    public string Message { get; }
    public FailureCategory Category { get; }
    public string? Target { get; init; }                              // "Servings", "Ingredients[3].Amount"
    public IReadOnlyDictionary<string, object?> Metadata { get; init; } = ReadOnlyDictionary<string, object?>.Empty;

    public static Failure Validation(string code, string message, string? target = null) => …;
}
```

`Target` als optionaler `init`-Parameter hält die vorhandenen Factory-Aufrufe kompatibel. `Metadata` deckt strukturierte Zusatzinformation ab (`{ "max": 50, "actual": 51 }`), die ein Client für eine gute Meldung braucht, ohne den Text zu parsen.

**Schritt 2 — Fehlercode zum fachlichen Vertrag machen.** Der Code entsteht bei der Regel, nicht beim Behavior:

```csharp
public interface IBusinessRule
{
    string Code { get; }       // "nutrition.recipe.too-many-ingredients"
    string Message { get; }
    bool IsBroken();
}
```

Transport über die Exception:

```csharp
public sealed class BusinessRuleViolationException : Exception
{
    public string? Code { get; }

    public BusinessRuleViolationException(IBusinessRule rule)
        : base(rule.Message) => Code = rule.Code;
}
```

`RuleChecker` befüllt das automatisch:

```csharp
public static void Check(IBusinessRule rule)
{
    ArgumentNullException.ThrowIfNull(rule);
    if (rule.IsBroken())
    {
        throw new BusinessRuleViolationException(rule);
    }
}
```

Und `ExceptionToResultBehavior` reicht ihn durch, mit dem bisherigen Wert als Rückfallebene:

```csharp
catch (BusinessRuleViolationException exception)
{
    return FailureResults.Create<TResponse>(
        Failure.BusinessRule(exception.Code ?? BusinessRuleFailureCode, exception.Message));
}
```

Das ist die eigentlich wertvolle Änderung: Sie schließt die Lücke zwischen Domain und Application, ohne dass die Domain die Application kennt. Die Regel — das Objekt, das die Fachlichkeit trägt — definiert ihren eigenen stabilen Identifikator, und dieser Identifikator wird zum API-Vertrag.

**Schritt 3 — Codes konventionieren und prüfbar machen.** Empfohlenes Schema: `<context>.<aggregat>.<regel>`, durchgehend kebab-case. Ein Test, der alle `IBusinessRule`- und `IDomainValidationRule`-Implementierungen im Assembly reflektiert und Format sowie Eindeutigkeit prüft, kostet zwanzig Zeilen und verhindert Drift dauerhaft.

**Schritt 4 — Meldungstrennung.** Mittelfristig `Message` als _Entwickler_-Meldung deklarieren und die nutzerseitige Formulierung über `Code` + `Metadata` im BFF erzeugen. Das ist die Voraussetzung für Lokalisierung und verhindert, dass interne Formulierungen nach außen gelangen. Als Zwischenschritt genügt die Konvention, dass Regel-Meldungen ausschließlich fachlich formuliert sind — dann ist das Durchreichen unbedenklich.

---

# IMP-18 - `FailureCategory` fehlen Autorisierung und Unerwartet

- **Hoch**

## Beschreibung

```csharp
public enum FailureCategory
{
    Validation,
    BusinessRule,
    NotFound,
    Conflict,
}
```

Der BFF leitet aus dieser Kategorie den HTTP-Status ab. Zwei praktisch unverzichtbare Fälle fehlen:

**`Unauthorized` / `Forbidden`.** VitalSync ist eine Multi-User-Anwendung — jeder Nutzer sieht nur seine eigenen Rezepte, Mahlzeiten und Trainings. Die Prüfung „gehört dieses Rezept dem anfragenden Nutzer?" findet im Handler statt (oder in einem Autorisierungs-Behavior, siehe IMP-16). Sie muss ein Ergebnis liefern, das der BFF auf **403** abbilden kann.

Aktuell gibt es zwei gleich schlechte Auswege: `NotFound` zurückgeben (verschleiert die Ursache, ist bei Existenzprüfungen aber sogar bewusst gewollt — nur eben nicht als Universallösung) oder `BusinessRule` missbrauchen (liefert 422 statt 403, wodurch Clients Autorisierungsfehler nicht als solche behandeln können — z. B. kein Re-Login-Flow).

**`Unexpected`.** Es gibt keine Kategorie für „etwas ist schiefgegangen". Unerwartete Exceptions blubbern derzeit an der Pipeline vorbei zu einem globalen Handler. Das ist eine legitime Entscheidung — aber sie schließt aus, dass ein Handler bewusst einen unerwarteten Zustand als `Result` meldet (etwa ein fehlgeschlagener Aufruf eines externen Systems, der kein Programmfehler ist).

Weil das Enum die Übersetzungstabelle zum Transport darstellt, ist jede Lücke darin eine Lücke im gesamten API-Verhalten der Plattform.

## Lösungsvorschlag

**Schritt 1 — Enum ergänzen.** Werte anhängen ist rückwärtskompatibel, solange keine expliziten numerischen Werte vergeben und keine Werte umsortiert werden:

```csharp
public enum FailureCategory
{
    Validation,
    BusinessRule,
    NotFound,
    Conflict,
    Unauthorized,   // 401 — nicht authentifiziert
    Forbidden,      // 403 — authentifiziert, aber nicht berechtigt
    Unexpected,     // 500 — unerwarteter Zustand, bewusst als Result gemeldet
}
```

`Unauthorized` und `Forbidden` bewusst getrennt: Der Unterschied zwischen „melde dich an" und „du darfst das nicht" ist für den Client verhaltensrelevant.

Passende Factories auf `Failure`:

```csharp
public static Failure Unauthorized(string code, string message) => new(code, message, FailureCategory.Unauthorized);
public static Failure Forbidden(string code, string message)    => new(code, message, FailureCategory.Forbidden);
public static Failure Unexpected(string code, string message)   => new(code, message, FailureCategory.Unexpected);
```

**Schritt 2 — Vollständigkeit der Abbildung erzwingen.** Die Zuordnung Kategorie → HTTP-Status liegt korrekterweise beim BFF. Damit eine neue Kategorie dort nicht übersehen wird, sollte die Abbildung als `switch`-**Expression ohne Discard-Arm** geschrieben werden:

```csharp
private static int ToStatusCode(FailureCategory category) => category switch
{
    FailureCategory.Validation   => StatusCodes.Status400BadRequest,
    FailureCategory.BusinessRule => StatusCodes.Status422UnprocessableEntity,
    FailureCategory.NotFound     => StatusCodes.Status404NotFound,
    FailureCategory.Conflict     => StatusCodes.Status409Conflict,
    FailureCategory.Unauthorized => StatusCodes.Status401Unauthorized,
    FailureCategory.Forbidden    => StatusCodes.Status403Forbidden,
    FailureCategory.Unexpected   => StatusCodes.Status500InternalServerError,
};
```

Ohne `_ =>`-Arm meldet der Compiler bei einem neuen Enum-Wert **CS8509** — und weil das Repo `TreatWarningsAsErrors` gesetzt hat, wird daraus ein Build-Fehler. Das ist die eleganteste verfügbare Absicherung: Eine neue Kategorie kann nicht eingeführt werden, ohne alle Abbildungen zu aktualisieren.

**Schritt 3 — Autorisierungs-Behavior als natürlicher Nutzer.** Mit `Forbidden` wird ein Autorisierungs-Behavior sinnvoll umsetzbar (registriert über den Ordnungsmechanismus aus IMP-16, direkt nach Logging und vor Validierung):

```csharp
public interface IRequestAuthorizer<in TRequest>
{
    ValueTask<Failure?> AuthorizeAsync(TRequest request, CancellationToken cancellationToken);
}
```

Damit verlässt die Eigentümerprüfung die Handler und wird zu einem deklarativen, testbaren Querschnittsbelang — statt in jedem Handler als wiederholte `if`-Abfrage aufzutauchen.

---

# IMP-19 - Ein Assembly für EF Core, Marten, Wolverine und RabbitMQ

- **Hoch**

## Beschreibung

`BuildingBlocks.Infrastructure` referenziert:

```xml
<PackageReference Include="Marten" Version="9.20.1" />
<PackageReference Include="Microsoft.EntityFrameworkCore" Version="10.0.10" />
<PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" Version="10.0.3" />
<PackageReference Include="WolverineFx.RabbitMQ" Version="6.23.0" />
<PackageReference Include="WolverineFx.Marten" Version="6.23.0" />
<PackageReference Include="WolverineFx.EntityFrameworkCore" Version="6.23.0" />
```

Jeder Konsument bekommt **alles**. Ein Service, der nur EF Core nutzt und keine Events publiziert, lädt trotzdem Marten, den RabbitMQ-Client und drei Wolverine-Pakete.

Konkrete Auswirkungen:

- **Startzeit und Speicher.** Für ein Aspire-Setup mit mehreren Services pro Entwicklermaschine summiert sich das messbar. Trimming und AOT sind mit diesem Abhängigkeitsprofil praktisch ausgeschlossen.
- **Kopplung der Versionierung.** Ein Marten-Update betrifft Services, die Marten nie benutzen. Ein Breaking Change in einem der sechs Pakete blockiert alle.
- **Angriffsfläche.** Jede transitive Abhängigkeit ist Teil der Supply Chain jedes Services — auch der ungenutzten.
- **Widerspruch zur eigenen Zielsetzung.** Die Building Blocks sind ausdrücklich als „wiederverwendbare, VitalSync-unabhängige Plattform" konzipiert. Eine wiederverwendbare Plattform, die einen kompletten Technologie-Stack erzwingt, ist keine.
- **Verwischte Grenzen im Code.** Weil alles im selben Assembly liegt, kann `EfCoreUnitOfWork` `Wolverine.EntityFrameworkCore` verwenden und `MartenUnitOfWork` gleichzeitig `Wolverine.Marten` — ohne dass irgendetwas erzwingt, dass diese Abhängigkeiten getrennt bleiben. Das macht spätere Trennung teurer, je länger man wartet.

## Lösungsvorschlag

Aufteilung entlang der Technologiegrenzen. Das ist ein reines Verschiebe-Refactoring — der Code selbst ist bereits sauber nach Ordnern getrennt, was die Aufteilung fast mechanisch macht:

```
BuildingBlocks.Infrastructure                → Dispatching/, Events/, DependencyInjection/
                                               Abhängigkeiten: DI.Abstractions, Logging.Abstractions
                                               (kein EF, kein Marten, kein Wolverine)

BuildingBlocks.Persistence.EfCore            → EfCoreRepository, EfCoreUnitOfWork,
                                               EntityKeyValueConverter, ModelBuilder-Extension

BuildingBlocks.Persistence.Marten            → MartenEventSourcedRepository, MartenUnitOfWork,
                                               MartenAggregateTracker, EntityKeyFormatter

BuildingBlocks.Messaging.Wolverine           → Envelope, Serializer, EnvelopeHandler,
                                               Transport, WolverineOptions-Extensions
```

Das Kern-Assembly wird dadurch abhängigkeitsarm — es enthält nur noch `Sender`, die Behaviors und die Event-Verteilung, alles gegen `Microsoft.Extensions.*`-Abstraktionen. Ein Service, der nur die CQRS-Pipeline will, bekommt genau das.

**Konfigurationsoberfläche pro Paket:**

```csharp
builder.Services
    .AddBuildingBlocks(o => o.AddHandlersFrom(assembly))
    .AddEfCorePersistence<NutritionWriteDbContext>()
    .AddWolverineMessaging(rabbitMqUri);
```

Jede Extension lebt in ihrem eigenen Paket. Die aktuelle `BuildingBlocksOptions` mit `UseEfCorePersistence`/`UseMartenEventSourcing`/`UseWolverineMessaging` wird dabei aufgelöst — sie ist heute der Grund, warum das Kern-Assembly alle Pakete braucht: Sie referenziert `DbContext`, `Marten` und `Wolverine` in einer einzigen Klasse.

**Reihenfolge-Abhängigkeiten sauber lösen.** Nach der Aufteilung muss die Messaging-Konfiguration wissen, welche Persistenz gewählt wurde (EF-Outbox vs. Marten-Outbox). Statt einer direkten Abhängigkeit reicht ein Marker im Service-Container, den die Messaging-Extension beim Konfigurieren abfragt — oder, expliziter, ein Parameter:

```csharp
.AddWolverineMessaging(rabbitMqUri, outbox: OutboxStore.EntityFrameworkCore)
```

**Zeitfenster.** Es gibt aktuell **keinen einzigen Konsumenten** der Building Blocks. Die Aufteilung kostet damit heute etwa zwei Stunden: Projekte anlegen, Dateien verschieben, Namespaces korrigieren, `.slnx` ergänzen. Mit dem ersten produktiven Service wird daraus ein Refactoring mit Breaking Changes über alle Service-Projekte hinweg — und wird erfahrungsgemäß nie durchgeführt. Das ist der Punkt aus diesem Dokument mit dem stärksten Zeitbezug.

---

# IMP-20 - `DbContext` als DI-Schlüssel kollidiert mit dem Read/Write-Paar

- **Hoch**

## Beschreibung

```csharp
public BuildingBlocksOptions UseEfCorePersistence<TContext>() where TContext : DbContext
{
    _services.TryAddScoped<DbContext>(static provider => provider.GetRequiredService<TContext>());
    _services.TryAddScoped(typeof(IRepository<,>), typeof(EfCoreRepository<,>));
    …
}
```

`EfCoreRepository` injiziert den **Basistyp** `DbContext`. Damit wird `DbContext` zu einem globalen DI-Schlüssel, der genau einmal belegt werden kann.

Das Datenbank-Design des Projekts sieht pro Bounded Context ein **Paar** aus Write- und Read-Datenbank vor. Ein Service wird also typischerweise zwei Kontexte haben:

```csharp
services.AddDbContext<NutritionWriteDbContext>(...);   // nutrition-write
services.AddDbContext<NutritionReadDbContext>(...);    // nutrition-read
```

Aktuell funktioniert das zufällig richtig — `TryAddScoped` bindet an den in `UseEfCorePersistence<T>()` genannten Typ, und das ist der Write-Kontext. Aber:

- Registriert ein Service versehentlich `UseEfCorePersistence<NutritionReadDbContext>()`, schreiben **alle** Repositories in die Read-Datenbank. Kein Fehler, keine Warnung — bis auffällt, dass die Write-DB leer bleibt.
- Jeder andere Code, der `DbContext` injiziert (Health Checks, Migrations-Runner, ein generischer Helper), bekommt implizit den Write-Kontext, ohne dass das an der Signatur erkennbar wäre.
- Der Zusammenhang „welcher Kontext ist gemeint" ist im Code nicht sichtbar. `EfCoreRepository(DbContext context)` sagt nicht, welcher.

Das ist ein Fall von unnötiger Typunschärfe: Der konkrete Typ ist als Generic-Parameter bereits vorhanden und wird direkt danach weggeworfen.

## Lösungsvorschlag

**Schritt 1 — Repository am konkreten Kontext typisieren:**

```csharp
public class EfCoreRepository<TContext, TAggregate, TKey>(TContext context) : IRepository<TAggregate, TKey>
    where TContext : DbContext
    where TAggregate : AggregateRoot<TKey>
    where TKey : struct, IEntityKey
```

Registrierung mit gebundenem Kontext:

```csharp
public BuildingBlocksOptions UseEfCorePersistence<TContext>() where TContext : DbContext
{
    _services.TryAddScoped(typeof(IRepository<,>), typeof(EfCoreRepository<,,>).MakeGenericType(typeof(TContext)));
    …
}
```

_(Anmerkung: Ein teilweise geschlossenes offenes Generic ist mit `Microsoft.Extensions.DependencyInjection` nicht direkt registrierbar. Praktikable Varianten: eine abgeleitete Hilfsklasse pro Kontext erzeugen, oder — einfacher und empfohlen — eine `IWriteDbContextAccessor<TContext>`-Indirektion bzw. ein Keyed-Service unter einem eigenen Marker-Interface:)_

```csharp
internal interface IWriteDbContext { DbContext Instance { get; } }

internal sealed class WriteDbContext<TContext>(TContext context) : IWriteDbContext
    where TContext : DbContext
{
    public DbContext Instance => context;
}

// Registrierung
_services.TryAddScoped<IWriteDbContext, WriteDbContext<TContext>>();
// Repository injiziert IWriteDbContext statt DbContext
```

Die globale `DbContext`-Registrierung entfällt damit ersatzlos. Der Marker macht die Absicht explizit: Repositories schreiben in den **Write**-Kontext, und das steht im Typ.

**Schritt 2 — Read-Seite bewusst freilassen.** Query-Handler sollten ihren Read-Kontext (oder ihre Dapper-Connection) direkt und konkret injizieren:

```csharp
internal sealed class GetRecipeSummaryQueryHandler(NutritionReadDbContext read) : IQueryHandler<…>
```

Das ist Absicht und richtig: Die Read-Seite ist bewusst nicht abstrahiert, weil sie je nach Abfrage EF, Dapper oder rohes SQL nutzen darf. Nach Schritt 1 gibt es keinen Weg mehr, sie versehentlich über die Repository-Registrierung zu erwischen.

**Schritt 3 — Fehlkonfiguration früh erkennen.** Als Teil des Startup-Checks aus IMP-13: Wenn mehr als ein `DbContext` registriert ist und `UseEfCorePersistence<T>()` auf einen Kontext zeigt, dessen Name auf `Read` endet, ist das mit sehr hoher Wahrscheinlichkeit ein Fehler und sollte mindestens geloggt werden. Eine Heuristik über Namen ist unschön — sie fängt aber genau den Fehler, der hier teuer ist, und kostet nichts.

---

# IMP-21 - `IRepository` koppelt an die konkrete Domain-Basisklasse

- **Hoch**

## Beschreibung

Die beiden Repository-Verträge in der Application-Schicht constrainen unterschiedlich:

```csharp
public interface IRepository<TAggregate, in TKey>
    where TAggregate : AggregateRoot<TKey>                        // konkrete Klasse

public interface IEventSourcedRepository<TAggregate, in TKey>
    where TAggregate : class, IEventSourcedAggregateRoot<TKey>    // Interface
```

`IRepository` bindet an die Implementierungsklasse `AggregateRoot<TKey>`, obwohl mit `IAggregateRoot<TKey>` ein passendes Interface existiert und im selben Namespace liegt.

Warum das relevant ist:

- **Inkonsistenz ohne Begründung.** Zwei benachbarte Verträge, zwei unterschiedliche Kopplungsniveaus. Wer den einen liest, kann nicht auf den anderen schließen.
- **Testbarkeit.** Ein Test-Double für ein Aggregat muss von `AggregateRoot<TKey>` erben statt nur `IAggregateRoot<TKey>` zu implementieren. Bei einer Klasse mit `sealed override Equals`/`GetHashCode` und einem `protected`-Konstruktor ist das mehr Aufwand als nötig.
- **Erweiterbarkeit.** Ein Aggregat, das sowohl state-stored persistiert als auch (etwa für eine Migration) über den ES-Pfad geladen wird, kann nicht beide Repositories bedienen — die Constraints sind unvereinbar.
- **Prinzip.** Die Application-Schicht soll gegen Verträge programmieren, nicht gegen Implementierungen. Die Kopplung an eine konkrete Basisklasse ist genau das, was die Schichtung verhindern soll — hier innerhalb desselben Schichtenpaars, aber mit derselben Wirkung.

Bei einer Umsetzung von IMP-10 (einheitliches Aggregat-Modell) verschärft sich das zusätzlich, weil sich die Basisklassenhierarchie dann ändert.

## Lösungsvorschlag

**Schritt 1 — Auf das Interface umstellen:**

```csharp
public interface IRepository<TAggregate, in TKey>
    where TAggregate : class, IAggregateRoot<TKey>
    where TKey : struct, IEntityKey
{
    Task<TAggregate?> GetByIdAsync(TKey id, CancellationToken cancellationToken);

    void Add(TAggregate aggregate);
    void Remove(TAggregate aggregate);
}
```

`class` ist notwendig, weil `IAggregateRoot<TKey>` allein Werttypen zuließe — die hier weder sinnvoll noch von EF Core unterstützt sind.

**Schritt 2 — Implementierung nachziehen.** `EfCoreRepository` braucht `TAggregate` als Referenztyp für `DbSet<TAggregate>`, was durch das `class`-Constraint bereits erfüllt ist. Die Implementierung greift nicht auf Member von `AggregateRoot<TKey>` zu — die Umstellung ist dort eine reine Signaturänderung ohne Codeanpassung.

**Schritt 3 — Als Regel festhalten.** Der zugrunde liegende Punkt ist allgemeiner und lohnt eine kurze, prüfbare Konvention:

> Verträge in `BuildingBlocks.Application` constrainen ausschließlich gegen Interfaces aus `BuildingBlocks.Domain`, nie gegen deren Basisklassen.

Ein Architekturtest (siehe IMP-09) kann das durchsetzen: über alle öffentlichen generischen Typen in `BuildingBlocks.Application` iterieren und prüfen, dass kein Constraint eine `class` aus `BuildingBlocks.Domain` ist. Zwanzig Zeilen, die eine ganze Kategorie schleichender Kopplung ausschließen.

**Verwandt — `IEntityKey`-Constraints prüfen.** Bei dieser Gelegenheit lohnt ein Blick auf die Varianz-Modifikatoren (IMP-43): `in TKey` ist bei `where TKey : struct` wirkungslos und suggeriert eine Flexibilität, die nicht existiert.

---

# IMP-22 - `AssemblyQualifiedName` als Event-Typ-Token

- **Mittel**

## Beschreibung

`DomainEventEnvelopeSerializer` identifiziert Event-Typen über den vollqualifizierten Assembly-Namen:

```csharp
var eventTypeName = eventType.AssemblyQualifiedName
    ?? throw new InvalidOperationException(…);
// → "VitalSync.Nutrition.Domain.RecipeCreated, VitalSync.Nutrition.Domain, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null"

var eventType = Type.GetType(envelope.EventTypeName, throwOnError: true)!;
var domainEvent = JsonSerializer.Deserialize(envelope.Payload, eventType, SerializerOptions);
```

Drei getrennte Probleme:

**1. Versionierung.** Die Assembly-Version steht im Token. Envelopes liegen in der durablen Wolverine-Inbox und überleben Neustarts und Deployments. Ein Deployment mit geänderter Assembly-Version kann persistierte Nachrichten nicht mehr auflösen. `Type.GetType` toleriert Versionsabweichungen bei nicht-signierten Assemblies zwar meist, aber „meist" ist für die Wiederherstellung nach einem Absturz keine belastbare Eigenschaft.

**2. Refactoring-Sperre.** Namespace umbenennen, Event in ein anderes Projekt verschieben, Assembly umbenennen — jede dieser normalen Operationen macht alle in-flight Nachrichten unlesbar. Der Fehler tritt erst beim Deployment auf, betrifft nur die zum Umstellungszeitpunkt offenen Nachrichten und ist damit schwer zu reproduzieren.

**3. Deserialisierung eines beliebigen Typnamens.** `Type.GetType(<String aus der Nachricht>)` gefolgt von `JsonSerializer.Deserialize(payload, type)` lädt und instanziiert den Typ, der im Payload steht. Solange die Queue ausschließlich lokal ist und nur von diesem Prozess befüllt wird, ist das Risiko theoretisch. Es wird real, sobald ein `DomainEventEnvelope` je über RabbitMQ läuft oder die Outbox-Tabelle anderweitig beschreibbar ist. Da die Envelope-Klasse `public` ist und nichts sie auf die lokale Queue festnagelt, ist das kein hypothetisches Szenario.

Dasselbe Muster gilt auf einer zweiten Ebene: Marten leitet den Event-Typ-Alias im Event Store standardmäßig vom CLR-Typnamen ab. Auch dort ist eine Umbenennung eine Breaking Change — nur mit einem deutlich schmerzhafteren Ergebnis, weil der Event Store die Quelle der Wahrheit ist.

## Lösungsvorschlag

**Schritt 1 — Logischer Event-Name als expliziter Vertrag:**

```csharp
namespace BuildingBlocks.Domain;

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class DomainEventNameAttribute(string name) : Attribute
{
    public string Name { get; } = name;
}

// [DomainEventName("nutrition.recipe-created.v1")]
public sealed record RecipeCreated(RecipeId Id, string Name, int Servings) : DomainEvent;
```

Der Name ist ein bewusst gewählter, stabiler Bezeichner — unabhängig von Namespace, Assembly und Klassennamen, und mit expliziter Version. Umbenennungen im Code werden dadurch folgenlos.

**Schritt 2 — Registry mit Allowlist:**

```csharp
internal sealed class DomainEventTypeRegistry
{
    private readonly Dictionary<string, Type> _byName;
    private readonly Dictionary<Type, string> _byType;

    public string GetName(Type eventType) =>
        _byType.TryGetValue(eventType, out var name)
            ? name
            : throw new InvalidOperationException(
                $"Der Event-Typ '{eventType}' ist nicht registriert. " +
                "Ergänze [DomainEventName] und stelle sicher, dass die Assembly in AddHandlersFrom übergeben wird.");

    public Type GetType(string name) =>
        _byName.TryGetValue(name, out var type)
            ? type
            : throw new InvalidOperationException(
                $"Unbekannter Event-Name '{name}'. Wurde ein Event entfernt, das noch in der Outbox liegt?");
}
```

Aufgebaut beim Start aus denselben Assemblies, die `AddHandlersFrom` bekommt. Der Serializer schlägt nur noch dort nach:

```csharp
public DomainEventEnvelope Wrap(IDomainEvent domainEvent) =>
    new(registry.GetName(domainEvent.GetType()), JsonSerializer.Serialize(...));

public IDomainEvent Unwrap(DomainEventEnvelope envelope) =>
    (IDomainEvent)JsonSerializer.Deserialize(envelope.Payload, registry.GetType(envelope.EventTypeName), ...)!;
```

Damit sind alle drei Probleme gleichzeitig gelöst: Version und Namespace spielen keine Rolle mehr, und es kann nur noch ein explizit registrierter Typ deserialisiert werden. Der Serializer wird dadurch von einer statischen Klasse zu einem injizierten Service — was ihn nebenbei testbar macht.

**Schritt 3 — Registrierung beim Start erzwingen.** Ein fehlendes Attribut soll nicht erst beim ersten Auftreten des Events auffallen. Der Startup-Check aus IMP-13 prüft mit: Jeder `IDomainEvent`-Typ in den registrierten Assemblies hat ein `[DomainEventName]`, und alle Namen sind eindeutig.

**Schritt 4 — Marten-Aliase analog setzen.** Denselben Namen für den Event Store verwenden, damit es genau eine Quelle der Wahrheit gibt:

```csharp
foreach (var (name, type) in registry)
{
    options.Events.MapEventType(type, name);
}
```

**Schritt 5 — Serializer-Optionen festlegen.** Aktuell:

```csharp
private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.General);
```

Für persistierte Daten sollten die Optionen explizit und stabil sein — Casing, `NumberHandling`, `DefaultIgnoreCondition`. Insbesondere fehlt ein Konverter für typisierte IDs: `readonly record struct RecipeId(Guid Value)` serialisiert derzeit als `{"Value":"…"}` und rundet über die Ctor-Bindung von System.Text.Json zufällig richtig. Ein expliziter `EntityKeyJsonConverter` — als Gegenstück zum bereits vorhandenen `EntityKeyValueConverter` für EF Core — macht daraus einen kompakten, stabilen Skalarwert und schließt eine Symmetrielücke:

```csharp
// Ziel: "recipeId": "8f3a…" statt "recipeId": { "Value": "8f3a…" }
```

Ein Round-Trip-Test mit typisierter ID, `decimal` und `DateTimeOffset` (siehe IMP-09, Stufe 2) sichert das ab.

---

# IMP-23 - Marten-Stream-Key hängt am Klassennamen

- **Mittel**

## Beschreibung

```csharp
public static string GetStreamKey(Type aggregateType, object key) =>
    string.Create(CultureInfo.InvariantCulture, $"{aggregateType.Name}/{GetKeyValue(key)}");
// → "Workout/8f3a2c1e-…"
```

Der Stream-Key — der **primäre Schlüssel im Event Store** — enthält den CLR-Klassennamen des Aggregats. Wird `Workout` zu `TrainingSession` umbenannt, erzeugt `GetStreamKey` ab dem Deployment `"TrainingSession/8f3a…"`. Alle bestehenden Streams liegen unter `"Workout/8f3a…"` und sind damit unauffindbar.

Das Fehlerbild ist besonders unangenehm: `FetchStreamAsync` liefert eine leere Liste, `GetByIdAsync` gibt `null` zurück, und der Handler meldet korrekt `NotFound`. Es gibt **keinen Fehler** — nur Daten, die verschwunden scheinen. Ein anschließender Schreibvorgang legt einen neuen, leeren Stream unter dem neuen Namen an, wodurch der alte endgültig verwaist.

Da der Event Store bei Event Sourcing die alleinige Quelle der Wahrheit ist, ist das der teuerste Datenverlust, der in diesem System möglich ist — und er wird durch ein gewöhnliches Rename ausgelöst, das jede IDE als sichere Operation anbietet.

Nebenpunkt: Die Erzeugung erfolgt über Reflection auf `IEntityKey<TValue>.Value` mit kompilierten Expressions und Cache — technisch sauber gelöst. Sie wirft aber `InvalidOperationException`, wenn ein Key nur `IEntityKey` (ohne `TValue`) implementiert. Auch das ist ein Laufzeitfehler für etwas, das ein Constraint sein könnte.

## Lösungsvorschlag

**Schritt 1 — Stabiles Stream-Präfix als expliziter Vertrag**, analog zu IMP-22:

```csharp
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class StreamNameAttribute(string name) : Attribute
{
    public string Name { get; } = name;
}

[StreamName("fitness.workout")]
public sealed class Workout : EventSourcedAggregateRoot<WorkoutId, WorkoutState> { … }
```

```csharp
public static string GetStreamKey(Type aggregateType, object key)
{
    var prefix = StreamNames.GetOrAdd(aggregateType, static type =>
        type.GetCustomAttribute<StreamNameAttribute>()?.Name
        ?? throw new InvalidOperationException(
            $"Das Aggregat '{type}' hat kein [StreamName]-Attribut. " +
            "Der Stream-Name ist Teil des Persistenzvertrags und muss stabil und explizit sein."));

    return string.Create(CultureInfo.InvariantCulture, $"{prefix}/{GetKeyValue(key)}");
}
```

Der harte Fehler ohne Attribut ist beabsichtigt: Ein implizit aus dem Klassennamen abgeleiteter Persistenzschlüssel ist die eigentliche Ursache. Der Zwang, ihn einmal bewusst zu benennen, kostet eine Zeile pro Aggregat und macht das Rename dauerhaft sicher.

**Schritt 2 — Beim Start prüfen.** Als Teil des Startup-Checks aus IMP-13: Alle `IEventSourcedAggregateRoot`-Implementierungen haben ein `[StreamName]`, und alle Präfixe sind eindeutig. Damit tritt der Fehler beim ersten Start nach dem Hinzufügen eines Aggregats auf, nicht beim ersten Schreibvorgang in Produktion.

**Schritt 3 — Key-Constraint verschärfen.** Der Reflection-Pfad in `GetKeyValue` existiert nur, weil `IEventSourcedRepository` gegen `IEntityKey` constraint statt gegen `IEntityKey<TValue>`. Wenn Stream-Identität in diesem Design immer stringbasiert ist, kann der Vertrag das ausdrücken:

```csharp
where TKey : struct, IEntityKey<Guid>   // oder ein eigenes IStreamKey : IEntityKey<string>
```

Dann entfallen `ValueAccessors`, der Expression-Cache und die Laufzeit-Exception ersatzlos — der Compiler garantiert, was heute zur Laufzeit geprüft wird. Falls die Flexibilität mehrerer Key-Wertetypen gebraucht wird, sollte zumindest der Fehlerfall beim Start geprüft werden statt beim ersten Zugriff.

**Migrationshinweis für bestehende Streams:** Da noch kein Event Store existiert, ist der Wechsel jetzt folgenlos. Später wäre er nur über eine Stream-Kopie mit Archivierung der alten Streams machbar — eine Operation, die man in Produktion nicht leichtfertig fährt.

---

# IMP-24 - `DomainEventEnvelope` trägt zu wenig Metadaten

- **Mittel**

## Beschreibung

```csharp
public sealed record DomainEventEnvelope(string EventTypeName, string Payload);
```

Der Envelope ist die einzige Datenstruktur zwischen Write-Transaktion und Projektion. Er enthält einen Typnamen und einen JSON-Blob. Es fehlen `EventId`, `AggregateId`, `AggregateType`, `Version` und `OccurredAt`.

Die Konsequenzen sind konkret und betreffen genau die Eigenschaften, die für eine at-least-once-Zustellung zwingend sind:

**1. Idempotenz in Projektionen ist praktisch nicht umsetzbar.** Der übliche Ansatz lautet „speichere pro Aggregat die zuletzt verarbeitete Version und ignoriere alles, was nicht größer ist". Dafür braucht der Handler Aggregat-Id und Version. Beide stecken nur im Payload — und dort typspezifisch, sodass jeder Handler sie einzeln auspacken müsste. Bei state-stored Aggregaten gibt es die Version überhaupt nicht (siehe IMP-10). Ergebnis: Die Anforderung „Projektionen müssen idempotent und ordnungsbewusst sein" ist gestellt, aber von der Infrastruktur nicht bedienbar.

**2. Partitionierung ist unmöglich.** Ohne Aggregat-Id im Envelope kann die Verarbeitung nicht nach Aggregat gruppiert werden — die Grundlage für IMP-25.

**3. Diagnose ist blind.** In der Outbox-Tabelle steht ein Typname und ein JSON-String. Fragen wie „welche Events hängen für Rezept X?" oder „seit wann liegt das hier?" lassen sich nur durch Volltextsuche im Payload beantworten.

**4. Kein Zeitbezug für Verzögerungsmetriken.** Die Projektionsverzögerung (Zeit zwischen Commit und Projektion) ist die zentrale Betriebsmetrik eines CQRS-Systems mit eventual consistency. Ohne `OccurredAt` im Envelope ist sie nicht messbar.

## Lösungsvorschlag

**Schritt 1 — Envelope vervollständigen:**

```csharp
public sealed record DomainEventEnvelope
{
    public required string EventName { get; init; }        // logischer Name, siehe IMP-22
    public required string Payload { get; init; }
    public required Guid EventId { get; init; }
    public required string AggregateType { get; init; }    // Stream-Präfix, siehe IMP-23
    public required string AggregateId { get; init; }
    public required DateTimeOffset OccurredAt { get; init; }
    public long? Version { get; init; }                    // nur bei event-sourced Aggregaten
}
```

`required` statt eines positionellen Records: Bei sieben Feldern ist die Lesbarkeit an der Erzeugungsstelle wichtiger als Kürze, und ein vergessenes Feld wird zum Compile-Fehler.

**Schritt 2 — Aggregatbezug am Event verfügbar machen.** Damit die Unit of Work den Envelope befüllen kann, muss sie zum Event das Aggregat kennen. Zwei Wege:

_Variante A (empfohlen) — beim Einsammeln zuordnen._ Die Unit of Work iteriert ohnehin über Aggregate und deren Events; die Zuordnung ist dort kostenlos vorhanden:

```csharp
foreach (var aggregate in aggregates)
{
    foreach (var domainEvent in aggregate.DomainEvents)
    {
        var envelope = EnvelopeFactory.Create(domainEvent, aggregate, occurredAt);
        await outbox.PublishAsync(envelope).ConfigureAwait(false);
    }
}
```

Dafür muss `IDomainEventsManager` (oder ein schmaleres Interface) Id und Typ freigeben — das ist über `IAggregateRoot<TKey>` bereits gegeben und braucht nur einen nicht-generischen Zugang:

```csharp
public interface IHasAggregateIdentity
{
    string AggregateStreamId { get; }   // stringifizierte Id
}
```

_Variante B — Aggregat-Id im Event._ Jedes Domain Event trägt ohnehin fast immer die Id des Aggregats. Eine Konvention (`IDomainEvent.AggregateId`) wäre einfacher, verschiebt die Pflicht aber in jedes Event und lässt sich nicht erzwingen. Variante A ist robuster.

**Schritt 3 — Idempotenz-Baustein anbieten.** Mit den Metadaten im Envelope wird die geforderte Idempotenz zu einem lösbaren Problem, und die Lösung gehört genau einmal in die Plattform statt N-mal in die Services:

```csharp
public abstract class IdempotentProjectionHandler<TDomainEvent> : IProjectionHandler<TDomainEvent>
    where TDomainEvent : IDomainEvent
{
    public async Task Handle(TDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        if (await checkpoints.IsAlreadyProcessedAsync(Context, cancellationToken))
        {
            return;
        }

        await ApplyAsync(domainEvent, cancellationToken);
        await checkpoints.RecordAsync(Context, cancellationToken);
    }

    protected abstract Task ApplyAsync(TDomainEvent domainEvent, CancellationToken cancellationToken);
}
```

Der Checkpoint-Store (`(projection_name, aggregate_id) → last_version` bzw. eine Menge verarbeiteter `EventId`s bei state-stored) liegt in der Read-Datenbank des jeweiligen Kontexts und wird in derselben Transaktion wie die Projektion geschrieben. Das ist der Punkt, an dem die Anforderung „idempotent und ordnungsbewusst" von einer Dokumentationsaussage zu einer durchsetzbaren Eigenschaft wird.

**Schritt 4 — Kontext an den Handler durchreichen.** `IProjectionHandler<T>.Handle(TDomainEvent, CancellationToken)` bekommt heute nur das Event. Die Envelope-Metadaten müssen zusätzlich ankommen — entweder als zweiter Parameter oder über einen scoped `IProjectionContext`, den `ProjectionRunner` vor dem Aufruf befüllt. Ein expliziter Parameter ist vorzuziehen: sichtbar in der Signatur, trivial testbar, kein verstecktes Ambient-State.

---

# IMP-25 - `Sequential()` auf einer einzigen Queue für alle Domain Events

- **Mittel**

## Beschreibung

```csharp
private const string DomainEventLocalQueueName = "building-blocks-domain-events";

options.PublishMessage<DomainEventEnvelope>()
    .ToLocalQueue(DomainEventLocalQueueName)
    .Sequential()
    .UseDurableInbox();
```

`Sequential()` bedeutet: **ein** Verarbeitungsthread für **alle** Domain Events des gesamten Service. Die durable Inbox und die strikte Ordnung sind richtig gewählt — die Granularität ist es nicht.

Benötigt wird Ordnung **pro Aggregat**: Für ein einzelnes Rezept müssen `RecipeCreated`, `IngredientAdded`, `IngredientRemoved` in dieser Reihenfolge projiziert werden. Zwischen zwei _verschiedenen_ Rezepten gibt es keine Ordnungsanforderung — und erst recht nicht zwischen einem Rezept und einem Trainingsplan.

Die aktuelle Konfiguration erzwingt die stärkste denkbare Garantie (globale Totalordnung) für eine deutlich schwächere Anforderung. Effekte:

- **Die Projektionsstufe skaliert nicht.** Egal wie viele Kerne oder Instanzen zur Verfügung stehen — der Durchsatz ist durch einen Thread begrenzt. Bei einem Import von 500 Lebensmitteln bedeutet das 500 sequenzielle Projektionsdurchläufe.
- **Ein langsames Event blockiert alle.** Eine Projektion mit einem teuren Query hält die gesamte Warteschlange des Service an.
- **Retries multiplizieren die Blockade.** Die aktuelle Policy (`OnException<Exception>` mit drei Cooldowns, siehe IMP-13) blockiert bei einem Fehler rund 2,6 Sekunden — für **jedes** nachfolgende Event des Service.
- **Zusammen mit IMP-08** (fehlender Flush auf der Marten-Seite) addieren sich Zustellungs- und Verarbeitungsverzögerung.

## Lösungsvorschlag

Voraussetzung ist IMP-24 (Aggregat-Id im Envelope). Ohne sie ist keine der Optionen umsetzbar — das ist der Grund, die beiden Punkte gemeinsam anzugehen.

**Option 1 — Ordnung nach Gruppenschlüssel (bevorzugt).** Wolverine unterstützt Message-Ordering pro Gruppe. Die Gruppe ist der Stream-Schlüssel des Aggregats:

```csharp
options.PublishMessage<DomainEventEnvelope>()
    .ToLocalQueue(DomainEventLocalQueueName)
    .MaximumParallelMessages(Environment.ProcessorCount)
    .UseDurableInbox();

// Gruppenschlüssel am Envelope setzen, damit Wolverine pro Aggregat serialisiert
envelope.GroupId = $"{envelope.AggregateType}/{envelope.AggregateId}";
```

Damit gilt exakt die benötigte Garantie: Events desselben Aggregats streng geordnet, Events verschiedener Aggregate parallel. _(Die genaue API für Gruppen-Ordering ist gegen die eingesetzte Wolverine-Version zu prüfen — das Konzept ist vorhanden, die Benennung hat sich zwischen Versionen geändert.)_

**Option 2 — Partitionierung auf N Queues.** Falls Option 1 nicht verfügbar ist, deterministisches Routing über einen Hash:

```csharp
private static string QueueFor(DomainEventEnvelope envelope) =>
    $"{DomainEventLocalQueueName}-{(uint)envelope.AggregateId.GetHashCode() % PartitionCount}";
```

Jede Partition bleibt `Sequential()`, aber es laufen N parallel. Da ein Aggregat immer auf dieselbe Partition abgebildet wird, bleibt die Ordnung pro Aggregat erhalten. Nachteil: `PartitionCount` ist zur Laufzeit nicht änderbar, ohne die Ordnung während der Umstellung zu verletzen — also bewusst großzügig wählen (z. B. 16) und dokumentieren. `string.GetHashCode()` ist in .NET pro Prozess randomisiert; hier ist ein stabiler Hash (z. B. FNV-1a oder xxHash über die UTF-8-Bytes) erforderlich, sonst landen dieselben Aggregate nach einem Neustart in anderen Partitionen.

**Option 3 — Projektionen von Integration Events trennen.** Unabhängig von 1 und 2 gehören die beiden Verbraucher auf getrennte Endpoints mit eigener Parallelität und eigener Retry-Policy — siehe IMP-26. Integration Events sind ordnungsunkritischer als Projektionen und können deutlich aggressiver parallelisiert werden.

**Messbar machen, bevor optimiert wird.** Der sinnvolle erste Schritt ist eine Metrik: Verzögerung zwischen `envelope.OccurredAt` und dem Beginn der Verarbeitung, plus Queue-Tiefe. Beides ist nach IMP-24 trivial verfügbar und beantwortet die Frage, ob und wann das Problem akut wird. Ohne diese Zahl ist jede Parallelisierung eine Vermutung — und `Sequential()` ist die konservative, korrekte Ausgangslage, von der aus man mit Daten optimiert.

---

# IMP-26 - `Publisher` koppelt Projektion und Integration-Event-Publikation

- **Mittel**

## Beschreibung

```csharp
public async Task PublishAsync(IDomainEvent domainEvent, CancellationToken cancellationToken)
{
    await projectionRunner.RunAsync(domainEvent, cancellationToken).ConfigureAwait(false);

    foreach (var mapper in _mappers)
    {
        foreach (var integrationEvent in mapper.Map(domainEvent))
        {
            await transport.PublishAsync(integrationEvent, cancellationToken).ConfigureAwait(false);
        }
    }
}
```

Zwei fachlich und technisch völlig unterschiedliche Vorgänge laufen in einer Nachrichtenverarbeitung:

- **Projektion** — lokaler Schreibvorgang in die Read-Datenbank desselben Kontexts. Fehlerursachen: DB nicht erreichbar, Constraint-Verletzung, Bug im Handler.
- **Integration Event** — Publikation an einen externen Broker. Fehlerursachen: Broker nicht erreichbar, Serialisierungsfehler, Routing-Problem.

Weil beides denselben Envelope teilt, teilen sie auch das Retry-Budget und den Fehlerausgang:

**Wirft eine Projektion nach erfolgreichem Transport-Publish**, wird der gesamte Envelope wiederholt. Beim Retry läuft die Projektion erneut (das ist gewollt) — aber das Integration Event geht **ein zweites Mal** raus. Da `IIntegrationEvent` keine Id trägt (IMP-11), kann der empfangende Service das nicht erkennen. Zusammen mit IMP-04 ist das der zweite unabhängige Weg, auf dem Duplikate über die Servicegrenze gelangen.

Umgekehrt gilt dasselbe: Ist der Broker kurzzeitig nicht erreichbar, wird die bereits erfolgreich gelaufene Projektion beim Retry erneut ausgeführt — was nur bei idempotenten Handlern folgenlos ist (siehe IMP-24).

Zusätzlich: Beide Vorgänge blockieren dieselbe `Sequential()`-Queue (IMP-25). Eine langsame Broker-Verbindung verzögert damit alle lokalen Projektionen.

## Lösungsvorschlag

**Schritt 1 — Zwei Verbraucher statt einem.** Der `DomainEventEnvelope` wird an zwei getrennte lokale Queues geroutet, jede mit eigenem Handler, eigener Parallelität und eigener Retry-Policy:

```csharp
options.PublishMessage<DomainEventEnvelope>()
    .ToLocalQueue("building-blocks-projections")
    .UseDurableInbox();

options.PublishMessage<DomainEventEnvelope>()
    .ToLocalQueue("building-blocks-integration-events")
    .UseDurableInbox();
```

```csharp
public sealed class ProjectionEnvelopeHandler(ProjectionRunner runner, IDomainEventTypeRegistry registry)
{
    public Task Handle(DomainEventEnvelope envelope, CancellationToken cancellationToken) => …
}

public sealed class IntegrationEventEnvelopeHandler(IIntegrationEventRunner runner, IDomainEventTypeRegistry registry)
{
    public Task Handle(DomainEventEnvelope envelope, IMessageContext context, CancellationToken cancellationToken) => …
}
```

Jeder Pfad scheitert und wiederholt für sich. Eine kaputte Projektion hält keine Integration Events mehr auf und umgekehrt; beide landen bei dauerhaftem Fehler in getrennten Error-Queues, was die Diagnose deutlich vereinfacht.

Der zweite Handler nimmt `IMessageContext` entgegen und löst damit gleichzeitig IMP-04.

**Schritt 2 — Unterschiedliche Policies setzen.** Erst nach der Trennung lassen sich die Unterschiede ausdrücken:

```csharp
// Projektionen: lokale DB, schnelle Retries, moderate Parallelität, Ordnung pro Aggregat
options.LocalQueue("building-blocks-projections")
    .MaximumParallelMessages(Environment.ProcessorCount);

// Integration Events: externer Broker, längere Backoffs, höhere Parallelität, keine Ordnungsanforderung
options.LocalQueue("building-blocks-integration-events")
    .MaximumParallelMessages(Environment.ProcessorCount * 4);
```

**Schritt 3 — `Publisher` auflösen.** Nach der Trennung hat `Publisher` keine eigene Aufgabe mehr: Er war nur die Klammer um zwei Vorgänge, die jetzt getrennt sind. `IDomainEventPublisher` in `BuildingBlocks.Application` wird damit ebenfalls überflüssig — es ist ohnehin eine Abstraktion, die von Application-Code nie aufgerufen wird und nur von Infrastructure zu Infrastructure führt.

Das ist der eigentliche Gewinn dieser Änderung: Ein Konzept weniger, zwei klar benannte Verarbeitungswege statt eines diffusen „Publisher", und der Datenfluss ist an der Wolverine-Konfiguration ablesbar statt in einer Schleife versteckt.

---

# IMP-27 - `FailureResults`-Reflection ist vermeidbar

- **Mittel**

## Beschreibung

```csharp
internal static class FailureResults
{
    private static readonly ConcurrentDictionary<Type, Func<Failure, Result>> Factories = new();

    private static Func<Failure, Result> CreateFactory(Type responseType)
    {
        if (responseType == typeof(Result)) …

        var method = responseType.GetMethod(nameof(Result.Failure), [typeof(Failure)])
            ?? throw new InvalidOperationException(
                $"The response type '{responseType}' does not expose a static Failure(Failure) factory.");

        var parameter = Expression.Parameter(typeof(Failure), "failure");
        var call = Expression.Convert(Expression.Call(method, parameter), typeof(Result));
        return Expression.Lambda<Func<Failure, Result>>(call, parameter).Compile();
    }
}
```

Der Zweck ist, aus einem generischen `TResponse : Result` ein Fehlerergebnis zu erzeugen. Gelöst wird das mit Reflection, kompilierten Expressions und einem statischen Cache.

Warum das mehr Aufwand ist als nötig: Die eigentliche Ursache liegt in `Result<T>`:

```csharp
public static new Result<TResult> Failure(Failure failure)
```

Statisches Member-Hiding mit `new`. `Result.Failure` und `Result<T>.Failure` sind zwei **unabhängige** statische Methoden ohne Beziehung im Typsystem — generischer Code kann sie deshalb nicht ansprechen, und die einzige verbleibende Brücke ist Reflection.

Konkrete Nachteile:

- Der Fehler „Typ hat keine passende Factory" tritt zur **Laufzeit** auf, bei der ersten Verwendung eines neuen Response-Typs.
- Kompilierte Expressions werden nie freigegeben und liegen in einem prozessglobalen statischen Cache (siehe IMP-35).
- `Expression.Compile()` ist mit Trimming und Native AOT nicht kompatibel — was für Aspire-Deployments relevant werden kann.
- Es ist die komplexeste Stelle einer sonst durchweg expliziten und lesbaren Codebasis.

## Lösungsvorschlag

C# 11 `static abstract` Interface-Member lösen das vollständig statisch.

**Schritt 1 — Fabrik-Vertrag in Application:**

```csharp
namespace BuildingBlocks.Application;

public interface IFailureResultFactory<out TSelf>
    where TSelf : Result
{
    static abstract TSelf FromFailures(IReadOnlyList<Failure> failures);
}
```

Die Signatur nimmt bewusst eine Liste — damit deckt sie zugleich das Validierungs-Behavior aus IMP-16 ab, das mehrere Fehler erzeugt.

**Schritt 2 — Implementieren:**

```csharp
public class Result : IFailureResultFactory<Result>
{
    static Result IFailureResultFactory<Result>.FromFailures(IReadOnlyList<Failure> failures) => Failure(failures);
}

public sealed class Result<TResult> : Result, IFailureResultFactory<Result<TResult>>
{
    static Result<TResult> IFailureResultFactory<Result<TResult>>.FromFailures(IReadOnlyList<Failure> failures)
        => new(failures);
}
```

Explizite Implementierung, damit die öffentliche Oberfläche unverändert bleibt und die Fabrik nur über den generischen Vertrag erreichbar ist — dieselbe Kapselungstechnik wie bei `IDomainEventsManager`.

**Schritt 3 — Behaviors constrainen:**

```csharp
public sealed class ExceptionToResultBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TResponse : Result, IFailureResultFactory<TResponse>
{
    catch (DomainValidationException exception)
    {
        return TResponse.FromFailures([Failure.Validation(ValidationFailureCode, exception.Message)]);
    }
}
```

Ergebnis: `FailureResults`, der `ConcurrentDictionary`, alle `Expression`-Aufrufe und die Laufzeit-Exception entfallen **ersatzlos**. Ein Response-Typ ohne Fabrik ist ein Compile-Fehler. Der Aufruf ist ein direkter statischer Call ohne Indirektion.

**Nebeneffekt: `new`-Hiding kann verschwinden.** Nach dieser Umstellung sind `Result<T>.Failure(...)` und `Result<T>.Success(...)` als versteckende statische Member nicht mehr für die Pipeline nötig. Sie bleiben als Bequemlichkeit für Handler-Code sinnvoll, aber die problematische Kopplung ist aufgelöst (siehe IMP-39).

**Prüfen:** `TryAddEnumerable` mit offenen Generics und einem `static abstract`-Constraint — der DI-Container schließt das Generic beim Auflösen und prüft dabei die Constraints. Das funktioniert, weil `Result` und `Result<T>` die einzigen Response-Typen sind und beide den Vertrag erfüllen. Ein Test, der die Pipeline für beide Varianten auflöst, sichert das ab.

---

# IMP-28 - Kein `IClock` im Container

- **Mittel**
-   - **Status**: Gelöst

## Beschreibung

`IClock` ist in der Domain definiert:

```csharp
public interface IClock
{
    DateTimeOffset Now { get; }
}
```

Es wird von `EventSourcedAggregateRoot.RaiseEvent(IDomainEvent, IClock)` **zwingend** verlangt. Es gibt in den Building Blocks jedoch:

- keine Implementierung (kein `SystemClock`, kein `UtcClock`)
- keine Registrierung in `AddBuildingBlocks`

Jeder Service muss die Implementierung selbst schreiben und registrieren. Das ist genau das Boilerplate, dessen Beseitigung der Zweck einer Building-Blocks-Bibliothek ist — und es ist Boilerplate mit Fehlerpotenzial: Ob `DateTimeOffset.Now` (lokale Zeitzone) oder `DateTimeOffset.UtcNow` verwendet wird, ist eine folgenreiche Entscheidung, die hier N-mal unabhängig getroffen wird. In einem verteilten System mit Services in potenziell unterschiedlichen Zeitzonen führt lokale Zeit zu Events, deren Reihenfolge nicht der Realität entspricht.

Der Port selbst ist richtig platziert und richtig geschnitten. Nur die Standardimplementierung fehlt.

## Lösungsvorschlag

**Schritt 1 — Implementierung bereitstellen:**

```csharp
namespace BuildingBlocks.Infrastructure;

internal sealed class SystemClock : IClock
{
    public DateTimeOffset Now => DateTimeOffset.UtcNow;
}
```

Bewusst `UtcNow`. Alles, was persistiert, projiziert oder über Servicegrenzen transportiert wird, ist UTC. Zeitzonen sind eine Darstellungsfrage und gehören ins Frontend — nicht in Events, die Jahre im Event Store liegen.

**Schritt 2 — Registrieren:**

```csharp
services.TryAddSingleton<IClock, SystemClock>();
```

`TryAdd`, damit ein Service für Tests oder Sonderfälle eine eigene Implementierung vorschalten kann. Singleton, weil die Klasse zustandslos ist.

**Schritt 3 — Auf `TimeProvider` aufsetzen.** .NET 8 hat mit `TimeProvider` eine Standardabstraktion für Zeit eingeführt, inklusive `FakeTimeProvider` aus `Microsoft.Extensions.TimeProvider.Testing`. Statt eine parallele Abstraktion zu pflegen, sollte `IClock` darauf aufbauen:

```csharp
internal sealed class SystemClock(TimeProvider timeProvider) : IClock
{
    public DateTimeOffset Now => timeProvider.GetUtcNow();
}
```

`TimeProvider.System` ist in modernen Hosts bereits registriert. Der Gewinn: Tests bekommen mit `FakeTimeProvider` eine erprobte, vollständige Zeitsteuerung (inklusive Timern) geschenkt, statt dass jedes Testprojekt eine eigene `FakeClock` schreibt — wie es `BuildingBlocks.Domain.Tests` heute tut.

`IClock` in der Domain bleibt trotzdem sinnvoll: Es ist der schmale, domänenspezifische Port („die Domäne kennt nur _jetzt_"), während `TimeProvider` eine breite Infrastrukturabstraktion mit Timern und Zeitzonen ist. Die Domain soll die breitere Abstraktion nicht sehen. Ein Zweizeiler-Adapter ist der richtige Preis dafür.

**Schritt 4 — Nach IMP-01 neu bewerten.** Wenn das Stempeln in die Unit of Work wandert, braucht die Domäne `IClock` nur noch dort, wo Zeit **fachlich** relevant ist („Trainingsdatum darf nicht in der Zukunft liegen"). Der Port bleibt richtig, aber die Zahl der Aufrufstellen sinkt drastisch — und jede verbleibende ist dann eine bewusste fachliche Aussage statt Infrastruktur-Durchreichung.

---

# IMP-29 - Unique-Constraint-Verletzungen werden nicht übersetzt

- **Mittel**

## Beschreibung

`UnitOfWorkBehavior` übersetzt zwei Ausnahmen:

```csharp
catch (ConcurrencyException exception)          { return … Failure.Conflict(…); }
catch (DbUpdateConcurrencyException exception)  { return … Failure.Conflict(…); }
```

Nicht behandelt wird `DbUpdateException` mit einer zugrunde liegenden PostgreSQL-Unique-Violation (SQLSTATE `23505`).

Das ist kein Randfall, sondern die **einzige** verfügbare Methode zur Durchsetzung kontextweiter Eindeutigkeit. `IRepository` bietet nur `GetByIdAsync`, `AddAsync` und `Remove` — keine Query-Fähigkeit. Eine Regel wie „Rezeptname pro Nutzer eindeutig" oder „nur ein aktives Trainingsprogramm pro Nutzer" kann ein Aggregat nicht selbst prüfen, weil sie über die Aggregatgrenze hinausgeht. Der Unique-Index in der Datenbank ist der korrekte Ort dafür — und der Standardansatz („optimistisch einfügen, Constraint-Fehler abfangen und übersetzen") ist auch der einzige, der ohne Race Condition funktioniert.

Aktuell fliegt eine solche Verletzung als unbehandelte `DbUpdateException` bis zum globalen Handler und wird zu **HTTP 500**. Für den Client bedeutet das: „Serverfehler, versuch es später" statt „dieser Name ist bereits vergeben". Der Nutzer kann den Fehler nicht beheben, obwohl er trivial behebbar wäre. In den Logs erscheint ein Error mit Stacktrace für einen völlig normalen Vorgang.

Verwandt: Auch `DbUpdateException` mit Foreign-Key-Verletzung (`23503`) und Check-Constraint-Verletzung (`23514`) haben sinnvolle fachliche Entsprechungen.

## Lösungsvorschlag

**Schritt 1 — Übersetzung ergänzen.** Die Zuordnung von Datenbankfehlern zu `FailureCategory` ist Persistenz-Wissen und gehört nicht in das generische `UnitOfWorkBehavior`. Sauberer ist ein Port, den die jeweilige Persistenz-Implementierung bedient:

```csharp
namespace BuildingBlocks.Infrastructure.Persistence;

internal interface IPersistenceExceptionTranslator
{
    bool TryTranslate(Exception exception, out Failure failure);
}
```

```csharp
internal sealed class NpgsqlExceptionTranslator : IPersistenceExceptionTranslator
{
    public bool TryTranslate(Exception exception, out Failure failure)
    {
        if (exception is DbUpdateException { InnerException: PostgresException postgres })
        {
            switch (postgres.SqlState)
            {
                case PostgresErrorCodes.UniqueViolation:
                    failure = Failure.Conflict(
                        $"persistence.unique_violation.{postgres.ConstraintName}",
                        "Ein Datensatz mit diesen Werten existiert bereits.");
                    return true;

                case PostgresErrorCodes.ForeignKeyViolation:
                    failure = Failure.Conflict("persistence.foreign_key_violation", …);
                    return true;
            }
        }

        failure = default!;
        return false;
    }
}
```

Der Constraint-Name im Fehlercode ist der entscheidende Teil: Er macht die Verletzung für den Client **unterscheidbar**. `persistence.unique_violation.ix_recipes_owner_name` sagt dem BFF, welches Feld betroffen ist — und lässt sich dort in eine präzise Meldung mit `Target` (siehe IMP-17) übersetzen.

Damit das trägt, müssen Constraint-Namen bewusst vergeben werden (`HasDatabaseName("ix_recipes_owner_name")` in der EF-Konfiguration) statt EF-generierte Namen zu verwenden. Das ist eine kleine Konvention mit großer Wirkung und gehört in die Mapping-Richtlinie.

**Schritt 2 — In `UnitOfWorkBehavior` einhängen:**

```csharp
catch (Exception exception) when (TryTranslate(exception, out var failure))
{
    return FailureResults.Create<TResponse>(failure);
}
```

Die vorhandenen Concurrency-Fälle wandern ebenfalls in die Translator-Implementierungen (`ConcurrencyException` in den Marten-Translator, `DbUpdateConcurrencyException` in den EF-Translator). Das Behavior wird dadurch persistenzunabhängig — was Voraussetzung für die Assembly-Trennung aus IMP-19 ist, wo `UnitOfWorkBehavior` im Kern-Assembly landet und EF Core dort nicht mehr referenziert werden darf.

**Schritt 3 — Marten-Ausnahmen prüfen.** `catch (ConcurrencyException)` aus `JasperFx` fängt Martens Stream-Konflikte vermutlich mit ab, weil `EventStreamUnexpectedMaxEventIdException` davon ableitet. Das ist plausibel, aber nicht verifiziert. Ein Integrationstest (IMP-09, Stufe 3), der zwei konkurrierende Appends auf denselben Stream fährt und `Failure.Conflict` erwartet, klärt das eindeutig — und schützt gleichzeitig gegen eine Änderung der Ausnahmehierarchie in einer künftigen Marten-Version.

---

# IMP-30 - Keine Tracing-Instrumentierung der CQRS-Pipeline

- **Mittel**

## Beschreibung

Weder `Sender` noch `Publisher`/`ProjectionRunner` erzeugen `Activity`-Spans. `LoggingBehavior` misst zwar die Dauer und loggt sie:

```csharp
var startedAt = Stopwatch.GetTimestamp();
…
Log.RequestSucceeded(logger, requestName, elapsed.TotalMilliseconds);
```

…aber ein Log-Eintrag ist kein Span. Er lässt sich nicht in einer Trace-Ansicht verschachteln, nicht mit dem eingehenden HTTP-Request oder der Wolverine-Nachricht verbinden und nicht aggregieren.

Aspire liefert automatische Instrumentierung für HTTP, EF Core, Npgsql und Wolverine. Damit sieht man in der Trace-Ansicht: eingehender gRPC-Call → SQL-Statements. Unsichtbar bleibt die gesamte Schicht dazwischen — welcher Command lief, wie lange die Pipeline brauchte, welche Behaviors Zeit gekostet haben, welche Projektionen für ein Event liefen und wie lange.

Für ein verteiltes System mit asynchroner Projektion ist gerade das die kritische Information. Die typische Betriebsfrage lautet „warum sieht der Nutzer seine Änderung nicht?" — und die Antwort liegt in der Kette Command → Commit → Outbox → Queue → Projektion. Ohne Spans über diese Kette ist die Frage nur durch Log-Korrelation von Hand beantwortbar.

Zusätzlich fehlt jede Metrik: keine Zähler für Commands, keine Histogramme für Latenz, keine Messung der Projektionsverzögerung — der zentralen Kennzahl eines eventual-consistent Systems.

## Lösungsvorschlag

**Schritt 1 — `ActivitySource` und Spans im Sender:**

```csharp
namespace BuildingBlocks.Infrastructure;

public static class BuildingBlocksDiagnostics
{
    public const string ActivitySourceName = "BuildingBlocks";
    internal static readonly ActivitySource ActivitySource = new(ActivitySourceName);
    internal static readonly Meter Meter = new(ActivitySourceName);
}
```

Als Behavior implementiert — dann greift es automatisch für Commands, Queries und alles, was durch die Pipeline geht, und die Reihenfolge ist über den Mechanismus aus IMP-16 steuerbar (als äußerstes Behavior, damit die Spanne die gesamte Verarbeitung umfasst):

```csharp
public sealed class TracingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TResponse : Result
{
    public async Task<TResponse> Handle(TRequest request, RequestPipelineContinuation<TResponse> continuation, CancellationToken cancellationToken)
    {
        using var activity = BuildingBlocksDiagnostics.ActivitySource
            .StartActivity($"{RequestKind} {typeof(TRequest).Name}", ActivityKind.Internal);

        var response = await continuation(cancellationToken).ConfigureAwait(false);

        if (activity is not null && response.IsFailure)
        {
            activity.SetStatus(ActivityStatusCode.Error);
            activity.SetTag("failure.category", response.Failures[0].Category.ToString());
            activity.SetTag("failure.code", response.Failures[0].Code);
        }

        return response;
    }
}
```

`StartActivity` gibt `null` zurück, wenn kein Listener registriert ist — die Kosten ohne aktives Tracing sind damit vernachlässigbar.

**Schritt 2 — Projektionen instrumentieren.** Der wertvollste Span des gesamten Systems, weil er die eventual consistency sichtbar macht. In `ProjectionRunner`:

```csharp
using var activity = ActivitySource.StartActivity($"Project {typeof(TDomainEvent).Name}");
activity?.SetTag("aggregate.id", envelope.AggregateId);
activity?.SetTag("projection.lag_ms", (clock.Now - envelope.OccurredAt).TotalMilliseconds);
```

Der `lag_ms`-Tag setzt IMP-24 voraus und beantwortet direkt die Frage „wie weit hinken die Read-Modelle hinterher?".

**Schritt 3 — Trace-Kontext über die Outbox propagieren.** Damit der Projektions-Span als Kind des ursprünglichen Commands erscheint, muss der W3C-Trace-Kontext (`traceparent`) im Envelope mitgeführt und beim Verarbeiten wiederhergestellt werden. Wolverine macht das für seine eigenen Nachrichten bereits — ob der Kontext über die Outbox-Persistenz hinweg erhalten bleibt, ist zu prüfen. Falls nicht, ist ein zusätzliches Envelope-Feld (IMP-24) die Lösung.

Erst damit entsteht der durchgehende Trace `HTTP POST /recipes → CreateRecipeCommand → SaveChanges → [async] Project RecipeCreated → INSERT recipe_list` — genau die Ansicht, die eine Verzögerungsfrage in Sekunden statt in Stunden beantwortet.

**Schritt 4 — Metriken:**

```csharp
private static readonly Counter<long> RequestCounter =
    Meter.CreateCounter<long>("buildingblocks.requests");

private static readonly Histogram<double> RequestDuration =
    Meter.CreateHistogram<double>("buildingblocks.request.duration", unit: "ms");

private static readonly Histogram<double> ProjectionLag =
    Meter.CreateHistogram<double>("buildingblocks.projection.lag", unit: "ms");
```

`buildingblocks.projection.lag` ist die Kennzahl, auf der ein sinnvolles SLO für ein CQRS-System aufsetzt („95 % der Read-Modelle sind binnen 2 s aktuell"). Sie ist ohne diese Instrumentierung nicht messbar.

**Schritt 5 — Registrierung dokumentieren.** In `VitalSync.ServiceDefaults`:

```csharp
.WithTracing(tracing => tracing.AddSource(BuildingBlocksDiagnostics.ActivitySourceName))
.WithMetrics(metrics => metrics.AddMeter(BuildingBlocksDiagnostics.ActivitySourceName))
```

Ohne diese zwei Zeilen bleibt die Instrumentierung wirkungslos — sie gehört deshalb in die Konfigurationsprüfung aus IMP-13 oder mindestens prominent in die Setup-Anleitung.

---

# IMP-31 - Read-Modelle im state-stored Pfad sind nicht wiederaufbaubar

- **Mittel**

## Beschreibung

Read-Modelle sind als abgeleitet und wegwerfbar konzipiert — sie sollen durch erneutes Abspielen der Events rekonstruierbar sein. Das ist die Eigenschaft, die einen Projektions-Bugfix beherrschbar macht: Handler korrigieren, Read-Tabelle leeren, neu aufbauen.

Für **event-sourced** Kontexte funktioniert das: Der Marten-Stream ist die dauerhafte Quelle, `FetchStreamAsync` liefert die Historie, und die Events lassen sich erneut durch `ProjectionRunner` schicken.

Für **state-stored** Kontexte funktioniert es nicht. Dort gibt es keine Event-Historie:

- Die Write-Datenbank enthält nur den aktuellen Zustand.
- Die Domain Events existieren ausschließlich als `DomainEventEnvelope` in der Wolverine-Outbox — und die wird nach erfolgreicher Zustellung geleert.

Nach der Migration auf Wolverines native Outbox (die als Entscheidung richtig war, siehe Analyse) ist der frühere eigene `IOutboxStore` entfallen. Mit ihm ist auch die Möglichkeit entfallen, die Envelopes aufzubewahren. Ein Replay im state-stored Pfad ist damit **strukturell unmöglich**, nicht nur unbequem.

Die praktische Konsequenz: Wird ein Fehler in `RecipeNutritionProjection` entdeckt, gibt es keinen unterstützten Weg, die bereits geschriebenen Read-Daten zu korrigieren. Der Ausweg wäre ein einmaliges Migrationsskript, das die Read-Tabelle aus der Write-Tabelle neu berechnet — also eine zweite, parallele Implementierung derselben Projektionslogik, die dann ihrerseits gepflegt werden muss.

Das ist ein Zielkonflikt, der bewusst entschieden werden sollte — derzeit ist er nur unbemerkt eingetreten.

## Lösungsvorschlag

Drei Optionen, je nach gewünschtem Anspruch.

**Option 1 — Domain-Event-Journal (empfohlen, wenn Replay ein Ziel bleibt).** Eine append-only Tabelle in der **Write**-Datenbank, die jeden Envelope dauerhaft festhält. Sie wird in derselben Transaktion wie das Aggregat geschrieben, parallel zur Outbox:

```sql
CREATE TABLE domain_event_journal (
    sequence      bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    event_id      uuid        NOT NULL UNIQUE,
    event_name    text        NOT NULL,
    aggregate_type text       NOT NULL,
    aggregate_id  text        NOT NULL,
    occurred_at   timestamptz NOT NULL,
    payload       jsonb       NOT NULL
);

CREATE INDEX ix_journal_aggregate ON domain_event_journal (aggregate_type, aggregate_id, sequence);
```

Das Schreiben ist ein zusätzlicher Insert in einer bereits offenen Transaktion — vernachlässigbar. Der Replay wird zu einem einfachen, verständlichen Vorgang:

```csharp
public interface IDomainEventJournal
{
    IAsyncEnumerable<DomainEventEnvelope> ReadAsync(
        long fromSequence, string? aggregateType, CancellationToken cancellationToken);
}
```

Wichtige Abgrenzung: Das ist **kein Event Store**. Die Write-Tabelle bleibt die Quelle der Wahrheit; das Journal ist reine Ableitungsgrundlage für Projektionen. Es hat keine Optimistic Concurrency, keine Streams, keine Rekonstruktion von Aggregaten. Diese Unterscheidung muss klar sein, sonst entsteht durch die Hintertür ein halbes Event Sourcing mit allen Nachteilen und ohne die Vorteile.

Die Tabelle wächst unbegrenzt — dafür braucht es eine Retention-Regel (z. B. Partitionierung nach Monat, Löschen nach 12 Monaten). Danach ist ein vollständiger Replay nicht mehr möglich, ein Replay „ab Zeitpunkt X" schon. Das reicht für den praktischen Anwendungsfall (Bugfix in einer Projektion) fast immer aus und sollte als bewusste Grenze dokumentiert werden.

**Option 2 — Rebuild aus dem Write-Modell.** Statt Events erneut abzuspielen, wird das Read-Modell direkt aus dem aktuellen Zustand neu berechnet. Dafür braucht jede Projektion eine zweite Methode:

```csharp
public interface IRebuildableProjection<TAggregate>
{
    Task RebuildAsync(TAggregate aggregate, CancellationToken cancellationToken);
}
```

Ehrlicher als Option 1 in Bezug auf das, was ein state-stored Modell hergibt — aber jede Projektion existiert dann doppelt (inkrementell aus Events, vollständig aus dem Zustand), mit dem entsprechenden Risiko, dass beide auseinanderlaufen. Vertretbar bei wenigen, einfachen Projektionen.

**Option 3 — Grenze explizit ziehen.** Festhalten, dass Replay nur für event-sourced Kontexte unterstützt wird, und daraus die Konsequenz ableiten: Kontexte mit anspruchsvollen, fehleranfälligen Projektionen werden event-sourced modelliert. Das ist eine legitime Entscheidung — sie muss nur getroffen und in einem ADR festgehalten sein, weil sie die Wahl der Persistenzstrategie beeinflusst.

**Empfehlung:** Option 1, kombiniert mit einer klaren Retention-Regel. Der Aufwand ist gering, der Nutzen bei jedem Projektionsfehler unmittelbar, und die Metadaten aus IMP-24 liefern die Tabellenstruktur bereits vollständig mit.

---

# IMP-32 - Keine Batch- oder Bulk-Fähigkeit

- **Mittel**

## Beschreibung

`ISender.Send` verarbeitet genau einen Request, und `UnitOfWorkBehavior` committet nach jedem erfolgreichen Command. Es gibt keinen Weg,

- mehrere Commands in einer Transaktion zu bündeln,
- den automatischen Commit für einen Vorgang zu unterdrücken,
- oder die Event-Publikation für einen Massenvorgang zu unterdrücken.

Bei einem realistischen Anwendungsfall wie „Nährwertkatalog mit 500 Lebensmitteln importieren" bedeutet das:

- 500 einzelne Transaktionen mit je einem Roundtrip
- 500 Outbox-Einträge
- 500 Envelopes durch eine `Sequential()`-Queue (IMP-25)
- 500 Projektionsdurchläufe, jeder mit eigener Transaktion in der Read-Datenbank

Der Import ist damit funktional korrekt, aber um Größenordnungen langsamer als nötig — und er blockiert währenddessen die Projektionsqueue für den gesamten Service, sodass reguläre Nutzeraktionen ihre Read-Modelle verzögert aktualisiert bekommen.

Verwandte Lücken derselben Ursache: Es gibt keinen Weg, ein Aggregat ohne Event-Publikation zu speichern (z. B. bei einer Datenmigration), und keinen Weg, mehrere Aggregate atomar zu ändern (was in DDD selten, aber bei Datenkorrekturen gelegentlich nötig ist).

Das ist kein Fehler, sondern eine bewusste Vereinfachung — sie sollte nur als solche erkannt und bei Bedarf gezielt geöffnet werden, statt jeden Bulk-Vorgang an der Plattform vorbei zu implementieren.

## Lösungsvorschlag

**Option 1 — Transaktionsklammer explizit steuern (empfohlen, minimal invasiv).** Ein Marker-Interface, das `UnitOfWorkBehavior` erkennt:

```csharp
namespace BuildingBlocks.Application;

/// Der Handler verwaltet seine Transaktion selbst; das UnitOfWorkBehavior committet nicht.
public interface ISelfManagedTransaction;
```

```csharp
private static readonly bool ManagesOwnTransaction =
    typeof(ISelfManagedTransaction).IsAssignableFrom(typeof(TRequest));

if (ManagesOwnTransaction || !IsCommand || response.IsFailure)
{
    return response;
}
```

Ein Import-Command implementiert das Interface und steuert `IUnitOfWork.CommitAsync` selbst — etwa alle 100 Datensätze:

```csharp
internal sealed class ImportFoodCatalogCommandHandler(IRepository<Food, FoodId> repository, IUnitOfWork unitOfWork)
    : ICommandHandler<ImportFoodCatalogCommand, int>
{
    public async Task<Result<int>> Handle(ImportFoodCatalogCommand command, CancellationToken cancellationToken)
    {
        var imported = 0;

        foreach (var chunk in command.Entries.Chunk(BatchSize))
        {
            foreach (var entry in chunk)
            {
                repository.Add(Food.Create(FoodId.New(), entry.Name, entry.Nutrients));
            }

            await unitOfWork.CommitAsync(cancellationToken).ConfigureAwait(false);
            imported += chunk.Length;
        }

        return imported;
    }
}
```

Wenige Zeilen in der Plattform, volle Kontrolle im Handler, und die Standardfälle bleiben unverändert. Die Batchgröße ist eine fachliche Entscheidung und gehört zum Handler.

**Option 2 — Bulk-Pfad für die Event-Publikation.** Bei sehr großen Importen ist nicht der Commit das Problem, sondern die Projektionsstufe. Ein Marker, der die Event-Publikation unterdrückt, verlagert die Aktualisierung des Read-Modells auf einen expliziten anschließenden Rebuild:

```csharp
public interface ISuppressesDomainEventPublication;
```

Das ist bewusst gefährlich — die Read-Modelle sind danach veraltet, bis der Rebuild läuft. Es sollte deshalb nur zusammen mit dem Journal/Rebuild-Mechanismus aus IMP-31 angeboten werden, damit es einen definierten Weg zurück gibt. Ohne diesen Weg besser gar nicht anbieten.

**Option 3 — Massendaten außerhalb der Domäne.** Für reine Stammdaten (Nährwertkatalog, Übungsbibliothek) ist die Frage berechtigt, ob sie überhaupt durch Aggregate und Events laufen müssen. Ein Katalog ohne Invarianten und ohne Historie ist kein Aggregat — ein direkter `COPY`-Import in die Katalogtabelle plus separater Read-Modell-Aufbau ist einfacher, schneller und ehrlicher.

**Empfehlung:** Option 1 umsetzen (klein, sicher, deckt die meisten Fälle) und Option 3 als bewusste Modellierungsfrage stellen, bevor Option 2 überhaupt nötig wird.

---

# IMP-33 - Keine Saga- oder Process-Manager-Abstraktion

- **Mittel**

## Beschreibung

Die Plattform deckt drei Auslöser ab: eingehender Command, eingehende Query, eingetroffenes Event. Nicht abgedeckt ist alles, was **Zustand über Zeit** und **Zeitsteuerung** braucht:

- „Erinnere den Nutzer, wenn er 3 Tage nichts protokolliert hat."
- „Sende die Wochenauswertung sonntags um 18 Uhr."
- „Wenn nach 24 Stunden keine Bestätigung kommt, mache die Reservierung rückgängig."
- „Führe die Auswertung erst aus, wenn sowohl Nutrition als auch Fitness ihre Tagesdaten gemeldet haben."

Der letzte Fall ist besonders relevant: Ein Bounded Context, der auf die **Kombination** mehrerer Integration Events reagieren muss, braucht einen Ort, um den Teilzustand zu halten. Ohne Saga-Konzept wird daraus ein selbstgebautes Aggregat mit Statusfeldern plus ein Hintergrunddienst, der pollt — die Variante, die man in vielen Systemen findet und die schwer zu testen und zu betreiben ist.

Wolverine bringt genau dafür Saga-Unterstützung mit (Zustandspersistenz, Timeouts, Korrelation). Die Building Blocks stellen davon nichts bereit. Ein Service, der es braucht, müsste direkt auf Wolverine zugreifen und damit die Abstraktionsschicht umgehen — womit die Kapselung, die `IIntegrationEventTransport` bewusst herstellt, an genau der Stelle bricht, an der sie am meisten wert wäre.

Das ist keine Kritik am aktuellen Stand — es ist eine Lücke, die zum jetzigen Zeitpunkt völlig in Ordnung ist. Sie sollte nur bewusst benannt sein, weil sie mit Sicherheit auftritt und die Antwort darauf besser vorab entschieden wird als unter Zeitdruck.

## Lösungsvorschlag

Nichts jetzt bauen. Stattdessen die Entscheidung vorbereiten und die Grenze dokumentieren.

**Schritt 1 — Als bewusstes Nicht-Ziel festhalten.** Ein kurzer ADR-Eintrag: „Prozesssteuerung über Zeit ist derzeit kein Building Block. Services, die sie brauchen, verwenden Wolverine-Sagas direkt." Das ist eine legitime Position und verhindert, dass jemand aus Unsicherheit etwas Eigenes baut.

**Schritt 2 — Beim ersten Bedarf einen dünnen Port ergänzen.** Wenn der Fall auftritt, ist die naheliegende Form ein Vertrag in Application, der die zwei benötigten Fähigkeiten kapselt, ohne Wolverine sichtbar zu machen:

```csharp
namespace BuildingBlocks.Application;

public interface IProcessManager<TState>
    where TState : class, IProcessState
{
    // Korrelation, Zustand, Timeouts
}

public interface IScheduledMessageScheduler
{
    Task ScheduleAsync<TMessage>(TMessage message, DateTimeOffset deliverAt, CancellationToken cancellationToken);
    Task ScheduleAsync<TMessage>(TMessage message, TimeSpan delay, CancellationToken cancellationToken);
}
```

`IScheduledMessageScheduler` ist der kleinere und wertvollere der beiden: Er deckt „mach das später" ab, was den überwiegenden Teil der Fälle löst, und ist mit Wolverines `ScheduleAsync` ein Zehnzeiler. Sagas mit Zustand lohnen erst, wenn es mehr als einen Anwendungsfall gibt.

**Schritt 3 — Abgrenzung zu geplanten Aufgaben.** Nicht jeder zeitgesteuerte Vorgang ist eine Saga. „Sonntags 18 Uhr Wochenauswertung" ist ein Cron-Job und gehört zu einem Hintergrunddienst mit `IHostedService`, nicht in eine Prozesssteuerung. Die Unterscheidung lautet:

- **Zustandslos und zeitgesteuert** → Hintergrunddienst
- **Zustandsbehaftet, korreliert mit einer Entität, mit Timeout** → Saga

Diese Trennung vorab zu benennen verhindert, dass beides in denselben Mechanismus gepresst wird.

---

# IMP-34 - `Result` hat keine Kombinatoren

- **Mittel**

## Beschreibung

`Result` und `Result<T>` bieten Erzeugung (`Success`, `Failure`, implizite Konvertierungen) und Abfrage (`IsSuccess`, `IsFailure`, `Value`, `Failures`). Es gibt **keine** Verknüpfungsoperationen: kein `Map`, `Bind`, `Match`, `Tap`, `Ensure`.

Praktische Folge in jedem Handler mit mehr als einem Schritt:

```csharp
var recipe = await repository.GetByIdAsync(command.RecipeId, cancellationToken);
if (recipe is null)
{
    return Result<RecipeId>.Failure(Failure.NotFound("nutrition.recipe.not-found", "…"));
}

var validation = await ValidateOwnership(recipe, command.UserId, cancellationToken);
if (validation.IsFailure)
{
    return Result<RecipeId>.Failure(validation.Failures);   // manuelles Umhängen
}

var nutrients = await CalculateNutrients(recipe, cancellationToken);
if (nutrients.IsFailure)
{
    return Result<RecipeId>.Failure(nutrients.Failures);
}
```

Zwei Probleme darin:

**1. Es gibt keine Konvertierung zwischen `Result` und `Result<T>`.** Ein fehlgeschlagenes `Result` in ein `Result<T>` zu überführen, erfordert das manuelle Durchreichen der Failure-Liste. Das ist bei jedem Zwischenschritt zu wiederholen und leicht zu vergessen — insbesondere die Variante mit einer einzelnen `Failure` statt der ganzen Liste, die stillschweigend Fehler verliert.

**2. Die eigentliche Fachlogik verschwindet zwischen Kontrollfluss.** Von den zwölf Zeilen oben sind drei fachlich relevant.

Da `Result` der meistverwendete Typ der gesamten Plattform ist und in jedem Handler jedes Services auftaucht, ist der Hebel hier ungewöhnlich groß: Fünf Erweiterungsmethoden sparen über die Projektlebensdauer erhebliche Mengen Rauschen.

## Lösungsvorschlag

**Schritt 1 — Konvertierung zwischen den Varianten (das Wichtigste):**

```csharp
namespace BuildingBlocks.Application;

public static class ResultExtensions
{
    /// Überträgt die Fehler eines gescheiterten Ergebnisses in einen anderen Ergebnistyp.
    public static Result<TTarget> ToFailure<TTarget>(this Result result)
    {
        if (result.IsSuccess)
        {
            throw new InvalidOperationException("Ein erfolgreiches Ergebnis kann nicht in einen Fehler überführt werden.");
        }

        return Result<TTarget>.Failure(result.Failures);
    }
}
```

Damit wird aus dem manuellen Umhängen ein `return validation.ToFailure<RecipeId>();` — kürzer und ohne die Möglichkeit, Fehler zu verlieren.

**Schritt 2 — Der minimale sinnvolle Satz.** Bewusst klein halten; eine vollständige Monaden-Bibliothek passt nicht zur expliziten Linie des Projekts:

```csharp
// Wert transformieren, Fehler durchreichen
public static Result<TTarget> Map<TSource, TTarget>(this Result<TSource> result, Func<TSource, TTarget> map);

// Verketten mit einer Operation, die selbst scheitern kann
public static async Task<Result<TTarget>> BindAsync<TSource, TTarget>(
    this Result<TSource> result, Func<TSource, CancellationToken, Task<Result<TTarget>>> next, CancellationToken ct);

// Nachgelagerte Bedingung prüfen
public static Result<T> Ensure<T>(this Result<T> result, Func<T, bool> predicate, Failure failure);

// Auflösen in beide Richtungen — für den BFF
public static TOut Match<T, TOut>(this Result<T> result, Func<T, TOut> onSuccess, Func<IReadOnlyList<Failure>, TOut> onFailure);
```

Der Handler von oben wird damit zu:

```csharp
return await (await repository.GetByIdAsync(command.RecipeId, cancellationToken))
    .ToResult(() => Failure.NotFound("nutrition.recipe.not-found", "…"))
    .BindAsync(ValidateOwnership, cancellationToken)
    .BindAsync(CalculateNutrients, cancellationToken);
```

**Schritt 3 — `Match` für den BFF.** Das ist der Ort mit dem klarsten Nutzen: Die Übersetzung `Result` → HTTP-Antwort ist in jedem Endpunkt identisch und lässt sich mit `Match` genau einmal schreiben:

```csharp
app.MapPost("/recipes", async (CreateRecipeRequest request, ISender sender, CancellationToken ct) =>
    (await sender.Send(request.ToCommand(), ct))
        .Match(
            onSuccess: id => Results.Created($"/recipes/{id.Value}", null),
            onFailure: ToProblemDetails));
```

**Bewusste Zurückhaltung.** Nicht implementieren: `Try`, `Recover`, `Combine`, LINQ-Query-Syntax, `Result`-Zip. Die Erfahrung mit Result-Bibliotheken ist, dass ein zu reichhaltiges API dazu verleitet, Kontrollfluss in Ausdrücke zu pressen, die am Ende schwerer zu lesen sind als das explizite `if`. Fünf Methoden, die die tatsächlich wiederkehrenden Muster abdecken, sind das richtige Maß — und lassen sich später erweitern, wenn ein konkreter Bedarf mehrfach auftritt.

**Ergänzend — `Result.Success()` cachen.** `Result.Success()` erzeugt bei jedem Aufruf eine neue Instanz, obwohl der Typ unveränderlich und zustandslos ist. Ein statisches Singleton spart eine Allokation pro Command. Marginal, aber kostenlos.

---

# IMP-35 - Statische Caches über Container-Grenzen hinweg

- **Mittel**

## Beschreibung

Fünf Stellen halten prozessglobalen statischen Zustand:

| Ort                               | Cache                                                                    |
| --------------------------------- | ------------------------------------------------------------------------ |
| `Sender`                          | `CommandDispatchers`, `CommandWithResultDispatchers`, `QueryDispatchers` |
| `ProjectionRunner`                | `Invokers`                                                               |
| `FailureResults`                  | `Factories` (kompilierte Expressions)                                    |
| `EntityKeyFormatter`              | `ValueAccessors` (kompilierte Expressions)                               |
| `EntityKeyModelBuilderExtensions` | `Converters`                                                             |

Die gecachten Werte sind zustandslos, weshalb es kein Korrektheitsproblem im Normalbetrieb gibt. Aber:

- **Unbegrenztes Wachstum in Testläufen.** Eine Testsuite mit vielen `WebApplicationFactory`- oder `ServiceProvider`-Instanzen füllt die Caches über alle Container hinweg. Kompilierte Expressions (`FailureResults`, `EntityKeyFormatter`) werden **nie** freigegeben — sie leben in dynamischen Methoden, die der GC nicht einsammelt.
- **Implizite Prozessglobalität.** Die Klassen sind als Scoped/Singleton registriert, verhalten sich aber wie statische Dienste. Zwei Container im selben Prozess teilen Zustand, ohne dass das an der Registrierung erkennbar wäre.
- **Testisolation.** Der Cache-Bug aus IMP-06 ist genau deshalb schwer zu reproduzieren: Er hängt davon ab, welcher Test zuerst lief. Statischer Zustand macht Tests reihenfolgeabhängig.
- **Trimming und AOT.** `Expression.Compile()` ist mit Native AOT nicht kompatibel. Für ein Aspire-Deployment kann das relevant werden.

## Lösungsvorschlag

**Schritt 1 — Caches an die Instanz binden.** `Sender` und `ProjectionRunner` sind bereits als Scoped registriert. Ein Instanz-Cache wäre allerdings pro Request neu — der Effekt ginge verloren. Die richtige Form ist ein **Singleton-Cache-Service**, der explizit injiziert wird:

```csharp
internal sealed class DispatcherCache
{
    private readonly ConcurrentDictionary<Type, CommandDispatcher> _commands = new();
    private readonly ConcurrentDictionary<DispatcherKey, object> _commandsWithResult = new();
    private readonly ConcurrentDictionary<DispatcherKey, object> _queries = new();
    …
}

services.TryAddSingleton<DispatcherCache>();
services.TryAddScoped<ISender, Sender>();   // Sender(IServiceProvider, DispatcherCache)
```

Der Cache lebt damit exakt so lange wie der Container, wird mit ihm entsorgt, und zwei Container teilen nichts. Die Lebensdauer steht in der Registrierung statt im Schlüsselwort `static`. Analog für `ProjectionRunner` und `EntityKeyFormatter`.

**Schritt 2 — `FailureResults` entfällt.** Mit IMP-27 (`static abstract` Interface-Member) verschwindet dieser Cache samt kompilierten Expressions ersatzlos. Das ist die beste Form der Behebung: kein Cache statt eines besser verwalteten Caches.

**Schritt 3 — `EntityKeyFormatter` vereinfachen.** Der Reflection-Cache existiert nur, weil das Repository gegen `IEntityKey` statt `IEntityKey<TValue>` constraint (siehe IMP-23, Schritt 3). Mit dem schärferen Constraint entfallen `ValueAccessors` und die kompilierten Accessoren komplett — `key.Value` ist dann ein direkter Zugriff.

**Schritt 4 — `EntityKeyModelBuilderExtensions.Converters` behalten.** Dieser Cache ist unkritisch: Er wird ausschließlich beim Modellaufbau verwendet (einmalig pro `DbContext`-Typ), die Anzahl der Key-Typen ist durch die Domäne begrenzt, und `ValueConverter`-Instanzen sind klein. Hier wäre eine Umstellung mehr Aufwand als Nutzen — eine Instanzbindung ist bei einer statischen Erweiterungsmethode ohnehin nicht möglich, ohne die API zu ändern.

**Zusammengefasst:** Nach IMP-27 und IMP-23 bleiben von fünf Caches zwei übrig, und beide werden explizit als Singleton-Services verwaltet. Das ist der eigentliche Gewinn — nicht die Cache-Verwaltung selbst, sondern dass drei davon durch schärfere Typisierung überflüssig werden.

---

# IMP-36 - `RuleChecker` schluckt `null` still

- **Mittel**

## Beschreibung

```csharp
public static void Check(IBusinessRule rule)
{
    if (rule?.IsBroken() == true)
    {
        throw new BusinessRuleViolationException(rule.Message);
    }
}

public static void Check(params IBusinessRule[] rules)
{
    foreach (var rule in rules ?? [])
    {
        Check(rule);
    }
}
```

`RuleChecker.Check(null)` tut nichts und meldet nichts. Ein `null`-Array wird zu einem leeren Array.

Ein `null` an dieser Stelle ist **immer** ein Programmierfehler: eine Methode, die statt einer Regel `null` zurückgibt; ein Feld, das nicht initialisiert wurde; ein Refactoring, bei dem eine Zuweisung verlorenging. Die aktuelle Behandlung deutet diesen Fehler in „Regel gilt als erfüllt" um.

Das ist die gefährlichste Form von Null-Toleranz: Sie tritt genau dort auf, wo der Zweck des Codes die **Durchsetzung von Invarianten** ist. Eine übersprungene Prüfung erzeugt keinen Fehler, sondern einen ungültigen Aggregatzustand, der erst viel später auffällt — und dann als Datenproblem, nicht als Codeproblem.

Da das Projekt `Nullable` aktiviert und `TreatWarningsAsErrors` gesetzt hat, ist die Null-Toleranz zusätzlich inkonsistent: An jeder anderen Stelle der Codebasis wird `ArgumentNullException.ThrowIfNull` verwendet (`AggregateRoot.AddDomainEvent`, `EventSourcedAggregateRoot.RaiseEvent`, `Publisher.PublishAsync`, alle Behaviors). `RuleChecker` ist die einzige Ausnahme.

**Zweiter, unabhängiger Punkt:** Die vier Überladungen erlauben nicht, Validierungsregeln und Geschäftsregeln gemeinsam zu prüfen:

```csharp
RuleChecker.Check(new NameMustNotBeBlank(name));                    // IDomainValidationRule
RuleChecker.Check(new MaxIngredientsRule(_ingredients));            // IBusinessRule
```

In der Praxis ist die Reihenfolge fachlich relevant — strukturelle Validierung vor Geschäftsregeln —, und zwei getrennte Aufrufe drücken das nicht aus.

## Lösungsvorschlag

**Schritt 1 — Null als Fehler behandeln:**

```csharp
public static void Check(IBusinessRule rule)
{
    ArgumentNullException.ThrowIfNull(rule);

    if (rule.IsBroken())
    {
        throw new BusinessRuleViolationException(rule);
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

Wichtig: `ArgumentNullException` (nicht `DomainValidationException`) ist hier richtig. Ein fehlendes Regelobjekt ist kein Domänenfehler, sondern ein Programmierfehler — er soll **nicht** von `ExceptionToResultBehavior` in ein `Failure` übersetzt und dem Nutzer als 400 präsentiert werden, sondern als unerwarteter Fehler nach oben laufen und einen Error-Log erzeugen.

**Schritt 2 — Gemeinsame Basis für beide Regelarten.** Die beiden Verträge unterscheiden sich nur im Methodennamen (`IsBroken` vs. `IsInvalid`) und der geworfenen Exception:

```csharp
public interface IRule
{
    string Code { get; }        // siehe IMP-17
    string Message { get; }
    bool IsSatisfied();
}

public interface IBusinessRule : IRule;
public interface IDomainValidationRule : IRule;
```

Damit wird eine gemischte Überladung möglich, die die Reihenfolge explizit macht:

```csharp
public static void Check(params IRule[] rules)
{
    ArgumentNullException.ThrowIfNull(rules);

    foreach (var rule in rules)
    {
        ArgumentNullException.ThrowIfNull(rule);

        if (rule.IsSatisfied())
        {
            continue;
        }

        throw rule switch
        {
            IDomainValidationRule validation => new DomainValidationException(validation),
            IBusinessRule business           => new BusinessRuleViolationException(business),
            _ => new InvalidOperationException($"Unbekannte Regelart '{rule.GetType()}'."),
        };
    }
}
```

Anwendung im Aggregat:

```csharp
RuleChecker.Check(
    new NameMustNotBeBlank(name),           // Validierung zuerst
    new ServingsMustBePositive(servings),
    new MaxIngredientsRule(_ingredients));  // dann Geschäftsregeln
```

**Trade-off, offen benannt:** Die gemischte Überladung erlaubt es, die Reihenfolge falsch zu wählen (Geschäftsregel vor Validierung). Das ist ein akzeptierter Preis für die bessere Lesbarkeit — und der aktuelle Zustand mit zwei getrennten Aufrufen verhindert es ebenso wenig. Wer die Trennung erzwingen will, behält die getrennten Überladungen und ergänzt nur die Null-Prüfungen aus Schritt 1; das ist die minimale Variante und behebt den eigentlichen Befund vollständig.

**Namenswechsel `IsBroken`/`IsInvalid` → `IsSatisfied`:** Die positive Formulierung ist beim Lesen einer Regel-Implementierung deutlich klarer (`return _ingredients.Count <= MaxIngredients;` statt `return _ingredients.Count > MaxIngredients;`) und macht doppelte Verneinungen überflüssig. Das ist ein Breaking Change am Domain-Vertrag — jetzt, ohne Implementierungen, kostenlos.

---

# IMP-37 - Async-Suffix ist inkonsistent

- **Niedrig**

## Beschreibung

Alle asynchronen Methoden geben `Task` zurück, aber nur etwa die Hälfte trägt das `Async`-Suffix:

| Ohne Suffix                         | Mit Suffix                                           |
| ----------------------------------- | ---------------------------------------------------- |
| `ICommandHandler.Handle`            | `IRepository.GetByIdAsync` / `AddAsync`              |
| `IQueryHandler.Handle`              | `IEventSourcedRepository.GetByIdAsync` / `SaveAsync` |
| `IProjectionHandler.Handle`         | `IUnitOfWork.CommitAsync`                            |
| `IPipelineBehavior.Handle`          | `IDomainEventPublisher.PublishAsync`                 |
| `ISender.Send`                      | `IIntegrationEventTransport.PublishAsync`            |
| `DomainEventEnvelopeHandler.Handle` | `ProjectionRunner.RunAsync`                          |

Die Aufteilung folgt keiner erkennbaren Regel — sie verläuft entlang der Datei, in der die jeweilige Methode entstanden ist.

Warum das über Kosmetik hinausgeht: Das `Async`-Suffix ist die etablierte .NET-Konvention (und wird von VSTHRD200 sowie mehreren Analyzern eingefordert). Bei aktivem `AnalysisMode=All` und `TreatWarningsAsErrors` ist es Zufall, dass hier kein Build-Fehler entsteht. Vor allem aber: Der Wert einer Konvention liegt in ihrer Vorhersagbarkeit. Eine zur Hälfte befolgte Konvention kostet bei jeder Nutzung eine Nachfrage — „heißt es jetzt `Handle` oder `HandleAsync`?" — und verleitet dazu, dass jeder neue Handler die Schreibweise des zuletzt gesehenen übernimmt. Über Dutzende Service-Projekte hinweg zementiert sich das Durcheinander.

## Lösungsvorschlag

Eine Konvention wählen und vollständig durchziehen. Empfehlung: **überall `Async`**, weil das der .NET-Standard ist, die Analyzer es einfordern und die Mehrheit der bestehenden Signaturen es bereits so hält.

Betroffene Umbenennungen:

```csharp
ICommandHandler.Handle              → HandleAsync
ICommandHandler<,>.Handle           → HandleAsync
IQueryHandler.Handle                → HandleAsync
IProjectionHandler.Handle           → HandleAsync
IPipelineBehavior.Handle            → HandleAsync
ISender.Send                        → SendAsync
DomainEventEnvelopeHandler.Handle   → (bleibt: Wolverine-Konvention, siehe unten)
```

**Ausnahme mit Begründung:** `DomainEventEnvelopeHandler.Handle` wird von Wolverine per Konvention entdeckt. Wolverine akzeptiert `Handle` und `HandleAsync` — hier lohnt die Prüfung gegen die eingesetzte Version, bevor umbenannt wird. Falls beide funktionieren, gilt die Projektkonvention; falls nicht, bleibt der Framework-Name und die Ausnahme wird im Code kurz begründet.

**Absicherung.** Damit die Konvention hält, sollte sie erzwungen statt dokumentiert werden. `.editorconfig` im `BuildingBlocks/src`-Verzeichnis:

```ini
dotnet_diagnostic.VSTHRD200.severity = warning
```

Bei `TreatWarningsAsErrors` wird daraus ein Build-Fehler — die einzige Form von Konvention, die dauerhaft trägt.

**Zeitpunkt.** Es gibt keinen Konsumenten. Die Umbenennung ist ein IDE-Rename über drei Projekte und zwei Testprojekte, in wenigen Minuten erledigt. Mit dem ersten Service, der Dutzende Handler implementiert hat, wird daraus ein Breaking Change über alle Service-Projekte.

---

# IMP-38 - Sichtbarkeits-Disziplin ist uneinheitlich

- **Niedrig**

## Beschreibung

Die öffentliche Oberfläche von `BuildingBlocks.Infrastructure` folgt keiner erkennbaren Regel:

| Typ                                        | Aktuell      | Angemessen              |
| ------------------------------------------ | ------------ | ----------------------- |
| `IIntegrationEventTransport`               | `internal`   | ✔                       |
| `WolverineIntegrationEventTransport`       | **`public`** | `internal`              |
| `NullIntegrationEventTransport`            | **`public`** | `internal`              |
| `Publisher`                                | `internal`   | ✔                       |
| `ProjectionRunner`                         | **`public`** | `internal`              |
| `MartenAggregateTracker`                   | **`public`** | `internal`              |
| `EfCoreUnitOfWork<T>` / `MartenUnitOfWork` | **`public`** | `internal`              |
| `EfCoreRepository<,>`                      | **`public`** | `public` (siehe IMP-15) |
| `MartenEventSourcedRepository<,>`          | **`public`** | `internal`              |
| `Sender`, Behaviors                        | **`public`** | `internal`              |

Besonders auffällig: `WolverineIntegrationEventTransport` und `NullIntegrationEventTransport` sind `public`, implementieren aber ein `internal` Interface. Von außen sind diese Klassen damit vollständig nutzlos — man kann sie sehen, aber nicht gegen ihren Vertrag programmieren. Der jüngste Commit hat `Publisher` und `IIntegrationEventTransport` bewusst auf `internal` gesetzt; die Implementierungen wurden dabei übersehen.

Warum das relevant ist: Jeder `public` Typ ist ein Versprechen. Er kann von Service-Code direkt instanziiert, abgeleitet oder gemockt werden, und jede Änderung daran ist potenziell eine Breaking Change. Bei einer Bibliothek, die explizit als wiederverwendbare Plattform gedacht ist, sollte die öffentliche Oberfläche eine bewusste, kleine Auswahl sein — nicht das Ergebnis dessen, wo `public` beim Tippen stehen geblieben ist.

Konkret erwünschte öffentliche Oberfläche:

- alle Verträge aus `Domain` und `Application`
- `AddBuildingBlocks` und `BuildingBlocksOptions`
- die `WolverineOptions`- und `ModelBuilder`-Extensions
- `EfCoreRepository` (weil ableitbar, siehe IMP-15)
- `DomainEventEnvelope` (Serialisierung)

Alles andere sind Adapter und gehören hinter die Abstraktion.

## Lösungsvorschlag

**Schritt 1 — Adapter auf `internal` setzen.** Die Registrierung über `services.TryAddScoped<IUnitOfWork, EfCoreUnitOfWork<TContext>>()` funktioniert mit `internal` Typen unverändert, solange die Registrierung im selben Assembly liegt — was hier der Fall ist.

**Schritt 2 — Sichtbarkeit für Tests öffnen, nicht für Konsumenten:**

```xml
<ItemGroup>
  <InternalsVisibleTo Include="BuildingBlocks.Infrastructure.Tests" />
</ItemGroup>
```

Das ist der richtige Weg, um IMP-09 zu ermöglichen, ohne die öffentliche Oberfläche aufzublähen. Tests dürfen Interna kennen; Service-Code nicht.

**Schritt 3 — Wolverine-Discovery prüfen.** `DomainEventEnvelopeHandler` muss für Wolverine auffindbar bleiben. Wolverine entdeckt standardmäßig `public` Typen; ob `internal` Handler gefunden werden, ist versionsabhängig. Bleibt der Handler `public`, ist das eine bewusste, begründete Ausnahme — und gehört als kurzer Kommentar an die Klasse.

**Schritt 4 — Dauerhaft absichern.** Die einfachste Form ist ein Test, der die öffentliche Oberfläche gegen eine erwartete Liste prüft:

```csharp
[Fact]
public void PublicApiSurface_MatchesApprovedList()
{
    var actual = typeof(ServiceCollectionExtensions).Assembly
        .GetExportedTypes()
        .Select(type => type.FullName)
        .Order(StringComparer.Ordinal)
        .ToArray();

    Assert.Equal(ApprovedPublicTypes, actual);
}
```

Ein neu hinzugefügter `public` Typ lässt den Test scheitern und zwingt zu einer bewussten Entscheidung. Das ist deutlich wirksamer als eine Konvention im Review — und der Test dient gleichzeitig als lesbare Dokumentation dessen, was die Plattform nach außen anbietet.

---

# IMP-39 - `Result`-API: Namenskollision und implizite Konvertierungen

- **Niedrig**

## Beschreibung

Drei zusammenhängende Schwächen im `Result`-API.

**1. Namenskollision.** Innerhalb einer Klasse existieren nebeneinander:

```csharp
public bool IsFailure => !IsSuccess;                     // Property
public IReadOnlyList<Failure> Failures { get; }          // Property
public static Result Failure(Failure failure)            // Methode
public static Result Failure(IReadOnlyList<Failure> f)   // Methode
```

Ein Typ namens `Failure`, eine Methode namens `Failure`, eine Property `Failures`, eine Property `IsFailure`. Innerhalb des Klassenkörpers muss der Typ teilweise qualifiziert werden, weil der Methodenname ihn verdeckt. Beim Lesen von `Result.Failure(x)` ist auf den ersten Blick nicht erkennbar, ob es sich um einen Konstruktoraufruf, eine Factory oder einen Property-Zugriff handelt.

**2. Statisches Member-Hiding.**

```csharp
public static new Result<TResult> Failure(Failure failure);
public static new Result<TResult> Failure(IReadOnlyList<Failure> failures);
```

`Result.Failure(f)` und `Result<int>.Failure(f)` sehen identisch aus, liefern aber unterschiedliche Typen — abhängig vom **statischen** Empfängertyp an der Aufrufstelle. In generischem Code ist das nicht auflösbar und der unmittelbare Grund, warum `FailureResults` Reflection benötigt (IMP-27).

**3. Implizite Konvertierung vom Wert.**

```csharp
public static implicit operator Result<TResult>(TResult value) => Success(value);
public static implicit operator Result<TResult>(Failure failure) => Failure(failure);
```

Für `Result<Failure>` kollidieren beide Operatoren — ein Compile-Fehler an einer sehr überraschenden Stelle. Bei `Result<bool>` oder `Result<string>` ist die Konvertierung zwar legal, macht aber an der Aufrufstelle unsichtbar, dass ein `Result` entsteht. Ein `return true;` in einer Methode mit Rückgabetyp `Result<bool>` liest sich wie ein Wahrheitswert, ist aber ein Erfolgsergebnis.

## Lösungsvorschlag

**Schritt 1 — Factory umbenennen.** Der Typ `Failure` ist das etablierte Vokabular und bleibt; die Methode weicht:

```csharp
public static Result Fail(Failure failure);
public static Result Fail(IReadOnlyList<Failure> failures);
```

`Fail` ist ein Verb und passt zum Gegenstück `Success`. Die Kollision zwischen Typ, Methode und Property verschwindet vollständig, und `Result.Fail(Failure.NotFound(…))` liest sich eindeutig.

**Schritt 2 — Hiding auflösen.** Mit IMP-27 (`IFailureResultFactory<TSelf>` mit `static abstract`) ist das Hiding für die Pipeline nicht mehr nötig. Die statischen Factories auf `Result<T>` können als Bequemlichkeit bleiben — sie sind dann aber keine tragende Infrastruktur mehr, sondern nur noch Zucker, und ihr Verschwinden bräche nichts.

**Schritt 3 — Implizite Wert-Konvertierung entfernen, Fehler-Konvertierung behalten.**

```csharp
// entfernen — verdeckt die Ergebnissemantik
public static implicit operator Result<TResult>(TResult value);

// behalten — eindeutig, keine Kollision, hoher praktischer Wert
public static implicit operator Result<TResult>(Failure failure);
public static implicit operator Result(Failure failure);
```

Die Konvertierung von `Failure` ist unproblematisch: Sie ist eindeutig lesbar (`return Failure.NotFound(…)` kann nur ein Fehlerergebnis meinen) und spart bei jedem Fehlerpfad Rauschen. Die Konvertierung vom Wert dagegen kostet Lesbarkeit und erzeugt den Sonderfall `Result<Failure>`.

Ersatz an der Aufrufstelle:

```csharp
return Result.Success(recipe.Id);   // explizit statt implizit
```

Eine Zeile, die klar sagt, was passiert.

**Schritt 4 — `Result.Success()` als Singleton.** Der parameterlose Erfolgsfall ist unveränderlich und zustandslos:

```csharp
private static readonly Result SuccessResult = new(true, NoFailures);
public static Result Success() => SuccessResult;
```

Spart eine Allokation pro erfolgreichem Command. Marginal, aber ohne Nachteil.

**Zeitpunkt.** Alle vier Punkte sind Breaking Changes am meistgenutzten Typ der Plattform. Ohne Konsumenten kosten sie ein Rename und wenige manuelle Anpassungen in den Testprojekten.

---

# IMP-40 - `State` ist `public` und bricht die Kapselung

- **Niedrig**

## Beschreibung

```csharp
public abstract class EventSourcedAggregateRoot<TKey, TState>(TState initialState)
{
    public TState State { get; private set; } = initialState;
}
```

Der vollständige Innenzustand jedes event-sourced Aggregats ist von außen lesbar. Der Setter ist privat, Mutation ist also ausgeschlossen — Lesen aber nicht.

Warum das die Kapselung untergräbt: Ein Command-Handler kann `workout.State.Sets.Count` oder `recipe.State.Ingredients` direkt auswerten, statt eine Methode oder Property des Aggregats zu nutzen. In der Praxis passiert genau das, weil es der kürzeste Weg ist. Das Aggregat verkommt damit zum Datenhalter mit angehängten Methoden, und Logik, die ins Aggregat gehört, wandert in die Handler.

Die Inkonsistenz ist dabei auffällig: Der Rest der Domain-Schicht betreibt bemerkenswerten Aufwand für Kapselung — `ClearDomainEvents`, `LoadFromHistory` und `Version` sind explizit implementiert und damit aus IntelliSense verschwunden. Ausgerechnet der Zustand, das schützenswerteste Element eines Aggregats, ist offen.

Nebenaspekt: `State` ist im aktuellen Code auch für die Infrastruktur nicht nötig. `MartenEventSourcedRepository` greift ausschließlich über `IEventSourcedAggregateRoot<TKey>` zu (`LoadFromHistory`, `Version`, `DomainEvents`) und nie auf `State`. Die öffentliche Sichtbarkeit hat also keinen Nutzer.

## Lösungsvorschlag

**Schritt 1 — Sichtbarkeit reduzieren:**

```csharp
protected TState State { get; private set; }
```

Das Aggregat greift weiterhin uneingeschränkt zu; von außen ist der Zustand nur noch über bewusst gewählte Properties und Methoden erreichbar:

```csharp
public sealed class Workout : EventSourcedAggregateRoot<WorkoutId, WorkoutState>
{
    public bool IsCompleted => State.CompletedAt is not null;
    public int SetCount => State.Sets.Count;
}
```

Diese Zwischenschicht ist der eigentliche Gewinn: Sie ist der Ort, an dem entschieden wird, was nach außen sichtbar sein _soll_ — statt dass alles sichtbar ist, weil niemand widersprochen hat.

**Schritt 2 — Zugriff für Tests.** Domain-Tests prüfen üblicherweise beobachtbares Verhalten (welche Events wurden erzeugt), nicht den Zustand. Wo der Zustand doch geprüft werden muss, ist eine öffentliche Property am konkreten Aggregat der richtige Weg — genau die, die der Anwendungscode ohnehin braucht. Ein `internal`-Zugang für Tests ist hier nicht nötig und wäre ein Signal, dass die Tests zu tief greifen.

**Schritt 3 — Bei IMP-10 mitziehen.** Wird das einheitliche Aggregat-Modell umgesetzt, gilt `protected State` für beide Varianten. Das ist die Gelegenheit, die Entscheidung einmal für alle Aggregate zu treffen.

**Prüfen:** `IState<TSelf, TKey>` bleibt `public`, weil konkrete State-Typen in den Services das Interface implementieren müssen. Nur die _Instanz_ am Aggregat wird geschützt — nicht der Vertrag.

---

# IMP-41 - `DomainEvent` als `record` mit garantiert ungleicher Wertgleichheit

- **Niedrig**

## Beschreibung

```csharp
public abstract record DomainEvent : IDomainEvent
{
    protected DomainEvent()
    {
        EventId = Guid.NewGuid();
    }

    public Guid EventId { get; init; }
    public DateTimeOffset OccurredAt { get; init; }
}
```

`record` verspricht strukturelle Gleichheit — zwei Instanzen mit gleichen Werten sind gleich. Durch `EventId = Guid.NewGuid()` im Konstruktor ist dieses Versprechen jedoch **strukturell unerfüllbar**: Zwei fachlich identische Events sind nie gleich.

Die Falle zeigt sich in Tests:

```csharp
var expected = new RecipeCreated(recipeId, "Haferbrei", 2);
Assert.Equal(expected, recipe.DomainEvents.Single());   // schlägt immer fehl
```

Die Fehlerausgabe zeigt zwei scheinbar identische Objekte, die sich nur in einem Feld unterscheiden, das der Testautor nie gesetzt hat. Das kostet beim ersten Auftreten Zeit und führt danach dazu, dass Tests auf Pattern Matching ausweichen — was funktioniert, aber die Absicht schlechter ausdrückt.

Dasselbe gilt für `OccurredAt`: Nach dem Stempeln (IMP-01) unterscheiden sich zwei sonst identische Events zusätzlich im Zeitstempel.

Fachlich ist die Situation eindeutig: Zwei Domain Events sind genau dann dasselbe Event, wenn sie **dieselbe `EventId`** haben. Alles andere ist Nutzdaten. Die vom `record` erzeugte Gleichheit ist also nicht nur unbrauchbar, sondern auch semantisch falsch.

## Lösungsvorschlag

**Option 1 — Identitätsbasierte Gleichheit (empfohlen).** Die Gleichheit wird das, was sie fachlich sein sollte:

```csharp
public abstract record DomainEvent : IDomainEvent
{
    protected DomainEvent() => EventId = Guid.CreateVersion7();

    public Guid EventId { get; init; }
    public DateTimeOffset OccurredAt { get; init; }

    public virtual bool Equals(DomainEvent? other) =>
        other is not null && other.GetType() == GetType() && EventId.Equals(other.EventId);

    public override int GetHashCode() => HashCode.Combine(GetType(), EventId);
}
```

Das ist konsistent mit dem Rest der Domain: `Entity<TKey>` und `AggregateRoot<TKey>` verwenden ebenfalls Typ + Id. Ein Domain Event ist ein identifizierbarer Fakt, kein Wert — genau wie eine Entität.

Nebeneffekt: `record`-Instanzen in einem `HashSet` oder als Dictionary-Schlüssel verhalten sich dann korrekt und stabil, was bei Deduplizierung (IMP-24) direkt gebraucht wird.

`Guid.CreateVersion7()` statt `Guid.NewGuid()`: zeitlich sortierbar, damit indexfreundlich in Journal- und Checkpoint-Tabellen.

**Option 2 — Nutzdaten-Gleichheit für Tests.** Alternativ `EventId` und `OccurredAt` aus der Gleichheit ausnehmen, sodass `record`-Semantik über die fachlichen Felder gilt. Das macht die Testfälle oben funktionsfähig, ist aber fachlich fragwürdig: Zwei separat aufgetretene, identische Ereignisse wären dann gleich — was für ein Ereignis-Log falsch ist.

**Empfehlung: Option 1**, ergänzt durch eine Test-Hilfsmethode für den Vergleich der Nutzdaten:

```csharp
public static void AssertRaised<TEvent>(this IHasDomainEvents aggregate, Func<TEvent, bool> predicate)
    where TEvent : IDomainEvent =>
    Assert.Contains(aggregate.DomainEvents.OfType<TEvent>(), predicate);
```

Anwendung:

```csharp
recipe.AssertRaised<RecipeCreated>(e => e.Name == "Haferbrei" && e.Servings == 2);
```

Das ist ohnehin die bessere Testform: Sie prüft genau die relevanten Felder und bleibt stabil, wenn dem Event später ein Feld hinzugefügt wird.

**Dokumentieren.** Unabhängig von der Wahl gehört die getroffene Entscheidung an `DomainEvent` — die Gleichheitssemantik eines `record` ist eine Erwartung, die man nur bewusst brechen sollte.

---

# IMP-42 - `IRepository`-API ist asymmetrisch und irreführend benannt

- **Niedrig**

## Beschreibung

```csharp
public interface IRepository<TAggregate, in TKey>
{
    Task<TAggregate?> GetByIdAsync(TKey id, CancellationToken cancellationToken);
    Task AddAsync(TAggregate aggregate, CancellationToken cancellationToken);
    void Remove(TAggregate aggregate);
}
```

**1. Asymmetrie.** `AddAsync` ist asynchron, `Remove` synchron. `DbSet.AddAsync` ist ausschließlich dann erforderlich, wenn ein serverseitiger Wertgenerator (Hi/Lo-Sequenz) einen Datenbankzugriff zur Id-Vergabe braucht. Bei den hier vorgesehenen client-generierten typisierten IDs tritt dieser Fall nie ein. Die Asynchronität ist also reine Fassade, die pro Aufruf eine `Task`-Allokation kostet und jeden Aufrufer zu einem `await` zwingt, das nichts erwartet.

**2. Irreführende Namen.** Bei einem Unit-of-Work-Muster tut keine der Methoden, was ihr Name verspricht:

- `AddAsync` fügt nichts hinzu — es meldet das Aggregat beim ChangeTracker an.
- `Remove` löscht nichts — es markiert zur Löschung.
- `IEventSourcedRepository.SaveAsync` speichert nicht — es staged Events in der Marten-Session und gibt `Task.CompletedTask` zurück.

Das ist eine etablierte Konvention und für erfahrene Entwickler kein Hindernis. Es erklärt aber einen wiederkehrenden Fehlertyp: Code, der nach `AddAsync` davon ausgeht, die Daten seien geschrieben, und daraufhin liest oder eine Id erwartet.

Besonders `SaveAsync` sticht heraus: Eine Methode, die `Async` heißt, `Task` zurückgibt, kein `await` enthält und nichts speichert.

## Lösungsvorschlag

**Schritt 1 — Symmetrie herstellen:**

```csharp
public interface IRepository<TAggregate, in TKey>
    where TAggregate : class, IAggregateRoot<TKey>          // siehe IMP-21
    where TKey : struct, IEntityKey
{
    Task<TAggregate?> GetByIdAsync(TKey id, CancellationToken cancellationToken);

    void Add(TAggregate aggregate);
    void Remove(TAggregate aggregate);
}
```

`GetByIdAsync` bleibt asynchron — es führt tatsächlich einen Datenbankzugriff aus. `Add` und `Remove` sind synchron, weil sie nur den Änderungsverfolger anfassen. Die Signaturen sagen damit die Wahrheit über das I/O-Verhalten, was für den Aufrufer die eigentlich relevante Information ist.

Implementierung:

```csharp
public void Add(TAggregate aggregate) => context.Set<TAggregate>().Add(aggregate);
```

**Schritt 2 — Staging-Semantik im Namen ausdrücken.** Bei `IEventSourcedRepository` ist der Fall am deutlichsten:

```csharp
public interface IEventSourcedRepository<TAggregate, in TKey>
{
    Task<TAggregate?> GetByIdAsync(TKey id, CancellationToken cancellationToken);

    void Stage(TAggregate aggregate);   // statt Task SaveAsync(...)
}
```

`Stage` beschreibt exakt, was passiert: Die Events werden für den nächsten Commit vorgemerkt. Es verschwindet ein irreführendes `Async`, eine unnötige `Task`-Allokation und eine `return Task.CompletedTask;`-Zeile.

Damit werden beide Repository-Verträge auch untereinander konsistent: eine asynchrone Lesemethode, synchrone Änderungsmethoden, Commit ausschließlich über `IUnitOfWork`.

**Schritt 3 — Erwartung explizit machen.** Die Trennung „Repository staged, Unit of Work committet" ist der zentrale Vertrag dieser API. Sie sollte einmal an prominenter Stelle stehen — als `<remarks>` an `IRepository` (was in `BuildingBlocks/src` ohnehin gefordert ist) und als Test, der sie festhält:

```csharp
[Fact]
public async Task Add_WithoutCommit_DoesNotPersist()
```

Ein solcher Test dokumentiert die Semantik verbindlicher als jeder Kommentar und schlägt an, falls jemand später ein `SaveChanges` ins Repository einbaut.

---

# IMP-43 - Wirkungslose Varianz-Modifikatoren

- **Niedrig**

## Beschreibung

Fünf Verträge deklarieren Varianz auf einem Typparameter, der auf `struct` eingeschränkt ist:

```csharp
public interface IEntity<out TKey>                        where TKey : struct, IEntityKey
public interface IAggregateRoot<out TKey>                 where TKey : struct, IEntityKey
public interface IEventSourcedAggregateRoot<out TKey>     where TKey : struct, IEntityKey
public interface IState<TSelf, out TKey>                  where TKey : struct, IEntityKey
public interface IRepository<TAggregate, in TKey>         where TKey : struct, IEntityKey
public interface IEventSourcedRepository<TAggregate, in TKey>
```

Varianzkonvertierungen sind in .NET ausschließlich für Referenztypen definiert. Bei einem `struct`-Constraint gibt es keine Konvertierung, die `out` oder `in` ermöglichen würde — die Modifikatoren sind **wirkungslos**.

Kein Fehler, aber zwei kleine Kosten:

- **Falsches Signal.** `out TKey` suggeriert, dass `IAggregateRoot<SpecificId>` irgendwo als `IAggregateRoot<BaseId>` verwendbar wäre. Wer das annimmt und darauf aufbaut, verliert Zeit an einem Compile-Fehler, der nicht erklärt, warum die Varianz nicht greift.
- **Hinweis auf Herkunft.** Die Modifikatoren sind offenbar nicht aus einem konkreten Bedarf entstanden, sondern reflexhaft gesetzt. Das ist bei einer sonst sehr bewusst entworfenen Domain-Schicht die auffälligste Stelle, an der eine Entscheidung nicht durchdacht wurde.

Ein Sonderfall: `IAggregateRoot<out TKey>` erbt von `IEntity<out TKey>`. Würde das `struct`-Constraint je fallen (etwa für string-basierte Schlüssel), wäre die Varianz plötzlich wirksam und `IRepository<TAggregate, in TKey>` bekäme eine Semantik, die niemand geprüft hat.

## Lösungsvorschlag

**Schritt 1 — Modifikatoren entfernen:**

```csharp
public interface IEntity<TKey>                    where TKey : struct, IEntityKey
public interface IAggregateRoot<TKey>             where TKey : struct, IEntityKey
public interface IEventSourcedAggregateRoot<TKey> where TKey : struct, IEntityKey
public interface IState<TSelf, TKey>              where TKey : struct, IEntityKey
public interface IRepository<TAggregate, TKey>    where TKey : struct, IEntityKey
public interface IEventSourcedRepository<TAggregate, TKey>
```

Rein additive Änderung ohne Auswirkung auf bestehenden Code — die Varianz wurde nie genutzt, weil sie nie nutzbar war.

**Schritt 2 — Grundsatz festhalten.** Der allgemeine Punkt lohnt eine Zeile in der Coding-Konvention:

> Varianz-Modifikatoren werden nur gesetzt, wenn eine konkrete Konvertierung sie benötigt — nicht vorsorglich.

Vorsorgliche Varianz ist besonders tückisch, weil sie zusätzlich einschränkt: Ein `out`-Parameter darf nicht mehr an Eingabepositionen auftauchen. Das kann eine spätere, sinnvolle Erweiterung des Vertrags blockieren, ohne dass der Grund erkennbar ist.

**Schritt 3 — Constraint bewusst bestätigen.** `where TKey : struct` ist eine bewusste und gute Entscheidung: Typisierte IDs als `readonly record struct` sind allokationsfrei, kopiersicher und ohne Null-Sonderfall. Diese Entscheidung sollte sichtbar begründet sein — dann ist auch klar, dass die Varianz auf absehbare Zeit wirkungslos bleiben wird.

---

# IMP-44 - Uneinheitliche Projektstruktur

- **Niedrig**

## Beschreibung

Die drei Projekte sind unterschiedlich organisiert:

- **`BuildingBlocks.Domain`** — 17 Dateien, alle flach im Projektwurzelverzeichnis
- **`BuildingBlocks.Application`** — 19 Dateien, ebenfalls alle flach
- **`BuildingBlocks.Infrastructure`** — fünf Ordner (`DependencyInjection`, `Dispatching`, `Events`, `Messaging`, `Persistence`)

Bei knapp 20 Dateien ist eine flache Struktur noch handhabbar. Sie vermischt aber bereits jetzt Konzepte, die nichts miteinander zu tun haben: Im Wurzelverzeichnis von `Application` stehen CQRS-Verträge, das Ergebnismodell, Persistenz-Ports und Event-Ports nebeneinander. Beim Öffnen des Projekts ist nicht erkennbar, welche Konzeptgruppen es überhaupt gibt.

Zwei weitere Detailabweichungen:

**1. Zwei Typen in einer Datei.** `EntityKeyValueConverter.cs` enthält `EntityKeyValueConverter<TKey, TValue>` **und** `EntityKeyModelBuilderExtensions` — während im übrigen Repository strikt ein Typ pro Datei gilt (bis hin zu einzelnen Dateien für Ein-Zeilen-Interfaces wie `IClock` und `IHasDomainEvents`).

**2. Unnötige Vollqualifizierung.** In derselben Datei:

```csharp
public static Microsoft.EntityFrameworkCore.ModelBuilder ApplyEntityKeyConversions(
    this Microsoft.EntityFrameworkCore.ModelBuilder modelBuilder)
```

Es besteht kein Namenskonflikt — ein `using` würde genügen. Das wirkt wie ein Überbleibsel aus einem Refactoring und fällt in einer sonst sehr sauberen Codebasis auf.

## Lösungsvorschlag

**Schritt 1 — Ordner in Domain und Application einführen**, analog zur bereits bewährten Struktur in Infrastructure:

```
BuildingBlocks.Domain/
├── Aggregates/     AggregateRoot, EventSourcedAggregateRoot, IAggregateRoot,
│                   IEventSourcedAggregateRoot, IState
├── Entities/       Entity, IEntity, IEntityKey
├── Events/         DomainEvent, IDomainEvent, IHasDomainEvents, IDomainEventsManager
├── Rules/          IBusinessRule, IDomainValidationRule, RuleChecker,
│                   BusinessRuleViolationException, DomainValidationException
└── IClock.cs

BuildingBlocks.Application/
├── Cqrs/           ICommand, ICommandHandler, IQuery, IQueryHandler, ISender,
│                   IPipelineBehavior, RequestPipelineContinuation
├── Results/        Result, Result<T>, Failure, FailureCategory
├── Persistence/    IRepository, IEventSourcedRepository, IUnitOfWork
└── Events/         IProjectionHandler, IIntegrationEvent, IIntegrationEventMapper,
                    IDomainEventPublisher
```

**Namespaces bewusst flach lassen.** Die Ordner dienen der Navigation, nicht der Namensgebung. `BuildingBlocks.Application` als einziger Namespace bleibt für Konsumenten deutlich angenehmer als vier Namespaces mit vier `using`-Direktiven pro Handler-Datei. Dafür in beiden Projekten:

```ini
# .editorconfig
dotnet_style_namespace_match_folder = false
dotnet_diagnostic.IDE0130.severity = none
```

Diese Entscheidung sollte explizit konfiguriert sein — sonst meldet der Analyzer bei `AnalysisMode=All` und `TreatWarningsAsErrors` einen Build-Fehler.

**Schritt 2 — `EntityKeyModelBuilderExtensions` in eigene Datei** und das `using Microsoft.EntityFrameworkCore;` ergänzen. Fünf Minuten, stellt die Konvention wieder her.

**Schritt 3 — Nach IMP-19 neu bewerten.** Wird `Infrastructure` in vier Pakete geteilt, verschwinden dort mehrere Ordner ohnehin, weil ihr Inhalt zum eigenen Projekt wird. Die beiden Schritte lassen sich sinnvoll zusammen erledigen.

---

# IMP-45 - `SenderContractTests` testet NSubstitute statt Produktionscode

- **Niedrig**

## Beschreibung

```csharp
[Fact]
public async Task Send_Command_ReturnsHandlerResult()
{
    var sender = Substitute.For<ISender>();
    var command = new DeleteRecipeCommand();
    sender.Send(command, Arg.Any<CancellationToken>()).Returns(Result.Success());

    var result = await sender.Send(command, CancellationToken.None);

    Assert.True(result.IsSuccess);
}
```

Der Test konfiguriert ein Mock und prüft anschließend, dass das Mock das Konfigurierte zurückgibt. Es wird **kein einziger Zeile Produktionscode** ausgeführt — `Sender` liegt in `BuildingBlocks.Infrastructure` und wird vom Testprojekt gar nicht referenziert.

Der einzige reale Nutzen ist ein Compile-Zeit-Nachweis: Die drei `Send`-Überladungen sind aus Aufrufersicht eindeutig auflösbar. Das ist eine legitime Frage — `Send(ICommand)`, `Send<T>(ICommand<T>)` und `Send<T>(IQuery<T>)` könnten sich bei einem Typ, der mehrere Verträge implementiert, in die Quere kommen (siehe IMP-06). Nur beantwortet ein Laufzeittest diese Frage nicht besser als der Compiler.

Der Schaden liegt in der Statistik: Drei grüne Tests im Bericht suggerieren Absicherung für `ISender`, während der tatsächliche Dispatcher völlig ungetestet ist (IMP-09). Solche Tests sind schlechter als keine Tests, weil sie eine Lücke verdecken.

## Lösungsvorschlag

**Schritt 1 — Ersetzen statt reparieren.** Die Tests wandern nach `BuildingBlocks.Infrastructure.Tests` und laufen gegen den echten `Sender` mit einem echten Container (siehe IMP-09, Stufe 1). Dann prüfen sie tatsächlich Überladungsauflösung, Handler-Auflösung **und** Pipeline-Durchlauf.

**Schritt 2 — Was in `Application.Tests` bleiben sollte.** Wenn die Überladungsauflösung als Compile-Zeit-Eigenschaft abgesichert werden soll, ist ein reiner Kompilierungstest ehrlicher — er behauptet keine Laufzeitabdeckung:

```csharp
/// Sichert ab, dass die drei Send-Überladungen für die üblichen Vertragsformen
/// eindeutig auflösbar sind. Der Wert liegt im Kompilieren, nicht im Ausführen.
internal static class SenderOverloadResolution
{
    public static void CommandWithoutResult(ISender sender, DeleteRecipeCommand command) =>
        _ = sender.Send(command, CancellationToken.None);

    public static void CommandWithResult(ISender sender, CreateRecipeCommand command) =>
        _ = sender.Send(command, CancellationToken.None);

    public static void Query(ISender sender, GetRecipeQuery query) =>
        _ = sender.Send(query, CancellationToken.None);
}
```

Kein `[Fact]`, keine Assertion, keine irreführende Statistik — die Datei erfüllt ihren Zweck dadurch, dass sie kompiliert.

**Schritt 3 — Als Prinzip festhalten.** Der allgemeine Punkt gilt über diesen Fall hinaus:

> Ein Test, der ausschließlich ein Mock konfiguriert und dessen Rückgabe prüft, testet das Mocking-Framework. Mocks gehören an die Grenzen des zu testenden Codes, nicht in dessen Zentrum.

Das ist auch der Grund, warum die Domain-Tests handgeschriebene Test-Doubles verwenden statt NSubstitute — eine Entscheidung, die dort konsequent und richtig umgesetzt ist. `SenderContractTests` ist die einzige Stelle, an der davon abgewichen wurde.

---

# IMP-46 - Behaviors nutzen Service Locator statt optionaler Abhängigkeiten

- **Niedrig**

## Beschreibung

```csharp
public sealed class UnitOfWorkBehavior<TRequest, TResponse>(IServiceProvider serviceProvider)
{
    var unitOfWork = serviceProvider.GetRequiredService<IUnitOfWork>();
}
```

Die Abhängigkeit wird nicht injiziert, sondern zur Laufzeit aus dem Container gezogen. Folgen:

- **Die Signatur lügt.** `UnitOfWorkBehavior(IServiceProvider)` verrät nicht, dass das Behavior eine Unit of Work braucht. Wer die Klasse verwendet oder umbaut, muss den Rumpf lesen.
- **Testbarkeit.** Für einen Unit-Test muss ein echter Container gebaut werden, statt `new UnitOfWorkBehavior<C, Result>(fakeUnitOfWork)` zu schreiben. Das ist der Hauptgrund, warum es für dieses Behavior keinen Test gibt.
- **Der einzige denkbare Vorteil wird nicht genutzt.** Service Location wäre hier gerechtfertigt, wenn die Abhängigkeit optional sein soll — genau das ist der Fall (siehe IMP-07), aber `GetRequiredService` gibt diesen Vorteil sofort wieder auf.

Bei `Sender` und `ProjectionRunner` ist `IServiceProvider` dagegen **notwendig**: Beide lösen zur Laufzeit geschlossene generische Typen auf, deren Typparameter erst aus `command.GetType()` bzw. `domainEvent.GetType()` hervorgehen. Das ist mit konstruktorbasierter Injektion nicht ausdrückbar. Diese beiden Fälle sind korrekt und sollten so bleiben.

## Lösungsvorschlag

**Schritt 1 — `UnitOfWorkBehavior` auf Konstruktorinjektion umstellen** (identisch zu IMP-07, hier aus Testbarkeitssicht):

```csharp
public sealed class UnitOfWorkBehavior<TRequest, TResponse>(IUnitOfWork? unitOfWork)
    : IPipelineBehavior<TRequest, TResponse>
    where TResponse : Result
```

Die Optionalität steht damit im Typsystem, der Test kommt ohne Container aus, und der Fehlerfall aus IMP-07 verschwindet gleichzeitig.

**Schritt 2 — Verwendung von `IServiceProvider` begründen, wo sie bleibt.** In `Sender` und `ProjectionRunner` ist sie sachlich notwendig. Ein kurzer `<remarks>`-Absatz (in `BuildingBlocks/src` ohnehin gefordert) hält fest, warum — sonst wird es beim nächsten Review erneut diskutiert oder, schlimmer, als Vorbild für neue Klassen genommen.

**Schritt 3 — Als Regel festhalten:**

> `IServiceProvider` wird ausschließlich injiziert, wenn zur Laufzeit ein Typ aufgelöst werden muss, der zur Kompilierzeit unbekannt ist. Optionale Abhängigkeiten werden als nullable Konstruktorparameter ausgedrückt, Mehrfachabhängigkeiten als `IEnumerable<T>`.

Beide Alternativen sind in .NET DI vollständig unterstützt und in der bestehenden Codebasis bereits im Einsatz (`Publisher` nimmt `IEnumerable<IIntegrationEventMapper>`), sodass die Regel nur das festschreibt, was ohnehin überwiegend praktiziert wird.

---

# IMP-47 - Keine zentrale Paketverwaltung

- **Niedrig**

## Beschreibung

Es existiert kein `Directory.Packages.props`. Paketversionen stehen einzeln in den `.csproj`-Dateien:

```xml
<PackageReference Include="Marten" Version="9.20.1" />
<PackageReference Include="WolverineFx.RabbitMQ" Version="6.23.0" />
<PackageReference Include="WolverineFx.Marten" Version="6.23.0" />
<PackageReference Include="WolverineFx.EntityFrameworkCore" Version="6.23.0" />
```

Die Lösung enthält bereits acht Projekte, und mit jedem Service kommen weitere hinzu, die dieselben Pakete referenzieren werden.

Konkretes Risiko: Die drei `WolverineFx.*`-Pakete **müssen** dieselbe Version haben — gemischte Versionen führen zu Bindungsfehlern, die sich als schwer deutbare `MissingMethodException` zur Laufzeit äußern. Aktuell hält sie nur die Sorgfalt beim manuellen Editieren zusammen. Dasselbe gilt für EF Core und Npgsql.

Nach der Aufteilung aus IMP-19 verschärft sich das: Vier Infrastructure-Pakete plus Testprojekte plus Services referenzieren dann überlappende Paketmengen an einem guten Dutzend Stellen.

## Lösungsvorschlag

**Schritt 1 — `Directory.Packages.props` im Repository-Wurzelverzeichnis:**

```xml
<Project>
  <PropertyGroup>
    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
    <CentralPackageTransitivePinningEnabled>true</CentralPackageTransitivePinningEnabled>
  </PropertyGroup>

  <ItemGroup Label="Persistence">
    <PackageVersion Include="Marten" Version="9.20.1" />
    <PackageVersion Include="Microsoft.EntityFrameworkCore" Version="10.0.10" />
    <PackageVersion Include="Npgsql.EntityFrameworkCore.PostgreSQL" Version="10.0.3" />
  </ItemGroup>

  <ItemGroup Label="Messaging">
    <PackageVersion Include="WolverineFx.RabbitMQ" Version="6.23.0" />
    <PackageVersion Include="WolverineFx.Marten" Version="6.23.0" />
    <PackageVersion Include="WolverineFx.EntityFrameworkCore" Version="6.23.0" />
  </ItemGroup>

  <ItemGroup Label="Testing">
    <PackageVersion Include="xunit.v3" Version="…" />
    <PackageVersion Include="NSubstitute" Version="…" />
  </ItemGroup>
</Project>
```

In den Projektdateien entfallen die Versionsangaben:

```xml
<PackageReference Include="Marten" />
```

`CentralPackageTransitivePinningEnabled` ist der wichtigere der beiden Schalter: Er fixiert auch transitive Abhängigkeiten auf die zentral festgelegte Version und verhindert damit die schwer zu diagnostizierende Situation, in der ein Paket eine ältere Version einer gemeinsam genutzten Bibliothek nach oben zieht.

**Schritt 2 — Zusammengehörigkeit sichtbar machen.** Die `Label`-Gruppen sind mehr als Kosmetik: Sie zeigen, welche Pakete gemeinsam aktualisiert werden müssen. Für die `WolverineFx.*`-Familie lohnt zusätzlich eine gemeinsame Property:

```xml
<PropertyGroup>
  <WolverineVersion>6.23.0</WolverineVersion>
</PropertyGroup>

<ItemGroup>
  <PackageVersion Include="WolverineFx.RabbitMQ" Version="$(WolverineVersion)" />
  <PackageVersion Include="WolverineFx.Marten" Version="$(WolverineVersion)" />
  <PackageVersion Include="WolverineFx.EntityFrameworkCore" Version="$(WolverineVersion)" />
</ItemGroup>
```

Damit ist die Invariante „alle Wolverine-Pakete auf derselben Version" nicht mehr nur Konvention, sondern strukturell garantiert.

**Schritt 3 — Testprojekt-Pakete gleich mit erfassen.** Die Testprojekte referenzieren xUnit, NSubstitute und die EF-Core-InMemory-Provider. Mit dem Infrastructure-Testprojekt aus IMP-09 kommen Testcontainers und `Microsoft.Extensions.Diagnostics.Testing` hinzu — ein guter Anlass, die zentrale Verwaltung vorher einzuführen statt nachher.

---

# IMP-48 - Uneinheitliche Benennung der Wolverine-Extensions

- **Niedrig**

## Beschreibung

```csharp
public static WolverineOptions ApplyBuildingBlockDomainEventRouting(this WolverineOptions options)
public static WolverineOptions ApplyBuildingBlockMessagingDefaults(this WolverineOptions options, Uri rabbitMqUri)
public static WolverineOptions ApplyBuildingBlockEfCoreOutbox(this WolverineOptions options)
```

Drei Abweichungen von der sonstigen Benennung im Repository:

**1. Singular statt Plural.** Assembly, Namespaces, Verzeichnis und die zentrale Erweiterungsmethode heißen durchgängig `BuildingBlocks` — hier steht `BuildingBlock`. Das sind zugleich die Methoden, die jeder Host-Entwickler beim Setup tippt, also die sichtbarste Stelle für eine Abweichung.

**2. `Apply*` statt `Use*`/`Add*`.** Der Rest der Konfiguration verwendet `AddBuildingBlocks`, `UseEfCorePersistence`, `UseMartenEventSourcing`, `UseWolverineMessaging`. `Apply*` ist ein vierter Stil ohne erkennbaren Unterschied in der Semantik — alle drei Methoden konfigurieren etwas.

**3. Namensgleiches Präfix für Ungleiches.** `ApplyBuildingBlockMessagingDefaults` setzt RabbitMQ-Verbindung und Retry-Policies, `ApplyBuildingBlockDomainEventRouting` konfiguriert eine lokale Queue, `ApplyBuildingBlockEfCoreOutbox` ist ein Ein-Zeilen-Wrapper. Die gemeinsame Benennung suggeriert Austauschbarkeit, obwohl die drei unterschiedliche Voraussetzungen haben und in einer bestimmten Kombination aufgerufen werden müssen (siehe IMP-13).

## Lösungsvorschlag

**Schritt 1 — Vereinheitlichen.** Da IMP-13 vorschlägt, die drei Methoden ohnehin hinter einem gemeinsamen Einstiegspunkt zu bündeln, ist der saubere Zielzustand:

```csharp
// Primärer Weg — deckt alle drei Schritte ab
public static BuildingBlocksOptions UseWolverineMessaging(this BuildingBlocksOptions options, Uri rabbitMqUri);

// Escape-Hatch für Hosts mit Sonderanforderungen, konsistent benannt
public static WolverineOptions UseBuildingBlocksDomainEventRouting(this WolverineOptions options);
public static WolverineOptions UseBuildingBlocksMessagingDefaults(this WolverineOptions options, Uri rabbitMqUri, bool autoProvision = false);
```

`Use*` statt `Apply*`, `BuildingBlocks` im Plural — damit gilt im gesamten Repository eine Regel: `Add*` registriert Dienste im Container, `Use*` konfiguriert Verhalten.

**Schritt 2 — `ApplyBuildingBlockEfCoreOutbox` ersatzlos streichen.**

```csharp
public static WolverineOptions ApplyBuildingBlockEfCoreOutbox(this WolverineOptions options)
{
    options.UseEntityFrameworkCoreTransactions();
    return options;
}
```

Ein Wrapper um genau einen Framework-Aufruf, ohne eigene Logik. Er fügt keinen Wert hinzu, sondern nur einen weiteren Namen, den man kennen und in der richtigen Kombination aufrufen muss. Nach IMP-13 wird `UseEntityFrameworkCoreTransactions()` intern aufgerufen, wenn EF-Persistenz konfiguriert ist — der Wrapper wird damit überflüssig.

Der allgemeine Grundsatz dahinter: Eine Fassade, die nichts kapselt, erhöht die Zahl der Konzepte, ohne die Komplexität zu senken. Sie lohnt erst, wenn sie mehrere Aufrufe bündelt, eine Voraussetzung prüft oder eine Entscheidung trifft.

**Schritt 3 — Benennungsregel festhalten.** Eine Zeile in der Coding-Konvention verhindert das Wiederauftreten:

> Erweiterungsmethoden der Plattform verwenden `AddBuildingBlocks*` für Container-Registrierung und `UseBuildingBlocks*` für Verhaltenskonfiguration. Das Präfix lautet immer `BuildingBlocks` im Plural.
