# Walking Skeleton — Plan und Stand

Arbeitsdokument zum vertikalen Durchstich unter `samples/`. Es hält fest, **warum** es
ihn gibt, **was** bereits erledigt ist, **was** als Nächstes kommt und **welche Fragen**
noch offen sind. Gedacht als Übergabe: wer hier weitermacht, soll ohne den
ursprünglichen Gesprächsverlauf auskommen.

Stand: 2026-08-01

---

## 1. Warum es diesen Durchstich gibt

`BuildingBlocks` validiert sich bisher **ausschließlich selbst**. Es gibt keinen
einzigen Konsumenten: `src/Services/*` sind leere Platzhalter, der BFF ebenfalls.
Damit ist unter anderem [ADR-0027](docs/architecture/decisions/0027-building-blocks-own-wolverine-wiring.md)
(„der Host konfiguriert nichts") eine **unbewiesene Behauptung**.

Der Durchstich ist bewusst **fachlich leer** und liegt unter `samples/`, damit er nach
der Erkenntnisgewinnung wieder gelöscht werden kann. Ziel ist technische
Machbarkeit, nicht Geschäftswert.

Dass sich das lohnt, hat sich bereits bestätigt: die Arbeit daran hat drei tote Pfade
in `BuildingBlocks` freigelegt (siehe §3) und eine Architekturlücke im EF-Mapping
(siehe §5).

---

## 2. Gesamtaufbau (Zielbild)

Drei Etappen, jede einzeln lauffähig und commitbar:

| Etappe | Inhalt                                                                    | Status        |
| ------ | ------------------------------------------------------------------------- | ------------- |
| 1      | `StateStored` — EF Core, ein Aggregat, gRPC, Aspire, Migrationen          | **in Arbeit** |
| 2      | `EventSourced` — dieselbe Struktur auf Marten                             | offen         |
| 3      | Kreuzverkehr — Integration Event von Etappe 1 nach Etappe 2 über RabbitMQ | offen         |

```
samples/
├── VitalSync.Samples.AppHost/                    eigener Aspire-Host (nicht der produktive!)
├── VitalSync.Sample.Contracts/                   Integration-Event-Verträge, beide Services (Etappe 3)
├── StateStored/                                  EF Core
│   ├── VitalSync.Sample.StateStored.Domain
│   ├── VitalSync.Sample.StateStored.Application
│   ├── VitalSync.Sample.StateStored.Infrastructure
│   ├── VitalSync.Sample.StateStored.Api               (gRPC, code-first)
│   ├── VitalSync.Sample.StateStored.MigrationService
│   └── VitalSync.Sample.StateStored.Tests
└── EventSourced/                                 Marten (Etappe 2, analog)
```

**Zwei Hosts sind nicht optional:** `BuildingBlocksOptions.SelectPersistenceStyle`
verbietet das Mischen von EF Core und Marten in einem Host (ADR-0019/0020/0021).

**Eigener AppHost:** Der produktive `src/Aspire/VitalSync.AppHost` darf nicht von
Wegwerf-Code abhängen. Beim Aufräumen wird genau ein Ordner gelöscht.

### Getroffene Entscheidungen

| Frage             | Entscheidung                                                                                                                                 |
| ----------------- | -------------------------------------------------------------------------------------------------------------------------------------------- |
| Ablage            | `samples/` als Wegwerf-Durchstich (nicht `src/Services/`)                                                                                    |
| Persistenz        | **beide** — je ein Service für EF Core und Marten                                                                                            |
| Migrationen       | eigener MigrationService-Worker, Api wartet per `WaitForCompletion`                                                                          |
| Routing-Topologie | ein Topic-Exchange `vitalsync.integration-events`                                                                                            |
| Topic-Namen       | explizites `[Topic("<kontext>.<event>")]` in kebab-case, **ohne** Startup-Validator                                                          |
| Umfang Messaging  | nur Publish-Seite; Subscribe kommt in Etappe 3                                                                                               |
| gRPC              | code-first (ADR-0003) — die Bibliothekswahl (`protobuf-net.Grpc`) trifft der Sample faktisch für die Plattform und verdient später einen ADR |

---

## 3. Was bereits gepusht ist

```
afb70d5  Bind integration-event publication to the handler's message context
2f4d862  Route integration events to the platform topic exchange
816d72d  Make container-backed tests fail instead of skip in CI
51a09ef  Add project skeleton for the state-stored sample service
78e6324  Add Widget aggregate and its CQRS slice to the state-stored sample
```

Jeder der ersten drei Commits wurde einzeln in einem temporären Worktree gebaut und
getestet — sie sind einzeln per `git revert` zurücknehmbar.

**Inhaltlich behoben:** Integration Events erreichten den Broker nie (keine
Routing-Regel, Wolverine verwirft routenlose Nachrichten stillschweigend); die erste
Domain-Event-Zustellung scheiterte immer (`ServiceLocationPolicy.NotAllowed`); der
EF-Persistenzpfad konnte nie committen (kein datenbankgestützter Message Store).

---

## 4. Was uncommitted im Arbeitsbaum liegt

Die Lösung des EF-Mapping-Problems aus §5. **Noch nicht committet**, bewusst zur
Durchsicht.

```
M  BuildingBlocks/src/BuildingBlocks.Domain/AggregateRoot.cs
M  BuildingBlocks/src/BuildingBlocks.Infrastructure/DependencyInjection/BuildingBlocksOptions.cs
M  BuildingBlocks/src/BuildingBlocks.Infrastructure/Persistence/EfCoreRepository.cs
M  BuildingBlocks/src/BuildingBlocks.Infrastructure/Persistence/EfCoreUnitOfWork.cs
M  BuildingBlocks/tests/BuildingBlocks.Infrastructure.Tests/OutboxFlushOnCommitTests.cs
M  samples/StateStored/VitalSync.Sample.StateStored.Tests/VitalSync.Sample.StateStored.Tests.csproj
?  BuildingBlocks/src/BuildingBlocks.Domain/IStateOwner.cs
?  BuildingBlocks/src/BuildingBlocks.Infrastructure/Persistence/EfCoreAggregateTracker.cs
?  BuildingBlocks/tests/BuildingBlocks.Infrastructure.Tests/EfCoreAggregateRoundTripTests.cs
?  samples/StateStored/VitalSync.Sample.StateStored.Infrastructure/Write/WidgetWriteDbContext.cs
?  samples/StateStored/VitalSync.Sample.StateStored.Tests/WidgetWriteModelTests.cs
```

**164 Tests grün**, Build ohne Warnungen.

Empfehlung beim Committen: **zwei** Commits — der BuildingBlocks-Teil getrennt vom
Sample-Teil.

---

## 5. Das EF-Mapping-Problem und seine Lösung

Der wichtigste Befund des Durchstichs. Diese Analyse sollte erhalten bleiben.

### Das Problem

`AggregateRoot<TKey, TState>` deklariert:

```csharp
public sealed override TKey Id => State.Id;   // berechnet, kein Setter, kein Backing Field
```

EF Core kann das nicht als Primärschlüssel abbilden:

```
No backing field could be found for property 'Widget.Id' and the property does not have a setter.
```

Ein Setter lässt sich im Override **nicht nachrüsten** (`EntityBase<TKey>.Id` ist
`public abstract TKey Id { get; }` → `CS0546`). Jede Lösung fasst also entweder die
Domain an oder ändert den Persistenzansatz.

Zusätzlich: als _Complex Type_ gemappt scheitert ein positional record mit
`No suitable constructor was found`, weil EF jeden Konstruktorparameter binden muss —
`State.Id` kann also nicht ignoriert werden. Dieser Folgefehler **verdeckt** den
eigentlichen; die Reihenfolge der EF-Fehler ist irreführend.

### Verworfene Varianten

| Variante                                        | Warum verworfen                                                                              |
| ----------------------------------------------- | -------------------------------------------------------------------------------------------- |
| Shadow Key + Repository füllt ihn               | funktioniert, hinterlässt aber dauerhaft eine redundante Id-Spalte pro Aggregat-Tabelle      |
| Setter in `EntityBase.Id` (`private protected`) | braucht Amendment an ADR-0008, Nachzug in 0010/0025; Identität hätte zwei Quellen            |
| Getrenntes DBO + Übersetzung                    | bricht ADR-0026 (ein Repository-Vertrag), größter Eingriff                                   |
| State als JSON-Spalte                           | tragfähig (die Write-DB wird nie gelesen!), aber unnötig, seit die gewählte Lösung existiert |
| Marten für alles / alles event-sourced          | supersedet ADR-0019/0020/0012, unverhältnismäßig                                             |

### Die gewählte Lösung

**Nicht das Aggregat mappen, sondern seinen State.** Das Aggregat ist Verhalten, der
State ist ein unveränderlicher Record mit Identität — also bereits ein DBO.

Als _Entity Type_ (statt Complex Type) verschwinden beide Probleme: `State.Id` ist bei
einem positional record eine Auto-Property mit Backing Field, und
`ApplyEntityKeyConversions` läuft über `Model.GetEntityTypes()` und greift damit genau
hier.

Umgesetzt:

- **`IStateOwner`** (`BuildingBlocks.Domain`, neu) — `StateType`, `State`,
  `Restore(object)`. Von `AggregateRoot` **explizit** implementiert, wie
  `IDomainEventsManager`. Domänencode sieht die Member nicht und kann den Event-Fold
  nicht per Hand-Restore umgehen.
- **`EfCoreRepository`** — lädt den State per `context.FindAsync(stateType, [id])` und
  rehydriert ein leeres Aggregat darum; `AddAsync` legt den State in den Context.
- **`EfCoreAggregateTracker`** (neu) — Gegenstück zum bestehenden
  `MartenAggregateTracker`. Nötig, weil EFs ChangeTracker nur noch States sieht.
  Beide Persistenzpfade sind damit erstmals symmetrisch.
- **`EfCoreUnitOfWork`** — kopiert vor dem Speichern per `CurrentValues.SetValues` den
  aktuellen State auf den getrackten Eintrag. **Das ist der Kern:** States sind
  unveränderlich, jedes Event ersetzt die Instanz, EF sähe sonst nichts zu speichern.

Ergebnis im Schema: **eine Tabelle, eine Id-Spalte**, kein Shadow Key, keine
Duplikation. Das Sample-Mapping ist 12 Zeilen.

**ADR-Bilanz: keine Änderung** an 0008, 0010, 0025, 0026. `Id => State.Id` bleibt
wortwörtlich stehen.

### Verhaltensänderung, die man kennen muss

Domain Events werden **nur noch von Aggregaten eingesammelt, die durch `IRepository`
gegangen sind**. Wer eine Entity direkt in den `DbContext` schreibt, erzeugt keine
Events mehr. Das ist konsistent mit ADR-0026, aber ein Bruch — der bestehende
EF-Outbox-Test lief deshalb zunächst in einen Timeout und wurde auf ein echtes
Aggregat umgestellt.

### Belegt durch

`EfCoreAggregateRoundTripTests` (Testcontainers, echtes PostgreSQL): Anlegen →
Umbenennen → in **frischem Scope** neu laden. Identität überlebt den Round-Trip, der
geänderte Wert kommt aus der Datenbank, ein rehydriertes Aggregat trägt keine
uncommitteten Events.

---

## 6. Etappe 1 — Restarbeit

### Schritt 3 — **erledigt**

Write-Kontext, Mapping, Repository, Unit of Work, Read-Kontext mit Read-Modell, beide
Projektionen, `WidgetReadStore`, Integration Event mit Mapper,
`AddSampleStateStoredInfrastructure` und beide EF-Migrationen.

Die erzeugte Write-Migration ist der sichtbare Beleg für §5:

```
widgets(id uuid PRIMARY KEY, name varchar(200) NOT NULL, rename_count integer NOT NULL)
```

Eine Id-Spalte, kein Shadow Key, keine Duplikation.

Zwei Entwurfsentscheidungen dabei:

- **`WidgetRenamed` trägt den resultierenden `RenameCount`**, statt ein Inkrement zu
  implizieren. Bei at-least-once würde ein selbst hochzählender Projektionshandler
  bei Redelivery driften. Die Projektion vergleicht stattdessen
  `existing.RenameCount < event.RenameCount` — das deckt Redelivery **und**
  Reihenfolgevertauschung ab.
- **Der Integration-Event-Vertrag liegt vorläufig in Infrastructure.** Mit Etappe 3
  kommt ein Konsument hinzu, dann wandert er nach `Sample.Contracts` — erst dann hat
  ADR-0024 überhaupt einen zweiten Konsumenten zu bewerten.

Werkzeug: `dotnet ef` läuft über ein **lokales** Tool-Manifest
(`.config/dotnet-tools.json`, Version 10.0.10). Die global installierte 9.0.8 ist zu
alt für die EF-10-Runtime.

### Schritt 4 — **erledigt**

`VitalSync.Samples.AppHost` (ein Postgres-Server mit `statestored-write` und
`statestored-read`, dazu RabbitMQ) und der MigrationService, der beide Kontexte
migriert und sich beendet. Kriterium 1 und 2 sind **empirisch belegt**: der Host
wurde gestartet, beide Datenbanken existieren mit `__EFMigrationsHistory` und
`widgets`, der Migrationsprozess war danach beendet und die Api lief.

Drei Befunde:

- **Der produktive AppHost war nicht lauffähig.** `Aspire.AppHost.Sdk/13.1.0` zieht
  ein `Aspire.Hosting.AppHost`, das die installierte DCP-Version ablehnt: „requires a
  newer version of the Aspire.Hosting.AppHost package". Beide AppHosts stehen jetzt
  auf `13.4.6`.
- **Die SDK-Version muss in allen AppHosts identisch sein.** MSBuild löst pro Build
  genau eine Version je SDK auf und ignoriert die andere mit `MSB4240` — welche
  gewinnt, hängt von der Auflösungsreihenfolge ab.
- **CA1848 erzwingt `LoggerMessage`-Delegaten** auch in einem dreizeiligen Worker. Der
  MigrationService verzichtet deshalb auf eigenes Logging; EF protokolliert die
  angewandten Migrationen ohnehin.

Der Worker migriert und **beendet sich**, statt den Host laufen zu lassen: `WaitForCompletion`
hängt daran, und ein Fehlschlag muss als Exit-Code ungleich null sichtbar werden — deshalb
darf die Exception aus `Main` herauslaufen.

### Schritt 4 — ursprüngliche Planung

- Worker registriert beide `DbContext`e **direkt** per `AddDbContext`, **nicht** über
  `AddBuildingBlocks` — er braucht kein Wolverine, keine Outbox, keinen Dispatcher.
- Migriert beide Kontexte, beendet sich; Api startet per `WaitForCompletion` danach.
- `VitalSync.Samples.AppHost`: ein Postgres-Server mit `statestored-write` und
  `statestored-read`, dazu RabbitMQ.
- **Wichtig:** Wolverine legt seine Envelope-Tabellen selbst im Schema `wolverine`
  derselben Write-Datenbank an. Die EF-Migration darf dieses Schema **nicht** kennen,
  sonst räumt sie beim nächsten Lauf die Outbox ab.

### Schritt 5 — Api + gRPC

- Code-first gRPC-Service als **dünner Adapter** auf `ISender` (ADR-0023 Scope Note).
- `Failure → StatusCode`-Übersetzung im Host — laut CLAUDE.md gehört Transport-Mapping
  nie in `Application`.
- gRPC-Server-Reflection aktivieren, damit `grpcurl` ohne BFF reicht.

### Abnahmekriterien Etappe 1

| #   | Nachweis                                                                           | Status       |
| --- | ---------------------------------------------------------------------------------- | ------------ |
| 1   | AppHost startet, alle Ressourcen healthy                                           | offen        |
| 2   | Migration-Worker läuft durch; Tabellen in beiden DBs                               | offen        |
| 3   | `CreateWidget` per grpcurl liefert eine Id                                         | offen        |
| 4   | Zeile in `statestored-write.public.widgets`                                        | offen        |
| 5   | `wolverine.*`-Tabellen in **derselben** Datenbank (eine Transaktion)               | offen        |
| 6   | Read-Modell in `statestored-read` innerhalb von Millisekunden, nicht durch Polling | offen        |
| 7   | `GetWidget` liefert die projizierte Sicht aus der Read-DB                          | offen        |
| 8   | Leerer Name → `InvalidArgument`, **keine** Zeile geschrieben                       | offen        |
| 9   | Integration Event im Exchange `vitalsync.integration-events`                       | offen        |
| 10  | `dotnet nuget why` zeigt `WolverineFx.RuntimeCompilation` transitiv                | **erledigt** |
| 11  | `Program.cs` ohne Wolverine-Konfiguration außer `UseWolverine()`                   | offen        |

**Kriterium 11 ist das eigentliche Urteil über ADR-0027.**

---

## 7. Etappe 2 — EventSourced (Marten)

Dieselbe Projektstruktur, `UseMartenEventSourcing` statt `UseEfCorePersistence`.

- Aggregat leitet von `EventSourcedAggregateRoot` ab (`Version`, `LoadFromHistory`).
- **Vom EF-Mapping-Problem nicht betroffen** — Marten speichert Events, kein
  Schlüssel-Mapping.
- Migrations-Worker ist bewusst **asymmetrisch**: nur der Read-Kontext braucht
  EF-Migrationen; Martens Schema und der Wolverine-Store kommen aus den Bibliotheken.
  Wenn sich diese Asymmetrie unangenehm anfühlt, ist das eine Erkenntnis über unsere
  Verdrahtung, nicht über Aspire.

---

## 8. Etappe 3 — Kreuzverkehr

```
StateStored.Api ── Command ──▶ EF-Commit (Aggregat + Outbox, eine Transaktion)
                          └──▶ Projection ──▶ statestored-read
                          └──▶ Integration Event ──▶ vitalsync.integration-events
                                                              │  [Topic: sample.widget-created]
                                                              ▼
EventSourced.Api ◀── eigene Queue, gebunden mit sample.*
                 └──▶ Command via ISender ──▶ Marten-Append + Outbox
                                         └──▶ Projection ──▶ eventsourced-read
```

Erzwingt die Entscheidung, ob `BuildingBlocksOptions` ein
`SubscribeToIntegrationEvents(...)` braucht — heute verdrahtet BuildingBlocks nur die
Publish-Hälfte, der Konsument müsste Queue-Deklaration und Binding selbst machen.

Ausserdem zu prüfen: **at-least-once über die Servicegrenze** und ob die Projektionen
tatsächlich idempotent sind.

---

## 9. Offene Fragen und Risiken

### Aus der EF-Lösung

- **Optimistische Nebenläufigkeit** — der Concurrency-Token säße jetzt am State. Noch
  nicht entworfen.
- **`Activator.CreateInstance(…, nonPublic: true)`** als Rehydrierungsweg — soll das so
  bleiben, oder bekommt `IRepository` eine `new()`-Beschränkung? Letzteres wäre eine
  Vertragsänderung.

### Aus Schritt 3

- **Reihenfolge über Events hinweg ist durch nichts garantiert.** ADR-0022 verlangt
  „per-aggregate order-aware" Projektionen, aber ein state-stored Aggregat hat **keine
  Version** — nur `EventSourcedAggregateRoot` führt eine. Die Sample-Projektionen
  behelfen sich mit `RenameCount` als fachlicher Ordnungsgröße; das ist kein
  allgemeines Verfahren. **Offene Architekturfrage:** braucht jedes Domain Event eine
  Sequenznummer pro Aggregat, damit Projektionen die ADR-0022-Regel überhaupt erfüllen
  können?
- **`Microsoft.EntityFrameworkCore.Design` verträgt kein `PrivateAssets="all"`** in
  einem Projekt, das von anderen referenziert wird — es kappt die transitive Kante zu
  `EntityFrameworkCore.Relational`, und Konsumenten scheitern zur Laufzeit mit
  `FileNotFoundException`. Sauberer wäre, die Design-Time-Factories in den
  MigrationService zu verschieben und mit diesem als Startup-Projekt zu scaffolden.

### Vorbestehend

- **`ApplyEntityKeyConversions` erfasst keine Complex Types** — es läuft nur über
  `Model.GetEntityTypes()`. Nach der neuen Lösung nicht mehr akut, bleibt aber eine
  Lücke im Helper.
- **`EfCoreMessageStoreRegistration`** — 70 Zeilen Reflection in Wolverine-Interna.
  Nötig, weil container-registrierte `IWolverineExtension`s nach dem Provider-Bau
  laufen und keine Services mehr beitragen können. Der Guard erkennt nur Form-, keine
  Verhaltensänderungen. **Etappe 1 sollte beantworten, ob eine Zeile im Host das
  ersetzen könnte** — dann fällt der Hack ersatzlos weg.
- **Keine CI-Pipeline** — `.github/workflows/` ist leer. Wer eine anlegt, **muss**
  `VITALSYNC_REQUIRE_CONTAINERS=1` setzen, sonst überspringen alle
  Testcontainers-Tests still und der Lauf ist trotzdem grün.
- **Der produktive AppHost widerspricht ADR-0021** — er legt je _eine_ Datenbank pro
  Service an (`nutritionDb`, `fitnessDb`) statt des geforderten Write/Read-Paars.
  Eigene Aufgabe, nicht Teil des Durchstichs.
- **`IntegrationEventSinkDeliveryTests`** ruft die Produktionsmethode inzwischen auf,
  aber die `Task.Delay(250)`-Negativassertion bleibt zeitbasiert.

---

## 10. Konventionen, die beim Anlegen neuer Ordner beißen

Beides ordner-lokal per `.editorconfig`, ein neuer Top-Level-Ordner erbt sie **nicht**:

- **CS1591** (fehlende XML-Docs) — `GenerateDocumentationFile` ist global an,
  abgeschaltet wird pro Ordner (`src/`, `tests/`, `samples/`).
- **CA1707** (Unterstriche in Methodennamen) — sprengt jeden Testnamen im
  `Given_When_Then`-Stil; Testordner brauchen eine eigene `.editorconfig` analog zu
  `BuildingBlocks/tests/.editorconfig`.

- **Generierte EF-Migrationen** erfüllen `AnalysisMode=All` mit
  `TreatWarningsAsErrors` **nicht** (IDE0161, CA1062, IDE0053 …). Einzelne Regel-Ids
  nachzupflegen ist zwecklos, weil sich die Ausgabe des Scaffolders ändert. Der eine
  Hebel, der alles abdeckt:

  ```ini
  [**/Migrations/**.cs]
  generated_code = true
  ```

  Das trifft **jeden** Service, nicht nur den Sample — beim ersten echten Service ist
  zu entscheiden, ob das in die Root-`.editorconfig` gehört.

`TreatWarningsAsErrors` und `AnalysisMode=All` gelten solutionweit.

---

## 11. Nützliche Kommandos

Container-Tests in CI erzwingen:

```bash
VITALSYNC_REQUIRE_CONTAINERS=1 dotnet test
```

---

## 12. Arbeitsweise, die sich bewährt hat

- **Mutationsprobe statt Annahme.** Jeder neue Schutz wurde geprüft, indem der
  Produktionscode kurzzeitig kaputtgemacht wurde. Zweimal stellte sich dabei heraus,
  dass ein Test wertlos war, obwohl er grün lief.
- **Empirie statt Spekulation** bei Framework-Verhalten. Wolverines und EFs
  Fehlermeldungen sind aussagekräftiger als jede Herleitung.
- **Kleine, einzeln verifizierte Commits.** Jeder Commit sollte für sich bauen und
  grün sein — sonst ist er nicht zurücknehmbar.
