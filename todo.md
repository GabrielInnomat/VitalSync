# TODO — konsolidierte Arbeitsliste

Zusammenführung der Befunde aus der Code-Analyse (ehemals `hacky.md`, seit dem Abarbeiten
aller Punkte entfernt — die Versionsgeschichte hat den Wortlaut), [Improvements.md](Improvements.md)
und [WalkingSkeleton.md](WalkingSkeleton.md) §9.

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
TODO-19 auf das erste Read-Modell mit eingebettetem Value Object, TODO-46 auf das erste echte
Aggregat.

## Übersicht

| Nr.     | Titel                                                      | Prio   | Status    | Quellen           |
| ------- | ---------------------------------------------------------- | ------ | --------- | ----------------- |
| TODO-19 | `ApplyEntityKeyConversions` erfasst keine Complex Types    | **P2** | teilweise | hacky-4, WS-15    |
| TODO-46 | Die MigrationService-Worker sind leere Hüllen              | **P2** | offen     | AppHost `e44ae9b` |
| TODO-33 | Ein Assembly für alle Persistenz-Pakete                    | **P3** | offen     | IMP-19            |
| TODO-36 | Der gRPC-Vertrag liegt noch beim Service                    | **P3** | offen     | WS-07             |
| TODO-39 | Keine Saga- oder Process-Manager-Abstraktion               | **P3** | offen     | IMP-33            |

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
`StateStoredReadModelRebuildRunner<WidgetWriteDbContext>`.

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
