# TODO — konsolidierte Arbeitsliste

Zusammenführung der Befunde aus [hacky.md](hacky.md), [Improvements.md](Improvements.md) und
[WalkingSkeleton.md](WalkingSkeleton.md) §9.

**Diese Liste führt nur noch offene Arbeit.** Gelöste Punkte wurden am 2026-08-09 entfernt; ihre
Begründungen leben dort weiter, wo sie beim Arbeiten auffallen — in den ADRs unter
`docs/architecture/decisions/`, in den Instruktionsdateien und in den Tests, die sie festhalten.
Die Versionsgeschichte hat den vollen Wortlaut, falls eine Entscheidung noch einmal
aufgerollt werden muss.

**Leitfrage der Priorisierung:** Die Basis soll stabil sein, bevor die echten Services kommen.
Je stiller ein Fehler im Produktivbetrieb wirkt und je gebastelter die Stelle, desto höher die
Priorität.

| Prio   | Bedeutung                                                                                |
| ------ | ---------------------------------------------------------------------------------------- |
| **P1** | Datenverlust oder stiller Fehlschlag im Produktivbetrieb. Vor dem ersten echten Service. |
| **P2** | Spürbar störend oder deutlich gebastelt. Vor dem zweiten Service.                        |
| **P3** | Sinnvoll, aber ohne akuten Druck. Beim nächsten Anfassen der Stelle.                     |
| **P4** | Kosmetik und Konsistenz. Aufräum-Commit.                                                 |

**Alle P1 sind erledigt.** Die verbliebenen P2 warten alle ausdrücklich auf einen Auslöser:
TODO-19 auf das erste Read-Modell mit eingebettetem Value Object, TODO-20 auf die erste
Lastmessung, TODO-46 auf das erste echte Aggregat.

## Übersicht

| Nr.     | Titel                                                          | Prio   | Status    | Quellen           |
| ------- | -------------------------------------------------------------- | ------ | --------- | ----------------- |
| TODO-19 | `ApplyEntityKeyConversions` erfasst keine Complex Types        | **P2** | teilweise | hacky-4, WS-15    |
| TODO-20 | Global sequentielle Domain-Event-Queue                         | **P2** | offen     | hacky-13, IMP-25  |
| TODO-46 | Die MigrationService-Worker sind leere Hüllen                  | **P2** | offen     | AppHost `e44ae9b` |
| TODO-29 | `DomainEventPublisher` koppelt Projektion und Publikation      | **P3** | offen     | IMP-26            |
| TODO-30 | `IIntegrationEventMapper` ist untypisiert                      | **P3** | offen     | IMP-12            |
| TODO-31 | `Result` hat keine Kombinatoren                                | **P3** | offen     | IMP-34            |
| TODO-33 | Ein Assembly für alle Persistenz-Pakete                        | **P3** | offen     | IMP-19            |
| TODO-34 | Keine zentrale Paketverwaltung                                 | **P3** | offen     | IMP-47            |
| TODO-35 | `EntityFrameworkCore.Design` verträgt kein `PrivateAssets`     | **P3** | offen     | WS-04             |
| TODO-36 | Der gRPC-Vertrag liegt noch beim Service                       | **P3** | offen     | WS-07             |
| TODO-38 | Keine Batch- oder Bulk-Fähigkeit                               | **P3** | offen     | IMP-32            |
| TODO-39 | Keine Saga- oder Process-Manager-Abstraktion                   | **P3** | offen     | IMP-33            |
| TODO-50 | Kein Read-Modell-Rebuild im event-sourced Pfad                 | **P3** | offen     | ADR-0036-Folge    |
| TODO-41 | Wirkungslose Varianz-Modifikatoren                             | **P4** | offen     | IMP-43            |

---

# TODO-19, `ApplyEntityKeyConversions` erfasst keine Complex Types

**P2 · teilweise · hacky-4 + WS-15**

Der ursprüngliche Befund hatte zwei Hälften. Die erste — der Scan legte Properties im Modell an
und machte so jede berechnete oder `Ignore()`-te Property zur Spalte — ist mit ADR-0033 gelöst,
indem der Discovery-Zweig ersatzlos entfernt wurde.

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

Nach Aggregat-Id partitionieren — gleiche Garantie, parallel über verschiedene Aggregate. Der
`DomainEventEnvelope` führt `AggregateName`/`AggregateId` seit ADR-0030 mit, die Voraussetzung
dafür ist also erfüllt.

Vor der ersten Lastmessung nicht anfassen — hier steht es, damit die Entscheidung bewusst fällt
statt als Default stehen zu bleiben.

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
nächsten Aufräum-Commit — sinnvollerweise gebündelt mit TODO-41.

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

## Nachtrag (2026-08-07): der Worker hat eine zweite Aufgabe bekommen

ADR-0036 gibt dem MigrationService-Worker den natürlichen Platz für den Read-Modell-Rebuild — er hat
beide `DbContext`e ohnehin und läuft vor der Api. Der Sample-Worker macht es vor
([StateStored MigrationService Program.cs](samples/StateStored/VitalSync.Sample.StateStored.MigrationService/Program.cs)):
nach beiden `MigrateAsync` und hinter dem Schalter `ReadModels:Rebuild` läuft
`ReadModelRebuildRunner<WidgetWriteDbContext>`.

Der Runner ist deshalb `public` und generisch über den Kontext: ein Worker ohne `AddBuildingBlocks`
kann ihn selbst instanziieren. Wer dieses TODO umsetzt, zieht das Muster mit.

## Nachtrag (2026-08-09): „ohne `AddBuildingBlocks`" gilt nicht mehr

ADR-0037 macht den Worker zum einzigen Host seines Kontexts, der Schema, Message-Store und
Broker-Topologie anlegen darf. Damit braucht er die volle Wiring — und zwar **dieselbe
Kontext-Erweiterungsmethode wie sein Service**, nur mit
`InfrastructureProvisioning.AtStartup`, damit die beiden sich über Verbindungszeichenfolge,
Kontextnamen und Event-Assembly nicht widersprechen können. Beide Sample-Worker zeigen die
Reihenfolge: Erweiterung mit `AtStartup` → `StartAsync` → `MigrateAsync` für Write- und
Read-Kontext → optionaler Rebuild → `StopAsync`. Der Absatz oben, der `AddBuildingBlocks`
ausdrücklich ausschließt, ist damit überholt; er bleibt stehen, weil er die ursprüngliche
Erwartung dokumentiert.

---

# TODO-50, Kein Read-Modell-Rebuild im event-sourced Pfad

**P3 · offen · Folgearbeit zu ADR-0036**

ADR-0036 hat den state-stored Pfad wiederaufbaubar gemacht. Der event-sourced Pfad hat mit dem
Marten-Stream zwar seit jeher die **Quelle** für einen Rebuild — das war das Argument, warum
ADR-0022 die Wiederaufbaubarkeit überhaupt zusagen konnte — aber kein **Werkzeug**: es gibt kein
Gegenstück zu `ReadModelRebuildRunner`. Wer heute nach einem Projektions-Bugfix ein
event-sourced Read-Modell reparieren will, schreibt das Ablaufen des Streams von Hand.

Das ist deutlich weniger dringend als im state-stored Pfad, denn hier geht nichts verloren: die Daten
liegen im Stream, es fehlt nur die Bequemlichkeit. Deshalb P3.

## Lösungsvorschlag

Ein `EventStreamRebuildRunner`, der die Streams eines Aggregattyps über `IDocumentStore`
aufzählt und jedes Ereignis mit seinen `DomainEventMetadata` durch den bestehenden
`ProjectionRunner` schickt. Damit ist es **derselbe** Ableitungspfad wie live, es braucht also
keinen Paritätstest wie im state-stored Fall, und die Watermark-Prüfung in den Handlern trägt
unverändert.

Zwei Punkte sind vor der Umsetzung zu klären:

1. **Der Runner darf nicht über `DomainEventPublisher` laufen**, sonst spielt ein Rebuild
   Integration Events erneut auf den Broker — genau der Fall, den ADR-0036 vermieden hat und der
   die fachliche Dedup über `IIntegrationEvent.EventId` erst nötig machen würde, die es bewusst
   nicht gibt (ADR-0023, Nachtrag 2026-08-06). Der Publisher koppelt Projektion und Publikation
   heute aber (**TODO-29**); die Entkopplung dort ist die saubere Voraussetzung.
2. **Streams eines Aggregattyps aufzählen** ist in Marten nicht kostenlos, wenn der Stream-Typ nicht
   indiziert ist. Vor der Umsetzung prüfen, ob `mt_streams` dafür ausreicht.
