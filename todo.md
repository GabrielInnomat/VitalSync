# TODO — konsolidierte Arbeitsliste

Zusammenführung der drei Befunddokumente, Stand 2026-08-02:

| Quelle                                        | Einträge | davon offen |
| --------------------------------------------- | -------- | ----------- |
| [hacky.md](hacky.md)                          | 13       | 13          |
| [Improvements.md](Improvements.md)            | 48       | 34          |
| [WalkingSkeleton.md](WalkingSkeleton.md) §9   | 17       | 17          |
| **Summe roh**                                 | **78**   | **64**      |
| **nach Zusammenführung von Überschneidungen** |          | **43**      |
| Nachtrag AppHost `e44ae9b` (TODO-45, TODO-46) | 2        | 2           |
| Nachtrag WS-08 (TODO-48)                      | 1        | 1           |
| Nachtrag ADR-0034-Folge (TODO-49)             | 1        | 1           |
| **Summe geführt**                             |          | **47**      |

Die 14 in `Improvements.md` als gelöst verifizierten Punkte wurden nicht übernomen.

**Leitfrage der Priorisierung:** Die Basis soll stabil sein, bevor die echten Services kommen.
Je stiller ein Fehler im Produktivbetrieb wirkt und je gebastelter die Stelle, desto höher die
Priorität.

| Prio   | Bedeutung                                                                                |
| ------ | ---------------------------------------------------------------------------------------- |
| **P1** | Datenverlust oder stiller Fehlschlag im Produktivbetrieb. Vor dem ersten echten Service. |
| **P2** | Spürbar störend oder deutlich gebastelt. Vor dem zweiten Service.                        |
| **P3** | Sinnvoll, aber ohne akuten Druck. Beim nächsten Anfassen der Stelle.                     |
| **P4** | Kosmetik und Konsistenz. Aufräum-Commit.                                                 |

**Status `Konflikt`** heißt: die Quelldokumente empfehlen gegenläufige Lösungen, oder ein
Dokument erklärt für gelöst, was ein anderes als Fehler führt. Diese Punkte brauchen zuerst
eine Entscheidung, keinen Code.

## Übersicht

| Nr.     | Titel                                                          | Prio   | Status            | Quellen                               |
| ------- | -------------------------------------------------------------- | ------ | ----------------- | ------------------------------------- |
| TODO-01 | Aggregate mit Kindkollektionen brechen still                   | **P1** | gelöst            | hacky-6, IMP-15                       |
| TODO-02 | Aggregat-Version und Envelope-Metadaten                        | **P1** | gelöst            | WS-01, WS-03, IMP-24, IMP-41, hacky-7 |
| TODO-03 | `AssemblyQualifiedName` als Persistenz-Contract                | **P1** | gelöst            | hacky-1, IMP-22                       |
| TODO-04 | Stream-Key hängt am CLR-Klassennamen                           | **P1** | gelöst            | hacky-2, IMP-23                       |
| TODO-05 | Kein `Id.IsEmpty`-Guard in `AddAsync`                          | **P1** | gelöst            | hacky-5                               |
| TODO-06 | Connection String zweimal, ohne Abgleich                       | **P1** | gelöst            | hacky-8, IMP-13                       |
| TODO-07 | Integration Events sind nicht persistent                       | **P1** | gelöst            | WS-08                                 |
| TODO-08 | Topic-Validierung und Kontextkennung                           | **P1** | gelöst            | WS-05, WS-13, WS-14                   |
| TODO-09 | Keine CI-Pipeline                                              | **P1** | gelöst            | WS-16                                 |
| TODO-10 | Rehydrierung: `new()` oder `Activator`?                        | **P1** | gelöst            | hacky-5, IMP-14, WS-02                |
| TODO-11 | Optionalität von `IUnitOfWork`                                 | **P2** | gelöst            | IMP-07, hacky-11, IMP-46              |
| TODO-12 | Name der `Result`-Fehlerfactory                                | **P2** | gelöst            | hacky-3, IMP-27, IMP-39               |
| TODO-13 | Wo lebt die Event-Identität?                                   | **P2** | gelöst            | IMP-41, IMP-11                        |
| TODO-14 | Idempotenz-Bookkeeping über die Kontextgrenze                  | **P2** | teilweise         | IMP-11, WS-12                         |
| TODO-15 | Mehrfachfehler und Feldvalidierung end-to-end                  | **P2** | offen             | IMP-16, IMP-17, hacky-12              |
| TODO-16 | `FailureCategory` fehlt Autorisierung                          | **P2** | gelöst            | IMP-18                                |
| TODO-17 | `RuleChecker` schluckt `null`                                  | **P2** | gelöst            | hacky-10, IMP-36                      |
| TODO-18 | `AddBuildingBlocks` ist nicht idempotent                       | **P2** | gelöst            | hacky-9                               |
| TODO-19 | `ApplyEntityKeyConversions` scannt und mappt zu viel           | **P2** | teilweise         | hacky-4, WS-15                        |
| TODO-20 | Global sequentielle Domain-Event-Queue                         | **P2** | offen             | hacky-13, IMP-25                      |
| TODO-21 | Read-Modelle im state-stored Pfad nicht wiederaufbaubar        | **P2** | offen             | IMP-31                                |
| TODO-22 | Unique-Constraint-Verletzungen werden nicht übersetzt          | **P2** | gelöst            | IMP-29                                |
| TODO-23 | Keine Tracing-Instrumentierung der CQRS-Pipeline               | **P2** | offen             | IMP-30                                |
| TODO-24 | `DbContext` als DI-Schlüssel                                   | **P2** | gelöst            | IMP-20                                |
| TODO-25 | Marten-Nebenläufigkeit verdrahtet, aber unbelegt               | **P2** | gelöst            | WS-10                                 |
| TODO-26 | Typisierte Schlüssel serialisieren `IsEmpty` in den Eventstrom | **P2** | gelöst            | WS-09                                 |
| TODO-27 | Schema-Erzeugung zur Laufzeit in Produktion                    | **P2** | offen             | WS-11                                 |
| TODO-28 | Restliche Messaging-Guard-Rails                                | **P2** | gelöst            | IMP-13, WS-06                         |
| TODO-29 | `DomainEventPublisher` koppelt Projektion und Integration-Publikation     | **P3** | offen             | IMP-26                                |
| TODO-30 | `IIntegrationEventMapper` ist untypisiert                      | **P3** | offen             | IMP-12                                |
| TODO-31 | `Result` hat keine Kombinatoren                                | **P3** | offen             | IMP-34                                |
| TODO-32 | Async-Suffix ist inkonsistent                                  | **P3** | gelöst            | IMP-37                                |
| TODO-33 | Ein Assembly für alle Persistenz-Pakete                        | **P3** | offen             | IMP-19                                |
| TODO-34 | Keine zentrale Paketverwaltung                                 | **P3** | offen             | IMP-47                                |
| TODO-35 | `EntityFrameworkCore.Design` verträgt kein `PrivateAssets`     | **P3** | offen             | WS-04                                 |
| TODO-36 | Der gRPC-Vertrag liegt noch beim Service                       | **P3** | offen             | WS-07                                 |
| TODO-37 | Zeitbasierte Assertionen in Tests                              | **P3** | gelöst            | WS-17                                 |
| TODO-38 | Keine Batch- oder Bulk-Fähigkeit                               | **P3** | offen             | IMP-32                                |
| TODO-39 | Keine Saga- oder Process-Manager-Abstraktion                   | **P3** | offen             | IMP-33                                |
| TODO-40 | Sichtbarkeits-Disziplin ist uneinheitlich                      | **P4** | gelöst            | IMP-38                                |
| TODO-41 | Wirkungslose Varianz-Modifikatoren                             | **P4** | offen             | IMP-43                                |
| TODO-42 | Uneinheitliche Projektstruktur                                 | **P4** | gelöst            | IMP-44                                |
| TODO-43 | Irreführende Test- und Methodennamen                           | **P4** | gelöst            | IMP-45, IMP-48                        |
| TODO-44 | Bewusste Ausnahmen dokumentieren                               | **P4** | wird nicht gelöst | IMP-35, IMP-46                        |
| TODO-45 | Api-Readiness prüft nicht mehr existierende Connection-Namen   | **P1** | gelöst            | AppHost `e44ae9b`                     |
| TODO-46 | Die MigrationService-Worker sind leere Hüllen                  | **P2** | offen             | AppHost `e44ae9b`                     |
| TODO-47 | Kind-Entitäten haben kein Verhalten                            | **P2** | gelöst            | ADR-0031 Folgearbeit                  |
| TODO-48 | Publisher Confirms sind unbelegt                                | **P2** | gelöst            | WS-08 Nachtrag                        |
| TODO-49 | Feldnamen in Events sind abgeleitet, ein Rename zerstört still | **P1** | gelöst            | ADR-0034-Folge                        |

---

# TODO-01, Aggregate mit Kindkollektionen brechen still

**P1 · gelöst · hacky-6 + IMP-15**

## Gelöst — Kinder sind Owned Types, und der Commit kopiert den Graphen (ADR-0031, 2026-08-04)

Vor der Umsetzung wurde gemessen statt vermutet, und **eine der beiden Hälften stimmte nicht**:

- **Lesen war nie kaputt.** `FindAsync(stateType, [id])` lädt Owned Dependents sehr wohl mit
  ihrem Owner — auch in der nicht-generischen Überladung. Kein `Include`, kein `AutoInclude`,
  kein rekursives `LoadAsync`. Der geplante Ladepfad-Fix und die `IAggregateGraph`-Abstraktion
  aus IMP-15 entfielen ersatzlos.
- **Schreiben war kaputt, wie beschrieben.** `CurrentValues.SetValues` kopiert nur Skalare, das
  `UPDATE` enthielt die Kinder gar nicht: hinzugefügte fehlten, entfernte blieben, geänderte
  wurden verworfen. Ohne Fehler, ohne Log.

Der Fix ist ein schlüsselbasierter Abgleich plus drei Wächter:

- `EfCoreUnitOfWork` ruft `AggregateStateGraph.Reconcile`. Das gleicht Skalare **und** den
  **Owned-Graphen** gegen den getrackten Eintrag ab, über den **Schlüssel** und in **jeder
  Tiefe**: ein noch vorhandenes Kind bekommt `CurrentValues.SetValues` und wird weiterverfolgt, ein
  neues wandert in die getrackte Kollektion, ein verschwundenes heraus. EF Core macht daraus
  `UPDATE` (behalten), `DELETE` (entfernt), `INSERT` (neu), bei **stabiler Zeilenidentität**.
  Das blosse Zuweisen der Kollektion an die Navigation — der erste Wurf — trägt nur eine Ebene weit:
  die Enkel kollidieren mit den bereits getrackten unter demselben Schlüssel, EF Core wirft. Eine
  `ToJson()`-Kollektion bleibt bewusst beim Zuweisen. Der im Vorschlag angedachte generische
  Graph-Diff (150-250 Zeilen Reflection über zusammengesetzte Schlüssel, Shadow-FKs, Zyklen, Orphan
  Removal) wurde trotzdem nicht gebaut: der Abgleich läuft über EF-Core-Metadaten, nur über Owned
  Types, und die erzwungenen Konventionen machen genau jene Sonderfälle unerreichbar.
- `AggregateStateModelCheck` lehnt beim **Hoststart** jede Navigation eines States auf
  einen **unabhängigen** Entity-Typ ab und nennt State und Navigation. Ein Modell, das Daten
  verlieren würde, läuft gar nicht erst an.
- Derselbe Validator lehnt eine Owned-Kollektion ohne deklarierten, nicht-schattigen Schlüssel ab —
  ohne ihn hat der Commit nichts, woran er ein ersetztes Kind wiedererkennt.
- Eine read-only, fixed-size oder `null` gesetzte Kindkollektion fliegt sofort mit
  `NotSupportedException` auf, in jeder Tiefe des Graphen. Das ist nicht theoretisch: ein Collection
  Expression an `IReadOnlyCollection<T>` kompiliert zu `Array.Empty<T>()` bzw. zu einem
  compilergenerierten read-only Array — nie zu einer `List<T>`, und EF Core fügt Dependents über die
  Kollektionsinstanz selbst ein und entfernt sie darüber.

Zwei Autorenregeln, die aus C# folgen und dokumentiert statt erzwungen sind: die Kollektion ist
eine `{ get; init; }`-Property (als positional Record-Parameter findet EF Core „no suitable
constructor"), und sie wird mit `ToList()` gebaut.
Der ursprünglich vorgeschlagene Guard („ehrlich abriegeln") war nur der erste Schritt, nicht das
Ziel — geliefert ist der eigentliche Fix. ADR-0025/0026 blieben unangetastet; die Entscheidung
steht in der eigenständigen
[ADR-0031](docs/architecture/decisions/0031-aggregate-child-collections-as-owned-types.md).

**Belegt durch:**

- `EfCoreChildCollectionTests` (Testcontainers/PostgreSQL, 12 Tests): Anlegen mit Kind, Neuladen,
  Hinzufügen, Ändern bei gleichbleibender Zeile, Entfernen, Version-Fortschritt bei reiner
  Kindänderung, Nebenläufigkeitskonflikt, Ablehnung eines Modells mit freier Navigation beim
  Hoststart, Ablehnung read-only und `null` gesetzter Kollektionen — und, seit dem Nachtrag vom
  2026-08-04, ein **zweistufiger** Owned-Graph (`Cart → CartLine → CartTag`) mit Einfügen, Ändern
  und Löschen auf der Enkelebene, ein `OwnsOne`-Kind inklusive Löschen durch `null`, eine
  `ToJson()`-Kollektion und die Ablehnung einer read-only Kollektion **innerhalb** eines Kindes.
- **Negativkontrolle:** ohne den Navigations-Abgleich fallen 3 dieser Tests, mit ihm keiner. Der
  zweistufige Fall lieferte seine eigene Negativkontrolle: gegen die erste Fassung, die die
  Kollektion nur zuwies, schlug er mit EFs `cannot be tracked because another instance with the same
  key value is already being tracked` fehl.
- Das Sample ist nicht mehr flach: `Widget` hat `Parts` (`OwnsMany` → Tabelle `widget_parts`,
  eigener typisierter Schlüssel), mit Domänen-, Modell- und Projektionstests sowie zwei
  Smoke-Tests über gRPC gegen echtes Postgres/RabbitMQ — hinzufügen, Menge ändern, entfernen,
  und ein doppeltes Entfernen wird als `FailedPrecondition` abgelehnt.

Der ursprüngliche Befund steht unverändert unten.

## Ursprünglicher Befund

Die beiden Quellbefunde sind die zwei Hälften desselben Problems und gehören zusammen:

- **Lesen:** `EfCoreRepository` holt den State per `FindAsync(stateType, [id])`
  ([EfCoreRepository.cs:40](BuildingBlocks/src/BuildingBlocks.Infrastructure/Persistence/StateStored/EfCoreRepository.cs:40)) —
  das lädt keine Navigationen.
- **Schreiben:** `EfCoreUnitOfWork` kopiert per `CurrentValues.SetValues`
  ([EfCoreUnitOfWork.cs:47](BuildingBlocks/src/BuildingBlocks.Infrastructure/Persistence/StateStored/EfCoreUnitOfWork.cs:47)) —
  das kopiert nur Skalare.

Ein `RecipeState` mit `IReadOnlyCollection<IngredientState>` käme also mit leerer Zutatenliste
aus dem Repository, und Änderungen daran würden nicht geschrieben. Beides ohne Fehler, ohne Log.
Die Samples sind flach, also fängt es kein Test — es schlägt beim **ersten echten Aggregat** zu.
ADR-0025/0026 sagen „State mappt als gewöhnlicher Entity-Typ"; diese Zusage hält heute nur für
flache States.

## Lösungsvorschlag

Sofort und billig — ehrlich abriegeln statt still falsch zu arbeiten:

```csharp
var entry = outbox.DbContext.Entry(tracked.PersistedState);

if (entry.Navigations.Any())
{
    throw new NotSupportedException(
        $"Der State '{entry.Metadata.ClrType}' hat Navigationen. SetValues kopiert nur Skalare.");
}
```

Danach der eigentliche Fix, beide Hälften in einem Zug: die Include-Kette gehört ans Aggregat
(oder per `AutoInclude` an den Kontext), und der Commit muss den State-**Graphen** ersetzen statt
ihn zu patchen — alte Instanz detachen, neuen State als geänderten Graphen attachen.

Verdient ein Amendment zu ADR-0025/0026, weil es die Kernzusage der State-Mapping-Entscheidung
betrifft. **Vor dem ersten Aggregat mit Kindkollektion erledigen** — danach ist es zusätzlich ein
Datenmigrationsthema.

---

# TODO-02, Aggregat-Version und Envelope-Metadaten

**P1 · gelöst · WS-01 + WS-03 + IMP-24 (+ IMP-41, hacky-7 via TODO-13 erledigt)**

## Gelöst — die Version lebt auf dem State (ADR-0030, 2026-08-03)

Zusammen mit TODO-03 und TODO-04 in einem Zug umgesetzt, weil alle drei dasselbe
Persistenzformat betreffen. Aus dem Interface `IState<TSelf, TKey>` ist die abstrakte
Record-Basis `AggregateState<TSelf, TKey>` geworden, die `Id`, `Version` und `Apply` trägt;
`AggregateRoot.ApplyEvent` zählt bei jedem gefalteten Event hoch — auch bei einem, das der State
ignoriert. Der private Zähler in `EventSourcedAggregateRoot` ist gelöscht; beide Persistenzformen
lesen dieselbe Zahl.

Die eine Zahl bedient alle drei ursprünglichen Symptome:

| Symptom                                              | Jetzt                                                                 |
| ---------------------------------------------------- | --------------------------------------------------------------------- |
| Keine optimistische Nebenläufigkeit state-stored     | `version`-Spalte mit `IsConcurrencyToken()` — belegt durch `EfCoreAggregateRoundTripTests.TwoConcurrentRenames_LetTheSecondCommitFailAsAConflict` gegen echtes Postgres |
| Projektionen können nicht ordnungsbewusst sein       | `IProjectionHandler.Handle` bekommt die `DomainEventMetadata`; die Samples führen einen Versions-Wasserstand statt `RenameCount` |
| `DomainEventEnvelope` trägt keine Aggregat-Metadaten | `AggregateName`, `AggregateId`, `Version` — jedes Event einer Transaktion bekommt seine eigene Version |

**Anders als vorgeschlagen:** `IState` ist kein Interface mehr, sondern die abstrakte Record-Basis
`AggregateState<TSelf, TKey>`. Der Copy-Konstruktor von Records ist virtuell, also liefert
`this with { … }` in der Basis den abgeleiteten Laufzeittyp zurück — die Basis kann die Version
damit selbst setzen. `WithVersion` ist `internal` und für Domänencode weder erreichbar noch falsch
implementierbar. Ein State-Record schreibt deshalb **null** Zeilen zur Version, und der ursprünglich
gebaute Guard in `AggregateRoot` samt Exception, Test und Test-Double ist wieder entfallen — der
Fehlermodus existiert nicht mehr. Preis: ein ungeprüfter Cast, einmal, in den Building Blocks.

**Semantikwechsel, den man kennen muss:** Ein Event auf oder unter dem Wasserstand wird
**verworfen**, wo vorher feldweise gemerged wurde. Unter der heutigen Zustellung (pro Aggregat
geordnet, nur Redelivery) ist das richtig; unter echt ungeordneter Zustellung wäre es das nicht.

Damit ist auch **TODO-20** erst machbar (der Envelope trägt jetzt die Aggregat-Identität, nach
der partitioniert würde) und die state-stored Hälfte von **TODO-25** abgedeckt.

## Ursprünglicher Befund

Ursprünglich fünf Befunde, eine Ursache: es gibt keine fortlaufende Zahl pro Aggregat, und der
Envelope trägt keine Aggregat-Metadaten. Zwei davon (IMP-41, hacky-7) sind mit TODO-13
(ADR-0029) erledigt — `EventId`/`OccurredAt` liegen bereits auf dem Envelope. Offen bleiben:

| Symptom                                           | Quelle        |
| ------------------------------------------------- | ------------- |
| Keine optimistische Nebenläufigkeit state-stored  | WS-01         |
| Projektionen können nicht ordnungsbewusst sein    | WS-03, IMP-24 |
| `DomainEventEnvelope` trägt keine Aggregat-Metadaten | IMP-24     |

Verifiziert: weder `RowVersion` noch `IsConcurrencyToken` existieren irgendwo. Beide Samples
behelfen sich mit `RenameCount` als fachlicher Ordnungsgröße
([WidgetProjections.cs:62](samples/StateStored/VitalSync.Sample.StateStored.Infrastructure/Read/WidgetProjections.cs:62)) —
kein allgemeines Verfahren. Die ADR-0022-Anforderung „idempotent und per-aggregate order-aware"
ist gestellt, aber von der Infrastruktur nicht bedienbar.

## Lösungsvorschlag

Eine Zahl für drei Zwecke — Nebenläufigkeit, Projektions-Ordnung, Envelope-Metadatum:

```csharp
public abstract record AggregateState<TSelf, TKey>
{
    public abstract TKey Id { get; init; }
    public long Version { get; init; }
    public abstract TSelf Apply(IDomainEvent domainEvent);
}

public sealed record DomainEventEnvelope(
    string EventName, string Payload, Guid EventId,
    string AggregateName, string AggregateId, long Version, DateTimeOffset OccurredAt);
```

Zieht die Aggregat-Metadaten in den Envelope — `EventId`/`OccurredAt` sind dort seit TODO-13
(ADR-0029) schon vorhanden, es fehlen `AggregateType`, `AggregateId` und `Version`.

Voraussetzung für TODO-14 (Idempotenz) und TODO-20 (Partitionierung). Braucht eine ADR.

---

# TODO-03, `AssemblyQualifiedName` als Persistenz-Contract

**P1 · gelöst · hacky-1 + IMP-22**

## Gelöst — `[EventName]` und eine geschlossene Typ-Registry (ADR-0030, 2026-08-03)

Wie vorgeschlagen umgesetzt, plus eine Hälfte, die im Befund fehlte.

`[EventName("widget-created-v1")]` ist Pflicht, kebab-case wird im Attributkonstruktor geprüft.
Eine `DomainEventTypeRegistry` wird aus den per `options.AddDomainEventsFrom(assembly)`
genannten Assemblies gebaut und wirft **bei der Registrierung** bei fehlendem Attribut oder
doppeltem Namen. Wer eine Persistenzstrategie wählt, ohne ein Domain-Event-Assembly zu nennen,
scheitert beim Start. `Type.GetType` ist ersatzlos weg — die lesbare Typmenge ist geschlossen,
und damit auch die unbegrenzte Aktivierungsfläche.

**Der Befund war halbiert:** `UseMartenEventSourcing` konfigurierte **keine** Event-Aliase, also
leitete Marten `mt_events.type` selbst aus dem CLR-Typnamen ab. Ein `[EventName]`, das nur die
Outbox repariert, hätte ausgerechnet den Event Store — die einzige Stelle ohne „einfach neu
aufbauen" — weiter am Klassennamen hängen lassen. Dieselbe Registry speist jetzt
`options.Events.MapEventType`; belegt durch `MartenEventAliasTests`, das ein Event schreibt und
den gespeicherten `EventTypeName` aus der Datenbank zurückliest.

Weiter abgesichert durch `DomainEventTypeRegistryTests` (fehlendes Attribut, Namenskollision,
Idempotenz beim Doppelscan) und `DomainEventEnvelopeSerializerTests.StoredPayload_SurvivesARenameOfTheClrType`.

## Ursprünglicher Befund

In jeder Outbox-Zeile steht `"Foo.WidgetCreated, MyAsm, Version=1.0.0.0, Culture=neutral,
PublicKeyToken=null"`, zurückgeholt mit `Type.GetType(..., throwOnError: true)`
([DomainEventEnvelopeSerializer.cs:23,33](BuildingBlocks/src/BuildingBlocks.Infrastructure/Messaging/DomainEvents/DomainEventEnvelopeSerializer.cs:23)).

Version-Bump, Assembly-Umbenennung oder Typ-Umzug macht jede noch nicht zugestellte Nachricht
unlesbar — und das ist Crash-Recovery-Datenbestand, also genau der Fall, in dem man es am
wenigsten gebrauchen kann. Zusätzlich ist `Type.GetType` auf persistierten Daten eine unbegrenzte
Typ-Aktivierungsfläche.

## Lösungsvorschlag

```csharp
[EventName("widget-created-v1")]
public sealed record WidgetCreated(...) : DomainEvent;
```

Macht Event-Versionierung überhaupt erst ausdrückbar (`-v2` neben `-v1`). Gemeinsam mit TODO-02
und TODO-04 planen — alle drei betreffen dasselbe Persistenz-Format, und danach ist jede Änderung
eine Datenmigration.

---

# TODO-04, Stream-Key hängt am CLR-Klassennamen

**P1 · gelöst · hacky-2 + IMP-23**

## Gelöst — `[AggregateName]` statt `[StreamPrefix]` (ADR-0030, 2026-08-03)

Wie vorgeschlagen, aber unter anderem Namen: `[AggregateName("gadget")]` statt des skizzierten
`[StreamPrefix]`, weil derselbe Name auch als `AggregateName` auf dem Envelope gebraucht wird
(TODO-02). Ein Begriff, ein Attribut. `EntityKeyFormatter.GetAggregateName` wirft bei fehlendem
Attribut — das Werfen ist wie vorgeschlagen Absicht.

`AggregateConventionTests` scannt in beiden Samples auf `[AggregateName]` und `[EventName]`,
`EntityKeyFormatterTests` belegt, dass zwei unterschiedlich heißende CLR-Typen mit demselben
Attribut denselben Stream-Key ergeben — also genau die Rename-Resistenz, um die es ging.

Die bestehenden Sample-Streams heißen danach `gadget/…` statt `Gadget/…` und sind verwaist.
Heute folgenlos (Wegwerf-Durchstich, Datenbanken werden neu erzeugt) — und genau der Grund, das
vor dem ersten echten Service zu machen.

## Ursprünglicher Befund

`$"{aggregateType.Name}/{keyValue}"`
([EntityKeyFormatter.cs:20](BuildingBlocks/src/BuildingBlocks.Infrastructure/Persistence/EntityKeyFormatter.cs:20)).
Ein Rename von `Gadget` nach `Device` verwaist alle bestehenden Streams — im Event Store, wo es
per Definition kein „einfach neu aufbauen" gibt.

Das Fehlerbild ist das unangenehmste denkbare: `FetchStreamAsync` liefert leer, `GetByIdAsync`
gibt `null`, der Handler meldet korrekt `NotFound`. **Kein Fehler** — nur Daten, die verschwunden
scheinen. Ein anschließender Schreibvorgang legt einen neuen Stream an und macht den alten
endgültig unauffindbar.

## Lösungsvorschlag

```csharp
[StreamPrefix("gadget")]
public sealed class Gadget : EventSourcedAggregateRoot<GadgetId, GadgetState>;

private static string PrefixOf(Type aggregateType) =>
    aggregateType.GetCustomAttribute<StreamPrefixAttribute>()?.Prefix
    ?? throw new InvalidOperationException(
        $"'{aggregateType}' braucht ein [StreamPrefix]; der Klassenname ist kein Persistenz-Contract.");
```

Das Werfen ist Absicht: der Contract soll bewusst gesetzt werden, nicht aus dem
Refactoring-Zufall entstehen.

---

# TODO-05, Kein `Id.IsEmpty`-Guard in `AddAsync`

**P1 · gelöst · hacky-5**

Die Domäne bewacht Leer-Identität an zwei Stellen (`RaiseEvent`, `IStateOwner.Restore`) — das
Repository ist die einzige Tür ohne Schloss. Ein Aggregat mit leerer Identität schreibt eine
Zeile mit `Guid.Empty` bzw. öffnet Stream `Gadget/00000000-…`
([EfCoreRepository.cs:55](BuildingBlocks/src/BuildingBlocks.Infrastructure/Persistence/StateStored/EfCoreRepository.cs:55),
[MartenEventSourcedRepository.cs:48](BuildingBlocks/src/BuildingBlocks.Infrastructure/Persistence/EventSourced/MartenEventSourcedRepository.cs:48)).

**Der bequeme Weg dorthin ist mit TODO-10 zu.** `repository.AddAsync(new Widget())` kompiliert
nicht mehr: der Konstruktor ist privat, `CreateEmpty` explizit implementiert. Erreichbar bleibt
eine leere Hülle nur noch über eine selbstgeschriebene generische Methode mit
`IReconstitutable`-Constraint — die bewusst in Kauf genommene Restlücke. Der Guard schließt
genau die, ist davon unabhängig und unstrittig; die Priorität sinkt dadurch, der Bedarf nicht.

## Gelöst — der Guard steht in beiden Repositories (2026-08-03)

Wie unten vorgeschlagen umgesetzt: beide `AddAsync`-Implementierungen werfen bei
`aggregate.Id.IsEmpty` eine `InvalidOperationException`, bevor irgendetwas getrackt oder
geschrieben wird. Damit ist auch die letzte Tür bewacht — die per `IReconstitutable`-Constraint
noch erreichbare leere Hülle landet nicht mehr in der Datenbank. Abgesichert durch
`RepositoryEmptyIdentityGuardTests` (leer → wirft, mit Identität → passiert) für beide Pfade.

**Nachtrag (2026-08-04):** Mit dem Rückbau von `IReconstitutable` (siehe TODO-10) ist die oben
beschriebene Restlücke — eine selbstgeschriebene generische Methode mit
`IReconstitutable`-Constraint — mitsamt dem Interface verschwunden; an eine leere Hülle kommt
Anwendungscode jetzt nur noch per Reflection. Der Guard bleibt unverändert nötig und unverändert
bestehen: er bewacht die Tür unabhängig davon, wie jemand an die Hülle kam.

## Ursprünglicher Lösungsvorschlag

```csharp
if (aggregate.Id.IsEmpty)
{
    throw new InvalidOperationException(
        $"'{typeof(TAggregate)}' hat keine Identität. Ein Aggregat erhält sie durch sein erstes " +
        "Event — der parameterlose Konstruktor dient nur der Rehydrierung.");
}
```

In beide `AddAsync`-Implementierungen. Kosten null, verhinderter Fehler ist Datenkorruption.
Kleinster P1-Punkt der Liste, sofort machbar.

---

# TODO-06, Connection String zweimal, ohne Abgleich

**P1 · gelöst · hacky-8 + IMP-13 (Teil)**

`UseEfCorePersistence(cs)` legt den String in `EfCoreMessageStoreConnectionString` ab und nutzt
ihn ausschließlich für ein `RequiresWolverine`-Bool
([BuildingBlocksOptions.cs:316](BuildingBlocks/src/BuildingBlocks.Infrastructure/DependencyInjection/BuildingBlocksOptions.cs:316)).
Der Host muss denselben String ein zweites Mal an `UseBuildingBlocksEfCorePersistence(cs)`
reichen ([Program.cs:10-19](samples/StateStored/VitalSync.Sample.StateStored.Api/Program.cs:10)).

Dass die Duplizierung strukturell erzwungen ist (Wolverine 3.0 verbietet der Extension den
Zugriff auf die ServiceCollection), ist nachvollziehbar. Dass sie **ungeprüft** bleibt, nicht:
zwei Tippfehler auseinander, und die Outbox sitzt in einer anderen Datenbank als die Aggregate —
die ADR-0022-Atomaritätsgarantie ist dann still weg.

## Gelöst — die zweite Nennung gibt es nicht mehr

Der ursprüngliche Vorschlag war, die beiden Strings beim Start zu **vergleichen**. Umgesetzt wurde
stattdessen die Ursache: Building Blocks ruft `UseWolverine` selbst auf.

```csharp
builder.UseWolverine(options =>
{
    if (wiring.EfCoreMessageStoreConnectionString is { } writeConnectionString)
    {
        options.PersistMessagesWithPostgresql(writeConnectionString);
        options.UseEntityFrameworkCoreTransactions();
    }

    configureWolverine?.Invoke(options);
});
```

Der Host registriert über `builder.AddBuildingBlocks(options => …)` auf `IHostApplicationBuilder`
und benennt die Write-Datenbank **einmal**, in `UseEfCorePersistence`. Kein Reflection: die
Wolverine-3.0-Schranke gilt nur für container-registrierte Extensions, ein `UseWolverine`-Callback
darf die ServiceCollection ändern.

Belegt durch `HostBuilderWiringTests` — insbesondere, dass Wolverines `DatabaseSettings.ConnectionString`
genau die Datenbank ist, die `UseEfCorePersistence` ausgewählt hat. Beide Sample-Hosts rufen kein
`UseWolverine` mehr auf. ADR-0027 hat dazu ein zweites Amendment (2026-08-03).

**Kein Restweg:** Die drei EF-Integrationstests (`EfCoreOutboxAtomicityTests`,
`EfCoreAggregateRoundTripTests`, `OutboxFlushOnCommitTests`) laufen ebenfalls über die
Builder-Überladung — damit hatte `UseBuildingBlocksEfCorePersistence` keinen Aufrufer mehr und
`WolverineHostExtensions` ist gelöscht. Es gibt exakt **eine** Art, den EF-Outbox zu verdrahten, und
keine öffentliche API, über die ein Host eine zweite Datenbank nennen könnte. Nebeneffekt: die Tests,
die die ADR-0022-Atomarität belegen, gehen jetzt denselben Weg wie echte Hosts.

---

# TODO-07, Integration Events sind nicht persistent

**P1 · gelöst · WS-08**

## Gelöst — durabler Sending-Endpoint, durable Topologie, Quorum-Queues (ADR-0023-Amendment, 2026-08-04)

Der Befund stimmte, war aber **kleiner beschrieben als er war**. Gemessen statt vermutet:

- **Ein Schalter, zwei Verluste.** `delivery_mode: 1` ist nur die sichtbare Hälfte. Wolverines
  RabbitMQ-Sender leitet das AMQP-Persistenzflag aus dem Endpoint-Modus ab, und derselbe Modus
  entscheidet, ob überhaupt eine Zeile nach `wolverine_outgoing_envelopes` geschrieben wird. Der
  geerbte Default `BufferedInMemory` verlor die Nachricht also **auch** bei einem
  Prozessabsturz zwischen Commit und Broker-Bestätigung — ohne Broker-Neustart. Die
  Outbox-Zusage aus ADR-0022 endete faktisch am Übergabepunkt.
- **Der Rückgabewert war schon da.** `PublishMessagesToRabbitMqExchange<IIntegrationEvent>(…)`
  liefert eine `RabbitMqExchangeConfiguration`, die der Code verwarf. `UseDurableOutbox()`
  darauf schließt beide Lücken auf einmal.
- **Quorum am Transport, nicht an der Queue.** `UseRabbitMq(uri).UseQuorumQueues()` erfasst auch
  die Queues, die Building Blocks nie selbst benennt — allen voran die
  `wolverine-dead-letter-queue`, laut ADR-0023 „operationally the one place to look" und sonst
  die am wenigsten haltbare Queue im System. Verifiziert: die Policy greift nur für
  `EndpointRole.Application`, Wolverines eigene System-Queues bleiben korrekterweise klassisch.
- **Fail fast statt scheinbar durabel.** Ein durabler Sending-Endpoint braucht einen Message
  Store; `UseWolverineMessaging` allein liefert keinen. Wolverine degradiert in dieser Lage
  still — also genau der Fehlertyp, gegen den dieser Eintrag angetreten ist. `AddBuildingBlocks`
  wirft jetzt, wenn eine Broker-URI ohne `UseEfCorePersistence`/`UseMartenEventSourcing` gesetzt
  wurde. Die Prüfung läuft **nach** dem gesamten Options-Lambda, die Reihenfolge der Aufrufe ist
  damit egal.

Belegt durch `IntegrationEventDurabilityTests` gegen einen echten Broker (Nachricht kommt mit
`Persistent == true` an, kompilierter Endpoint ist `Durable`, Subscriber- und Dead-Letter-Queue
sind Quorum und durabel) und ohne Docker durch `WolverineExtensionTests`. Gegenprobe gemacht:
ohne den Fix fallen alle vier Fakten, ohne `UseQuorumQueues()` genau die zwei Quorum-Fakten.

**Konsequenz zu kennen:** der Queue-Typ gehört zur Deklaration und lässt sich an einer
bestehenden Queue nicht ändern. Ein Broker, der aus einem früheren Lauf noch eine klassische
Queue gleichen Namens trägt, lässt `AutoProvision` scheitern; die Queue muss gelöscht werden.
Heute betrifft das keine Umgebung — der produktive AppHost lief noch nie, und der
Samples-AppHost bekam sein `WithDataVolume()` erst mit dieser Änderung.

**Nicht enthalten:** Publisher Confirms, siehe TODO-48.

## Ursprünglicher Befund

Verifiziert: nirgends in `BuildingBlocks/src` wird persistente Zustellung konfiguriert, die
Nachrichten gehen mit `delivery_mode: 1` raus. Die Outbox schützt bis zur Übergabe an den Broker;
**danach** verliert ein RabbitMQ-Neustart die Nachricht. Das untergräbt genau die Zusage, für die
die Outbox gebaut wurde.

## Lösungsvorschlag

```csharp
options.Publish(publishing => publishing
    .MessagesImplementing<IIntegrationEvent>()
    .ToRabbitTopics(IntegrationEventExchangeName, exchange => exchange.Durable = true));
```

Konsumentenseite mitentscheiden: Quorum-Queues überleben einen Broker-Neustart, klassische nicht.
Beides in denselben Schritt, sonst ist die Kette nur halb dicht. Der Durchsatzpreis ist für
Integration Events die richtige Wahl; den lokalen Domain-Event-Pfad betrifft es nicht (der läuft
über die Datenbank). Anmerkung seit dem ADR-0023-Amendment 2026-08-03: die Publishing-Regel heißt
inzwischen `PublishMessagesToRabbitMqExchange<IIntegrationEvent>`; die Durable-Einstellung gehört
dann an deren Exchange-Konfiguration statt an `ToRabbitTopics`.

---

# TODO-08, Topic-Validierung und Kontextkennung

**P1 · gelöst · WS-05 + WS-13 + WS-14**

Drei Befunde, die alle dieselbe fehlende Information brauchen — den eigenen Kontextnamen:

- **Vergessenes `[Topic]`** (WS-05): **gelöst** durch das ADR-0023-Amendment 2026-08-03 —
  `[IntegrationEventTopic("<kontext>.<event>")]` in `BuildingBlocks.Application` validiert sein
  Argument bei Konstruktion, und ein Event ohne Attribut wirft beim Publish statt still unter
  einem CLR-Schlüssel zu verschwinden (gepinnt durch `IntegrationEventRoutingTests`).
- **Pattern gegen Vertrag unbewacht** (WS-14): `SubscriptionDiscoveryTests` fängt inzwischen die
  vergessene Consumer-Assembly ab, aber ein Tippfehler im Topic-Pattern verhält sich exakt wie
  ein Upstream-Kontext, der noch nichts publiziert hat.
- **Eigene Events konsumieren** (WS-13): folgenlos nur solange kein Handler existiert — die
  Folgenlosigkeit beruht auf der Abwesenheit von Code, nicht auf einer Regel.

## Lösung (ADR-0023-Amendment 2026-08-05, ADR-0027- und ADR-0018-Amendment)

`UseWolverineMessaging(rabbitMqUri, exchangeName, contextName)` nimmt die drei
Transport-Koordinaten gemeinsam entgegen. Der Kontextname ist **verpflichtend** und ein
einzelnes kebab-case-Wort; ein Punkt darin wird abgewiesen, weil das fast immer der
Exchange-Name an der falschen Stelle ist.

- **Publish-Guard:** Das Präfix aus `[IntegrationEventTopic]` muss dem eigenen Kontext
  entsprechen, sonst wirft der Topic-Provider der Publishing-Regel.
- **Absenderkennung:** Jedes publizierte Event trägt den Header
  `buildingblocks.source-context`; eine Consumer-Middleware verwirft ein Integration Event,
  dessen Quelle der konsumierende Kontext selbst ist.
- **Handler ⇒ Pattern statt Pattern ⇒ Vertrag:** Die im Lösungsvorschlag unten geplante
  *Warnung* entfällt. Die Prüfrichtung wurde umgedreht — „publiziert jemand auf mein Pattern?"
  ist lokal nicht entscheidbar, „bekomme ich, wofür ich einen Handler habe?" schon. Damit ist
  es ein **harter Startfehler**, und der Tippfehler wird genau dann gefangen, wenn er weh tut.
  Ein Handler auf ein Event des **eigenen** Kontexts ist ebenfalls ein Startfehler — zusammen
  mit der Unterdrückung ist diese beweisbar verlustfrei.
- **Exchange-Name:** verlässt Building Blocks (siehe ADR-0018-Amendment). VitalSync definiert
  ihn einmal in `VitalSync.ServiceDefaults`; `vitalsync` kommt unter `BuildingBlocks/src` nicht
  mehr vor.
- **Abschaltbare Checks entfallen:** `ValidateHandlersOnStart` und `ValidateWolverineOnStart`
  sind gestrichen. Ein Opt-out für eine Prüfung, die eine sonst stille Fehlerlage abfängt,
  stellt genau diese stille Fehlerlage wieder her. Querbezug: TODO-11 entscheidet weiterhin
  eigenständig über `UnitOfWorkPresenceCheck`.
- **Folge für die Samples:** Beide Durchstiche publizierten unter dem Präfix `sample.` — zwei
  Kontexte mit einer Kennung. Sie heißen jetzt `sample-state-stored` und `sample-event-sourced`.

Gepinnt durch `IntegrationEventContextTests`, `TopicPatternMatcherTests` (ohne Docker) und
`IntegrationEventSubscriptionValidationTests` (mit echtem Broker).

## Ursprünglicher Lösungsvorschlag

```csharp
options.ContextName = "nutrition";

envelope.Headers["vitalsync.source-context"] = options.ContextName;
```

Die Pattern-Prüfung bewusst als **Warning**: ein Service bindet legitim auf einen noch nicht
existierenden Upstream-Kontext, und ein Fehler dort würde genau die Reihenfolge blockieren, in
der man Kontexte normalerweise baut.

---

# TODO-09, Keine CI-Pipeline

**P1 · gelöst · WS-16**

Verifiziert: `.github/workflows/` existiert und ist **leer**. Kein automatischer Build, kein
Testlauf, keine Prüfung der „warnings as errors"-Zusage aus `Directory.Build.props`.

Das ist P1, weil es das Netz ist, das alle anderen Punkte hält: ohne CI hängt die Korrektheit
jedes Fixes daran, dass jemand lokal `dotnet test` tippt.

## Lösungsvorschlag

```yaml
- run: dotnet build --configuration Release
- run: dotnet test --configuration Release
  env:
      VITALSYNC_REQUIRE_CONTAINERS: "1"
```

Die Umgebungsvariable ist der Punkt, an dem es sonst schiefgeht: ohne sie überspringen die
Testcontainers-Tests kommentarlos und der Lauf ist trotzdem grün — die Pipeline würde also genau
die Tests nicht ausführen, für die sie am wertvollsten ist. GitHub-Runner haben Docker
vorinstalliert.

## Umgesetzt als `.github/workflows/build.yml`

Ein Job auf `ubuntu-latest`, Trigger `push` auf `main`, `pull_request` und `workflow_dispatch`,
mit `concurrency`/`cancel-in-progress`. Schritte: Restore → Build (Release; damit ist es zugleich
das Analyzer- und Style-Gate) → `dotnet test` mit `VITALSYNC_REQUIRE_CONTAINERS=1`.

**Ein Schritt mehr als ursprünglich vorgeschlagen:** Der Workflow startet danach den
Samples-AppHost, wartet auf beide Sample-APIs und lässt die Sample-Testprojekte mit gesetzten
`SAMPLE_*_API_URL` erneut laufen — dann überspringen die elf Smoke-Tests nicht mehr. Begründung ist
§3 des Durchstichs: alle dort gefundenen Fehler waren durch Build und Testsuite unsichtbar, nur ein
echter Web-Host mit echtem Broker hat sie gezeigt. Bei `failure()` gibt der Job das AppHost-Log aus,
weil Wolverines interessante Fehler (routenlose Nachricht, nicht entdeckter Consumer) genau dort
stehen und nirgends sonst.

Die Smoke-Stufe hängt an `samples/`. Wenn der erste echte Service steht, ändern sich dort nur die
Projektpfade und die zwei URLs — nicht der Aufbau.

Dazu neu: **`global.json`** nagelt das SDK fest (`10.0.302`, `rollForward: latestFeature`), damit CI
und Entwicklerrechner dieselbe Feature-Band benutzen. **Kein Aspire-Workload-Schritt** — die AppHosts
referenzieren `Aspire.AppHost.Sdk` als Package.

**Erster Lauf (`f56840c`): grün in 2 Minuten**, alle Schritte inklusive Smoke-Stufe. Damit ist auch
die Workload-Frage beantwortet: Der Build läuft ohne installierten Aspire-Workload, die entsprechende
Voraussetzung in README und Instruktionsdateien war überholt und ist korrigiert.

**Nachgezogen:** Die Smoke-Tests hatten zunächst kein Gegenstück zu `ContainerRequirement` — bei
fehlender `SAMPLE_*_API_URL` (Tippfehler im Workflow, umbenannte Variable) hätten sie kommentarlos
übersprungen und der Schritt wäre grün geblieben. `SmokeRequirement` schließt das: mit gesetztem
`VITALSYNC_REQUIRE_SMOKE` wird die fehlende URL zum Fehler statt zum Skip, und der Workflow setzt die
Variable in der Smoke-Stufe. Empirisch geprüft — ohne URLs und mit gesetztem Flag fallen die
betroffenen Tests um (4 im state-stored, 7 im event-sourced Sample); ohne Flag überspringen sie
weiterhin, damit die Suite lokal ohne laufendes System benutzbar bleibt.

**Nachgezogen (2026-08-07): der Name des fehlgeschlagenen Tests fehlte im Log.** Trotz
`--output Detailed` schrieb der Lauf nur „Failed! - Failed: 1, Passed: 244" auf die Konsole; der
Testname stand ausschließlich in der 668 KB großen Logdatei im Artefakt — und bei älteren Läufen
war das Artefakt bereits abgelaufen, die Information also unwiederbringlich weg. Jede rote Pipeline
kostete damit einen Artefakt-Download. Beide Teststufen erzeugen jetzt zusätzlich einen
xUnit-TRX-Report (`--report-xunit-trx`), und je ein `if: failure()`-Schritt schreibt die Einträge
mit `outcome="Failed"` samt Meldung und Stacktrace direkt in eine aufklappbare Log-Gruppe. Sagt
kein Report einen fehlgeschlagenen Test, gibt der Schritt das ausdrücklich aus — dann lag der
Fehlschlag außerhalb des Testlaufs (etwa der Runner, der eine Zeit lang gar nicht erst zugeteilt
wurde).

---

# TODO-10, Rehydrierung: `new()` oder `Activator`?

**P1 · GELÖST · hacky-5 + IMP-14 + WS-02**

Die Quellen widersprechen sich in der Richtung:

| Quelle           | Empfehlung                                                                                     |
| ---------------- | ---------------------------------------------------------------------------------------------- |
| WS-02 (original) | offene Frage: „soll das so bleiben, **oder bekommt `IRepository` eine `new()`-Beschränkung**?" |
| hacky-5, IMP-14  | umgekehrt: `new()` **streichen**, `Activator.CreateInstance(nonPublic: true)` überall          |

Der damalige Sachstand: der EF-Pfad nutzte `Activator`, der Marten-Pfad eine `new()`-Constraint.
Beide waren open-generisch auf denselben Vertrag registriert, der nur `IAggregateRoot<TKey>`
verlangte — ein falsch zugeschnittenes Aggregat scheiterte also erst im Container zur Laufzeit.

Der Kern des Konflikts: `new()` verlangt einen **öffentlichen** parameterlosen Konstruktor. Damit
diktierte die Infrastruktur in die Domäne hinein, entgegen ADR-0025 („darf non-public sein"), und
`new Widget()` war überall legaler Code.

## Lösung

**Keins von beiden.** Die Frage war falsch gestellt: „öffentlicher Konstruktor oder Reflection?"
sind zwei Varianten derselben Kapitulation, obwohl das, was das Repository braucht — der State-Typ
und eine Instanz zum Hineinfalten — zur **Compile-Zeit** feststeht. Umgesetzt ist stattdessen ein
expliziter Domänenvertrag:

```csharp
public interface IReconstitutable<TSelf> where TSelf : IReconstitutable<TSelf>
{
    static abstract TSelf CreateEmpty();
}

public sealed class Widget : AggregateRoot<WidgetId, WidgetState>, IReconstitutable<Widget>
{
    private Widget() : base(WidgetState.Empty) { }
    static Widget IReconstitutable<Widget>.CreateEmpty() => new();
}

var aggregate = TAggregate.CreateEmpty();
```

Ein `static abstract` Member ist **nur über einen constraint-gebundenen Typparameter** aufrufbar.
Die explizite Implementierung schließt damit jeden öffentlichen Weg — empirisch gegen den Compiler
geprüft: `new Widget()` → `CS1729`, `Widget.CreateEmpty()` → `CS0117`, über eine
interface-typisierte Instanz → `CS0176`. `Widget.Create(…)` bleibt die einzige Entstehung.

Die Constraint sitzt auf **`IRepository<TAggregate, TKey>`**, nicht auf den Implementierungen:
ein falsch zugeschnittenes Aggregat scheitert dadurch **beim Injizieren zur Compile-Zeit** statt
beim Schließen des offenen Generics im Container. Der ursprünglich vorgeschlagene Startup-Check
über die gescannten Assemblies ist damit überflüssig. `Activator` und `new()` sind beide weg, und
die EF/Marten-Asymmetrie — nie eine Entscheidung, nur ein Zufall — ebenfalls.

**Bewusst akzeptierte Restlücke:** Anwendungscode *könnte* eine eigene generische Methode mit
`IReconstitutable`-Constraint schreiben und so an eine Hülle kommen. Das ist ein lauter, greppbarer,
absichtlicher Akt statt eines Tippfehlers; Reflection war ohnehin nie verhinderbar. Die Latte ist
„kann nicht versehentlich passieren", nicht „kann nicht passieren".

**Preis:** eine Zeile Boilerplate pro Aggregat. Die Basisklasse kann sie nicht liefern — dafür
bräuchte sie `TSelf` und damit wieder `new()`. Ein Source Generator wäre später rein additiv.

Abgesichert durch `ReconstitutableTests` (beide Persistenzformen) und je einen
`AggregateConventionTests`-Scan pro Sample, der bei fehlendem Interface oder öffentlichem
parameterlosem Konstruktor fehlschlägt. ADR-0025 und ADR-0026 sind um je ein Amendment ergänzt
(2026-08-03).

**Nachtrag (2026-08-04): das Interface ist wieder weg — Konvention statt Vertrag.**
`IReconstitutable<TSelf>` kaufte seinen Compile-Zeit-Beweis mit sichtbarer Zeremonie an jedem
Aggregat (Interface in der Basisliste plus explizites `CreateEmpty`), und die Alternativen, die
den Beweis behalten (CRTP-`TSelf` an der Basis, Source Generator), verschieben die Kosten nur.
Gelöscht. Rekonstitution ist jetzt eine **Konvention** — privater parameterloser Konstruktor,
sonst nichts — die eine interne, pro Typ gecachte `AggregateFactory` in der Infrastruktur bedient
und die `AddBuildingBlocks` **beim Startup** validiert (Scan der `AddDomainEventsFrom`-Assemblies;
fehlt der Konstruktor, scheitert die Registrierung mit dem Namen des Aggregats). Die Richtung von
hacky-5/IMP-14 (Reflection überall) hatte also doch recht, nur gecacht und mit Fail-Fast statt
Laufzeitüberraschung; der oben für „überflüssig" erklärte Startup-Check ist genau so gekommen.
Abgesichert durch `AggregateFactoryTests` (inkl. Negativprobe via `HullFixture`-Assembly) und die
verschärften `AggregateConventionTests`. ADR-0025-Amendment vom 2026-08-04.

---

# TODO-11, Optionalität von `IUnitOfWork`

**P2 · gelöst · IMP-07 vs. hacky-11 (+ IMP-46)**

| Quelle   | Aussage                                                                               |
| -------- | ------------------------------------------------------------------------------------- |
| IMP-07   | **gelöst** — `IUnitOfWork? unitOfWork = null` als Konstruktorparameter ist die Lösung |
| hacky-11 | genau das ist der Fehler — ein stiller Default entscheidet statt des Entwicklers      |

Beide beschreiben denselben Code
([UnitOfWorkBehavior.cs:27](BuildingBlocks/src/BuildingBlocks.Infrastructure/Dispatching/UnitOfWorkBehavior.cs:27)).
Unstrittig ist, dass der ursprüngliche Bug (Absturz ohne Persistenz) weg ist; strittig ist, ob
„kein UoW registriert ⇒ Command committet stillschweigend nicht" ein akzeptabler Endzustand ist.
Heute hängt die Sichtbarkeit an einem einzelnen `Information`-Log beim Start.

## Ursprünglicher Lösungsvorschlag

**Empfehlung: hacky-11 folgen.** Ein `Information`-Log ist keine Absicherung — im Produktivbetrieb
liest ihn niemand, und der Fehlermodus ist „Command meldet Erfolg, Daten fehlen".

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

Das Behavior nimmt dann `IUnitOfWork` ohne `?` und ohne Default, der Null-Check im `Handle`
entfällt. Der Startup-Hinweis bleibt sinnvoll. IMP-07 wäre danach weiterhin gelöst — nur anders.

## Gelöst — die Entscheidung ist explizit, nicht still (ADR-0027-Amendment, 2026-08-05)

Der Vorschlag oben allein hätte nur den `?`-Operator weggeräumt: der Fehlermodus „Command meldet
Erfolg, nichts committet" wäre exakt derselbe geblieben, nur besser verpackt. Umgesetzt ist deshalb
die Variante, die den stillen Pfad **nur noch nach ausdrücklicher Ansage** erlaubt.

- `NullUnitOfWork` wird in `RegisterCore` per `TryAddScoped` als Fallback registriert — ein echter
  UoW (aus der Persistenz-Selektion oder vom Host) gewinnt immer. `UnitOfWorkBehavior` nimmt
  `IUnitOfWork` ohne `?` und ohne Default, der Null-Check entfällt.
- `UnitOfWorkPresenceCheck` wird vom Logger zum harten Check. Er besteht, wenn eine Persistenz
  gewählt wurde, wenn der Host ein eigenes `IUnitOfWork` registriert hat, oder wenn die gescannten
  Assemblies gar keine Commands enthalten (Tests, reine Query-Hosts). Er **wirft** — unter Nennung
  der betroffenen Commands —, wenn Commands in den Fallback laufen würden.
- Ein Host, der bewusst nichts committet (Gateway, Facade, eigene Persistenz), sagt das mit dem
  neuen `options.UseNoPersistence()`. Das ist eine **positive Selektion** auf `PersistenceChoice`,
  kein Opt-out-Flag im Sinne des ADR-0027-Amendments: es schaltet keine Prüfung ab, ist zu
  `UseEfCorePersistence`/`UseMartenEventSourcing` exklusiv (Kombination wirft), und der Check läuft
  weiter und protokolliert die bewusste Wahl.
- `PersistenceChoice` bekommt dafür einen vierten Fall; deshalb unterscheidet die Wahl jetzt
  `IsChosen` (es wurde etwas gesagt) von `IsSelected` (ein echter Store existiert — die Tatsache,
  die Outbox, Domain-Event-Routing und Wolverine steuert).

Damit sind **beide** Quellen erfüllt: der Absturz aus IMP-07 bleibt behoben, und der stille Pfad
aus hacky-11 ist nur noch erreichbar, wenn ihn jemand hinschreibt. Getestet in
`UnitOfWorkPresenceCheckTests` und `PersistenceChoiceTests`.

---

# TODO-12, Name der `Result`-Fehlerfactory

**P2 · gelöst · hacky-3 + IMP-27 vs. IMP-39**

| Quelle          | Vorschlag                                                                          |
| --------------- | ---------------------------------------------------------------------------------- |
| hacky-3, IMP-27 | `static abstract TSelf Failure(Failure failure)` — Name **`Failure` bleibt**       |
| IMP-39          | Umbenennen zu **`Failed(...)`**, weil `Failure`/`Failures`/`IsFailure` kollidieren |

Der gemeinsame Nenner: `FailureResults` sucht die statische Methode per Reflection über den Namen
([FailureResults.cs:39](BuildingBlocks/src/BuildingBlocks.Infrastructure/Dispatching/FailureResults.cs:39)),
weil `Result` und `Result<T>` keine gemeinsame Abstraktion haben. Beide Vorschläge beseitigen die
Reflection — aber die Codeskizzen widersprechen sich im Namen.

## Gelöst — der Dispatcher reicht die Factory mit (Variante H)

Umgesetzt am 2026-08-05
([ADR-0015-Amendment](docs/architecture/decisions/0015-hand-rolled-cqrs-mediator.md)),
in zwei Schritten.

**Schritt 1 — Umbenennung (IMP-39).** `Result.Failure(...)` heißt jetzt `Result.Failed(...)`,
ebenso in `Result<T>` ([ADR-0017-Amendment](docs/architecture/decisions/0017-application-error-handling-and-result.md)).
`Failure` ist der Name des Fehler**werts**; `Failures`/`IsFailure` belegen das Substantiv bereits.

**Schritt 2 — die Reflection ist weg (hacky-3, IMP-27).** Statt der vorgeschlagenen
`IFailureResult<TSelf>`-Abstraktion wurde die Ursache beseitigt: der Dispatcher **kannte** den
konkreten Ergebnistyp und warf die Information beim Aufruf von `BuildPipeline<TRequest, TResponse>`
weg — genau die Information, die `FailureResults` per Expression-Tree wiederherstellte. Jetzt
bekommt ein Behavior statt eines nackten Delegates ein `RequestPipeline<TResponse>`:

```csharp
public interface IPipelineBehavior<in TRequest, TResponse>
{
    Task<TResponse> HandleAsync(TRequest request, RequestPipeline<TResponse> pipeline, CancellationToken cancellationToken);
}

public sealed class RequestPipeline<TResponse>
{
    public Task<TResponse> NextAsync(CancellationToken cancellationToken);
    public TResponse Failed(Failure failure);
}
```

Der Sender konstruiert es dort, wo der Typ konkret ist (`Result.Failed` bzw.
`Result<TResult>.Failed`). Ergebnis: `FailureResults` **ersatzlos gelöscht** — samt Reflection,
`ConcurrentDictionary` und `Expression.Compile` —, kein Generic Constraint, kein `static abstract`,
und ein kurzschließendes Behavior ist per Konstruktion compile-sicher. `RequestPipelineContinuation`
bleibt als Konstruktorparameter erhalten (kleinerer Breaking Change als ursprünglich skizziert).

**Verworfen:** Variante A (`IFailureResult<TSelf>` + `where TResponse : Result,
IFailureResult<TResponse>`) — prototypisch verifiziert, funktioniert, repariert den Typparameter
aber nachträglich und zwingt jedem künftigen service-eigenen Behavior den Constraint auf.
Ebenfalls verworfen: `Result` als `Result<Unit>` — das hätte die Kosten in jeden void-Command-Handler
jedes Service verlagert, um einen Infrastrukturtyp zu sparen.

**Breaking Change:** Hosts mit eigenen Behaviors nehmen `RequestPipeline<TResponse> pipeline` und
rufen `pipeline.NextAsync(ct)`. Tests: `RequestPipelineTests` (6 neu) und der `Result<T>`-Pfad in
`FailureTranslationTests`; 455 Tests grün.

## Ursprünglicher Lösungsvorschlag

**Empfehlung: beides, in dieser Reihenfolge** — der Konflikt ist scheinbar. Erst umbenennen, dann
abstrahieren, weil das `new`-Hiding in `Result<T>` sonst bestehen bleibt:

```csharp
public static Result Failed(Failure failure);
public static Result<T> Failed<T>(Failure failure);

public interface IFailureResult<out TSelf> { static abstract TSelf Failed(Failure failure); }
return TResponse.Failed(Failure.Validation(...));
```

Danach ist `FailureResults` ersatzlos löschbar und aus einem Laufzeitfehler wird ein
Compile-Fehler. Breaking Change für Handler, die `Result.Failure(...)` aufrufen — mechanisch.

---

# TODO-13, Wo lebt die Event-Identität?

**P2 · gelöst · IMP-41 vs. IMP-11**

| Quelle | Richtung                                                                          |
| ------ | --------------------------------------------------------------------------------- |
| IMP-41 | `EventId`/`OccurredAt` **raus** aus `DomainEvent`, rein in den Envelope (TODO-02) |
| IMP-11 | `IIntegrationEvent` bekommt `EventId`/`OccurredAt` **ans Event**                  |

Gegenläufige Empfehlungen für die zwei Event-Familien, ohne dass eine der Quellen die andere
erwähnt. Ohne Entscheidung entsteht ein Modell, in dem Identität mal im Umschlag und mal im Brief
steht — und niemand weiß mehr, welches gilt.

## Gelöst — Asymmetrie ist die Regel (ADR-0029)

Genau wie unten vorgeschlagen entschieden und umgesetzt
([ADR-0029](docs/architecture/decisions/0029-event-identity-placement.md)):

- **Domain Events** sind reine Wert-Records ohne Identitätsfelder — `IDomainEvent` und
  `DomainEvent` sind leer, `EventId`/`OccurredAt` werden vom Unit of Work beim Commit geprägt
  und reisen auf dem `DomainEventEnvelope`. Damit sind IMP-41 und hacky-7 miterledigt, der
  `DomainEventStamper` ist gelöscht.
- **Integration Events** tragen die Identität am Event: `IIntegrationEvent` verlangt
  `EventId`/`OccurredAt` (IMP-11 erledigt). Mapper erhalten `DomainEventMetadata` und übernehmen
  die Identität daraus — nie ein frisches Guid pro Aufruf, sonst bricht die Deduplizierung bei
  Redelivery.

Die Regel steht in `docs/architecture/communication.md`; TODO-14 kann darauf aufsetzen.

## Ursprünglicher Lösungsvorschlag

**Empfehlung: der Widerspruch ist auflösbar, beide haben recht** — die Fälle sind verschieden:

- **Domain Events** reisen ausschließlich im `DomainEventEnvelope` durch die eigene Outbox. Der
  Envelope ist immer da, kann die Metadaten tragen, und ohne sie am Event bleibt `DomainEvent` ein
  sauberer Wert-Record mit funktionierender Wertgleichheit. → **Identität in den Envelope.**
- **Integration Events** sind Verträge auf der Leitung. Es gibt keinen Envelope, den der
  Konsument kennt — die Identität muss also am Event hängen, sonst kann er keine Duplikate
  erkennen (TODO-14). → **Identität am Event.**

Das explizit als Regel festhalten (`docs/architecture/communication.md`), sonst wird die
Asymmetrie später als Inkonsistenz „aufgeräumt". Entscheidung vor TODO-02 und TODO-14 treffen.

---

# TODO-14, Idempotenz-Bookkeeping über die Kontextgrenze

**P2 · teilweise · IMP-11 + WS-12**

Der Spiegel in Etappe 3 ist nur deshalb idempotent, weil das Gadget die Widget-Id übernimmt
([MirrorWidget.cs:20](samples/EventSourced/VitalSync.Sample.EventSourced.Application/MirrorWidget.cs:20)).
Für ein Spiegelbild angemessen, aber kein allgemeines Verfahren: ein Kontext, der aus einem
fremden Ereignis ein **eigenes** Aggregat mit eigener Identität ableitet, braucht echtes
Bookkeeping über verarbeitete `EventId`s. Das gibt es nicht — aber seit TODO-13 (ADR-0029) trägt
`IIntegrationEvent` `EventId`/`OccurredAt`
([IIntegrationEvent.cs](BuildingBlocks/src/BuildingBlocks.Application/IntegrationEvents/IIntegrationEvent.cs)), es
gibt also inzwischen eine Id, über die man Buch führen kann.

## Befund 2026-08-06: die Hälfte war bereits gelöst, nur mit einer stillen Frist

Vor der Umsetzung wurde Wolverines Empfangspfad recherchiert statt vermutet — und der Befund hat
die Aufgabe halbiert:

**Die durable Inbox dedupliziert bereits.** `ListenToRabbitQueue(...).UseDurableInbox()` speichert
jede eingehende Envelope in `wolverine_incoming_envelopes`, deren Primärschlüssel die
`Envelope.Id` ist. Ein zweiter `INSERT` derselben Id verletzt den Constraint (PostgreSQL `23505`),
Wolverine erkennt das als `DuplicateIncomingEnvelopeException` und **quittiert die Nachricht ohne
sie zu verarbeiten**. Die Envelope-Id überlebt die Leitung, weil der `RabbitMqEnvelopeMapper` sie
als AMQP-`MessageId` schreibt und beim Empfang zurückliest. Ein Redelivery durch Nack, Requeue,
Consumer-Crash vor dem Ack oder Broker-Reconnect ist damit gedeckt — ebenso ein Outbox-Retry des
Senders, der dieselbe Envelope-Id behält.

**Aber der Schutz verfiel nach fünf Minuten.** `DurabilitySettings.KeepAfterMessageHandling` stand
auf dem Framework-Default `5.Minutes()`; ein Hintergrundjob löscht `Handled`-Zeilen nach Ablauf.
Wolverine nennt diese Zeilen im eigenen Quellkommentar „*records to use in idempotency checking*" —
die Idempotenzzusage des Systems hing also an einer Frist, die niemand hier je entschieden hat.
Dasselbe Muster wie ADR-0034 (`IsEmpty` im Eventstrom) und ADR-0035 (abgeleitete Feldnamen): eine
dauerhafte Entscheidung, die aus einem Default entstanden ist.

## Gelöst (Teil A, 2026-08-06): die Frist ist jetzt eine Entscheidung

`ApplyBuildingBlocksIdempotencyWindow` setzt `Durability.KeepAfterMessageHandling` auf **7 Tage**
([WolverineOptionsExtensions.cs](BuildingBlocks/src/BuildingBlocks.Infrastructure/DependencyInjection/Wiring/WolverineOptionsExtensions.cs)),
angewendet genau dann, wenn eine Persistenzstrategie gewählt wurde — ohne Message Store gibt es
keine Inbox-Zeilen, die man aufheben könnte. Sieben Tage decken ein Wochenende plus
Betriebsreaktionszeit ab, also den realistischsten offenen Fall: ein Operator spielt Stunden oder
Tage später eine Nachricht aus der Dead-Letter-Queue zurück.

Drei Tests halten das fest: der Wert wird bei gewählter Persistenz gesetzt, er ist beweisbar
**nicht** der Wolverine-Default (sonst wäre der Test wertlos, sobald das Framework seinen Default
ändert), und ohne Persistenz bleibt die Einstellung unberührt.

**Nachtrag 2026-08-07:** Diese drei Tests prüfen die *Konfiguration*, nicht die *Wirkung* — sie
wären auch dann grün, wenn Wolverines Inbox gar nicht deduplizierte. `InboxDeduplicationTests`
schließt das: eine echte Envelope wird aus der Queue gelesen und **byteweise samt `MessageId`**
zweimal zurückpubliziert; der Handler läuft danach genau einmal. Erst damit ist die Zusage aus
diesem Abschnitt belegt statt angenommen. Nebenbefund aus dem Bau dieses Tests: dieselbe
Deduplizierung war die Ursache des jahrelang ungeklärten Zustellraces aus TODO-37 — sie wirkte über
die geteilte Test-Exchange hinweg zwischen Testklassen.

## Offen (Teil B): fachliche Dedup über `EventId`

Nicht gedeckt bleibt die **Republikation mit neuer Envelope-Id** — ein Outbox-Replay, ein
Betriebseingriff, oder ein künftiger Event-Replay. Der Inbox-Schlüssel ist die Transportidentität;
die fachliche Identität ist `IIntegrationEvent.EventId`, und ADR-0029 hat sie ausdrücklich mit der
Auflage eingeführt, sie nicht pro Aufruf neu zu erzeugen, „*or redeliveries break deduplication*".
Diese Zusage hat bis heute keinen Konsumenten.

Lösungsweg, wenn es soweit ist: eine Wolverine-Middleware auf dem Consumer-Pfad (analog
`OwnContextIntegrationEventFilter`), die `EventId` gegen eine `processed_integration_events`-Tabelle
in der Write-DB des konsumierenden Kontexts prüft und den Eintrag **in derselben Transaktion** wie
die Handler-Arbeit schreibt. Beide Unit-of-Work-Pfade haben eine scoped Session
(`IDocumentSession` bzw. `DbContext`), der Eintrag reist also mit dem Commit mit; der
Primärschlüssel auf `EventId` fängt parallele Zustellungen ab, die Vorabprüfung ist nur Optimierung.
Kosten: zwei Implementierungen, ein Startup-Check für den EF-Pfad (die Tabelle muss gemappt sein)
und eine Aufräumstrategie, sonst wächst die Tabelle unbegrenzt.

**Bewusst vertagt**, bis **TODO-21** entschieden ist. Dort fällt die Entscheidung, ob ein Replay
überhaupt existieren wird — und ohne Replay ist der verbleibende ungedeckte Fall so schmal, dass
die Tabelle auf Verdacht gebaut wäre. Fällt TODO-21 zugunsten eines Domain-Event-Journals, wird
Teil B fällig, weil ein Replay per Definition Ereignisse mit neuer Transportidentität erzeugt.

Bis dahin gilt unverändert: **geteilte Identität ist der sanktionierte Idempotenzweg** — festgehalten
in `docs/architecture/communication.md`, damit der Sonderfall des Spiegels nicht als allgemeines
Muster kopiert wird.

---

# TODO-15, Mehrfachfehler und Feldvalidierung end-to-end

**P2 · offen · IMP-16 + IMP-17 + hacky-12**

Drei Befunde auf derselben Kette, die nur gemeinsam Sinn ergeben:

- **Erzeugen** (IMP-16): `Result` trägt eine `IReadOnlyList<Failure>`, aber der einzige Produzent
  ist `ExceptionToResultBehavior` und erzeugt genau einen Fehler aus einer Exception.
- **Beschreiben** (IMP-17): `Failure` hat kein `Target`/`PropertyName`
  ([Failure.cs:40-50](BuildingBlocks/src/BuildingBlocks.Application/Results/Failure.cs:40)), und alle
  Domänenfehler tragen denselben technischen Code.
- **Transportieren** (hacky-12): der gRPC-Adapter nimmt nur `result.Failures[0]`
  ([WidgetGrpcService.cs:62](samples/StateStored/VitalSync.Sample.StateStored.Api/WidgetGrpcService.cs:62)).

Ergebnis: feldweise Validierung im UI ist mit dem heutigen Modell nicht umsetzbar, obwohl das
Datenmodell so aussieht, als wäre sie vorgesehen.

## Lösungsvorschlag

```csharp
public sealed record Failure(string Code, string Message, FailureCategory Category)
{
    public string? Target { get; init; }
    public IReadOnlyDictionary<string, object?>? Metadata { get; init; }
}

public interface IRequestValidator<in TRequest>
{
    ValueTask<IReadOnlyList<Failure>> ValidateAsync(TRequest request, CancellationToken ct);
}
```

Fachliche Codes kommen von der Regel selbst (`IBusinessRule.Code` → `recipe.name_required`), nicht
aus einer Konstante im Behavior. Alternative, falls das zu viel ist: Mehrfach-Failures aus
`Result` streichen — dann ist die API wenigstens ehrlich.

---

# TODO-16, `FailureCategory` fehlt Autorisierung

**P2 · gelöst · IMP-18**

## Gelöst — `Forbidden` ergänzt, `Unexpected` bewusst verworfen (2026-08-08)

`FailureCategory` hat jetzt fünf Werte; `Failure.Forbidden(code, message)` ist die zugehörige
Factory, beide gRPC-Adapter bilden sie auf `StatusCode.PermissionDenied` ab.

**Anders als vorgeschlagen, in zwei Punkten.**

**1. `Unexpected` wird nicht ergänzt.** ADR-0017 verwirft den Wert nicht versehentlich, sondern
ausdrücklich (`Alternatives considered`: *„rejected: it would invite wrapping bugs in
`Result.Failure`"*, und im Entscheidungstext: *„There is deliberately **no** `Unexpected`
FailureCategory"*). Ein unerwarteter Fehler bleibt eine Ausnahme und erreicht den dünnen globalen
Handler; ihn zu einem `Result` zu degradieren wäre genau der zweite Fehlerkanal, den ADR-0017
schließt. Es gibt heute auch keinen einzigen Aufrufer, der ihn bräuchte. Der ursprüngliche
Befund wird damit **teilweise abgelehnt**, nicht vergessen — das ist die Entscheidung, kein
offener Rest.

**2. Die Begründung des Befunds war sachlich falsch.** Dort stand, die neuen Werte würden die
`switch`-Ausdrücke im Transport „compile-time-vollständigkeitsgeprüft" machen. Das kann C# nicht
leisten: ein `switch`-Ausdruck über ein Enum **verlangt** einen Discard-Arm (CS8509, weil ein
Enum jeden `int` tragen kann), und dieser Arm schluckt jeden künftigen Wert klaglos. Der Compiler
wird also nie melden, dass jemand eine Kategorie vergessen hat. Ein bloßes Erweitern des Enums
hätte das Problem nur verschoben.

Deshalb drei Schritte statt einem:

| Schritt | Wirkung |
| --- | --- |
| Mapping aus beiden gRPC-Services in je eine `FailureStatusMapping` herausgezogen | überhaupt erst testbar; die Aufrufstellen bleiben durch `using static` unverändert |
| Wächter je Sample: iteriert `Enum.GetValues<FailureCategory>()` und meldet jeden Wert, der in `StatusCode.Unknown` fällt | ersetzt die Compiler-Prüfung, die es nicht gibt |
| Wächter in `FailureTests`: jeder deklarierte Wert braucht eine gleichnamige Factory auf `Failure` | verhindert das halbe Erweitern |

Der Discard-Arm bleibt erhalten und wird separat mit einem gecasteten `(FailureCategory)int.MaxValue`
belegt — er ist für undeklarierte Werte zuständig, nicht für vergessene.

**Gegenprobe:** ein testweise eingefügter sechster Wert `Probe` ließ genau die beiden Wächter
fallen, je einen Test, mit den vorgesehenen Meldungen; danach zurückgenommen.

`Unauthorized` (401) bleibt wie im Befund draußen: Authentifizierung ist Sache des Hosts und
erreicht die Application-Schicht nie.

## Ursprünglicher Befund

Vier Werte: `Validation`, `BusinessRule`, `NotFound`, `Conflict`
([FailureCategory.cs](BuildingBlocks/src/BuildingBlocks.Application/Results/FailureCategory.cs)). Ein
Autorisierungsfehler (403) hat keine Kategorie und landet im `_ => StatusCode.Unknown`-Arm des
gRPC-Adapters; dasselbe gilt für einen bewusst zu einem `Result` degradierten Infrastrukturfehler.

## Ursprünglicher Lösungsvorschlag

```csharp
public enum FailureCategory
{
    Validation, BusinessRule, NotFound, Conflict,
    Forbidden,
    Unexpected,
}
```

`Unauthorized` (401) bewusst **nicht**: Authentifizierung ist Sache des Hosts und erreicht die
Application-Schicht nie. Die `switch`-Ausdrücke im Transport werden durch die neuen Werte
compile-time-vollständigkeitsgeprüft.

---

# TODO-17, `RuleChecker` schluckt `null`

**P2 · gelöst (2026-08-05) · hacky-10 + IMP-36**

`rule?.IsBroken() == true` und `foreach (var rule in rules ?? [])`
([RuleChecker.cs:18-63](BuildingBlocks/src/BuildingBlocks.Domain/Rules/RuleChecker.cs:18)). Eine
Factory, die versehentlich `null` liefert, bedeutet „Regel bestanden" — die Validierung schweigt
genau im Fehlerfall. Begründet ist das mit „damit Guard-Klauseln knapp bleiben".

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
Null-Toleranz festschreiben, mitziehen. Kleiner Eingriff, entfernt eine stille Sicherheitslücke
in der Domänenvalidierung.

## Umgesetzt (2026-08-05)

Alle vier Überladungen prüfen ihr Argument mit `ArgumentNullException.ThrowIfNull`; die
`params`-Varianten prüfen zusätzlich das Array selbst. Es gab **keinen** Test, der die alte
Null-Toleranz festschrieb — sechs neue Fakten in `RuleCheckerTests` decken die Lücke jetzt ab,
darunter zwei, die belegen, dass eine `null`-Regel *inmitten* eines `params`-Arrays die
nachfolgenden Regeln gar nicht erst auswertet.

---

# TODO-18, `AddBuildingBlocks` ist nicht idempotent

**P2 · gelöst · hacky-9**

`services.TryAddSingleton(behaviorRegistry)` behält beim zweiten Aufruf die **erste** Registry,
`options` bekommt aber die **zweite**
([ServiceCollectionExtensions.cs:54-58](BuildingBlocks/src/BuildingBlocks.Infrastructure/DependencyInjection/ServiceCollectionExtensions.cs:54)).
Host-eigene Behaviors aus dem zweiten Aufruf schreiben ihre Order in eine verwaiste Instanz und
laufen zur Laufzeit auf Order 0.

Dazu passt: `GetOrder` liefert für Unbekanntes `0`
([PipelineBehaviorRegistry.cs:40](BuildingBlocks/src/BuildingBlocks.Infrastructure/Dispatching/PipelineBehaviorRegistry.cs:40)) —
exakt `LoggingBehaviorOrder`. Ein direkt auf der `IServiceCollection` registriertes Behavior
kollidiert also lautlos mit dem Logging und untergräbt die in IMP-03 hart erkämpfte Reihenfolge.

## Ursprünglicher Lösungsvorschlag

```csharp
var behaviorRegistry = (PipelineBehaviorRegistry?)services
    .FirstOrDefault(d => d.ServiceType == typeof(PipelineBehaviorRegistry))?.ImplementationInstance
    ?? new PipelineBehaviorRegistry();

public int GetOrder(Type closed) =>
    _orders.TryGetValue(Definition(closed), out var order)
        ? order
        : throw new InvalidOperationException($"Behavior '{closed}' hat keine Order.");
```

Dazu ein Test, der `AddBuildingBlocks` zweimal aufruft und die Order des zweiten Behaviors prüft.

## Gelöst — ein Aufruf, und eine Order ist Pflicht (ADR-0027-Amendment, 2026-08-05)

Der Befund war beim Umsetzen **breiter** als beschrieben: nicht nur die `PipelineBehaviorRegistry`,
sondern **drei** geteilte Objekte wurden per `TryAddSingleton` registriert und beim zweiten Aufruf
verworfen — `WolverineWiringSettings` (Persistenz-/Messaging-Selektion) und `DomainEventTypeRegistry`
(`[EventName]`-Namen) ebenso. Ein zweiter Aufruf hat also drei stille Fehler auf einmal erzeugt.

Statt den Zustand über Aufrufe hinweg zu teilen (Vorschlag oben), ist der **zweite Aufruf jetzt ein
Fehler**. Das kostet nichts, was ein Host legitim will: ein Bounded Context hat eine Write-Datenbank
(ADR-0021), Wolverine erlaubt ein `UseWolverine`, und `AddDomainEventsFrom` friert die `Validate`-Phase
ohnehin ein.

- `BuildingBlocksComposition.EnsureSingleCall` setzt einen Marker-Descriptor und wirft, wenn er schon
  da ist; beide öffentlichen Overloads laufen durch `AddBuildingBlocksCore`, also genau eine Stelle.
- Die drei geteilten Objekte werden danach mit `AddSingleton` statt `TryAddSingleton` registriert —
  eine Fremdregistrierung soll knallen, nicht gewinnen.
- `PipelineBehaviorRegistry.GetOrder` wirft für Unbekanntes statt `0` zurückzugeben; `TryGetOrder`
  ist die stille Variante für die Validierung.
- Neue Phase `ValidateBehaviorOrders` **nach** `RegisterCore` scannt die `IServiceCollection` nach
  `IPipelineBehavior<,>`-Descriptors und wirft, wenn der Implementierungstyp der Registry unbekannt
  ist — inklusive Factory-Registrierungen, deren Typ gar nicht inspizierbar ist. Damit wird aus einem
  Laufzeit- ein Startfehler, und `options.AddPipelineBehavior(type, order)` ist der einzige Weg.

Getestet in `CompositionSingleCallTests` (zweiter Aufruf auf `IServiceCollection` und auf dem
Host-Builder, direkt registriertes Behavior, Factory-Registrierung, unbekannte Order, gültiger Pfad).

---

# TODO-19, `ApplyEntityKeyConversions` scannt und mappt zu viel

**P2 · teilweise gelöst · hacky-4 + WS-15**

Zwei Befunde in derselben Schleife
([EntityKeyModelBuilderExtensions.cs](BuildingBlocks/src/BuildingBlocks.Infrastructure/Persistence/EntityKeyModelBuilderExtensions.cs)):

- **Zu viel** (hacky-4): der Scan läuft über CLR-Properties und ruft `entityType.AddProperty(…)` —
  das **legt die Property im Modell an**, wenn sie fehlt. Jede berechnete, get-only oder
  `Ignore()`-te Property vom Key-Typ landet still als Spalte.
- **Zu wenig** (WS-15): Complex Types werden gar nicht erfasst, der Helper kennt nur
  `Model.GetEntityTypes()`. Ein typisierter Schlüssel darin bekäme keinen Konverter und scheiterte
  erst beim Migrieren gegen PostgreSQL.

## Gelöst — der Discovery-Zweig ist weg (2026-08-05, ADR-0033)

> Das „zu viel" ist behoben, indem die Ursache entfernt wurde statt der Symptome.
>
> Erster Anlauf war die Guard-Variante (`IsIgnored` + kein Setter neben dem Navigations-Check,
> vier Zeilen). Sie beseitigte beide Symptome und ließ den Mechanismus stehen — einen
> Reflection-Scan, der ins Modell schreibt. Der Einwand, der sie erledigt hat: **echte Services
> mappen ihren `DbContext` immer explizit**, weil sie ohnehin Spaltennamen, `IsRequired`,
> `IsConcurrencyToken` und für Kinder `OwnsMany`+`HasKey` brauchen. Nachgeprüft: von allen
> Aufrufern mappten nur `EntityKeyConversionTests` und `EntityKeyPersistenceTests` konventionell —
> also genau die zwei Tests, die den Zweig testen sollten. Im Produktivbetrieb war er toter Code.
>
> Jetzt läuft der Helper nur noch über `entityType.GetProperties()` und hängt Konverter an. Er
> kann dem Modell nicht widersprechen, weil er nicht mehr hineinschreibt: `Ignore()` und
> berechnete Properties sind per Konstruktion sicher, die Guards entfallen ersatzlos, ebenso die
> defensive Kopie um die Entity-Type-Schleife.
>
> Preis: ein vergessener typisierter Schlüssel bricht den Modellaufbau. Aber **laut** — EF Core
> nennt Property, Typ und beide Auswege. `AnUnmappedKeyProperty_FailsLoudlyInsteadOfBeingDiscovered`
> hält das fest, `ApplyEntityKeyConversions_ConfiguresConverterForOwnedChildKeys` den Owned-Fall.
> Achtung beim Testschreiben: der InMemory-Provider mappt **jeden** CLR-Typ, das laute Scheitern
> zeigt sich nur relational — die Testkontexte laufen deshalb auf `UseNpgsql` (ohne DB-Zugriff,
> es geht nur um den Modellaufbau).
## Offen: Complex Types (WS-15)

Bewusst **nicht** miterledigt, weil heute kein Anwendungsfall existiert: `ComplexProperty` kommt
im gesamten Repo nicht vor. Auf der **Write**-Seite ist der Weg für Kinder eines Aggregats durch
ADR-0031 auf `OwnsMany` (+ `ToJson()` für identitätslose Werte) festgelegt, und ADR-0025 schließt
Complex Types für den State selbst ausdrücklich aus. Owned Types sind eigene Entity-Types und
damit von der Schleife bereits erfasst. Auf der **Read**-Seite sind die Modelle heute flach.

Der Fix wäre eine Zeile (`entityType.GetComplexProperties()` mit `complex.ComplexType`
nachziehen) und gehört in den Moment, in dem das erste Read-Modell ein eingebettetes Value Object
mit typisiertem Schlüssel bekommt — vorher wäre es ungedeckter Code mit einem konstruierten Test.

---

# TODO-20, Global sequentielle Domain-Event-Queue

**P2 · offen · hacky-13 + IMP-25**

```csharp
options.PublishMessage<DomainEventEnvelope>()
    .ToLocalQueue(DomainEventLocalQueueName).Sequential().UseDurableInbox();
```

([WolverineOptionsExtensions.cs:77-80](BuildingBlocks/src/BuildingBlocks.Infrastructure/DependencyInjection/Wiring/WolverineOptionsExtensions.cs:77))

Sämtliche Domain Events eines Service laufen durch eine strikt sequentielle Queue, um eine
**pro-Aggregat**-Ordnungsgarantie zu erkaufen. Global serialisieren für eine lokale Zusage: der
Durchsatz ist auf ein Event zur Zeit gedeckelt.

## Lösungsvorschlag

Nach Aggregat-Id partitionieren — gleiche Garantie, parallel über verschiedene Aggregate. Setzt
**TODO-02** voraus, weil der Envelope die Aggregat-Identität heute nicht mitführt.

Vor der ersten Lastmessung nicht anfassen — hier steht es, damit die Entscheidung bewusst fällt
statt als Default stehen zu bleiben.

---

# TODO-21, Read-Modelle im state-stored Pfad nicht wiederaufbaubar

**P2 · offen · IMP-31**

ADR-0022 nennt Read-Modelle „abgeleitet und wiederaufbaubar". Für event-sourced Kontexte stimmt
das (Marten-Stream als Quelle). Für state-stored nicht: die Write-Datenbank hält nur den aktuellen
Zustand, und die Domain Events existieren ausschließlich als Outbox-Zeilen, die nach erfolgreicher
Zustellung gelöscht werden. Ein Projektions-Bugfix ist damit nicht auf Altdaten anwendbar.

## Lösungsvorschlag

**Empfehlung: Domain-Event-Journal.** Die Envelopes zusätzlich in eine append-only Tabelle der
Write-DB schreiben, in derselben Transaktion. Kosten: Speicher plus eine Tabelle. Nutzen: echter
Replay, Audit-Trail, und die Vorstufe zu einer späteren ES-Migration des Kontexts — also genau der
in ADR-0025 versprochene Wechsel state-stored ↔ event-sourced.

Die Alternative (Rebuild aus dem aktuellen Zustand) ist billiger, kostet aber einen zweiten
Codepfad pro Read-Modell und trägt nicht für Modelle, die Historie aggregieren („Anzahl
Umbenennungen"). Braucht eine ADR.

## Nachtrag (2026-08-06): Read-Modell-Schema gehört an dieselbe Entscheidung

Aus TODO-49 / ADR-0035 übernommen. Der Schema-Snapshot deckt Domain- und Integration-Events ab,
Read-Modelle bewusst **nicht** — mit genau der Begründung, die dieses TODO bestreitet: sie seien
abgeleitet und wiederaufbaubar, also koste ein Rename dort nichts außer einem Rebuild. Solange
dieses TODO offen ist, stimmt das für state-stored Kontexte nicht: ohne Replay-Quelle ist ein
Read-Modell **kein** abgeleiteter Cache, sondern faktisch die einzige Kopie seiner Daten, und ein
umbenanntes Feld ist ein Datenverlust wie im Event.

Daraus folgt die Reihenfolge, nicht ein zweites Schutzverfahren: Wer dieses TODO löst, macht die
Ausnahme in ADR-0035 nachträglich wahr — dann ist ein Read-Modell wirklich wegwerfbar und braucht
keinen Snapshot. Wer es **nicht** löst, muss die Read-Modelle in den Snapshot aufnehmen (der
Renderer kann das ohne Änderung, er nimmt beliebige Typen). Beides ist vertretbar, aber die
Kombination „kein Replay **und** kein Snapshot" ist die einzige Variante, die still Daten verliert.
Die Entscheidung gehört in die ADR, die dieses TODO ohnehin braucht.

## Nachtrag (2026-08-06): TODO-14 Teil B hängt ebenfalls an dieser Entscheidung

Der zweite Nachzügler. **TODO-14** hat seinen Teil A gelöst (Wolverines Inbox-Idempotenzfenster ist
jetzt eine Entscheidung statt eines 5-Minuten-Defaults) und seinen Teil B — fachliche
Deduplizierung über `IIntegrationEvent.EventId` — bewusst hierhin vertagt. Grund: Der einzige von
Teil A **nicht** gedeckte Fall ist die Republikation eines Ereignisses mit **neuer**
Transportidentität, und die entsteht praktisch erst durch ein Replay. Ein Domain-Event-Journal, wie
dieses TODO es vorschlägt, erzeugt genau das.

Also: Fällt die Entscheidung hier für Replay, wird TODO-14 Teil B fällig — ein Replay ohne
fachliche Dedup beim Konsumenten spielt jedem nachgelagerten Kontext seine Historie ein zweites Mal
ein. Fällt sie dagegen für „Rebuild aus dem aktuellen Zustand", bleibt Teil B dauerhaft unnötig und
sollte gestrichen werden — zusammen mit der `EventId`-Zusage aus ADR-0029, auf die sich dann
niemand verlässt.

---

# TODO-22, Unique-Constraint-Verletzungen werden nicht übersetzt

**P2 · gelöst · IMP-29**

`UnitOfWorkBehavior` fängt `ConcurrencyException` und `DbUpdateConcurrencyException`
([UnitOfWorkBehavior.cs:58-65](BuildingBlocks/src/BuildingBlocks.Infrastructure/Dispatching/UnitOfWorkBehavior.cs:58)).
Eine `DbUpdateException` mit PostgreSQL-Unique-Violation (SQLSTATE `23505`) fällt durch und wird
zur unerwarteten Exception — obwohl „dieser Name existiert bereits" ein erwarteter fachlicher Fall
ist. Der Nutzer bekommt einen 500er statt eines 409ers, und die Error-Metrik zählt einen
Systemfehler.

## Lösungsvorschlag

```csharp
catch (DbUpdateException exception) when (exception.InnerException is PostgresException
    { SqlState: PostgresErrorCodes.UniqueViolation } pg)
{
    return pipeline.Failed(
        Failure.Conflict("persistence.unique_violation", $"'{pg.ConstraintName}' verletzt."));
}
```

Der Constraint-Name gehört in die Meldung, sonst ist der Fehler im Log nicht zuzuordnen. Das
Mapping von Constraint-Namen auf fachliche Codes (`ux_recipes_name` → `recipe.name_taken`) gehört
in den Service, nicht in die Building Blocks — hängt an TODO-15.

## Gelöst (2026-08-07) — und zwar auf beiden Persistenzpfaden

Wichtig für die Nachvollziehbarkeit: Commit `6918414` heißt „Improve unique constraint violation
handling", hat aber **ausschließlich `todo.md` und `Improvements.md`** angefasst und dort nur
`FailureResults.Create<TResponse>` durch `pipeline.Failed` im Vorschlags-Snippet ersetzt
(Nachzug zu TODO-12). Der Produktivcode blieb unverändert. Die Commit-Nachricht las sich, als wäre
der Punkt erledigt — er war es nicht.

Umgesetzt ist jetzt der Vorschlag oben, mit **einer Abweichung**: der Vorschlag deckte nur den
EF-Core-Pfad ab (`DbUpdateException` mit `PostgresException` als `InnerException`). Marten wirft
die `PostgresException` jedoch **nackt**, weil dort kein EF-Core-`SaveChanges` dazwischenliegt.
Da `UnitOfWorkBehavior` von beiden Pfaden geteilt wird, wäre derselbe fachliche Fall je nach Store
einmal 409 und einmal 500 geworden — genau die Asymmetrie, die ADR-0026 an dieser Stelle nicht will.
`UnitOfWorkBehavior` fängt deshalb beide Formen und führt die Erkennung in **einem** Prädikat
(`IsUniqueViolation`) zusammen.

Zwei Details, die beim Nachbauen zählen:

- Die neuen `catch`-Blöcke stehen **nach** `DbUpdateConcurrencyException`. Die ist von
  `DbUpdateException` abgeleitet, ein vorgezogener Block würde den Nebenläufigkeitskonflikt
  fälschlich als Unique-Verletzung melden — der Compiler erzwingt diese Reihenfolge hier nicht,
  weil der neue Block einen `when`-Filter trägt.
- `Failure` verlangt eine nicht-leere Meldung, `ConstraintName` kann aber `null` sein. Ohne den
  Fallback auf die Exception-Meldung wäre aus einem 409 eine `ArgumentException` und damit doch
  wieder ein 500 geworden.

Belegt durch vier neue Tests in `UnitOfWorkBehaviorTests`: EF-Pfad, Marten-Pfad, fehlender
Constraint-Name, und eine Abgrenzung — eine **Foreign-Key**-Verletzung (SQLSTATE `23503`) wird
weiterhin durchgereicht statt still zu einem `Conflict` zu werden.

**Nicht** miterledigt und weiterhin an TODO-15 hängend: das Mapping des Constraint-Namens auf einen
fachlichen Code. Der Aufrufer bekommt heute `persistence.unique_violation` plus den Constraint-Namen
im Klartext.

---

# TODO-23, Keine Tracing-Instrumentierung der CQRS-Pipeline

**P2 · offen · IMP-30**

Verifiziert: **kein einziges `Activity`/`ActivitySource` in `BuildingBlocks/src`**.
`LoggingBehavior` misst die Dauer und loggt sie, erzeugt aber keinen Span. In einem
Aspire-/OpenTelemetry-Setup fehlt damit die Ebene zwischen HTTP/gRPC-Span und Datenbank-Span: man
sieht, dass ein Request 800 ms brauchte, aber nicht, welcher Handler oder welche Projektion.

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

Analog in `ProjectionRunner` und `DomainEventPublisher`; der Service-Default registriert die Quelle per
`AddSource("VitalSync.BuildingBlocks")`. Kleiner Aufwand, hoher Betriebsnutzen — sinnvollerweise
zusammen mit dem ersten produktiven Service.

---

# TODO-24, `DbContext` als DI-Schlüssel

**P2 · gelöst · IMP-20**

## Gelöst — der Schreibkontext hat einen eigenen Schlüssel (2026-08-08)

Der nackte `DbContext`-Eintrag ist ersatzlos weg. `PersistenceRegistrar` registriert stattdessen
einen internen `WriteDbContextAccessor`, der den bei `UseEfCorePersistence<TContext>` genannten
Kontext hält, und `EfCoreRepository` nimmt diesen Accessor statt eines `DbContext`. Der
mehrdeutige Schlüssel existiert damit nicht mehr — er ist nicht bloß besser dokumentiert, sondern
von niemandem sonst besetzbar. Es gab genau einen Konsumenten; `EfCoreUnitOfWork<TContext>` und
`AggregateStateModelCheck<TContext>` waren bereits typisiert.

**Anders als vorgeschlagen:** kein Marker-Interface `IWriteDbContext`. Ein Marker verlangt, dass
jeder Service seinen Write-Kontext davon ableitet — eine Auflage an fremden Code für ein Problem,
das ausschließlich in *unserer* Registrierung entsteht — und erzwingt im Repository einen Cast
zurück auf `DbContext`. Der Accessor löst dasselbe, ohne den Kontexttyp des Hosts anzufassen. Sein
Name endet auf `*Accessor` und nicht auf `WriteDbContext`, weil er ein Halter ist und kein
`DbContext`-Subtyp.

Der Test `WriteDbContextResolutionTests` stellt den Fehlermodus nach, statt nur die Verdrahtung
abzunicken: er registriert **vor** `AddBuildingBlocks` einen Read-Kontext unter dem nackten
`DbContext`-Schlüssel — unter der alten Registrierung hätte `TryAddScoped` damit verloren und das
Repository hätte in die Lesedatenbank geschrieben. Dass der Fremdeintrag wirklich gewinnt, ist
ausdrücklich verankert (`Assert.IsType<ReadProbeContext>(… GetRequiredService<DbContext>())`),
sonst bewiese der Test nichts. Danach wird gezeigt, dass `AddAsync` trotzdem im Write-Kontext
landet und der Read-Kontext leer bleibt. Der Test braucht keinen Container: `context.Add` und der
ChangeTracker arbeiten ohne Verbindung, obwohl Npgsql konfiguriert ist.

Die saubere Variante `EfCoreRepository<TContext, TAggregate, TKey>` bleibt aus dem im Vorschlag
genannten Grund unmöglich.

## Ursprünglicher Befund

```csharp
_services.TryAddScoped<DbContext>(static provider => provider.GetRequiredService<TContext>());
```

([BuildingBlocksOptions.cs:310](BuildingBlocks/src/BuildingBlocks.Infrastructure/DependencyInjection/BuildingBlocksOptions.cs:310))

Jeder Kontext hat laut ADR-0021 ein Write- **und** ein Read-Paar. Der unqualifizierte
`DbContext`-Schlüssel gehört per Konvention dem Write-Kontext — sichtbar ist das nirgends.
Registriert ein Service versehentlich seinen Read-Kontext ebenfalls als `DbContext`, entscheidet
die Registrierungsreihenfolge, in welche Datenbank das Repository schreibt.

## Ursprünglicher Lösungsvorschlag

Die Konvention ins Typsystem heben — die kleinste wirksame Änderung:

```csharp
public interface IWriteDbContext;
```

Die saubere Variante (`EfCoreRepository<TContext, TAggregate, TKey>`) scheitert daran, dass C#
keine partiell geschlossenen offenen Generics registrieren kann — sie bräuchte eine
Factory-Registrierung.

---

# TODO-25, Marten-Nebenläufigkeit verdrahtet, aber unbelegt

**P2 · gelöst · WS-10**

## Gelöst — der Konflikt kommt beim Aufrufer als `Failure.Conflict` an (2026-08-07)

`ConcurrencyConflictScenarioTests` fährt die Kette **als Ganzes** gegen echtes PostgreSQL, für
beide Persistenzpfade: ein Kommando geht über `ISender` hinein, verliert unterwegs das Rennen um
sein Aggregat und kommt als `Result` mit `FailureCategory.Conflict` und dem Code
`persistence.concurrency_conflict` zurück.

Das Verschachteln ist **nicht** getimt, sondern erzwungen: eine Test-Behavior auf
`UnitOfWorkBehaviorOrder + 100` liegt damit **innerhalb** der Unit of Work. Ihr `NextAsync`
kehrt zurück, nachdem der Handler das Aggregat geladen und verändert hat, aber bevor
`UnitOfWorkBehavior` committet — genau dort schreibt sie den Konkurrenten in einem eigenen
DI-Scope. Der Handler bleibt unangetastet; es gibt keinen Delay, keinen Task-Wettlauf und damit
nichts, was auf einem langsamen Runner kippen könnte.

| Pfad         | Konkurrent                            | Was der Verlierer sieht                                      |
| ------------ | ------------------------------------- | ------------------------------------------------------------ |
| State-stored | zweiter `DbContext` schreibt `version` | `UPDATE … WHERE version = <geladen>` trifft 0 Zeilen → `DbUpdateConcurrencyException` |
| Event-sourced | zweite Session hängt an den Stream an | erwartete Stream-Version passt nicht → `ConcurrencyException` |

Beide Ausnahmen fängt `UnitOfWorkBehavior` bereits ab; belegt ist jetzt, dass sie im echten Stack
überhaupt entstehen und die Übersetzung greift.

Der **Negativ-Anker** steckt in der Nachprüfung: nach dem Konflikt wird das Aggregat neu geladen
und auf den Stand des *Gewinners* geprüft (Name bzw. Zählerstand, und Version 2). Ohne diese
Zusicherung bliebe der Test auch dann grün, wenn beide Schreiber durchkämen und der Konflikt aus
einer ganz anderen Ecke stammte.

`MirrorWidgetTests` bleibt unverändert und wird **nicht** ersetzt: der dort gemockte
`Failure.Conflict` prüft eine andere Frage — dass ein Consumer bei einem fehlgeschlagenen
Kommando wirft, statt die Nachricht stillschweigend als erledigt zu quittieren. Nur als *Beleg
für die Nebenläufigkeit* war er ungeeignet.

Die state-stored Hälfte war mit TODO-02 bereits auf Unit-of-Work-Ebene abgedeckt
(`EfCoreAggregateRoundTripTests.TwoConcurrentRenames_LetTheSecondCommitFailAsAConflict`); neu ist,
dass beide Pfade den Weg **bis zum Aufrufer** gehen.

## Ursprünglicher Befund

Der Repository-Pfad hängt mit erwarteter Version an
([MartenUnitOfWork.cs:54](BuildingBlocks/src/BuildingBlocks.Infrastructure/Persistence/EventSourced/MartenUnitOfWork.cs:54)),
und ein BuildingBlocks-Integrationstest deckt die Versionsarithmetik ab. Dass ein echter Konflikt
als `FailureCategory.Conflict` beim **Aufrufer** ankommt, prüft kein Szenario:
`MirrorWidgetTests` arbeitet mit einem gemockten `Failure.Conflict`
([MirrorWidgetTests.cs:73](samples/EventSourced/VitalSync.Sample.EventSourced.Tests/MirrorWidgetTests.cs:73)).

## Ursprünglicher Lösungsvorschlag

Ein Szenario-Test gegen den echten Stack (Testcontainers, wie die übrigen).

Sichert die Kette Marten → `ConcurrencyException` → `UnitOfWorkBehavior` → `Result` als Ganzes ab.
Ohne ihn ist nur jedes Glied einzeln belegt. Sinnvollerweise zusammen mit TODO-02, das denselben
Test für den state-stored Pfad braucht.

---

# TODO-26, Typisierte Schlüssel serialisieren `IsEmpty` in den Eventstrom

**P2 · gelöst · WS-09**

## Gelöst — ein Schlüssel serialisiert als nackter Wert (ADR-0034, 2026-08-06)

`EntityKeyJsonConverterFactory` erzeugt für jedes `IEntityKey<TValue>` einen Converter, der den
Schlüssel als **nackten Wert** schreibt (`"GadgetId": "8f3a…"`) und ihn über denselben
Ein-Argument-Konstruktor zurückliest, den der EF-Core-Value-Converter ohnehin verlangt; beide
teilen sich `EntityKeyActivator<TKey, TValue>`.

`EntityKeyJsonOptions` ist die **eine** Stelle, die den Factory-Converter anhängt, und sie wird an
allen drei JSON-Pfaden angewandt, die Building Blocks besitzt:

| Pfad                            | Verdrahtung                                                       |
| ------------------------------- | ----------------------------------------------------------------- |
| Outbox-Payload                  | `DomainEventEnvelopeSerializer.SerializerOptions`                 |
| Marten-Eventstrom               | `UseSystemTextJsonForSerialization` in `PersistenceRegistrar`     |
| Integration-Event-Body (RabbitMQ) | `UseSystemTextJsonForSerialization` in `BuildingBlocksWolverineExtension` |

Dass Marten dabei auf System.Text.Json umgestellt wird, ist Teil der Entscheidung: das im
ursprünglichen Vorschlag genannte `[JsonIgnore]` bindet an **einen** Serializer, und Martens
Default war der andere — das Attribut wäre ausgerechnet dort wirkungslos geblieben, wo die
unveränderlichen Daten liegen.

Gelesen wird **nur** das neue Format. Ein Toleranz-Zweig für die alte Objektform würde sie
dauerhaft machen; produktive Streams gibt es nicht.

Belegt durch `EntityKeyJsonConverterTests` (Converter), `DomainEventEnvelopeSerializerTests`
(Outbox-Payload) und `EntityKeyEventStreamFormatTests`, das die rohe `mt_events.data`-Spalte gegen
einen echten PostgreSQL-Container prüft.

## Ursprünglicher Befund

`WidgetId`/`GadgetId` implementieren `IsEmpty` als berechnetes Member
([WidgetId.cs:7](samples/StateStored/VitalSync.Sample.StateStored.Domain/WidgetId.cs:7)),
verifiziert existierte **kein einziges `[JsonIgnore]`** im Repository. Im Eventstrom stand damit
`"GadgetId": {"Value": "…", "IsEmpty": false}`. Events sind unveränderlich — eine dauerhafte
Entscheidung, die unbemerkt getroffen wurde.

## Ursprünglicher Lösungsvorschlag

In BuildingBlocks lösen, nicht am Sample, sonst muss jeder Schlüsseltyp daran denken:

```csharp
public interface IEntityKey
{
    [JsonIgnore] bool IsEmpty { get; }
}
```

Sauberer: ein `JsonConverter` für `IEntityKey<TValue>`, der den Schlüssel als **nackten Wert**
schreibt (`"GadgetId": "8f3a…"`). Halbiert die Streamgröße und macht Events von Hand lesbar.
**Solange keine produktiven Streams existieren, ist das gratis** — danach eine Event-Migration.
Deshalb P2 trotz geringer akuter Wirkung.

---

# TODO-27, Schema-Erzeugung zur Laufzeit in Produktion

**P2 · offen · WS-11**

Der event-sourced MigrationService migriert **nur** den Read-Kontext
([Program.cs:21-22](samples/EventSourced/VitalSync.Sample.EventSourced.MigrationService/Program.cs:21)).
Die Write-Seite baut ihr Schema zur Laufzeit selbst — Marten und Wolverine tun das beide. Im
Sample unauffällig; in Produktion heißt es, dass eine Datenbank ihr Schema beim ersten Start eines
neuen Deployments ändert, ohne dass jemand es freigegeben hat.

## Lösungsvorschlag

```csharp
options.AutoCreateSchemaObjects = AutoCreate.None;
```

Gehört in denselben ADR wie die Frage, wie die Wolverine-Tabellen in Produktion entstehen — beide
Stores haben dasselbe Muster und sollten dieselbe Antwort bekommen. Spätestens beim ersten echten
event-sourced Service fällig.

## Nachtrag (2026-08-06): `AutoProvision` gehört hierher

Aus TODO-28 übernommen. `ApplyBuildingBlocksMessagingDefaults` ruft `UseRabbitMq(...).AutoProvision()`
auf, legt also Exchange, Queues und Bindings beim Start selbst an, wenn sie fehlen. Das ist
dieselbe Klasse wie die beiden Punkte oben, nur auf dem Broker statt in der Datenbank: eine
Komponente ändert beim ersten Start eines neuen Deployments fremde Infrastruktur, ohne Freigabe.

Der Broker hat dabei sogar den unangenehmeren Fall — der Typ einer Queue steht bei der Deklaration
fest. Hält der Broker noch eine klassische Queue desselben Namens, scheitert `AutoProvision` und
die Queue muss von Hand gelöscht werden (ADR-0023, Nachtrag 2026-08-04). Ein Deployment, das
Infrastruktur anlegen darf, kann sie also auch blockieren.

Die drei Fälle brauchen **eine** Antwort, nicht drei: wer legt Schema und Topologie an, wann, und
was macht der Service, wenn er sie beim Start nicht vorfindet. Kandidat ist der jeweilige
MigrationService-Worker (TODO-46), der die Datenbanken ohnehin schon migriert.

---

# TODO-28, Restliche Messaging-Guard-Rails

**P2 · gelöst · IMP-13 + WS-06**

Der große Teil ist erledigt: `BuildingBlocksWolverineExtension` wendet die passende Kombination
automatisch an, die `Apply*`-Methoden sind `internal`, ein Startup-Validator prüft `UseWolverine`,
und eine Subscription ohne Transport wird abgelehnt.

**Offen** bleiben vier Punkte, davon einer mit Zähnen: ein registrierter **Mapper ohne Transport**
bedeutet, dass jedes gemappte Event im `NullIntegrationEventSink` landet und nur eine Warning
erzeugt. Für Commands und Queries ist genau diese Fehlerklasse längst ein Startfehler (WS-06).

## Lösungsvorschlag

```csharp
if (mapperRegistriert && WolverineWiring.RabbitMqUri is null && !_noMessagingSelected)
    throw new InvalidOperationException(
        "Integration-Event-Mapper registriert, aber kein Transport konfiguriert.");
```

Für **Projektionen** bewusst nichts tun: mehrere Handler pro Event und Events ganz ohne Projektion
sind beide legitim. Die Begründung gehört nach `docs/architecture/cqrs-and-event-sourcing.md`, damit
die Asymmetrie nicht später als Lücke missverstanden wird.

## Gelöst (2026-08-06): der Punkt mit den Zähnen

`IntegrationEventMapperCheck` scheitert beim Start und nennt die Mapper. Zwei Abweichungen vom
Vorschlag oben, beide bewusst:

**Der Check fragt nach der Wirkung, nicht nach der Auswahl.** Nicht „`UseWolverineMessaging` wurde
nicht aufgerufen", sondern „es sind Mapper registriert **und** die aufgelöste
`IIntegrationEventSinkFactory` ist immer noch die Null-Variante". Beide Formulierungen fangen den
echten Fehler, aber nur die erste lässt einen Host durch, der seine Sink-Factory selbst stellt —
und genau das tut `IntegrationEventSinkDeliveryTests`. Die Wiring-Variante hätte einen grünen Test
rot gemacht, obwohl dort nichts still verschwindet. Dasselbe Muster wie bei
`UnitOfWorkPresenceCheck`, der `IUnitOfWork` gegen `NullUnitOfWork` prüft statt die
Persistenzauswahl zu lesen.

**Kein `UseNoMessaging()`.** Der Vorschlag sah es vor; es kommt nicht. ADR-0027 (Nachtrag
2026-08-05) verbietet Escape-Hatches an `BuildingBlocksOptions` grundsätzlich, und hier fehlt der
Anlass ohnehin: `UseNoPersistence()` existiert, weil „dieser Host committet bewusst nichts" eine
Absicht ist, die man dem Code nicht ansieht. „Dieser Host publiziert nichts" sieht man ihm an — es
gibt keinen Mapper. Der Ausweg aus dem Check ist, den toten Mapper zu löschen.

Die Asymmetrie zu den Projektionen steht jetzt in `cqrs-and-event-sourcing.md`, wie oben verlangt.
Belegt durch `IntegrationEventMapperCheckTests` (4 Tests, darunter der Host mit eigener
Sink-Factory).

## Gelöst (2026-08-06): die differenzierte Retry-Policy

Der letzte Punkt mit Substanz ist umgesetzt. `ApplyBuildingBlocksMessagingDefaults` registriert drei
Regeln statt einer; Wolverine nimmt die erste, die passt:

| Klasse | Erkannt an | Politik |
| --- | --- | --- |
| Hoffnungslos | `JsonException`, `DomainValidationException`, `BusinessRuleViolationException` | sofort Dead-Letter, kein Retry |
| Transient | `NpgsqlException` mit `IsTransient`, `TimeoutException` | 1 s / 5 s / 15 s / 30 s, **kein** Dead-Letter |
| Unbekannt | jede andere `Exception` | unverändert 100 ms / 500 ms / 2 s, dann Dead-Letter |

Der eigentliche Gewinn ist nicht die gesparte Wartezeit, sondern die **Fehlermetrik**: ein
deterministischer Fehler erzeugte vier Error-Logs, wo einer die Wahrheit ist — mal vier auf jedem
Alert-Schwellwert. Umgekehrt überlebt ein DB-Failover die 2,6 s der alten Leiter nie und landete in
der DLQ, obwohl er sich erholt. Genau deshalb endet die transiente Klasse **nicht** in
`MoveToErrorQueue`: die Nachricht bleibt auf der Queue und wird erneut zugestellt. Sicher ist das
nur wegen des 7-Tage-Idempotenzfensters aus TODO-14 — beide Punkte beschreiben dieselbe
Betriebsrealität von zwei Seiten.

Die transiente Regel matcht das **Prädikat** `IsTransient`, nicht den Typ: eine `PostgresException`
mit Unique-Violation (`23505`) ist keine transiente Störung und fällt bewusst in die unbekannte
Klasse, denn ihre Übersetzung nach `Failure.Conflict` ist TODO-22 und darf hier nicht vorweggenommen
werden. Die Zahlen der unbekannten Klasse bleiben, damit die vier Versuche in `DeadLetterTests` ihre
gepinnte Bedeutung behalten.

Zur zweiten Frage des offenen Punkts — ob Wolverines Broker-DLQ die richtige Endstation ist: ja, sie
bleibt. Sie ist durabel und quorum-repliziert (ADR-0023, Nachtrag 2026-08-04), und ein Requeue von
dort ist wegen des Idempotenzfensters gefahrlos. Die Alternative, eine eigene Tabelle, hätte nur den
Vorteil einer SQL-Abfrage — und den Nachteil, den Broker-Zustand zu duplizieren.

Belegt durch den neuen `ADeterministicFailure_IsDeadLetteredWithoutBeingRetried` neben dem
bestehenden Vier-Versuche-Test. Beide deklarieren jetzt eine **pro Lauf eindeutige Queue**: mehrere
Probe-Queues binden dasselbe Pattern `upstream.*`, und eine wiederverwendete dauerhafte Queue sammelt
Nachrichten früherer Tests ein — das hat den bestehenden Test einmalig rot gemacht.

## Offen

Nichts mehr in diesem TODO. Verbleibende Randnotizen:

- **Umgebungsabhängiges `AutoProvision`** ist nach **TODO-27** verschoben. Es ist derselbe Fall wie
  Martens `AutoCreateSchemaObjects`: eine Komponente, die beim ersten Start eines Deployments
  fremde Infrastruktur anlegt, ohne dass jemand es freigegeben hat. Beide gehören in dieselbe
  Entscheidung und dieselbe ADR, statt zweimal halb beantwortet zu werden.
- **`UseNoMessaging`** ist entschieden (kommt nicht, Begründung oben) und damit erledigt.

---

# TODO-29, `DomainEventPublisher` koppelt Projektion und Integration-Publikation

**P3 · offen · IMP-26**

Zwei Belange in einer Methode ohne Fehlerisolierung
([DomainEventPublisher.cs:32-40](BuildingBlocks/src/BuildingBlocks.Infrastructure/Messaging/DomainEvents/DomainEventPublisher.cs:32)):
wirft eine Projektion, wird kein Integration Event publiziert; wirft ein Mapper, laufen bei der
Redelivery alle Projektionen erneut. Bei at-least-once heißt das, dass ein Fehler auf der einen
Seite die andere wiederholt ausführt — tragfähig nur, solange beide idempotent sind.

## Lösungsvorschlag

**Empfehlung: ein Handler, aber getrennte Fehlerbehandlung** mit ausdrücklicher Reihenfolge (erst
Projektionen, dann Integration Events). Voraussetzung ist die Idempotenz beider Seiten.

Zwei getrennte Wolverine-Handler auf demselben Envelope wären sauberer isoliert, kosten aber eine
zweite Zustellung pro Event — erst wenn beide Seiten messbar unterschiedliche Fehlerraten haben.

---

# TODO-30, `IIntegrationEventMapper` ist untypisiert

**P3 · offen · IMP-12**

`IReadOnlyCollection<IIntegrationEvent> Map(IDomainEvent domainEvent)` — jeder Mapper wird für
**jedes** Domain Event aufgerufen und muss selbst per `switch` filtern, während das benachbarte
`IProjectionHandler<in TDomainEvent>` typisiert ist und gezielt aufgelöst wird. Zwei funktional
analoge Konzepte, gegensätzlich entworfen.

## Lösungsvorschlag

```csharp
public interface IIntegrationEventMapper<in TDomainEvent>
    where TDomainEvent : IDomainEvent
{
    IReadOnlyCollection<IIntegrationEvent> Map(TDomainEvent domainEvent);
}
```

Registrierung über denselben `MultiHandlerInterfaceDefinitions`-Pfad wie `IProjectionHandler<>`,
im `DomainEventPublisher` ein `MapperRunner` als Zwilling des `ProjectionRunner`. Nebeneffekt: der
`_ => []`-Default-Arm entfällt, und „welche Events verlassen diesen Kontext" wird an der
Typsignatur ablesbar statt im `switch` versteckt.

---

# TODO-31, `Result` hat keine Kombinatoren

**P3 · offen · IMP-34**

Kein `Map`, `Bind`, `Match`, `Tap`, `Ensure`. Jeder mehrstufige Handler schreibt dieselbe
`if (x is null) return Failure...`-Treppe.

## Lösungsvorschlag

Sparsam beginnen — `Match` ist der wertvollste, er ersetzt die
`IsSuccess ? … : throw ToRpcException(…)`-Zeilen in jedem gRPC-Adapter:

```csharp
public static TOut Match<TIn, TOut>(
    this Result<TIn> result, Func<TIn, TOut> onSuccess, Func<IReadOnlyList<Failure>, TOut> onFailure) =>
    result.IsSuccess ? onSuccess(result.Value) : onFailure(result.Failures);
```

Dazu `Map` und `Bind`. Als Extensions in `BuildingBlocks.Application`, damit `Result` selbst
schlank bleibt.

---

# TODO-32, Async-Suffix ist inkonsistent

**P3 · gelöst (2026-08-05) · IMP-37**

`Handle` ohne Suffix in `ICommandHandler`, `IQueryHandler`, `IProjectionHandler`,
`IPipelineBehavior` — dagegen `GetByIdAsync`, `AddAsync`, `CommitAsync`, `PublishAsync` mit
Suffix. Alle geben `Task` zurück.

## Lösungsvorschlag

**Suffix überall** — es ist die .NET-Konvention, und die Ports folgen ihr bereits; die Handler
sind die Ausnahme, nicht die Regel:

```csharp
Task<Result> HandleAsync(TCommand command, CancellationToken cancellationToken);
```

Breaking Change über alle Handler, aber rein mechanisch und ohne Verhaltensänderung. **Jetzt
billig, mit dem ersten Produktions-Service teuer** — deshalb P3 statt P4. Entscheidung in die
`.editorconfig`-Konventionen bzw. eine kurze ADR.

## Umgesetzt (2026-08-05)

`ICommandHandler`, `IQueryHandler`, `IProjectionHandler` und `IPipelineBehavior` heißen jetzt
`HandleAsync`, `ISender` heißt `SendAsync`. 33 Dateien, rein mechanisch — der Compiler war das
Gerüst: erst die vier Verträge umbenennen, dann jede Bruchstelle abarbeiten.

**Eine Falle, die der Compiler *nicht* zeigt:** Wolverine entdeckt Handler über den
**Methodennamen**. `DomainEventEnvelopeHandler.Handle` und der Sample-Consumer
`WidgetCreatedConsumer.Handle` implementieren **kein** Building-Blocks-Interface, sondern erfüllen
Wolverines Konvention — sie behalten `Handle` und wurden nach einem versehentlichen Rename wieder
zurückgesetzt. Wolverine akzeptiert zwar auch `HandleAsync`, aber die Konvention eines fremden
Frameworks ist kein Ort für unsere Namenspolitik. Regel für die Zukunft: **nur Methoden umbenennen,
die einen unserer Verträge implementieren.** Ebenfalls geprüft: Es gibt keinen
Reflection-Zugriff auf den Namen `"Handle"`, der still gebrochen wäre.

---

# TODO-33, Ein Assembly für alle Persistenz-Pakete

**P3 · offen · IMP-19**

`BuildingBlocks.Infrastructure` referenziert 11 Pakete, darunter Marten, EF Core, Npgsql und fünf
WolverineFx-Pakete. Ein rein state-stored Service zieht Marten mit, ein event-sourced Service
EF Core — und jedes Major-Upgrade betrifft alle Services gleichzeitig.

## Lösungsvorschlag

```
BuildingBlocks.Infrastructure            → Dispatching, Events, DI-Kern (keine Store-Pakete)
BuildingBlocks.Infrastructure.EfCore     → EF-Repository/UnitOfWork/Tracker, EntityKey-Konverter
BuildingBlocks.Infrastructure.Marten     → Marten-Repository/UnitOfWork/Tracker, EntityKeyFormatter
BuildingBlocks.Infrastructure.Wolverine  → Envelope, Handler, Sink, Wolverine-Extensions
```

Der Schnitt ist durch die Ordnerstruktur bereits vorgezeichnet, es wäre eine reine
Projektverschiebung. **Auslöser für die Umsetzung:** der erste Produktions-Service, der nur eine
der beiden Persistenzwelten braucht. Vorher ist der Aufwand höher als der Gewinn.

---

# TODO-34, Keine zentrale Paketverwaltung

**P3 · offen · IMP-47**

Verifiziert: kein `Directory.Packages.props`. Versionen stehen einzeln in den `.csproj`-Dateien;
`xunit.v3` etwa ist an sechs Stellen mit `3.2.2` gepflegt. Ein übersehenes Projekt
erzeugt eine Laufzeit-Bindungsdiskrepanz statt eines Build-Fehlers.

## Lösungsvorschlag

```xml
<Project>
  <PropertyGroup><ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally></PropertyGroup>
  <ItemGroup>
    <PackageVersion Include="Marten" Version="9.20.1" />
    <PackageVersion Include="WolverineFx.RabbitMQ" Version="6.23.0" />
  </ItemGroup>
</Project>
```

Rein mechanisch, keine Verhaltensänderung, bei 34 Projekten schon spürbar. Guter Kandidat für den
nächsten Aufräum-Commit — sinnvollerweise gebündelt mit TODO-41 bis TODO-43.

---

# TODO-35, `EntityFrameworkCore.Design` verträgt kein `PrivateAssets`

**P3 · offen · WS-04**

Das Design-Paket steht ohne `PrivateAssets` in beiden Sample-Infrastructure-Projekten. Mit
`PrivateAssets="all"` kappt es die transitive Kante zu `EntityFrameworkCore.Relational`, und
Konsumenten scheitern zur Laufzeit mit `FileNotFoundException` — die Standardempfehlung für
Design-Pakete ist hier also aktiv falsch, was niemand erwartet.

## Lösungsvorschlag

Die Design-Time-Factories dorthin verschieben, wo das Paket hingehört, und den MigrationService als
Startup-Projekt scaffolden:

```bash
dotnet ef migrations add Xyz \
  --project samples/StateStored/VitalSync.Sample.StateStored.Infrastructure \
  --startup-project samples/StateStored/VitalSync.Sample.StateStored.MigrationService
```

Damit verschwindet die Design-Referenz aus dem Projekt, das von anderen referenziert wird — die
Ursache, nicht das Symptom.

---

# TODO-36, Der gRPC-Vertrag liegt noch beim Service

**P3 · offen · WS-07**

`VitalSync.Sample.StateStored.Contracts` ist die richtige Struktur, aber der BFF konsumiert heute
noch nichts ([Program.cs](src/Bff/VitalSync.Bff/Program.cs) hat nur Controller). Für das
Integration Event ist die Frage mit Etappe 3 beantwortet (`VitalSync.Sample.Contracts`), für den
gRPC-Vertrag nicht.

## Lösungsvorschlag

Dieselbe Antwort wie beim Integration Event, sobald der zweite Konsument existiert: ein eigenes
Contracts-Projekt pro Bounded Context, das Service **und** BFF referenzieren.

Nicht vorwegnehmen — die Entscheidung gehört an den Tag, an dem der BFF den ersten Service
aufruft, und dann in einen ADR, zusammen mit der bisher nur faktisch getroffenen Bibliothekswahl
`protobuf-net.Grpc`.

---

# TODO-37, Zeitbasierte Assertionen in Tests

**P3 · gelöst · WS-17**

`IntegrationEventSinkDeliveryTests` ruft die Produktionsmethode auf, und beide Hälften des
Negativtests hängen seit dem CI-Flake-Fix (2026-08-07) an Signalen statt an Fristen:

- Die **Vorbedingung** „der abstürzende Mapper ist gelaufen" wartet auf ein
  `TaskCompletionSource` im `SinkProbeCrashSwitch` statt auf Wolverines Stille-Erkennung.
- Die **Negativassertion** benutzt ein **Sentinel**: nach dem Crash-Envelope wird der Switch
  abgeschaltet und ein zweiter, gesunder Envelope geschickt, auf dessen Zustellung deterministisch
  gewartet wird. Danach lautet die Prüfung `Assert.Equal(["sentinel"], delivered)` — der Test sagt
  jetzt „genau der Sentinel kam an, das Crash-Event nicht" statt „innerhalb von 250 ms kam nichts".
  Das ist zugleich eine stärkere Aussage: vorher hätte auch ein komplett toter Zustellpfad den Test
  grün gemacht.

In `DeadLetterTests` ist der 500-ms-Puffer vor „es gab keinen Retry" **ersatzlos entfallen**. Die
Ankunft in der Dead-Letter-Queue ist bereits der terminale Zustand: Wolverine verschiebt erst,
nachdem der Handler endgültig aufgegeben hat, also ist `recorder.Attempts` zu diesem Zeitpunkt
final. Der Delay hat nie etwas abgesichert, was der Anker davor nicht schon garantiert.

## Was bewusst zeitbasiert bleibt

Zwei Kategorien, die kein Defekt sind, und eine dritte, die eine echte Zustellunsicherheit abfängt:

1. **Polling-Intervalle innerhalb einer Schleife mit Deadline** (`WaitForDeadLetterAsync`,
   `WaitForMessageAsync`). Sie prüfen eine Bedingung und scheitern nach einem Timeout mit Meldung —
   das ist die korrekte Bauform, nicht die kritisierte.
2. **Die Publish-Schleifen in `IntegrationEventSubscriptionValidationTests`.** Sie sind kein
   Zeitpuffer, sondern ein **Retry gegen eine reale Zustellunsicherheit**. Ein Umbau auf einmaliges
   Publish wurde am 2026-08-07 zweimal versucht und beide Male empirisch widerlegt:

   - **Versuch 1** — einmaliges Publish plus explizites `QueueBindAsync`: beide Publish-Tests rot.
   - **Versuch 2** — zusätzlich der Test-Publisher auf den Produktionspfad umgestellt
     (`AddBuildingBlocks` mit `UseWolverineMessaging`, also ein **durabler** Sending-Endpoint mit
     Message-Store statt des vorherigen buffered Endpoints ohne Persistenz): in 5 Läufen 3× rot,
     wechselnd der eine oder der andere Test, jeweils 30-s-Timeout.

   Belegt ist dabei, dass das explizite Binding **nötig, aber nicht hinreichend** ist: ohne
   `QueueBindAsync` waren 2 von 3 Läufen rot, mit ihm immer noch 3 von 5. Wolverine kehrt also aus
   `StartAsync` zurück, bevor das Exchange-zu-Queue-Binding am Broker steht — und selbst danach
   erreicht ein einzelnes Publish den Consumer nicht verlässlich. Die verbleibende Ursache ist
   **nicht** geklärt; „buffered vs. durable" allein erklärt sie nachweislich nicht.

   Die Schleife publiziert deshalb weiter, bis der Anker (`control` bzw. der erste Handler-Aufruf)
   eintrifft. Wer das erneut angeht, braucht zuerst eine Erklärung für den Befund aus Versuch 2 —
   nicht eine weitere Vermutung.

3. **`WaitForTheQueueToDrainAsync`** im Selbstkonsum-Test, der nach `MessageCount == 0` noch 500 ms
   puffert. Der Test ist **nicht** allein zeitbasiert: er publiziert neben der zu unterdrückenden
   Nachricht eine `control`-Nachricht aus einem fremden Kontext und verankert die Negativassertion
   an deren Ankunft (`Assert.Contains("control", …)`). Der Drain ist die zusätzliche Absicherung,
   nicht die einzige.

## Nachtrag 2026-08-07 (nachmittags): die fehlende Erklärung — und damit sind Punkt 2 und 3 hinfällig

Die oben eingeforderte Erklärung für Versuch 2 liegt jetzt vor, und sie hat nichts mit dem Binding
oder mit buffered/durable zu tun. **Alle Broker-Tests teilten sich eine einzige Exchange**
(`TestMessaging.ExchangeName`). Eine Topic-Exchange fächert eine Publikation an *jede* passende
Queue aus — unter **einer** Envelope-Id. Alle Probe-Hosts hängen über
`[Collection(BrokerAndDatabaseCollection.Name)]` am selben Postgres-Container und damit an
derselben `wolverine_incoming_envelopes`. Also gewinnt genau **ein** Host das Rennen, verarbeitet
die Nachricht, und die durable Inbox verwirft alle übrigen Kopien als Duplikate — dieselbe
Deduplizierung, die TODO-14 Teil A als Feature beschreibt.

Das erklärt jeden Einzelbefund: dass ein Publish „verloren" ging, obwohl das Binding stand; dass
mal der eine, mal der andere Test rot war (wer gewinnt, ist Scheduling); und warum Versuch 2 es
**verschlimmerte** — der durable Sending-Endpoint schrieb den Message Store, also die geteilte
Inbox, überhaupt erst konsequent. Reproduziert wurde es durch zwei **neue** Tests
(`DeadLetterTests.ATransientFailure_…`, `InboxDeduplicationTests`), die sofort und deterministisch
mit „the consumer never saw the message" fielen, obwohl parallel im Log ein *fremder* Queue-Name
dieselbe Nachricht verarbeitete.

Behoben durch `TestMessaging.UniqueExchangeName(prefix)`: eine eigene Exchange pro Test, damit
existiert der Fan-out nicht mehr. Damit ist der Umbau, der zweimal scheiterte, jetzt umgesetzt:

- Die **Publish-Schleifen sind ersetzt** durch je ein einziges Publish. Der Anker ist nicht mehr
  „irgendwann kam control an", sondern die **FIFO-Ordnung**: suppressed und control gehen über
  dieselbe Exchange in dieselbe Queue, also ist die suppressed-Nachricht nachweislich zugestellt
  und verworfen, sobald control ankommt.
- **`WaitForTheQueueToDrainAsync` ist ersatzlos gelöscht.** Die Heuristik `MessageCount == 0` plus
  500 ms Puffer sagte ohnehin nichts über *verarbeitete* Nachrichten aus. Geblieben ist ein kurzes
  Nachlauffenster, das die Negativassertion wiederholt prüft.
- **`DeadLetterTests` liest die Dead-Letter-Queue nicht mehr destruktiv** (`autoAck: false`, danach
  `BasicNack` mit `requeue: true`). Die DLQ ist plattformweit geteilt; der vorherige
  `autoAck: true`-Drain löschte die Beweise der jeweils anderen Tests.
- Die **exakte** Attempt-Zahl (`Assert.Equal(4, …)`) ist eine untere Schranke geworden. Vier ist die
  Zahl der Policy-Versuche, nicht die der Handler-Läufe: ein Redelivery erhöht sie legitim.

Lehre, die über diesen Fall hinausgeht: die zwei vorherigen Versuche scheiterten nicht am
Lösungsansatz, sondern daran, dass die *Testumgebung* eine Kopplung enthielt, die niemand als
Testkopplung gelesen hat — sie sah aus wie Produktionsverhalten. „Einmal publizieren reicht nicht"
war die richtige Beobachtung mit der falschen Ursache.

Grundsätzlich gilt: „es kommt nie mehr etwas" ist in einem verteilten System ohne Zeitfenster nicht
entscheidbar. Erreichbar ist nur, das Fenster an einen beobachtbaren terminalen Zustand zu hängen
(DLQ-Ankunft, Sentinel-Zustellung) — genau das ist oben passiert, wo es ging.

## Sample-Smoke-Tests (2026-08-07 geprüft)

Die ursprüngliche Vermutung, hier lägen weitere Frist-Assertionen, war **falsch**: alle drei
`Task.Delay`-Stellen in den Smoke-Tests sind Polling-Intervalle innerhalb einer Schleife mit
Deadline und Fehlermeldung, also Kategorie 1 oben.

Der tatsächliche Defekt lag woanders. `CrossContextSmokeTests.RenamingTheMirroredGadget_DoesNotTravelBack`
prüfte **unmittelbar** nach dem Rename, dass das Widget unverändert ist — ohne jeden Anker. Da im
gesamten `samples/`-Baum nur **eine** `SubscribeToIntegrationEvents` existiert (im EventSourced-Sample)
und StateStored damit gar nichts abonniert, konnte der Test per Konstruktion nie fehlschlagen.

Er hat jetzt einen Anker über den nachweislich funktionierenden Vorwärtspfad: nach dem Rename wird ein
zweites Widget angelegt und dessen Spiegelung abgewartet. Erst danach wird das erste Widget geprüft. Ein
Rückkanal hätte in derselben Zeit zugestellt.

**Korrektur 2026-08-07 (nachmittags):** Dieser Anker war falsch konstruiert. Der Sentinel lief über
StateStored → EventSourced, also über **dieselbe Richtung, die ohnehin funktioniert**; über die
Laufzeit des Rückkanals EventSourced → StateStored sagt er nichts, denn beide Wege teilen weder
Queue noch Consumer. „Ein Rückkanal hätte in derselben Zeit zugestellt" war eine Annahme, keine
Ableitung. Da im `samples/`-Baum weiterhin nur eine einzige `SubscribeToIntegrationEvents` existiert,
ist die belastbare Aussage ohnehin **strukturell**: der StateStored-Kontext hat keinen Consumer, also
kann kein Ereignis zurückreisen. Genau das prüft jetzt
`SampleRegistrationTests.Infrastructure_SubscribesToNothing_…` reflektiv über die
Infrastructure-Assembly — ein Test, der beim Hinzufügen eines Consumers rot wird, statt erst dann,
wenn dieser Consumer auch noch zufällig schnell genug ist. Der Smoke-Test behält daneben ein
Beobachtungsfenster von 5 s, das durchgehend prüft, dass Name und `RenameCount` unverändert bleiben;
er ist damit ehrlich als das ausgewiesen, was er ist — eine Plausibilitätsprüfung ohne Anker, deren
Aussagekraft der strukturelle Test trägt.

## Der behobene Flake (2026-08-07)

`MapperFailingAfterSinkPublish_HoldsTheIntegrationEventBack` war über Wochen der **einzige** rote
Test im CI — immer genau einer, immer dieselbe Assembly. Er prüfte direkt nach
`PublishMessageAndWaitAsync` einen booleschen Zustand. Auf dem 2-Kern-Runner konnte das Tracking
zurückkehren, bevor der Handler gelaufen war; die Vorbedingung schlug dann fehl, obwohl das
Verhalten korrekt war. Lokal (32 Kerne) war der Flake auch unter künstlicher Volllast in 16 Läufen
nicht reproduzierbar.

## Queue-Namen pro Lauf (2026-08-07)

Alle Broker-Testklassen teilen sich über `[Collection(BrokerAndDatabaseCollection.Name)]` denselben
RabbitMQ- und Postgres-Container, und die Queues sind durable mit `autoDelete: false` — sie überleben
das Testende und sammeln weiter Nachrichten. Mehrere Klassen banden dasselbe Pattern (`upstream.*`,
`probe.*`) auf **fest benannte** Queues, obwohl die Konvention „eine eigene Queue pro Lauf" bereits
dokumentiert war; umgesetzt war sie nur in `DeadLetterTests`.

Der gemeinsame Helfer heißt jetzt `TestMessaging.UniqueQueueName(prefix)` und wird in
`DeadLetterTests`, `IntegrationEventRoutingTests`, `IntegrationEventDurabilityTests` und
`IntegrationEventSubscriptionValidationTests` benutzt. Wichtig dabei: xUnit erzeugt **pro Test** eine
neue Klasseninstanz, ein `readonly`-Instanzfeld liefert also pro Test einen eigenen Namen — ein
`static readonly` täte das nicht.

**Nachtrag am selben Tag:** Eine eigene Queue reicht nicht, es braucht auch eine eigene **Exchange**
(`TestMessaging.UniqueExchangeName(prefix)`). Der Grund steht oben im Nachtrag zu den Publish-Schleifen:
eine geteilte Topic-Exchange fächert eine Publikation unter einer Envelope-Id an alle passenden Queues
aus, und die geteilte durable Inbox lässt davon genau eine überleben. Eine eigene Queue schützt vor
*Nachrichtenresten früherer Läufe*, eine eigene Exchange vor *Konkurrenz gleichzeitig lebender Hosts* —
zwei verschiedene Probleme, die beide wie „die Nachricht kam nicht an" aussehen.

## Nicht weiterverfolgt: TrackActivity in den Smoke-Tests

Der frühere Vorschlag, die Smoke-Tests auf Wolverines `TrackActivity()` umzustellen, ist
gegenstandslos: die Smoke-Tests sprechen über gRPC gegen **fremde Prozesse**, die der Aspire-Host
startet. Ein In-Process-Tracking-Handle existiert dort nicht.

---

# TODO-38, Keine Batch- oder Bulk-Fähigkeit

**P3 · offen · IMP-32**

`ISender.Send` verarbeitet einen Request, `UnitOfWorkBehavior` committet danach. „Nährwertkatalog
mit 500 Einträgen importieren" heißt heute: 500 Transaktionen, 500 Outbox-Runden, 500
Projektionsläufe.

## Lösungsvorschlag

Kein generisches Batch-API bauen — das untergräbt die Ein-Command-eine-Transaktion-Regel.
Stattdessen den Massenvorgang als **eigenen Command** modellieren:

```csharp
public sealed record ImportFoodCatalog(IReadOnlyList<FoodEntry> Entries) : ICommand<ImportSummary>;
```

Deckt den Regelfall ab. Erst wenn ein Import die Transaktionsgröße sprengt, braucht es echtes
Chunking — dann als bewusst nicht-atomarer Vorgang mit eigenem Fortschrittszustand (TODO-39).

---

# TODO-39, Keine Saga- oder Process-Manager-Abstraktion

**P3 · offen · IMP-33**

Abgedeckt: eingehender Command, eingehende Query, eingetroffenes Event. Nicht abgedeckt: alles mit
Zustand über Zeit und Zeitsteuerung — „erinnere nach 3 Tagen ohne Eintrag", „warte, bis Nutrition
**und** Fitness gemeldet haben". Der letzte Fall ist für Analytics absehbar relevant.

## Lösungsvorschlag

Nicht selbst bauen — Wolverine bringt Sagas und `ScheduleAsync` mit, beides über dieselbe durable
Message-Infrastruktur, die hier schon konfiguriert ist.

Die Entscheidung, die eine ADR braucht: ob Wolverine damit vom reinen Transport (ADR-0015/0023)
zum Prozess-Host aufgewertet wird. Das ist eine bewusste Aufweichung der bisherigen Abgrenzung —
**vor** der ersten Saga klären, nicht danach.

---

# TODO-40, Sichtbarkeits-Disziplin ist uneinheitlich

**P4 · gelöst · IMP-38**

Die Messaging-Typen sind inzwischen konsequent `internal`. Offen bleibt: `ProjectionRunner` ist
`public`, obwohl er nur vom `internal` `DomainEventPublisher` genutzt wird; ebenso `EfCoreUnitOfWork`,
`MartenUnitOfWork`, `EfCoreRepository`, `MartenEventSourcedRepository` und beide Tracker — alle
ausschließlich über `BuildingBlocksOptions` registriert, nie direkt referenziert.

## Lösungsvorschlag

Regel festschreiben: **`public` ist nur, was ein Service-Host tatsächlich benennt.** Die genannten
Typen auf `internal`, `InternalsVisibleTo` für das Testprojekt. `public` bleiben beide
`AddBuildingBlocks`-Überladungen, `BuildingBlocksOptions`, die EntityKey-Konverter (im DbContext des
Service benutzt), `DomainEventEnvelope` samt Handler (Wolverine muss sie sehen) und die Behaviors als
Vorlage. (`WolverineHostExtensions` stand hier ebenfalls und ist mit TODO-06 entfallen — genau nach
dieser Regel: kein Host benennt es mehr.)

## Gelöst: alle sieben Typen sind `internal`, und die Regel ist getestet (2026-08-06)

Der Befund ist vollständig abgearbeitet — nachgeprüft am Code:

| Typ                            | heute                |
| ------------------------------ | -------------------- |
| `ProjectionRunner`             | `internal sealed`    |
| `EfCoreUnitOfWork<TContext>`   | `internal sealed`    |
| `MartenUnitOfWork`             | `internal sealed`    |
| `EfCoreRepository`             | `internal sealed`    |
| `MartenEventSourcedRepository` | `internal sealed`    |
| `EfCoreAggregateTracker`       | `internal sealed`    |
| `MartenAggregateTracker`       | `internal sealed`    |

Wichtiger als die sieben Einzelfälle ist, dass die geforderte **Regel** jetzt nicht mehr nur
aufgeschrieben, sondern festgenagelt ist. `PublicSurfaceTests` existiert in **allen drei**
Testprojekten und pinnt die vollständige exportierte Typliste der jeweiligen Assembly:

- `BuildingBlocks.Infrastructure.Tests` — die Oberfläche ist **exakt** vier beabsichtigte Typen
  (`ServiceCollectionExtensions`, `HostApplicationBuilderExtensions`, `BuildingBlocksOptions`,
  `EntityKeyModelBuilderExtensions`) plus sieben, die nur deshalb `public` sind, weil Wolverine
  C# in eine andere Assembly generiert und sie dort benennt. Ein zweiter Test verlangt, dass jeder
  dieser sieben Ausnahmen tatsächlich noch generierter Code gegenübersteht; ein dritter verbietet
  Implementierungs-Namespaces (`Persistence`, `Wiring`, `Registration`, `Validation`, …) an der
  Oberfläche überhaupt.
- `BuildingBlocks.Domain.Tests` / `BuildingBlocks.Application.Tests` — dort pinnt derselbe Test die
  gegenteilige Eigenschaft: hier ist alles `public`, und ein Verschieben oder Umbenennen ändert den
  `FullName` eines exportierten Typs und bricht jeden Konsumenten. Die Liste ist deshalb Vertrag.

Damit kann ein neuer `public` Typ in Infrastructure nicht mehr unbemerkt entstehen: der Test wird
rot, bis jemand ihn mit einer Begründung in die Liste einträgt. Die Anmerkung aus dem
Lösungsvorschlag, `InternalsVisibleTo` fürs Testprojekt zu setzen, war bereits vorher erfüllt.

Nicht übernommen wurde ein Detail des Vorschlags: die Behaviors sind **nicht** „als Vorlage"
`public` geblieben, sondern ebenfalls `internal` — ein Service, der ein eigenes Behavior schreibt,
braucht dafür nur `IPipelineBehavior` aus `Application`, nicht unsere Implementierung.

---

# TODO-41, Wirkungslose Varianz-Modifikatoren

**P4 · offen (teilweise gegenstandslos) · IMP-43**

> Nachtrag 2026-08-03: `IState<TSelf, out TKey>` gibt es nicht mehr — der State ist mit ADR-0030
> die Record-Basis `AggregateState<TSelf, TKey>`, und Klassen kennen keine Varianz. Dieser eine
> Modifikator ist damit weg; die übrigen vier stehen weiterhin offen.

`IEntity<out TKey>`, `IAggregateRoot<out TKey>`, `IEventSourcedAggregateRoot<out TKey>`,
`IState<TSelf, out TKey>`, `IRepository<TAggregate, in TKey>` — alle mit
`where TKey : struct, IEntityKey`. Varianz gilt nur für Referenztypen; bei einer
`struct`-Constraint ist der Modifikator wirkungslos und suggeriert eine Flexibilität, die es nicht
gibt.

## Lösungsvorschlag

Ersatzlos streichen. Rein mechanisch, kein Verhaltensunterschied, kein Breaking Change — der
Compiler akzeptiert exakt dieselben Verwendungen.

---

# TODO-42, Uneinheitliche Projektstruktur

**P4 · gelöst · IMP-44**

`BuildingBlocks.Domain` (20 Dateien) und `BuildingBlocks.Application` (18 Dateien) sind flach,
`BuildingBlocks.Infrastructure` hat fünf Ordner. In `Application` stehen CQRS-Verträge,
Ergebnismodell, Persistenz-Ports und Event-Ports unsortiert nebeneinander.

## Lösungsvorschlag

Die Ordnerstruktur der Infrastructure übertragen (`Cqrs/`, `Results/`, `Persistence/`, `Events/`
bzw. `Model/`, `Identity/`, `Events/`, `Rules/`). Namespaces bewusst **nicht** mitziehen, sonst
wird aus einer Aufräumaktion ein Breaking Change für jeden Service.

## Der Lösungsvorschlag ist so nicht umsetzbar (Befund 2026-08-05)

Die Wurzel-`.editorconfig` setzt `dotnet_style_namespace_match_folder = true:error`, und
`Directory.Build.props` schaltet `EnforceCodeStyleInBuild` ein. **Ordner ohne passenden Namespace
sind damit ein Build-Fehler (IDE0130)** — die Sparvariante „Ordner ja, Namespaces nein" existiert
in diesem Repository nicht. Es bleiben drei ehrliche Optionen: Ordner **samt** Namespaces (und
damit der Breaking Change, den der Vorschlag vermeiden wollte), gar keine Ordner, oder nur
`Application` aufteilen.

Abzuwägen ist dabei ein Kostenpunkt, den der Vorschlag nicht nennt: In `Infrastructure` ist fast
alles `internal`, die Namespaces sind für Konsumenten also unsichtbar. In `Domain`/`Application`
ist **alles public**; aus dem einen `using BuildingBlocks.Domain;` würden in jeder Aggregat-Datei
jedes Services drei bis vier usings — dauerhaft, in genau dem Code, der am häufigsten geschrieben
wird. Dem steht ein geringerer Nutzen gegenüber als bei `Infrastructure`: dort lagen wirklich
verschiedene Anliegen nebeneinander (Persistenz, Messaging, DI, Prüfungen), hier sind es 30
winzige Dateien mit einem einzigen Anliegen — dem Domänenmodell.

## Gelöst: Ordner **samt** Namespaces, entlastet durch Global Usings (2026-08-06)

Von den drei Optionen ist die erste gewählt worden. Der Kostenpunkt oben bleibt richtig, aber er
ist bezahlbar: ein Service trägt die vier bis fünf `using`-Zeilen **einmal** als
`<Using Include="…" />` in sein `.csproj` ein, und jede Aggregat-Datei kommt danach mit **null**
usings aus. Vorgeführt in allen vier Sample-Domain-/Application-Projekten; `Widget.cs` hat
seither keine einzige using-Zeile mehr.

Die Struktur ist damit:

```
BuildingBlocks.Domain/            BuildingBlocks.Application/
├── Aggregates/                   ├── Cqrs/
├── Entities/                     ├── Results/
├── Events/                       ├── Persistence/
├── Naming/                       ├── DomainEvents/
├── Rules/                        └── IntegrationEvents/
└── IClock.cs (Wurzel)
```

Zwei Abweichungen vom Vorschlag, beide bewusst: `Model/`+`Identity/` sind zu `Aggregates/`+
`Entities/` geworden, weil das die tatsächliche Achse ist (Wurzel gegen Kind, nicht Modell gegen
Identität), und `Application/Events/` ist in `DomainEvents/` und `IntegrationEvents/` geteilt —
die Trennung ist genau die Grenze des Bounded Context und verdient einen eigenen Ordner.

Was sich dadurch ändert und vorher gratis war: **die Namespaces sind jetzt Vertrag.** Ein
Dateiverschub ändert den `FullName` jedes exportierten Typs und bricht jeden Konsumenten. Deshalb
nageln `PublicSurfaceTests` in `BuildingBlocks.Domain.Tests` und `BuildingBlocks.Application.Tests`
— nach dem Vorbild der Infrastructure — die vollständige Liste der exportierten Typnamen fest.
Derselbe Test erschlägt die Sichtbarkeitsfrage mit: ein versehentlich `public` gewordener Typ
fällt sofort auf. Das Sichtbarkeitsaudit selbst ergab **nichts zu verstecken** — alle 19
Application-Typen haben externe Nutzer; in `Domain` haben `EntityBase`, `IEntity` und
`IHasDomainEvents` zwar keine, müssen aber public bleiben (public Klassen erben von `EntityBase`,
und `IAggregateRoot` leitet von den beiden anderen ab → sonst CS0061).

---

# TODO-43, Irreführende Test- und Methodennamen

**P4 · gelöst · IMP-45 + IMP-48**

- **`SenderContractTests`** baut ein `Substitute.For<ISender>()`, konfiguriert dessen Rückgabewert
  und prüft, dass dieser zurückkommt — getestet wird NSubstitute. Der Wert ist nicht null (die
  Signaturen und Constraints kompilieren nachweislich), aber der Name verspricht mehr.
  **Gelöst am 2026-08-07 durch Löschung** statt Umbenennung: dass die Signaturen kompilieren,
  beweist bereits jeder echte Aufrufer in `BuildingBlocks.Infrastructure.Tests`. Ein Testfall, der
  nie fehlschlagen kann, kostet bei jedem Lauf Zeit und suggeriert im Bericht eine Abdeckung, die
  er nicht liefert — ein ehrlicherer Name hätte das nicht geändert.
- **Wolverine-Extensions** mischten Singular und Plural: `ApplyBuildingBlock*` (drei `internal`)
  gegen `UseBuildingBlocksEfCorePersistence` (`public`). **Erledigt als Nebeneffekt von TODO-06** —
  die öffentliche Methode ist gelöscht, übrig sind nur noch die vier internen mit einheitlichem Namen.

## Nachtrag 2026-08-07 — vollständig gelöst

Die vier internen Methoden heißen jetzt `ApplyBuildingBlocksIdempotencyWindow`,
`ApplyBuildingBlocksDomainEventRouting`, `ApplyBuildingBlocksMessagingDefaults` und
`ApplyBuildingBlocksSubscription`. Drei Dateien, neun Vorkommen, alles `internal`, keine
Außenwirkung.

Der Umbau hing nicht am Code, sondern an der Frage, was mit den ADRs geschieht: **ADR-0023**
(Z. 287, 340) und **ADR-0027** (Z. 18–19) nennen die alten Namen im Fließtext, und ADRs sind
unveränderlich. Entschieden wurde, sie **nicht** anzufassen — und das ist die eigentlich
festhaltenswerte Regel:

> **Codenamen in ADRs sind historisch zu lesen.** Ein ADR hält eine *Entscheidung* fest, samt dem
> Code-Vokabular, das zum Zeitpunkt der Annahme galt. Er ist keine Referenzdokumentation und wird
> nicht namensaktuell gehalten. Der Beleg steht schon länger im Repo: ADR-0027 Z. 18 nennt
> `ApplyBuildingBlockEfCoreOutbox` — eine Methode, die seit TODO-06 nicht mehr existiert. Ein
> Nachtrag „eine interne Methode heißt jetzt anders" trüge auch keine Entscheidung, sondern nur
> Rauschen in ein Dokument, dessen Wert gerade in seiner Stabilität liegt.

Namensaktuell gehalten wird stattdessen die **lebende** Doku, die den aktuellen Stand beschreiben
soll: `docs/architecture/building-blocks-infrastructure.md`, `WalkingSkeleton.md`, `todo.md`,
`Improvements.md` und die beiden Instruktionsdateien — alle nachgezogen. Wer eine Methode aus einem
ADR sucht und sie nicht findet, sucht sie dort.

---

# TODO-44, Bewusste Ausnahmen dokumentieren

**P4 · wird nicht gelöst · IMP-35 + IMP-46**

Zwei Muster, die wie Nachlässigkeit aussehen, aber richtig sind:

- **Vier prozessglobale statische Caches** (`RequestSender` 3×, `ProjectionRunner`,
  `EntityKeyFormatter`, `EntityKeyModelBuilderExtensions`; `FailureResults` war der fünfte und ist
  mit TODO-12 entfallen). Alle ausschließlich `Type`-gekeyed mit
  unveränderlichen, rein typabgeleiteten Werten. Die einzige reale Fehlwirkung war der
  unvollständige Schlüssel in `RequestSender` — behoben. Testisolation ist nicht betroffen.
- **`IServiceProvider` in `RequestSender` und `ProjectionRunner`**: der aufzulösende Handler-Typ ergibt
  sich erst aus dem Laufzeittyp des Requests, lässt sich also nicht per Konstruktor injizieren.

**Kein Code zu ändern** — aber die Begründung ist nirgends festgehalten, und ohne sie wird beides
entweder später „aufgeräumt" (eine Verschlechterung ohne Gegenwert) oder als Muster kopiert.

## Lösungsvorschlag

Ein Absatz in `docs/architecture/building-blocks.md`: „Konstruktorinjektion für alles, was zur
Kompositionszeit feststeht; Service Location nur, wo der Typ erst zur Laufzeit bekannt ist."

---

# TODO-45, Api-Readiness prüft nicht mehr existierende Connection-Namen

**P1 · gelöst · AppHost `e44ae9b`**

Nutrition und Fitness prüften nach `e44ae9b` noch `nutritionDb` bzw. `fitnessDb`. Diese Ressourcen
gibt es nicht mehr, also lieferte `GetConnectionString(...)` `null` — und
`AddNpgSqlReadinessCheck` reichte das mit `connectionString!` ungeprüft an den Health Check weiter.
Folge: Der `/health`-Check wäre nie healthy geworden, `WithHttpHealthCheck` hätte den Service unten
gehalten und der BFF per `WaitFor` unbegrenzt gewartet. Der Build meldete nichts, weil der Name ein
String ist.

**Behoben:** Beide Services prüfen jetzt ihr Write/Read-Paar
([Nutrition Program.cs:5-6](src/Services/Nutrition/VitalSync.Nutrition.Api/Program.cs:5),
[Fitness Program.cs:5-6](src/Services/Fitness/VitalSync.Fitness.Api/Program.cs:5)), und
`AddNpgSqlReadinessCheck` wirft bei fehlender Verbindungszeichenfolge beim Start statt still einen
Dauer-Timeout zu erzeugen
([AspireExtensions.cs:98-105](src/Aspire/VitalSync.ServiceDefaults/AspireExtensions.cs:98)).
Abgedeckt durch `NpgSqlReadinessCheckTests` in `tests/VitalSync.ServiceDefaults.Tests`.

Analytics stand nie auf einem falschen Namen, hing aber noch am Default-Template
(`AddControllers`, `UseHttpsRedirection`). Es steht jetzt ebenfalls auf dem Muster der beiden
anderen: Readiness-Checks für `analytics-write`/`analytics-read` und RabbitMQ,
`AddProblemDetails`/`UseExceptionHandler`, `await app.RunAsync().ConfigureAwait(false)`
([Analytics Program.cs](src/Services/Analytics/VitalSync.Analytics.Api/Program.cs)).

Damit ist das Host-Muster über alle drei Services identisch und in
[.claude/CLAUDE.md](.claude/CLAUDE.md) sowie
[.github/copilot-instructions.md](.github/copilot-instructions.md) als Regel festgehalten.

---

# TODO-46, Die MigrationService-Worker sind leere Hüllen

**P2 · offen · AppHost `e44ae9b`**

```csharp
var builder = Host.CreateApplicationBuilder(args);
builder.AddServiceDefaults();
builder.Build();
```

([Nutrition MigrationService Program.cs](src/Services/Nutrition/VitalSync.Nutrition.MigrationService/Program.cs),
identisch für Fitness und Analytics)

Der Prozess endet sofort mit Exit-Code 0, also ist `WaitForCompletion` im AppHost erfüllt und die
Api startet — gegen leere Datenbanken. Solange es keine Aggregate gibt, fällt das nicht auf;
sobald der erste `DbContext` existiert, ist es ein Start gegen fehlende Tabellen.

Erwartet wird das Muster aus dem Durchstich
([WalkingSkeleton.md §Schritt 4](WalkingSkeleton.md)): ein `BackgroundService`, der migriert und
danach `IHostApplicationLifetime.StopApplication()` ruft, ohne `AddBuildingBlocks` — der Worker
braucht weder Wolverine noch Outbox noch Dispatcher.

## Lösungsvorschlag

Beim ersten echten Aggregat des jeweiligen Kontexts nachziehen, nicht vorher: der Worker braucht
den `DbContext`, den es noch nicht gibt. Bis dahin gilt die Zusage `WaitForCompletion` als
unbelegt und gehört hier vermerkt statt im AppHost still vorausgesetzt.

---

# TODO-47, Kind-Entitäten haben kein Verhalten

**P2 · gelöst · Folgearbeit zu ADR-0031**

## Ursprünglicher Befund

ADR-0031 gab einem Aggregat eine Kindkollektion, die den Commit überlebt — aber das Kind blieb ein
nackter Record. `WidgetPart` hatte kein Verhalten, und jede Regel über ein Part lag auf `Widget`.
Für ein Feld reicht das; für einen echten Kontext nicht: ein Kind mit eigenen Invarianten schiebt
seine Regeln in den Root, wo sie sich mit Regeln vermischen, die nichts mit ihm zu tun haben.

Offen war nicht das Verhalten, sondern die **Uncommitted Events** eines solchen Kindes. Die
naheliegende Variante — eigene Eventliste am Kind oder Registrierung im State während `Apply` —
scheitert an drei Eigenschaften, auf denen die Architektur bereits steht: Reihenfolge (ADR-0006,
eine Liste; ein Merge-Schlüssel existiert vor dem Commit gar nicht, ADR-0029), Record-Gleichheit
(ein pendendes Event im State macht zwei gleiche States ungleich) und Snapshot-Sicherheit
(`IStateOwner.State` wird persistiert, ADR-0025).

## Gelöst — das Kind raist über den Root (ADR-0032, 2026-08-04)

- `EntityState<TSelf, TKey>` ist das Kind-Gegenstück zu `AggregateState` — Identität und reines
  `Apply`, **ohne** `Version`: die Version gehört dem Aggregat (ADR-0030).
- `Entity<TKey, TState>` erbt direkt von `EntityBase<TKey>` und ist die **einzige** Basis für
  Nicht-Aggregat-Entitäten: Identität im Konstruktor, Leer-Guard, Entitätsgleichheit (ADR-0008)
  unverändert. Neu sind `GetCurrentState()` — liest **durch den
  Root**, statt zu kopieren — und ein `protected RaiseEvent`, das den Root über
  `IDomainEventRaiser` erreicht (explizit implementiert, wie `IDomainEventOwner`/`IStateOwner`).
- Das zustandslose `Entity<TKey>` ist **gelöscht**. Jede Entität hat einen State: die Daten eines
  Kindes liegen ohnehin im State-Graphen des Aggregats (ADR-0031), etwas ohne Identität ist ein
  Value Object, und ein Kind ohne Verhalten braucht gar keine Hülle. `EntityBase<TKey>` hat damit
  genau zwei Kinder — das ist die vollständige abstrakte Entitätshierarchie. Amendments an ADR-0008
  und ADR-0025, die den zustandslosen Typ namentlich führten.
- Der Kind-State liegt im Root-State (ADR-0031), also faltet ihn `State.Apply` ohnehin mit, meist
  durch Delegation an das `Apply` des Kind-States. **Kein zweites Routing, keine zweite Eventliste,
  kein `ApplyAndRecord`.**
- `GetCurrentState()` ist eine Methode, keine Property: sie wirft, wenn das Kind im selben Command
  entfernt wurde — und eine werfende Property verbietet CA1065. Das Werfen ist der Punkt; die
  Alternative wäre eine Hülle, die still veraltete Daten liest.

**Belegt durch:**

- `BuildingBlocks.Domain.Tests` (94 Tests, davon neu: `EntityStateTests` sowie Erweiterungen in
  `EntityTests`, `AggregateRootTests`, `AggregateVersionTests`, `ReconstitutableTests`,
  `EventSourcedAggregateRootTests`): Kind-Event landet in der Root-Liste, Reihenfolge Root ↔ Kind,
  Hülle liest live statt kopiert, Hülle eines entfernten Kindes wirft, Guards gegen fehlenden
  Kanal/Lookup und leere Id, Kind-Änderung erhöht die Root-Version, Kind hat keine eigene Version,
  `ClearDomainEvents` löscht auch Kind-Events, `Restore` und `LoadFromHistory` bauen Kinder mit auf,
  Replay-Guard greift nach einem Kind-Event.
- `EfCoreChildCollectionTests.ChildRaisedChange_RoundTripsThroughTheOwnedGraph` (Testcontainers/
  PostgreSQL): eine über die Kind-Hülle ausgelöste Änderung geht durch denselben Owned-Graph-
  Abgleich und dieselbe Zeile wie zuvor.
- Beide Samples: `WidgetPart` ist jetzt eine Hülle über `WidgetPartState` — **ohne Schemaänderung
  und ohne Migration** (`dotnet ef migrations has-pending-model-changes`: keine Änderungen), weil
  das Owned Mapping schon auf genau diesen Record zeigte. `Gadget` hat mit `GadgetComponent` das
  erste Kind im event-sourced Sample, inklusive Command- und Replay-Pfad.

---

# TODO-48, Publisher Confirms sind unbelegt

**P2 · gelöst · WS-08 Nachtrag**

Mit TODO-07 ist die Kette bis in den Broker hinein durabel: durabler Sending-Endpoint,
persistente Nachricht, durable Quorum-Queues. Was **nicht** belegt ist: ob der Publisher auf die
Bestätigung des Brokers wartet. Ohne Publisher Confirms gilt eine Nachricht als gesendet, sobald
sie im Socket steht — ein Broker, der sie danach verwirft (etwa bei vollem Speicher), meldet das
niemandem, und die Outbox-Zeile wird trotzdem als erledigt markiert. Das ist derselbe Fehlertyp
wie TODO-07, nur eine Ebene tiefer.

## Zuerst messen, dann entscheiden

Offen ist die Vorfrage: `RabbitMQ.Client` 7 hat sein Verhalten gegenüber Version 6 geändert, und
es ist ungeklärt, ob Wolverine Confirms bereits per Default aktiviert. Erst danach steht fest, ob
es überhaupt eine Änderung braucht. Stellschraube wäre

```csharp
options.UseRabbitMq(uri).ConfigureChannelCreation(channel =>
{
    channel.PublisherConfirmationsEnabled = true;
    channel.PublisherConfirmationTrackingEnabled = true;
});
```

Ein Test, der das belegt, ist teuer: ein verworfener Publish lässt sich ohne Broker-Manipulation
kaum herbeiführen. Realistisch ist die Prüfung der Konfiguration am gestarteten Host plus eine
Messung des Durchsatzpreises — Confirms serialisieren den Sendepfad spürbar.

## Gelöst — Confirms sind aktiv, der Preis ist gemessen (2026-08-07)

**Die Vorfrage ist beantwortet, und zwar negativ: niemand hatte Confirms an.** Die Kette wurde auf
drei Ebenen nachgesehen:

- **RabbitMQ.Client 7.1.2** (unsere transitiv aufgelöste Version) hat das Feature nicht
  abgeschafft, sondern nach `CreateChannelOptions` verschoben. Beide Schalter —
  `PublisherConfirmationsEnabled` und `PublisherConfirmationTrackingEnabled` — dokumentieren
  ausdrücklich „Defaults to `false`".
- **Wolverine 6.23.0** bietet `ConfigureChannelCreation(...)` mit einem eigenen
  `WolverineRabbitMqChannelOptions`, dessen beide Properties ebenfalls „Defaults to false"
  tragen. Wolverine reicht den Client-Default also unverändert durch und setzt nichts eigenes.
- **`ApplyBuildingBlocksMessagingDefaults`** rief `UseRabbitMq(...).AutoProvision().UseQuorumQueues()
  .DeclareExchange(...)` — kein `ConfigureChannelCreation`.

Damit war die Vermutung dieses Eintrags bestätigt: Die mit TODO-07 aufgebaute Durabilitätskette
endete an ihrem letzten Meter. Der Sending-Endpoint ist `UseDurableOutbox()`, also wird die
Outbox-Zeile gelöscht, sobald Wolverine den Envelope als gesendet betrachtet — und ohne Confirms
heißt „gesendet" *im Socket*, nicht *im Broker*. Genau dazwischen liegen die Fälle, für die TODO-07
gebaut wurde.

**Beide Schalter sind jetzt gesetzt**
([WolverineOptionsExtensions.cs](BuildingBlocks/src/BuildingBlocks.Infrastructure/DependencyInjection/Wiring/WolverineOptionsExtensions.cs)).
Nur `Confirmations` ohne `Tracking` wäre die schlechteste Variante gewesen: Der Broker antwortet
dann zwar, aber ohne korrelierbare Sequenznummer, und die Client-Doku empfiehlt für diesen Fall,
den `PublishSequenceNumberHeader` von Hand zu setzen. Mit Tracking wirft `BasicPublishAsync` bei
`nack` oder `basic.return` eine `PublishException` — der Fehler landet also als Exception genau
dort, wo Wolverines Retry-Policies greifen, und die Outbox-Zeile bleibt stehen.

### Der gemessene Preis

Einmalige Messung gegen den Testcontainer-Broker, 500 persistente Nachrichten à 512 Byte,
sequentiell über einen Kanal, roh über `RabbitMQ.Client` (also ohne Wolverine, um genau den
Mechanismus zu isolieren):

| | Dauer für 500 Nachrichten | Durchsatz |
| --- | --- | --- |
| ohne Confirms | 8 ms | ~62 500 msg/s |
| mit Confirms + Tracking | 435 ms | ~1 150 msg/s |

Rund **Faktor 54**, was nicht überrascht: Ohne Confirms ist der Publish fire-and-forget, mit
Confirms kostet jede Nachricht einen Round-Trip zum Broker (~0,87 ms lokal). Der Wert ist die
Untergrenze des Nutzens, nicht das reale Systemverhalten — er misst den ungünstigsten Fall,
sequentielles Einzelsenden.

**Warum das trotzdem akzeptabel ist**, und zwar aus zwei unabhängigen Gründen:

1. **Der Engpass liegt woanders.** Domain Events laufen über eine `.Sequential()`-Queue
   (TODO-20) — der Durchsatz ist dort bereits schärfer gedeckelt als durch Confirms. Wer diesen
   Preis für zu hoch hält, muss zuerst TODO-20 lösen; danach ist neu zu messen.
2. **Die Größenordnung passt zur Domäne.** ~1 150 msg/s sind rund 100 Millionen Integration
   Events pro Tag. VitalSync erzeugt pro Nutzeraktion in der Größenordnung *eines*
   Integration Events.

Die Messsonde wurde nach der Messung wieder gelöscht: Ein Durchsatztest im CI misst die Laune des
Runners, nicht den Code, und wäre der genaue Gegentyp zu den in
[testing-strategy.md](docs/architecture/testing-strategy.md) festgehaltenen Regeln. Belegt bleibt
die **Konfiguration**, durch
`Configure_WithBrokerUri_EnablesPublisherConfirmationsAndTheirTracking` in
`WolverineExtensionTests` — der Test prüft zuerst, dass ein frisches
`WolverineRabbitMqChannelOptions` beide Flags auf `false` hat, und erst danach, dass unsere
Konfiguration sie umlegt. Ohne diesen Anker wäre er stillschweigend wertlos, sobald Wolverine den
Default einmal selbst ändert.

### Was weiterhin unbelegt bleibt

Dass eine tatsächlich verworfene Nachricht zu einer `PublishException` führt. Das erfordert einen
Broker, der unter Speicherdruck steht oder dem die Ziel-Exchange fehlt — beides ist ohne
Broker-Manipulation nicht reproduzierbar. Der Mechanismus ist jetzt aktiv und die Zusage der
Client-Bibliothek dokumentiert; mehr ist zu vertretbaren Kosten nicht zu haben.

---

# TODO-49, Feldnamen in Events sind abgeleitet, ein Rename zerstört still

**P1 · gelöst · ADR-0034-Folge**

## Gelöst — ein Snapshot friert die persistierten Feldnamen ein (ADR-0035, 2026-08-06)

**Befund bei der Umsetzung von TODO-26.** ADR-0030 hat abgeleitete Namen auf **Typebene**
abgeschafft (`[EventName]`, `[AggregateName]`), die **Feldebene** aber offen gelassen: verifiziert
existierte **kein einziges `[JsonPropertyName]`** im Repository, der JSON-Name eines Feldes war also
der CLR-Property-Name. Ein Rename von `Titel` zu `Name` benennt damit das Feld auf der Leitung um;
gespeicherte Events tragen weiter den alten Namen, der Deserializer findet nichts und lässt die
Property auf `default`. Kein Fehler, kein Log, kein roter Test — dieselbe Fehlerklasse wie TODO-03
(`AssemblyQualifiedName`) und TODO-04 (Streamschlüssel am Klassennamen), beide P1.

**Kein Attribut pro Feld.** Attribute kaufen Rename-*Toleranz*, und die lohnt nur, wo ein Rename
häufig **und** bedeutungserhaltend ist — das ist die Typebene (`Created`/`Registered`/`Added` sind
dasselbe Event). Auf Feldebene ist ein Rename fast immer eine Bedeutungsänderung: `IngredientId` zu
`WorkoutId` ist ein anderes Feld, ein Attribut wäre dort eine dauerhafte Lüge. Dazu die
Kardinalität: rund eines pro Typ ist tragbar, rund fünf pro Typ nicht. Leitsatz: **ein Rename, der
Daten kaputtmacht, darf wehtun — er darf nur nicht still sein.**

Umgesetzt in drei Teilen:

| Teil                                                   | Ort                                                                  |
| ------------------------------------------------------ | -------------------------------------------------------------------- |
| Renderer + Vergleich gegen eine eingecheckte Baseline   | `BuildingBlocks.Infrastructure/Schema/PersistedSchema.cs`             |
| Baseline je Kontext                                    | `EventSchema.approved.txt` in beiden Sample-Testprojekten             |
| Spaltenname erzwungen statt nur gelebt                  | `AggregateStateModelCheck` (auch `HasJsonPropertyName` bei `ToJson()`) |

Der Renderer liest über **`JsonTypeInfo`**, nicht über `GetProperties()` — er friert damit ein, was
der Serializer *tut*, inklusive `[JsonPropertyName]` und einer künftigen `PropertyNamingPolicy`. Ein
typisierter Schlüssel rendert als der Wert, zu dem er serialisiert (`guid`, `int`), womit ADR-0034 in
der Baseline sichtbar bleibt. Sortiert wird nach Namen, weil Umsortieren für JSON bedeutungslos ist.

Bewusst ein **Test** und kein Startup-Check: ein Snapshot braucht eine eingecheckte Baseline, die zur
Laufzeit nicht existiert. Die Entscheidungsregel steht in der Fehlermeldung, weil genau dort falsch
entschieden wird — Feld **hinzugefügt** heißt Baseline aktualisieren, Feld **umbenannt, entfernt oder
umgetypt** heißt neues `-v2`-Event und `-v1` unangetastet.

**Nicht enthalten:** Read-Modelle. Ein Read-Modell ist abgeleitet und wiederaufbaubar, ein
Feld-Rename kostet dort einen Rebuild statt Daten — das gilt aber nur, solange der Rebuild wirklich
möglich ist, was für state-stored Kontexte an **TODO-21** hängt.

**Nachhut:** Marten-Snapshotting ist in ADR-0019 als „additiv" zurückgestellt. Für die Events stimmt
das, für den State nicht — ein Snapshot befördert `GadgetState` von „liegt nirgends" zu „liegt als
JSON ohne Schema" und muss dann in die Baseline. Verifiziert existiert heute weder ein
`session.Store<>` noch eine `SingleStreamProjection`, der ES-State wird also nirgends persistiert.

Belegt durch `PersistedSchemaTests` in `BuildingBlocks.Infrastructure.Tests` (Rendering, effektiver
JSON-Name, nackter Schlüsselwert, Verify-Verhalten), je ein `PersistedSchemaTests` in beiden Samples
gegen die echten Baselines, und
`EfCoreChildCollectionTests.StateWithAColumnNameLeftToConvention_IsRejectedAtStartup`.

---

## Empfohlene Reihenfolge

1. **TODO-09** (CI) zuerst — klein und sichert alles Weitere ab. (**TODO-06** stand hier
   gleichauf und ist erledigt: die zweite Nennung des Connection Strings existiert nicht mehr.)
2. **TODO-05** ist erledigt: der `IsEmpty`-Guard steht in beiden `AddAsync`-Implementierungen.
   (**TODO-13** stand hier gleichauf und ist entschieden und umgesetzt: Identität
   asymmetrisch — Envelope für Domain Events, am Event für Integration Events, ADR-0029.
   **TODO-10** stand hier
   gleichauf und ist erledigt: Rekonstitution ist jetzt ein expliziter Domänenvertrag, womit
   TODO-05 nur noch die Restlücke schloss statt eine offene Tür.)
3. **TODO-02 → TODO-03 → TODO-04** als ein Persistenzformat-Paket. Danach ist jede Änderung daran
   eine Datenmigration, also **vor** dem ersten echten Service.
4. **TODO-01** ist erledigt: Kinder eines Aggregats mappen als Owned Types, der Commit kopiert
   auch die Navigationen, und das Sample hat mit `widget_parts` das erste nicht-flache Aggregat
   (ADR-0031). **TODO-47** schließt daran an und ist ebenfalls erledigt: das Kind trägt jetzt
   Verhalten und raist über den Root (ADR-0032).
5. **TODO-07** ist erledigt: die Zustellung ist bis in den Broker hinein durabel, und Messaging
   ohne Persistenzwahl wirft, statt still zu degradieren (ADR-0023-Amendment). **TODO-08**
   schließt die verbliebene stille Lücke im Messaging; **TODO-48** trägt die Vorfrage der
   Publisher Confirms nach.
6. **TODO-45** ist erledigt; **TODO-46** kommt erst mit dem ersten Aggregat des jeweiligen
   Kontexts — vorher ist nicht entschieden, wie dort gespeichert wird, und ein Migrations-Worker
   ohne `DbContext` wäre geraten.
