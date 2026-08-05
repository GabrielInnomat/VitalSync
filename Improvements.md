# Improvements — BuildingBlocks

Befunde aus der Code-Analyse von `BuildingBlocks/src/BuildingBlocks.{Domain,Application,Infrastructure}`.

**Statusprüfung vom 2026-08-02:** Jeder Punkt wurde gegen den aktuellen Quellcode verifiziert, nicht
gegen den zuvor im Dokument hinterlegten Status — mehrere Einträge waren veraltet oder beschrieben
einen überholten Zwischenstand. Testlauf zum Prüfzeitpunkt: **199 bestanden, 11 übersprungen
(Docker-/Aspire-abhängig), 0 Fehler.**

Überschneidungen mit [hacky.md](hacky.md) sind beim jeweiligen Punkt vermerkt.

## Status

| Nr.    | Titel                                                                    | Status            |
| ------ | ------------------------------------------------------------------------ | ----------------- |
| IMP-01 | `OccurredAt` wird im state-stored Pfad nie gesetzt                       | gelöst            |
| IMP-02 | Kein `IUnitOfWork` bei gemischtem EF/Marten-Kontext                      | gelöst            |
| IMP-03 | Erwartete Domänenfehler werden als Error geloggt                         | gelöst            |
| IMP-04 | Integrations-Event-Transport nutzt `IMessageBus` statt `IMessageContext` | gelöst            |
| IMP-05 | `AddHandlersFrom` erzeugt Duplikate und überschreibt still               | gelöst            |
| IMP-06 | `RequestSender`-Cache ist nur nach Request-Typ gekeyed                          | gelöst            |
| IMP-07 | `UnitOfWorkBehavior` wirft ohne konfigurierte Persistenz                 | gelöst            |
| IMP-08 | `MartenUnitOfWork` flusht die Outbox nicht                               | gelöst            |
| IMP-09 | Kein Testprojekt für `BuildingBlocks.Infrastructure`                     | gelöst            |
| IMP-10 | Zwei inkompatible Aggregat-Programmiermodelle                            | gelöst            |
| IMP-11 | `IIntegrationEvent` ist ein leerer Marker                                | gelöst            |
| IMP-12 | `IIntegrationEventMapper` ist untypisiert                                | offen             |
| IMP-13 | Messaging-Konfiguration ohne Guard-Rails                                 | teilweise         |
| IMP-14 | Constraint-Mismatch zwischen Repository-Vertrag und Implementierung      | gelöst            |
| IMP-15 | Repository lädt Aggregate unvollständig                                  | gelöst            |
| IMP-16 | Kein Validierungs-Behavior, Mehrfachfehler nicht erzeugbar               | teilweise         |
| IMP-17 | `Failure` ohne Zielfeld und ohne fachliche Fehlercodes                   | offen             |
| IMP-18 | `FailureCategory` fehlen Autorisierung und Unerwartet                    | offen             |
| IMP-19 | Ein Assembly für EF Core, Marten, Wolverine und RabbitMQ                 | offen             |
| IMP-20 | `DbContext` als DI-Schlüssel kollidiert mit dem Read/Write-Paar          | offen             |
| IMP-21 | `IRepository` koppelt an die konkrete Domain-Basisklasse                 | gelöst            |
| IMP-22 | `AssemblyQualifiedName` als Event-Typ-Token                              | gelöst            |
| IMP-23 | Marten-Stream-Key hängt am Klassennamen                                  | gelöst            |
| IMP-24 | `DomainEventEnvelope` trägt zu wenig Metadaten                           | gelöst            |
| IMP-25 | `Sequential()` auf einer einzigen Queue für alle Domain Events           | offen             |
| IMP-26 | `DomainEventPublisher` koppelt Projektion und Integration-Event-Publikation         | offen             |
| IMP-27 | `FailureResults`-Reflection ist vermeidbar                               | gelöst            |
| IMP-28 | Kein `IClock` im Container                                               | gelöst            |
| IMP-29 | Unique-Constraint-Verletzungen werden nicht übersetzt                    | offen             |
| IMP-30 | Keine Tracing-Instrumentierung der CQRS-Pipeline                         | offen             |
| IMP-31 | Read-Modelle im state-stored Pfad sind nicht wiederaufbaubar             | offen             |
| IMP-32 | Keine Batch- oder Bulk-Fähigkeit                                         | offen             |
| IMP-33 | Keine Saga- oder Process-Manager-Abstraktion                             | offen             |
| IMP-34 | `Result` hat keine Kombinatoren                                          | offen             |
| IMP-35 | Statische Caches über Container-Grenzen hinweg                           | wird nicht gelöst |
| IMP-36 | `RuleChecker` schluckt `null` still                                      | **gelöst**    |
| IMP-37 | Async-Suffix ist inkonsistent                                            | **gelöst**    |
| IMP-38 | Sichtbarkeits-Disziplin ist uneinheitlich                                | gelöst            |
| IMP-39 | `Result`-API: Namenskollision und implizite Konvertierungen              | teilweise gelöst  |
| IMP-40 | `State` ist `public` und bricht die Kapselung                            | gelöst            |
| IMP-41 | `DomainEvent` als `record` mit garantiert ungleicher Wertgleichheit      | gelöst            |
| IMP-42 | `IRepository`-API ist asymmetrisch und irreführend benannt               | gelöst            |
| IMP-43 | Wirkungslose Varianz-Modifikatoren                                       | offen             |
| IMP-44 | Uneinheitliche Projektstruktur                                           | gelöst            |
| IMP-45 | `SenderContractTests` testet NSubstitute statt Produktionscode           | offen             |
| IMP-46 | Behaviors nutzen Service Locator statt optionaler Abhängigkeiten         | teilweise         |
| IMP-47 | Keine zentrale Paketverwaltung                                           | offen             |
| IMP-48 | Uneinheitliche Benennung der Wolverine-Extensions                        | teilweise         |

---

# IMP-01, `OccurredAt` wird im state-stored Pfad nie gesetzt

Domain Events state-stored Aggregate trugen `default(DateTimeOffset)`, weil nur der event-sourced
Pfad stempelte. Read-Modelle hätten „Jahr 1" gezeigt.

**Verifiziert gelöst.** `DomainEventStamper` existiert und wird in beiden Unit-of-Work-Implementierungen
mit einem einzigen `clock.Now`-Wert pro Transaktion aufgerufen
([EfCoreUnitOfWork.cs:50-58](BuildingBlocks/src/BuildingBlocks.Infrastructure/Persistence/StateStored/EfCoreUnitOfWork.cs:50),
[MartenUnitOfWork.cs:39-58](BuildingBlocks/src/BuildingBlocks.Infrastructure/Persistence/EventSourced/MartenUnitOfWork.cs:39)).
Der Marten-Pfad hängt die **gestempelten** Events an den Stream, der Replay rehydriert echte
Zeitstempel. `RaiseEvent` nimmt keinen `IClock` mehr.

## Lösungsvorschlag

Umgesetzt: Stempeln an der Transaktionsgrenze statt im Aggregat. Verbleibende Einschränkung siehe
IMP-24 und [hacky.md Nr. 7](hacky.md) — die Sentinel-Erkennung `OccurredAt.Ticks == 0` greift nur für
Events, die vom `DomainEvent`-Record erben.

---

# IMP-02, Kein `IUnitOfWork` bei gemischtem EF/Marten-Kontext

`TryAddScoped` in beiden Persistenzvarianten ließ die zweite Registrierung still verschwinden —
stiller Datenverlust bei erfolgreichem Command.

**Verifiziert gelöst.** `SelectPersistenceStyle` wirft mit ausführlicher Begründung, sobald ein Host
beide Stile wählt
([BuildingBlocksOptions.cs:127-141](BuildingBlocks/src/BuildingBlocks.Infrastructure/DependencyInjection/BuildingBlocksOptions.cs:127)).

## Lösungsvorschlag

Umgesetzt: eine Persistenzstrategie pro Bounded Context, die Mischung wird laut abgelehnt statt
nicht-atomar ermöglicht (ADR-0019/0020/0021).

---

# IMP-03, Erwartete Domänenfehler werden als Error geloggt

`ExceptionToResultBehavior` lag außerhalb des Loggings, jede Validierungsverletzung erzeugte einen
`Error`-Log und machte die SLO-Metrik unbrauchbar.

**Verifiziert gelöst.** Explizite numerische Reihenfolge `Logging(0) → ExceptionToResult(100) →
UnitOfWork(300)`
([BuildingBlocksOptions.cs:40-60](BuildingBlocks/src/BuildingBlocks.Infrastructure/DependencyInjection/BuildingBlocksOptions.cs:40)),
`Sender.BuildPipeline` sortiert `OrderByDescending`, sodass niedrige Orders außen liegen
([RequestSender.cs:73-88](BuildingBlocks/src/BuildingBlocks.Infrastructure/Dispatching/RequestSender.cs:73)).

## Lösungsvorschlag

Umgesetzt. Die Reihenfolge ist über `AddPipelineBehavior(type, order)` ein expliziter Vertrag.
Restrisiko siehe IMP-46 und [hacky.md Nr. 9](hacky.md): unregistrierte Behaviors landen still auf
Order `0` und kollidieren mit dem Logging.

---

# IMP-04, Integrations-Event-Transport nutzt `IMessageBus` statt `IMessageContext`

Der per DI injizierte `IMessageBus` war nicht in die Transaktion der verarbeiteten Nachricht
eingeschrieben: Correlation ging verloren, Integration Events gingen vor dem Retry raus.

**Verifiziert gelöst.** `DomainEventEnvelopeHandler` nimmt `IMessageContext` als Handler-Parameter
([DomainEventEnvelopeHandler.cs:33](BuildingBlocks/src/BuildingBlocks.Infrastructure/Messaging/DomainEvents/DomainEventEnvelopeHandler.cs:33)),
`IIntegrationEventSink` macht die Senke in der Signatur von `IDomainEventPublisher.PublishAsync`
sichtbar, `WolverineIntegrationEventSink` kapselt den Kontext. `IIntegrationEventTransport` existiert
nicht mehr.

**Korrektur am bisherigen Statustext:** Die dort beschriebene `EfCoreMessageStoreRegistration` und
`ApplyBuildingBlockEfCoreOutbox` existieren **nicht mehr**. Der Message-Store wird seit dem zweiten
ADR-0027-Amendment (2026-08-03) von `HostApplicationBuilderExtensions.AddBuildingBlocks` in dessen
eigenem `UseWolverine`-Callback registriert, aus dem bereits ausgewählten Connection String — der Host
verdrahtet nichts mehr selbst.

## Lösungsvorschlag

Umgesetzt. Der verbleibende Preis dieser Lösung — der Host muss den Connection String ein zweites Mal
angeben, ohne dass ihn jemand abgleicht — war als [hacky.md Nr. 8](hacky.md) erfasst und ist seit dem
2026-08-03 ebenfalls weg: Building Blocks setzt den `UseWolverine`-Aufruf selbst ab und nimmt den
String aus der `UseEfCorePersistence`-Auswahl (zweites ADR-0027-Amendment, [todo.md](todo.md) TODO-06).

---

# IMP-05, `AddHandlersFrom` erzeugt Duplikate und überschreibt still

`AddScoped` pro Fund: doppelte Projektionen bei doppeltem Scan, stilles Überschreiben bei zwei
Command-Handlern.

**Verifiziert gelöst.** `TryAddEnumerable` für Mehrfach-Handler, `RegisterSingleHandler` wirft bei
zwei verschiedenen Handlern desselben Vertrags, `ReflectionTypeLoadException` wird übersetzt
([BuildingBlocksOptions.cs:162-226](BuildingBlocks/src/BuildingBlocks.Infrastructure/DependencyInjection/BuildingBlocksOptions.cs:162)).
Der `HandlerRegistrationStartupValidator` prüft beim Start als **Opt-out** statt Opt-in.

## Lösungsvorschlag

Umgesetzt (Schritte 1–4), abgesichert durch `HandlerRegistrationTests` und
`HandlerStartupValidationTests` mit eigenen Fixture-Assemblies.

---

# IMP-06, `RequestSender`-Cache ist nur nach Request-Typ gekeyed

Ein Typ mit zwei Ergebnisverträgen holte den falschen Dispatcher aus dem Cache →
`InvalidCastException`, zustandsabhängig und schwer diagnostizierbar.

**Verifiziert gelöst.** `private readonly record struct DispatcherKey(Type Request, Type Result)`
([RequestSender.cs:23-27](BuildingBlocks/src/BuildingBlocks.Infrastructure/Dispatching/RequestSender.cs:23)).
Zusätzlich lehnt der Startup-Validator mehrdeutige Request-Typen ab
([HandlerRegistrationStartupValidator.cs:77-84](BuildingBlocks/src/BuildingBlocks.Infrastructure/DependencyInjection/Validation/HandlerRegistrationCheck.cs:77)).

## Lösungsvorschlag

Umgesetzt (Schritte 1–3).

---

# IMP-07, `UnitOfWorkBehavior` wirft ohne konfigurierte Persistenz

`GetRequiredService<IUnitOfWork>()` ließ jeden Host ohne Persistenz bei jedem Command abstürzen.

**Verifiziert gelöst.** `IUnitOfWork? unitOfWork = null` als Konstruktorparameter
([UnitOfWorkBehavior.cs:27](BuildingBlocks/src/BuildingBlocks.Infrastructure/Dispatching/UnitOfWorkBehavior.cs:27)),
dazu der `MissingUnitOfWorkStartupLogger`, der den No-Op beim Start sichtbar macht.

## Lösungsvorschlag

Umgesetzt. Die gewählte Form — optionale Constructor-Injection per Default-Wert — ist selbst als
Foot-Gun erfasst ([hacky.md Nr. 11](hacky.md)): ein explizit registrierter `NullUnitOfWork` würde aus
dem stillen Default eine Entscheidung machen.

---

# IMP-08, `MartenUnitOfWork` flusht die Outbox nicht

Der Befund war bereits bei der ursprünglichen Analyse falsch hergeleitet.

**Verifiziert gelöst — Prämisse trifft nicht zu.** `IMartenOutbox` hat kein
`FlushOutgoingMessagesAsync()`; `Enroll(session)` registriert den Session-Listener, der nach jedem
erfolgreichen `SaveChangesAsync` selbst flusht. `MartenUnitOfWork.CommitAsync` ruft `Enroll` als
ersten Schritt auf, **vor** dem Save
([MartenUnitOfWork.cs:37](BuildingBlocks/src/BuildingBlocks.Infrastructure/Persistence/EventSourced/MartenUnitOfWork.cs:37)) —
genau die Reihenfolge, auf die es ankommt. End-to-end gepinnt durch `OutboxFlushOnCommitTests`.

## Lösungsvorschlag

Keine Änderung nötig. Die Template-Basisklasse aus dem ursprünglichen Schritt 2 wurde bewusst
verworfen: zwei sanktionierte Persistenzstile rechtfertigen keine Abstraktion mit zwei Nutzern.

---

# IMP-09, Kein Testprojekt für `BuildingBlocks.Infrastructure`

Der gesamte Code mit Reflection, Nebenläufigkeit, Transaktionen und Versionsarithmetik war ungetestet.

**Verifiziert gelöst.** `BuildingBlocks.Infrastructure.Tests` existiert mit **91 Tests**, dazu
Architekturtests, die die Schichtregeln per Reflection durchsetzen
([ArchitectureTests.cs](BuildingBlocks/tests/BuildingBlocks.Infrastructure.Tests/ArchitectureTests.cs)),
und Testcontainers-gestützte Persistenztests, die ohne Docker automatisch skippen.

## Lösungsvorschlag

Umgesetzt. Alle zuvor als `Skip` hinterlegten Regressionstests sind aktiviert.

---

# IMP-10, Zwei inkompatible Aggregat-Programmiermodelle

`AggregateRoot<TKey>` und `EventSourcedAggregateRoot<TKey, TState>` teilten außer der Gleichheitslogik
nichts — zwei Autorenmodelle, zwei Repository-Verträge.

**Verifiziert gelöst.** `AggregateRoot<TKey, TState>` ist die einzige Basis, `EventSourcedAggregateRoot`
erbt davon und fügt ausschließlich `Version` und `LoadFromHistory` hinzu
([EventSourcedAggregateRoot.cs:17-49](BuildingBlocks/src/BuildingBlocks.Domain/Aggregates/EventSourcedAggregateRoot.cs:17)).
Ein Repository-Vertrag für beide Welten.

## Lösungsvorschlag

Umgesetzt via ADR-0025/ADR-0026.

---

# IMP-11, `IIntegrationEvent` ist ein leerer Marker

**Gelöst (2026-08-03)** via TODO-13 /
[ADR-0029](docs/architecture/decisions/0029-event-identity-placement.md): `IIntegrationEvent`
verlangt `Guid EventId` und `DateTimeOffset OccurredAt`; Mapper übernehmen beides aus der
`DomainEventMetadata` des auslösenden Envelopes. Auf die Basisklasse mit `Guid.NewGuid()` im
Konstruktor wurde bewusst verzichtet — eine pro Aufruf frisch geprägte Id würde bei Redelivery
die Deduplizierung brechen.

`public interface IIntegrationEvent;` — kein `EventId`, kein `OccurredAt`, während `IDomainEvent`
beides trägt. Der Konsument kann eine redelivered Nachricht nicht als Duplikat erkennen, obwohl die
Zustellung ausdrücklich at-least-once ist.

[IIntegrationEvent.cs](BuildingBlocks/src/BuildingBlocks.Application/IntegrationEvents/IIntegrationEvent.cs) —
unverändert leer.

## Lösungsvorschlag

Dem Marker dieselbe Mindestausstattung geben wie dem Domain Event, plus eine Basisklasse, die sie
liefert:

```csharp
public interface IIntegrationEvent
{
    Guid EventId { get; }
    DateTimeOffset OccurredAt { get; }
}

public abstract record IntegrationEvent : IIntegrationEvent
{
    protected IntegrationEvent() => EventId = Guid.NewGuid();

    public Guid EventId { get; init; }
    public DateTimeOffset OccurredAt { get; init; }
}
```

Der Mapper übernimmt `EventId`/`OccurredAt` sinnvollerweise aus dem auslösenden Domain Event, damit
die Kausalkette über die Kontextgrenze sichtbar bleibt. Erst damit ist die im Consumer geforderte
Idempotenz überhaupt implementierbar.

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

# IMP-13, Messaging-Konfiguration ohne Guard-Rails

Sechs voneinander unabhängige Aufrufe über zwei Oberflächen, jede Auslassung still.

**Teilweise gelöst.** Schritt 1 ist umgesetzt und strenger als vorgeschlagen:
`BuildingBlocksWolverineExtension` wendet beim `UseWolverine()` automatisch die passende Kombination
an, die `Apply*`-Methoden sind `internal`
([BuildingBlocksWolverineExtension.cs](BuildingBlocks/src/BuildingBlocks.Infrastructure/DependencyInjection/Wiring/BuildingBlocksWolverineExtension.cs)),
und der `WolverineWiringStartupValidator` prüft beim Start, ob `UseWolverine` überhaupt aufgerufen
wurde. `AddBuildingBlocks` lehnt außerdem eine Subscription ohne Transport ab
([ServiceCollectionExtensions.cs:63-69](BuildingBlocks/src/BuildingBlocks.Infrastructure/DependencyInjection/ServiceCollectionExtensions.cs:63)).

**Offen:** Mapper-ohne-Transport-Prüfung, `UseNoMessaging` statt stillem Null-Sink-Default,
umgebungsabhängiges `AutoProvision`, differenzierte Retry-Policy (heute `OnException<Exception>` für
alles).

## Lösungsvorschlag

```csharp
if (registrierteMapper.Any() && WolverineWiring.RabbitMqUri is null && !_noMessagingSelected)
    throw new InvalidOperationException(
        "Integration-Event-Mapper registriert, aber kein Transport. UseWolverineMessaging(...) " +
        "aufrufen oder UseNoMessaging() explizit wählen.");

options.UseRabbitMq(uri).AutoProvision();

options.Policies.OnException<JsonException>().MoveToErrorQueue();
options.Policies.OnException<NpgsqlException>().RetryWithCooldown(...);
```

Der neue, mit ADR-0027 hinzugekommene Bruch in derselben Oberfläche — der doppelt anzugebende
Connection String — ist als [hacky.md Nr. 8](hacky.md) erfasst.

---

# IMP-14, Constraint-Mismatch zwischen Repository-Vertrag und Implementierung

**Gelöst (2026-08-03).** `IEventSourcedRepository` existiert nicht mehr; es gibt nur noch
`IRepository<TAggregate, TKey>`. Der Mismatch bestand danach in neuer Form fort:
`MartenEventSourcedRepository` verlangte zusätzlich `IEventSourcedAggregateRoot<TKey>` **und**
`new()`, `EfCoreRepository` nur `IAggregateRoot<TKey>` — beide open-generisch auf denselben Vertrag
registriert, also scheiterte ein falsch zugeschnittenes Aggregat erst beim `GetRequiredService` mit
einer DI-Meldung, die die Constraint-Verletzung nicht nannte.

Der `new()`-Teil ist ersatzlos weg: beide Implementierungen verlangen jetzt
`IReconstitutable<TAggregate>` (`static abstract CreateEmpty()`), und die Constraint steht am
**Vertrag**, nicht nur an den Implementierungen. Ein falsch zugeschnittenes Aggregat ist damit ein
**Compile-Fehler an der Injektionsstelle** — der hier vorgeschlagene Startup-Check ist überflüssig
geworden, weil der Compiler die Prüfung übernimmt. Details in **TODO-10** und im
Rekonstitutions-Amendment von ADR-0025.

**Nachtrag (2026-08-04):** Der Absatz oben ist überholt — `IReconstitutable` ist wieder gelöscht
(die Zeremonie pro Aggregat wog schwerer als der Compile-Zeit-Beweis), und der hier ursprünglich
vorgeschlagene **Startup-Check ist doch gekommen**: `AddBuildingBlocks` scannt die
`AddDomainEventsFrom`-Assemblies und scheitert bei fehlendem parameterlosem Konstruktor mit dem
Namen des Aggregats. Die Hülle liefert eine interne, pro Typ gecachte `AggregateFactory`. Siehe
TODO-10-Nachtrag und ADR-0025-Amendment 2026-08-04.

Was **nicht** von der Constraint erfasst wird: `MartenEventSourcedRepository` verlangt weiterhin
`IEventSourcedAggregateRoot<TKey>`, `EfCoreRepository` nicht. Ein event-sourced Aggregat, das gegen
eine EF-Registrierung aufgelöst wird (oder umgekehrt), fällt also nach wie vor erst im Container
auf. Das ist aber eine **Wahl der Persistenzstrategie im Composition Root**, kein Zuschnitt des
Aggregats — und ADR-0025/0026 sagen ausdrücklich, dass diese Wahl dort und nur dort getroffen wird.

---

# IMP-15, Repository lädt Aggregate unvollständig

**Gelöst (2026-08-04) — die Annahme war falsch, das Symptom trotzdem echt.** Gemessen statt vermutet
(`EfCoreChildCollectionTests`, Testcontainers/PostgreSQL): `FindAsync(stateType, [id])` lädt
**Owned Dependents sehr wohl** mit ihrem Owner — auch in der nicht-generischen Überladung, ohne
`Include`, ohne `AutoInclude`. Die hier vorgeschlagene `IAggregateGraph`-Abstraktion wäre also
Code ohne Wirkung gewesen. Der Ladepfad blieb unverändert.

Echt war die Lücke auf der **Schreibseite** ([hacky.md Nr. 6](hacky.md)) — und die ist gefixt:
`EfCoreUnitOfWork` kopiert jetzt auch die Navigationen. Damit Ladepfad und Schreibpfad diese
Zusage überhaupt halten können, schreibt
[ADR-0031](docs/architecture/decisions/0031-aggregate-child-collections-as-owned-types.md)
Kinder als **Owned Types** fest; eine Navigation eines States auf einen unabhängigen Entity-Typ
lehnt `AggregateStateModelStartupValidator` beim Hoststart ab. Das Sample belegt es end-to-end
(`widget_parts`, Smoke-Tests).

Der ursprüngliche Befund und der damalige Vorschlag stehen unten unverändert.

**Ursprünglicher Befund.** `EfCoreRepository` lädt nicht mehr das Aggregat,
sondern dessen State via `FindAsync(stateType, [id])`
([EfCoreRepository.cs:40-42](BuildingBlocks/src/BuildingBlocks.Infrastructure/Persistence/StateStored/EfCoreRepository.cs:40)).
`FindAsync` lädt weiterhin **keine Navigationseigenschaften**: ein `RecipeState` mit
`IReadOnlyCollection<IngredientState>` käme mit leerer Liste aus dem Repository. Die Klasse ist
weiterhin `sealed`, ein Service kann das Laden also nicht überschreiben.

Die Schwesterlücke auf der Schreibseite — `CurrentValues.SetValues` kopiert nur Skalare — ist als
[hacky.md Nr. 6](hacky.md) erfasst. Zusammen bedeutet das: **Aggregate mit Kindkollektionen werden
heute weder vollständig geladen noch vollständig gespeichert**, und beides schweigt.

## Lösungsvorschlag

Ein Aggregat wird immer vollständig geladen — Teilladen widerspricht der Konsistenzgrenze. Die
Include-Kette gehört deshalb zum Aggregat, nicht zum Aufrufer:

```csharp
public interface IAggregateGraph<TState>
{
    static abstract IQueryable<TState> Include(IQueryable<TState> query);
}

var query = context.Set(stateOwner.StateType).AsQueryable();
if (stateOwner is IAggregateGraphProvider provider) query = provider.Include(query);
var state = await query.FirstOrDefaultAsync(...);
```

Alternative ohne neue Abstraktion: `AutoInclude` im `DbContext` konfigurieren (EF Core unterstützt das
per Navigation) — weniger Code, dafür im Kontext statt am Aggregat verortet. Vor der ersten
Kindkollektion entscheiden; danach ist es ein Datenmigrationsthema.

---

# IMP-16, Kein Validierungs-Behavior, Mehrfachfehler nicht erzeugbar

`Result` trägt eine `IReadOnlyList<Failure>`, aber der einzige Produzent erzeugt genau einen Fehler
aus einer Exception. Feldweise Validierung ist damit nicht darstellbar.

**Teilweise gelöst.** Schritt 4 (explizite Behavior-Reihenfolge) ist umgesetzt, Slot `200` ist
reserviert
([BuildingBlocksOptions.cs:52-60](BuildingBlocks/src/BuildingBlocks.Infrastructure/DependencyInjection/BuildingBlocksOptions.cs:52)).
Ein `IRequestValidator`/`ValidationBehavior` existiert nicht.

## Lösungsvorschlag

```csharp
public interface IRequestValidator<in TRequest>
{
    ValueTask<IReadOnlyList<Failure>> ValidateAsync(TRequest request, CancellationToken ct);
}

public sealed class ValidationBehavior<TRequest, TResponse>(
    IEnumerable<IRequestValidator<TRequest>> validators) : IPipelineBehavior<TRequest, TResponse>
    where TResponse : Result
{
}
```

Hängt mit IMP-17 zusammen: ohne Zielfeld im `Failure` bleibt der gesammelte Fehlerbericht für ein
Frontend unbrauchbar. Beide gemeinsam umsetzen.

---

# IMP-17, `Failure` ohne Zielfeld und ohne fachliche Fehlercodes

`Failure` trägt `Code`, `Message`, `Category` — kein `Target`/`PropertyName`, keine Metadaten
([Failure.cs:40-50](BuildingBlocks/src/BuildingBlocks.Application/Results/Failure.cs:40)). Ein BFF kann daraus
keine `ProblemDetails` mit `errors`-Objekt bauen. Zudem setzt `ExceptionToResultBehavior` für **alle**
Domänenfehler denselben Code (`domain.validation` / `domain.business_rule`) — der Code ist technisch,
nicht fachlich, und damit für Internationalisierung und clientseitige Fallunterscheidung wertlos.

## Lösungsvorschlag

```csharp
public sealed record Failure(string Code, string Message, FailureCategory Category)
{
    public string? Target { get; init; }
    public IReadOnlyDictionary<string, object?>? Metadata { get; init; }
}
```

Für die Fehlercodes: `IBusinessRule`/`IDomainValidationRule` um ein `string Code { get; }` erweitern,
das die Regel selbst liefert (`recipe.name_required`). `ExceptionToResultBehavior` reicht ihn dann
durch, statt eine Konstante zu setzen — die Exception muss den Code dafür mitführen.

---

# IMP-18, `FailureCategory` fehlen Autorisierung und Unerwartet

Vier Werte: `Validation`, `BusinessRule`, `NotFound`, `Conflict`
([FailureCategory.cs](BuildingBlocks/src/BuildingBlocks.Application/Results/FailureCategory.cs)). Ein
Autorisierungsfehler (403) hat keine Kategorie und landet im gRPC-Adapter im
`_ => StatusCode.Unknown`-Arm; dasselbe gilt für einen bewusst zu einem `Result` degradierten
Infrastrukturfehler.

## Lösungsvorschlag

```csharp
public enum FailureCategory
{
    Validation,
    BusinessRule,
    NotFound,
    Conflict,
    Forbidden,
    Unexpected,
}
```

`Unauthorized` (401) bewusst **nicht** aufnehmen: Authentifizierung ist Sache des Hosts und erreicht
die Application-Schicht nie. Die Transportabbildung bleibt beim BFF/Host (ADR-0017); die
`switch`-Ausdrücke dort werden durch die neuen Werte compile-time-vollständigkeitsgeprüft.

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

# IMP-20, `DbContext` als DI-Schlüssel kollidiert mit dem Read/Write-Paar

```csharp
_services.TryAddScoped<DbContext>(static provider => provider.GetRequiredService<TContext>());
```

([BuildingBlocksOptions.cs:310](BuildingBlocks/src/BuildingBlocks.Infrastructure/DependencyInjection/BuildingBlocksOptions.cs:310))

Jeder Bounded Context hat laut ADR-0021 ein Write- **und** ein Read-Datenbankpaar. Der unqualifizierte
`DbContext`-Schlüssel gehört per Konvention dem Write-Kontext — sichtbar ist das nirgends. Registriert
ein Service versehentlich seinen Read-Kontext ebenfalls als `DbContext`, entscheidet die
Registrierungsreihenfolge, in welche Datenbank das Repository schreibt.

## Lösungsvorschlag

Den generischen Schlüssel vermeiden und das Repository am konkreten Kontexttyp aufhängen:

```csharp
public sealed class EfCoreRepository<TContext, TAggregate, TKey>(TContext context, ...)
    where TContext : DbContext
```

Da C# keine partiell geschlossenen offenen Generics registrieren kann, in der Praxis über eine
Factory-Registrierung oder einen schmalen Marker lösen — Letzteres ist die kleinere Änderung und macht
die Konvention immerhin im Typsystem sichtbar:

```csharp
public interface IWriteDbContext;
```

---

# IMP-21, `IRepository` koppelt an die konkrete Domain-Basisklasse

**Verifiziert gelöst.** Der Vertrag lautet heute `where TAggregate : class, IAggregateRoot<TKey>`
([IRepository.cs:20-22](BuildingBlocks/src/BuildingBlocks.Application/Persistence/IRepository.cs:20)) — ein
Interface statt der konkreten Klasse `AggregateRoot<TKey>`. Application-Code kann Aggregate damit
gegen die Abstraktion testen, ohne die Domain-Basisklasse zu erben.

## Lösungsvorschlag

Umgesetzt im Zuge von ADR-0026.

---

# IMP-22, `AssemblyQualifiedName` als Event-Typ-Token — **gelöst (2026-08-03)**

Siehe [TODO-03](todo.md) und [ADR-0030](docs/architecture/decisions/0030-persisted-names-and-aggregate-version.md).


Unverändert offen — identisch mit [hacky.md Nr. 1](hacky.md).

Die Outbox speichert `"…, MyAsm, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null"` und liest sie
mit `Type.GetType(..., throwOnError: true)` zurück
([DomainEventEnvelopeSerializer.cs:23,33](BuildingBlocks/src/BuildingBlocks.Infrastructure/Messaging/DomainEvents/DomainEventEnvelopeSerializer.cs:23)).
Version-Bump, Assembly-Umbenennung oder Typ-Umzug macht jede unzugestellte Nachricht unlesbar — und
das ist Crash-Recovery-Datenbestand. Zusätzlich ist `Type.GetType` auf persistierten Daten eine
unbegrenzte Typ-Aktivierungsfläche.

## Lösungsvorschlag

Stabiler logischer Eventname statt CLR-Identität, AQN höchstens als Fallback:

```csharp
[EventName("widget-created-v1")]
public sealed record WidgetCreated(...) : DomainEvent;
```

Macht Event-Versionierung überhaupt erst ausdrückbar. Gemeinsam mit IMP-23 und IMP-24 planen — alle
drei betreffen dasselbe Persistenz-Format.

---

# IMP-23, Marten-Stream-Key hängt am Klassennamen — **gelöst (2026-08-03)**

Siehe [TODO-04](todo.md) und [ADR-0030](docs/architecture/decisions/0030-persisted-names-and-aggregate-version.md).


Unverändert offen — identisch mit [hacky.md Nr. 2](hacky.md).

`$"{aggregateType.Name}/{keyValue}"`
([EntityKeyFormatter.cs:20](BuildingBlocks/src/BuildingBlocks.Infrastructure/Persistence/EntityKeyFormatter.cs:20)).
Ein Rename verwaist alle bestehenden Streams — und zwar **ohne Fehler**: `FetchStreamAsync` liefert
leer, der Handler meldet korrekt `NotFound`, ein anschließender Schreibvorgang legt einen neuen Stream
an und macht den alten endgültig unauffindbar.

## Lösungsvorschlag

```csharp
[StreamPrefix("gadget")]
public sealed class Gadget : EventSourcedAggregateRoot<GadgetId, GadgetState>;

private static string PrefixOf(Type aggregateType) =>
    aggregateType.GetCustomAttribute<StreamPrefixAttribute>()?.Prefix
    ?? throw new InvalidOperationException(
        $"'{aggregateType}' braucht ein [StreamPrefix]; der Klassenname ist kein Persistenz-Contract.");
```

Das Werfen ist Absicht: der Contract soll bewusst gesetzt werden, nicht aus dem Refactoring-Zufall
entstehen.

---

# IMP-24, `DomainEventEnvelope` trägt zu wenig Metadaten — **gelöst (2026-08-03)**

Siehe [TODO-02](todo.md) und [ADR-0030](docs/architecture/decisions/0030-persisted-names-and-aggregate-version.md).


```csharp
public sealed record DomainEventEnvelope(string EventTypeName, string Payload);
```

([DomainEventEnvelope.cs:17](BuildingBlocks/src/BuildingBlocks.Infrastructure/Messaging/DomainEvents/DomainEventEnvelope.cs:17))

Es fehlen `EventId`, `AggregateId`, `AggregateType`, `Version`, `OccurredAt`. Damit ist der übliche
Idempotenz-Ansatz („speichere pro Aggregat die zuletzt verarbeitete Version") von der Infrastruktur
nicht bedienbar — jeder Projektionshandler muss die Werte selbst typspezifisch aus dem Payload
auspacken. Die Samples zeigen das exemplarisch: `WidgetRenamedProjection` behilft sich mit einem
fachlichen `RenameCount` als Ordinalzahl, weil keine technische Version verfügbar ist
([WidgetProjections.cs:62](samples/StateStored/VitalSync.Sample.StateStored.Infrastructure/Read/WidgetProjections.cs:62)).

Die Anforderung „Projektionen müssen idempotent und pro Aggregat ordnungsbewusst sein" ist damit
gestellt, aber nicht unterstützt.

## Lösungsvorschlag

```csharp
public sealed record DomainEventEnvelope(
    string EventName,
    string Payload,
    Guid EventId,
    string AggregateType,
    string AggregateId,
    long Version,
    DateTimeOffset OccurredAt);
```

Die Unit-of-Work-Implementierungen kennen alle Werte zum Wrap-Zeitpunkt. Für den state-stored Pfad
braucht es eine fortlaufende Version am State — dieselbe, die die optimistische Nebenläufigkeit dort
ohnehin bräuchte. Löst zusammen mit IMP-22 und IMP-25 das gesamte Envelope-Thema; auch
[hacky.md Nr. 7](hacky.md) (Zeitstempel-Sentinel) verschwindet damit, weil `OccurredAt` in den
Envelope wandert.

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

Nach Aggregat-Id partitionieren — gleiche Garantie, parallel über verschiedene Aggregate. Setzt IMP-24
voraus, weil der Envelope die Aggregat-Identität heute nicht mitführt.

Vor der ersten Lastmessung nicht anfassen. Hier festgehalten, damit die Entscheidung bewusst fällt und
nicht als Default stehen bleibt.

---

# IMP-26, `DomainEventPublisher` koppelt Projektion und Integration-Event-Publikation

```csharp
await projectionRunner.RunAsync(domainEvent, cancellationToken);
foreach (var mapper in _mappers) { foreach (...) await sink.PublishAsync(...); }
```

([DomainEventPublisher.cs:32-40](BuildingBlocks/src/BuildingBlocks.Infrastructure/Messaging/DomainEvents/DomainEventPublisher.cs:32))

Zwei Belange in einer Methode, ohne Fehlerisolierung: wirft eine Projektion, wird kein einziges
Integration Event publiziert; wirft ein Mapper, laufen bei der Redelivery alle Projektionen erneut.
Bei at-least-once-Zustellung heißt das, dass ein Fehler auf der einen Seite die andere wiederholt
ausführt — was nur solange gutgeht, wie beide Seiten idempotent sind.

## Lösungsvorschlag

Die beiden Wege trennen, statt sie im selben `try` zu bündeln. Zwei Optionen:

```
A) Zwei Wolverine-Handler auf demselben Envelope — jeder mit eigener Retry-/DLQ-Bilanz.
   Sauber isoliert, kostet eine zweite Zustellung pro Event.

B) Ein Handler, aber getrennte Fehlerbehandlung mit ausdrücklicher Reihenfolge:
   erst Projektionen (in-context, schnell), dann Integration Events (extern).
   Ein Fehler im zweiten Schritt darf den ersten nicht rückgängig machen wollen.
```

Empfehlung: B. Voraussetzung ist die Idempotenz beider Seiten. A erst, wenn Projektionen und
Integration Events messbar unterschiedliche Fehlerraten haben.

---

# IMP-27, `FailureResults`-Reflection ist vermeidbar — **gelöst (2026-08-05)**

> Gelöst per [ADR-0015-Amendment](docs/architecture/decisions/0015-hand-rolled-cqrs-mediator.md) —
> der Dispatcher reicht die Failure-Factory im neuen `RequestPipeline<TResponse>` mit,
> `FailureResults` ist gelöscht. Siehe [todo.md](todo.md) TODO-12 und [hacky.md](hacky.md) Nr. 3.

Ursprünglicher Befund — identisch mit [hacky.md Nr. 3](hacky.md).

`responseType.GetMethod("Failure", [typeof(Failure)])` plus Expression-Compile, abgesichert durch eine
Runtime-`InvalidOperationException`
([FailureResults.cs:39-45](BuildingBlocks/src/BuildingBlocks.Infrastructure/Dispatching/FailureResults.cs:39)).

## Lösungsvorschlag

Static abstract interface member (C# 11) — der Compiler erledigt es, die Klasse entfällt komplett:

```csharp
public interface IFailureResult<out TSelf> { static abstract TSelf Failure(Failure failure); }

public class Result : IFailureResult<Result> { }
public sealed class Result<TResult> : Result, IFailureResult<Result<TResult>> { }

return TResponse.Failure(Failure.Validation(ValidationFailureCode, exception.Message));
```

Betrifft nur `ExceptionToResultBehavior` und `UnitOfWorkBehavior` (Constraint erweitern). Aus einem
Laufzeitfehler wird ein Compile-Fehler.

---

# IMP-28, Kein `IClock` im Container

**Verifiziert gelöst.** `AddBuildingBlocks` registriert `TimeProvider.System` und `IClock → SystemClock`
([ServiceCollectionExtensions.cs:91-92](BuildingBlocks/src/BuildingBlocks.Infrastructure/DependencyInjection/ServiceCollectionExtensions.cs:91)).
Beide Unit-of-Work-Implementierungen beziehen ihn per Konstruktor.

## Lösungsvorschlag

Umgesetzt.

---

# IMP-29, Unique-Constraint-Verletzungen werden nicht übersetzt

`UnitOfWorkBehavior` fängt `ConcurrencyException` und `DbUpdateConcurrencyException`
([UnitOfWorkBehavior.cs:58-65](BuildingBlocks/src/BuildingBlocks.Infrastructure/Dispatching/UnitOfWorkBehavior.cs:58)).
Eine `DbUpdateException` mit PostgreSQL-Unique-Violation (SQLSTATE `23505`) fällt durch und wird zur
unerwarteten Exception — obwohl „dieser Name existiert bereits" ein erwarteter fachlicher Fall ist.
Der Nutzer bekommt einen 500er statt eines 409ers, und die Error-Metrik zählt einen Systemfehler.

## Lösungsvorschlag

```csharp
catch (DbUpdateException exception) when (exception.InnerException is PostgresException
    { SqlState: PostgresErrorCodes.UniqueViolation } pg)
{
    return pipeline.Failed(
        Failure.Conflict("persistence.unique_violation", $"'{pg.ConstraintName}' verletzt."));
}
```

Der Constraint-Name gehört in die Meldung, sonst ist der Fehler im Log nicht zuzuordnen. Ein Mapping
von Constraint-Namen auf fachliche Codes (`ux_recipes_name` → `recipe.name_taken`) gehört in den
Service, nicht in die Building Blocks — hängt an IMP-17.

---

# IMP-30, Keine Tracing-Instrumentierung der CQRS-Pipeline

Verifiziert: **kein einziges `Activity`/`ActivitySource` im gesamten `BuildingBlocks/src`**.
`LoggingBehavior` misst die Dauer und loggt sie
([LoggingBehavior.cs:31](BuildingBlocks/src/BuildingBlocks.Infrastructure/Dispatching/LoggingBehavior.cs:31)),
erzeugt aber keinen Span. In einem Aspire-/OpenTelemetry-Setup fehlt damit genau die Ebene zwischen
HTTP/gRPC-Span und Datenbank-Span: Man sieht, dass ein Request 800 ms brauchte, aber nicht, welcher
Handler oder welche Projektion.

**Nachtrag (2026-08-03):** Die ServiceDefaults registrieren seit Commit `981c0c5` die Quellen
`Npgsql`, `Wolverine` und `Marten` (Tracing) sowie den Meter `Npgsql`
([AspireExtensions.cs](src/Aspire/VitalSync.ServiceDefaults/AspireExtensions.cs)) — Datenbank- und
Transport-Spans existieren damit. Die hier beschriebene Lücke, der Span der CQRS-Pipeline selbst,
bleibt offen.

## Lösungsvorschlag

```csharp
internal static class BuildingBlocksActivitySource
{
    public static readonly ActivitySource Instance = new("VitalSync.BuildingBlocks", "1.0.0");
}

using var activity = BuildingBlocksActivitySource.Instance.StartActivity($"Send {requestName}");
activity?.SetTag("vitalsync.request.type", requestName);
```

Analog in `ProjectionRunner` und `DomainEventPublisher`. Der Service-Default registriert die Quelle dann per
`AddSource("VitalSync.BuildingBlocks")`. Kleiner Aufwand, hoher Betriebsnutzen — sinnvollerweise
zusammen mit dem ersten produktiven Service.

---

# IMP-31, Read-Modelle im state-stored Pfad sind nicht wiederaufbaubar

Read-Modelle sind laut ADR-0022 „abgeleitet und wiederaufbaubar". Für event-sourced Kontexte stimmt das
(Marten-Stream als Quelle). Für state-stored Kontexte nicht: Die Write-Datenbank hält nur den aktuellen
Zustand, und die Domain Events existieren ausschließlich als Outbox-Zeilen, die nach erfolgreicher
Zustellung gelöscht werden. Ein Projektions-Bugfix ist damit nicht nachträglich auf Altdaten anwendbar.

## Lösungsvorschlag

Zwei Wege, bewusst zu wählen:

```
A) Domain-Event-Journal: die Envelopes zusätzlich in eine append-only Tabelle der Write-DB
   schreiben (dieselbe Transaktion). Kosten: Speicher + eine Tabelle. Nutzen: echter Replay,
   Audit-Trail, und die Vorstufe zu einer späteren ES-Migration des Kontexts.

B) Rebuild-aus-Zustand: pro Read-Modell eine Projektion, die aus dem aktuellen Write-Zustand
   neu aufbaut statt aus Events. Billiger, aber ein zweiter Codepfad pro Read-Modell und
   nicht für Modelle geeignet, die Historie aggregieren ("Anzahl Umbenennungen").
```

Empfehlung A, weil es die ADR-0022-Zusage tatsächlich einlöst und den in ADR-0025 versprochenen Wechsel
state-stored ↔ event-sourced vorbereitet. Braucht eine ADR.

---

# IMP-32, Keine Batch- oder Bulk-Fähigkeit

`ISender.Send` verarbeitet einen Request, `UnitOfWorkBehavior` committet danach. Es gibt keinen Weg,
mehrere Commands in einer Transaktion zu bündeln oder die Event-Publikation für einen Massenvorgang zu
unterdrücken. „Nährwertkatalog mit 500 Einträgen importieren" heißt heute: 500 Transaktionen, 500
Outbox-Runden, 500 Projektionsläufe.

## Lösungsvorschlag

Kein generisches Batch-API bauen — das untergräbt die Ein-Command-eine-Transaktion-Regel. Stattdessen
den Massenvorgang als **eigenen Command** modellieren, der die Schleife im Handler hält:

```csharp
public sealed record ImportFoodCatalog(IReadOnlyList<FoodEntry> Entries) : ICommand<ImportSummary>;
```

Das deckt den Regelfall ab. Erst wenn ein Import die Transaktionsgröße sprengt, braucht es echtes
Chunking — dann als bewusst nicht-atomarer Vorgang mit eigenem Fortschrittszustand (siehe IMP-33).

---

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

# IMP-35, Statische Caches über Container-Grenzen hinweg

Vier prozessglobale Caches: `RequestSender` (3×), `ProjectionRunner`, `EntityKeyFormatter`,
`EntityKeyModelBuilderExtensions`. (`FailureResults` war der fünfte und ist mit TODO-12 entfallen.)

**Status: wird nicht gelöst.** Alle sind ausschließlich `Type`-gekeyed und speichern
unveränderliche, rein typabgeleitete Werte (Dispatcher, kompilierte Accessors, Konverter). Für einen
gegebenen Typ ist das Ergebnis über die Prozesslebensdauer konstant, unabhängig vom Container. Die
einzige reale Fehlwirkung war der unvollständige Schlüssel in `RequestSender` — das war IMP-06 und ist behoben.
Testisolation ist nicht betroffen, weil kein Cache Container- oder Scope-Zustand hält.

## Lösungsvorschlag

Keine Änderung. Die Begründung gehört allerdings nach `docs/architecture/building-blocks.md`, damit
niemand sie später „aufräumt" und die Caches pro Container instanziiert — das wäre eine
Verschlechterung ohne Gegenwert. Das ist derselbe Punkt wie IMP-46 Schritt 2.

---

# IMP-36, `RuleChecker` schluckt `null` still

Unverändert offen — identisch mit [hacky.md Nr. 10](hacky.md).

`rule?.IsBroken() == true` und `foreach (var rule in rules ?? [])`
([RuleChecker.cs:18-63](BuildingBlocks/src/BuildingBlocks.Domain/Rules/RuleChecker.cs:18)). Eine Factory, die
versehentlich `null` liefert, bedeutet „Regel bestanden" — die Validierung schweigt genau im Fehlerfall.

## Lösungsvorschlag

```csharp
public static void Check(IBusinessRule rule)
{
    ArgumentNullException.ThrowIfNull(rule);

    if (rule.IsBroken())
    {
        throw new BusinessRuleViolationException(rule.Message);
    }
}
```

Analog für die `params`-Überladung und für `IDomainValidationRule`. Bestehende Tests, die die
Null-Toleranz festschreiben, mitziehen.

---

# IMP-37, Async-Suffix ist inkonsistent

Verifiziert: `Handle` ohne Suffix in `ICommandHandler`, `IQueryHandler`, `IProjectionHandler`,
`IPipelineBehavior` — dagegen `GetByIdAsync`, `AddAsync`, `CommitAsync`, `PublishAsync` mit Suffix.
Alle geben `Task` zurück.

## Lösungsvorschlag

Eine Regel wählen und durchziehen. Empfehlung: **Suffix überall**, weil es die .NET-Konvention ist und
die Ports (`IRepository`, `IUnitOfWork`, `IDomainEventPublisher`) ihr bereits folgen — die Handler sind
die Ausnahme, nicht die Regel:

```csharp
Task<Result> HandleAsync(TCommand command, CancellationToken cancellationToken);
```

Breaking Change über alle Handler hinweg, aber rein mechanisch (Rename-Refactoring) und ohne
Verhaltensänderung. Jetzt billig, mit dem ersten Produktions-Service teuer. Die Entscheidung gehört in
die `.editorconfig`-Konventionen bzw. eine kurze ADR.

---

# IMP-38, Sichtbarkeits-Disziplin ist uneinheitlich

**Gelöst (2026-08-06).** Das ursprünglich beanstandete Transport-Trio existiert nicht mehr; die
Messaging-Typen sind heute konsequent `internal` (`DomainEventPublisher`, `WolverineIntegrationEventSink`,
`NullIntegrationEventSink`, `BuildingBlocksWolverineExtension`, `WolverineOptionsExtensions`, beide
Startup-Validatoren, `DomainEventStamper`, `PipelineBehaviorRegistry`, `EntityKeyFormatter`).

**Auch der Rest ist erledigt:** `ProjectionRunner`, `EfCoreUnitOfWork`, `MartenUnitOfWork`,
`EfCoreRepository`, `MartenEventSourcedRepository` und beide Tracker sind allesamt `internal sealed`.

Entscheidend ist aber nicht die Liste, sondern dass die Regel jetzt getestet ist:
`PublicSurfaceTests` existiert in allen drei Testprojekten. In `BuildingBlocks.Infrastructure.Tests`
pinnt er die Oberfläche auf **exakt** vier beabsichtigte Typen plus sieben, die nur deshalb `public`
sind, weil Wolverine C# in eine andere Assembly generiert und sie dort benennt
(`DomainEventEnvelope`, `DomainEventEnvelopeHandler`, `DomainEventEnvelopeSerializer`,
`DomainEventTypeRegistry`, `IIntegrationEventSinkFactory`, `IntegrationEventSourceContext`,
`OwnContextIntegrationEventFilter`); ein weiterer Test verbietet Implementierungs-Namespaces an der
Oberfläche überhaupt. Ein neuer `public` Typ wird damit rot, bis ihn jemand mit Begründung einträgt.

## Lösungsvorschlag

Regel festschreiben und anwenden: **`public` ist nur, was ein Service-Host tatsächlich benennt.**

```
internal: ProjectionRunner, EfCoreUnitOfWork, MartenUnitOfWork, EfCoreRepository,
          MartenEventSourcedRepository, EfCoreAggregateTracker, MartenAggregateTracker

public:   AddBuildingBlocks (beide Überladungen), BuildingBlocksOptions,
          EntityKeyValueConverter/-ModelBuilderExtensions (im DbContext des Service benutzt),
          DomainEventEnvelope + Handler (Wolverine muss sie sehen),
          die Behaviors (als Vorlage für eigene)
```

`InternalsVisibleTo` für das Testprojekt setzen, damit die Umstellung keine Tests kostet.

---

# IMP-39, `Result`-API: Namenskollision und implizite Konvertierungen

> **Punkt 1 gelöst (2026-08-05)** — die Factory heißt `Failed(...)`
> ([ADR-0017-Amendment](docs/architecture/decisions/0017-application-error-handling-and-result.md)),
> und `FailureResults` ist mit ihr entfallen
> ([ADR-0015-Amendment](docs/architecture/decisions/0015-hand-rolled-cqrs-mediator.md)).
> Das `static new` in `ResultOfT` bleibt bestehen, ist jetzt aber folgenlos: niemand muss es mehr
> per Reflection auflösen. Punkte 2 und 3 sind unverändert offen.

Drei Punkte:

1. **Namenskollision:** `IsFailure` (Property), `Failures` (Property) und `Failure(…)` (statische
   Methode) stehen nebeneinander, dazu heißt der Typ des Elements ebenfalls `Failure`. In `ResultOfT`
   erzwingt das `public static new Result<TResult> Failure(...)` — genau das Konstrukt, das
   `FailureResults` per Reflection wieder auseinanderfummeln muss (IMP-27).
2. **Zwei implizite Konvertierungen** auf `Result<TResult>` (aus `TResult` und aus `Failure`) — für
   `TResult = Failure` wären sie mehrdeutig; heute nur theoretisch, aber unbewacht.
3. **`Value` wirft** bei einem fehlgeschlagenen Result, statt den Fehler im Typsystem sichtbar zu
   machen.

[Result.cs](BuildingBlocks/src/BuildingBlocks.Application/Results/Result.cs),
[ResultOfT.cs](BuildingBlocks/src/BuildingBlocks.Application/Results/ResultOfT.cs)

## Lösungsvorschlag

Nur Punkt 1 lohnt eine Änderung, und er löst zugleich IMP-27:

```csharp
public static Result Failed(Failure failure);
public static Result<T> Failed<T>(Failure failure);
```

Damit kollidiert nichts mehr, das `new`-Hiding entfällt, und die statische Factory lässt sich sauber
als `static abstract` im Interface deklarieren. Punkt 2 mit einem Test absichern statt umbauen;
Punkt 3 durch `Match` (IMP-34) entschärfen, ohne `Value` zu entfernen.

---

# IMP-40, `State` ist `public` und bricht die Kapselung

**Verifiziert gelöst.** `protected TState State { get; private set; }`
([AggregateRoot.cs:47](BuildingBlocks/src/BuildingBlocks.Domain/Aggregates/AggregateRoot.cs:47)). Der Innenzustand
ist von außen nicht mehr lesbar; die Persistenz erreicht ihn ausschließlich über das explizit
implementierte `IStateOwner`.

## Lösungsvorschlag

Umgesetzt im Zuge von ADR-0025.

---

# IMP-41, `DomainEvent` als `record` mit garantiert ungleicher Wertgleichheit

**Gelöst (2026-08-03)** via TODO-13 /
[ADR-0029](docs/architecture/decisions/0029-event-identity-placement.md): die empfohlene
Alternative wurde umgesetzt — `EventId`/`OccurredAt` liegen im Envelope, `DomainEvent` ist ein
reiner Wert-Record mit korrekter generierter Gleichheit; IMP-24 (Teil) und hacky-7 sind
miterledigt.

```csharp
protected DomainEvent() => EventId = Guid.NewGuid();
```

([DomainEvent.cs:21-24](BuildingBlocks/src/BuildingBlocks.Domain/Events/DomainEvent.cs:21))

`record` verspricht Wertgleichheit, die generierte `Equals` bezieht `EventId` mit ein, und die ist pro
Instanz neu. Zwei inhaltlich identische Events sind damit **nie** gleich. Das ist genau die
Eigenschaft, die man beim Testen erwartet (`Assert.Equal(expectedEvent, actual)`) — und sie
funktioniert nicht, was sich als kryptischer Assert-Fehler äußert statt als klare Aussage.

## Lösungsvorschlag

Identität und Inhalt trennen, statt sie in einer `Equals` zu vermischen — das kostet allerdings
Boilerplate pro Event. Pragmatischere Alternative: `EventId`/`OccurredAt` gar nicht am Event führen,
sondern im Envelope (siehe IMP-24). Dann bleibt `DomainEvent` ein reiner Wert-Record, die generierte
Gleichheit ist korrekt, und der Punkt löst sich ohne Sonderlogik auf.

**Empfohlen**, weil es drei Befunde gleichzeitig erledigt: IMP-24, IMP-41 und
[hacky.md Nr. 7](hacky.md).

---

# IMP-42, `IRepository`-API ist asymmetrisch und irreführend benannt

**Verifiziert gelöst.** Der Vertrag hat heute exakt zwei Methoden, `GetByIdAsync` und `AddAsync`
([IRepository.cs:34,46](BuildingBlocks/src/BuildingBlocks.Application/Persistence/IRepository.cs:34)). Das
beanstandete synchrone `Remove` existiert nicht mehr: Entfernen ist per ADR-0026 eine
Soft-Delete-Zustandsänderung in der Domäne, Änderungen fließen über die Unit of Work.

## Lösungsvorschlag

Umgesetzt via ADR-0026.

---

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

# IMP-44, Uneinheitliche Projektstruktur

Verifiziert: `BuildingBlocks.Domain` (20 Dateien) und `BuildingBlocks.Application` (18 Dateien) sind
flach, `BuildingBlocks.Infrastructure` hat fünf Ordner. In `Application` stehen CQRS-Verträge,
Ergebnismodell, Persistenz-Ports und Event-Ports unsortiert nebeneinander.

## Lösungsvorschlag

Die Ordnerstruktur der Infrastructure auf die anderen beiden übertragen — sie bildet dort bereits die
Konzeptgruppen ab, die auch hier existieren:

```
BuildingBlocks.Application/
├── Cqrs/          ICommand, IQuery, *Handler, ISender, IPipelineBehavior, RequestPipelineContinuation
├── Results/       Result, ResultOfT, Failure, FailureCategory
├── Persistence/   IRepository, IUnitOfWork
└── Events/        IDomainEventPublisher, IProjectionHandler, IIntegrationEvent*, IIntegrationEventSink

BuildingBlocks.Domain/
├── Model/         EntityBase, Entity, AggregateRoot, EventSourcedAggregateRoot, IState, IStateOwner
├── Identity/      IEntityKey
├── Events/        DomainEvent, IDomainEvent, IHasDomainEvents, IDomainEventOwner
└── Rules/         IBusinessRule, IDomainValidationRule, RuleChecker, *Exception
```

Namespaces bewusst **nicht** mitziehen (`BuildingBlocks.Application` bleibt flach), sonst wird aus einer
Aufräumaktion ein Breaking Change für jeden Service.

## Blockiert: der Vorschlag ist in diesem Repository nicht baubar (2026-08-05)

`.editorconfig` (Wurzel, Zeile 86) setzt `dotnet_style_namespace_match_folder = true:error`, und
`Directory.Build.props` schaltet `EnforceCodeStyleInBuild` ein. Ein Ordner ohne passenden Namespace ist
damit **IDE0130 als Fehler**. „Ordner ja, Namespaces nein" gibt es hier also nicht — siehe TODO-42 für
die drei verbleibenden Optionen und die Kostenabwägung (in `Infrastructure` ist fast alles `internal`
und die Namespaces sind unsichtbar; in `Domain`/`Application` ist alles public, und aus einem `using`
würden drei bis vier in jeder Aggregat-Datei jedes Services).

## Gelöst: Ordner samt Namespaces (2026-08-06)

Umgesetzt wie in TODO-42 beschrieben: `Domain` bekam `Aggregates/`, `Entities/`, `Events/`,
`Naming/`, `Rules/` (`IClock` bleibt in der Wurzel), `Application` bekam `Cqrs/`, `Results/`,
`Persistence/`, `DomainEvents/`, `IntegrationEvents/`. Die Namespaces ziehen mit; die
zusätzlichen usings trägt ein Service **einmal** als `<Using Include="…" />` in sein `.csproj`,
statt sie in jeder Datei zu wiederholen. Neue `PublicSurfaceTests` in beiden Testprojekten nageln
die exportierte Typliste — und damit Namespace-Layout und Sichtbarkeit — fest.

---

# IMP-45, `SenderContractTests` testet NSubstitute statt Produktionscode

Verifiziert unverändert: Die Tests bauen ein `Substitute.For<ISender>()`, konfigurieren dessen
Rückgabewert und prüfen dann, dass dieser Rückgabewert zurückkommt
([SenderContractTests.cs:14-24](BuildingBlocks/tests/BuildingBlocks.Application.Tests/SenderContractTests.cs:14)).
Getestet wird damit NSubstitute, nicht der eigene Code. Der Wert ist nicht null — die Tests belegen,
dass die Signaturen kompilieren und die generischen Constraints zusammenpassen — aber der Name
verspricht mehr, als sie leisten.

## Lösungsvorschlag

Die Datei behalten, aber ehrlich benennen und ihren Zweck dokumentieren:

```csharp
public sealed class SenderSignatureTests
```

Das echte Verhalten ist inzwischen in `BuildingBlocks.Infrastructure.Tests` abgedeckt (IMP-09), der
Test hat seine Lücke also nicht mehr zu füllen — nur seinen Namen zu korrigieren.

---

# IMP-46, Behaviors nutzen Service Locator statt optionaler Abhängigkeiten

**Teilweise gelöst.** Schritt 1 ist umgesetzt: `UnitOfWorkBehavior` nutzt Konstruktorinjektion mit
`IUnitOfWork? = null` und ist ohne Container instanziierbar.

**Offen:** `RequestSender` und `ProjectionRunner` nehmen weiterhin einen `IServiceProvider`
([RequestSender.cs:19](BuildingBlocks/src/BuildingBlocks.Infrastructure/Dispatching/RequestSender.cs:19),
[ProjectionRunner.cs:20](BuildingBlocks/src/BuildingBlocks.Infrastructure/Messaging/DomainEvents/ProjectionRunner.cs:20)).
Das ist dort **richtig** — beide lösen zur Laufzeit typabhängig auf, was per Konstruktor nicht geht —
aber nirgends begründet. Ohne diese Begründung ist der Service-Locator-Zugriff ein Muster, das kopiert
wird.

## Lösungsvorschlag

Die Regel in `docs/architecture/building-blocks.md` festhalten, gemeinsam mit der Begründung aus
IMP-35 (statische Caches) — beides sind bewusste Ausnahmen, die als solche dokumentiert gehören:

> Der `IServiceProvider` ist in `RequestSender` und `ProjectionRunner` bewusst gewählt und keine
> Nachlässigkeit: der aufzulösende Handler-Typ ergibt sich erst aus dem Laufzeittyp des Requests,
> lässt sich also nicht per Konstruktor injizieren. Für Abhängigkeiten, die zur Kompositionszeit
> feststehen, gilt weiterhin Konstruktorinjektion — siehe `UnitOfWorkBehavior`.

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

---

# IMP-48, Uneinheitliche Benennung der Wolverine-Extensions

**Teilweise überholt.** Das ursprünglich beanstandete `ApplyBuildingBlockEfCoreOutbox` existiert nicht
mehr. Verblieben sind drei `internal` Methoden — `ApplyBuildingBlockDomainEventRouting`,
`ApplyBuildingBlockMessagingDefaults`, `ApplyBuildingBlockSubscription`
([WolverineOptionsExtensions.cs](BuildingBlocks/src/BuildingBlocks.Infrastructure/DependencyInjection/Wiring/WolverineOptionsExtensions.cs)).

**Gelöst (2026-08-03).** Die `public` Gegenspielerin `UseBuildingBlocksEfCorePersistence` ist mit
`WolverineHostExtensions` gelöscht (siehe [todo.md](todo.md), TODO-06); übrig sind die drei internen
Methoden mit einheitlichem `BuildingBlock*`-Präfix, der Singular/Plural-Mix existiert nicht mehr.

## Lösungsvorschlag

Auf den Plural vereinheitlichen, passend zum Assembly- und Paketnamen:

```csharp
internal static WolverineOptions ApplyBuildingBlocksDomainEventRouting(this WolverineOptions options);
internal static WolverineOptions ApplyBuildingBlocksMessagingDefaults(this WolverineOptions options, Uri uri);
internal static WolverineOptions ApplyBuildingBlocksSubscription(this WolverineOptions options, ...);
```

Drei `internal` Renames, kein Breaking Change nach außen. Mitnehmen, wenn ohnehin jemand in dieser
Datei arbeitet.
