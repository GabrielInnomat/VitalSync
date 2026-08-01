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
| 1      | `StateStored` — EF Core, ein Aggregat, gRPC, Aspire, Migrationen          | **erledigt**  |
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
│   ├── VitalSync.Sample.StateStored.Contracts       (gRPC-Vertrag, vom BFF referenzierbar)
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
| Topic-Namen       | explizites `[Topic("<kontext>.<event>")]` in kebab-case, **ohne** Startup-Validator (offen, siehe §9)                                        |
| Umfang Messaging  | nur Publish-Seite; Subscribe kommt in Etappe 3                                                                                               |
| gRPC              | code-first (ADR-0003) — die Bibliothekswahl (`protobuf-net.Grpc`) trifft der Sample faktisch für die Plattform und verdient später einen ADR |

---

## 3. Commit-Historie

```
afb70d5  Bind integration-event publication to the handler's message context
2f4d862  Route integration events to the platform topic exchange
816d72d  Make container-backed tests fail instead of skip in CI
51a09ef  Add project skeleton for the state-stored sample service
78e6324  Add Widget aggregate and its CQRS slice to the state-stored sample
4d8d492  Persist the aggregate's state instead of the aggregate itself
935183b  Complete the read side and migrations of the state-stored sample
795c619  Add the walking-skeleton plan and findings as a working document
d98fc9a  Wire the state-stored sample into its own Aspire host
16a0d63  Let the host wire the EF Core outbox, and complete the gRPC slice
accec33  Pin that the aggregate and its outbox entry share one command
ae0a415  Discover the sample's projections by scanning, and drop gRPC reflection
c79af52  Validate the gRPC contract at build time
```

Die Messaging- und Persistenz-Commits wurden einzeln in temporären Worktrees gebaut
und getestet, sind also einzeln per `git revert` zurücknehmbar. **Achtung:** dafür
braucht der Worktree einen **kurzen Pfad** — die Sample-Projektnamen sind lang genug,
dass `obj\Debug\net10.0\…` unter einem tiefen Verzeichnis die 260-Zeichen-Grenze von
Windows reißt. MSBuild meldet das irreführend als `MSB3030` („Datei konnte nicht
kopiert werden"), nicht als Pfadfehler.

**Was inhaltlich behoben wurde:** Integration Events erreichten den Broker nie (keine
Routing-Regel, Wolverine verwirft routenlose Nachrichten stillschweigend); die erste
Domain-Event-Zustellung scheiterte immer (`ServiceLocationPolicy.NotAllowed`); der
EF-Persistenzpfad konnte nie committen; der produktive Aspire-AppHost war nicht
startfähig; und ein Aggregat war mit EF Core überhaupt nicht abbildbar (§5).

**Keiner dieser Fehler war durch Build oder Testsuite sichtbar.** Das ist die
Rechtfertigung des Durchstichs in einem Satz.

---

## 4. Stand

Etappe 1 ist **abgeschlossen**, Arbeitsbaum sauber, **167 Tests grün**, Build ohne
Warnungen. Vier davon sind Smoke-Tests, die ohne gesetztes `SAMPLE_API_URL`
überspringen (siehe §11).

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

### Schritt 5 — **erledigt**, mit dem entscheidenden Befund

Code-first gRPC-Vertrag in eigener Bibliothek (`…​.Contracts`), `WidgetGrpcService` als
dünner Adapter auf `ISender`, `Failure → StatusCode` im Host, Server-Reflection, und
vier Smoke-Tests, die über einen echten gRPC-Kanal laufen und ohne
`SAMPLE_API_URL` überspringen.

**Abnahmekriterium 11 ist gefallen — ADR-0027 trägt nicht vollständig.** Wolverine 3.0
verbietet einer container-registrierten `IWolverineExtension`, die Service-Collection
zu ändern; beide Hälften des EF-Outbox tun genau das. Der Host ruft deshalb **eine**
Zeile selbst auf (`UseBuildingBlocksEfCorePersistence`); ADR-0027 hat dazu ein
Amendment. `EfCoreMessageStoreRegistration` (~70 Zeilen Reflection) ist ersatzlos
gelöscht — es löste ohnehin nur die halbe Aufgabe, und die Tests merkten es nicht,
weil sie ihre Hosts mit noch änderbarer Service-Collection bauen.

Alle übrigen Kriterien wurden am laufenden System belegt: Zeilen in der Write-DB,
acht `wolverine.*`-Tabellen in **derselben** Datenbank, Read-Modelle in der Read-DB,
`GetWidget` aus der Read-DB, leerer Name → `InvalidArgument` **ohne** geschriebene
Zeile, und der Topic-Exchange `vitalsync.integration-events` auf RabbitMQ.

Ein Projekt mehr als geplant: `VitalSync.Sample.StateStored.Contracts`. CA1515 hat es
erzwungen — öffentliche Typen gehören nicht in eine Anwendung, ein gRPC-Vertrag ist
aber öffentlich per Definition.

### Schritt 5 — ursprüngliche Planung

- Code-first gRPC-Service als **dünner Adapter** auf `ISender` (ADR-0023 Scope Note).
- `Failure → StatusCode`-Übersetzung im Host — laut CLAUDE.md gehört Transport-Mapping
  nie in `Application`.
- gRPC-Server-Reflection aktivieren, damit `grpcurl` ohne BFF reicht.

### Abnahmekriterien Etappe 1

Alle am **laufenden System** geprüft, nicht am Build.

| #   | Nachweis                                                              | Status          |
| --- | --------------------------------------------------------------------- | --------------- |
| 1   | AppHost startet, alle Ressourcen healthy                              | belegt          |
| 2   | Migration-Worker läuft durch; Tabellen in beiden DBs                  | belegt          |
| 3   | `CreateWidget` per gRPC liefert eine Id                               | belegt          |
| 4   | Zeilen in `statestored_write.public.widgets`                          | belegt          |
| 5   | 8 `wolverine.*`-Tabellen in **derselben** Datenbank                   | belegt          |
| 6   | Read-Modell in `statestored_read`, ohne Warten auf Polling            | belegt          |
| 7   | `GetWidget` liefert die projizierte Sicht aus der Read-DB             | belegt          |
| 8   | Leerer Name → `InvalidArgument`, **keine** Zeile geschrieben          | belegt          |
| 9   | Topic-Exchange `vitalsync.integration-events` auf RabbitMQ            | belegt          |
| 10  | `dotnet nuget why` zeigt `WolverineFx.RuntimeCompilation` transitiv   | belegt          |
| 11  | `Program.cs` ohne Wolverine-Konfiguration außer `UseWolverine()`      | **gescheitert** |

Kriterium 3 wurde nicht mit `grpcurl` geprüft (nicht installiert), sondern mit einem
typisierten code-first-Client aus dem Testprojekt — der zugleich zeigt, wie der BFF den
Service konsumieren wird.

Kriterium 8 ist doppelt belegt: der abgelehnte Aufruf hinterließ **keine** Zeile,
sichtbar daran, dass nach drei Create-Versuchen nur zwei Zeilen existierten.

---

### Nacharbeiten nach Etappe 1

Drei kleine Commits, ausgelöst durch Rückfragen statt durch Testläufe:

- **`EfCoreOutboxAtomicityTests`** (`accec33`) — ADR-0022 verspricht, dass Aggregat und
  Outbox-Eintrag gemeinsam committen. Das war nur in einer Logzeile beobachtet, nie
  behauptet. Der Test zeichnet über einen EF-Interceptor das ausgeführte SQL auf und
  verlangt **genau einen** Command, der beide Tabellen berührt.
  Nebenbefund aus der Mutationsprobe: `IDbContextOutbox.PublishAsync` hängt den Envelope
  bereits beim Publizieren an den ChangeTracker — ein zusätzliches `SaveChanges` danach
  trennt nichts.
- **Projektionen per Scan** (`ae0a415`) — `AddHandlersFrom` erfasst `IProjectionHandler<>`
  und `IIntegrationEventMapper` **schon immer**; der Sample zeigte nur auf die
  Application-Assembly, während beide in Infrastructure liegen. Eine Zeile mehr, drei
  Handregistrierungen weniger, und eine stille Fehlerquelle weniger: eine vergessene
  Projektion wirft nicht, sie aktualisiert das Read-Modell einfach nie.
- **`protobuf-net.BuildTools`** (`c79af52`) — Analyzer, der den gRPC-Vertrag zur Buildzeit
  prüft, plus Umstellung auf `[ProtoContract]`/`[ProtoMember(n)]`. Die Zahl ist eine
  **Feldidentität auf der Leitung**, keine Sortierung; `Order` legte das Gegenteil nahe.
  Nachgewiesen: doppelte Nummer → `error PBN0003`.

Die gRPC-Server-Reflection wurde wieder entfernt. Sie war für `grpcurl` gedacht, das gar
nicht verfügbar war, und hätte nur das Service-Schema offengelegt. Falls sie zum Debuggen
zurückkommt, gehört sie hinter `IsDevelopment()`.

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

### Aus Etappe 1 offen geblieben

- **Vergessenes `[Topic]` fällt nicht auf.** Ein `IIntegrationEvent` ohne das Attribut
  bekommt von Wolverine einen aus dem CLR-Typnamen abgeleiteten Routing Key, landet damit
  unter einem Schlüssel, den kein Konsument gebunden hat, und verschwindet still. Der
  naheliegende Schutz wäre ein Startup-Validator analog zu
  `HandlerRegistrationStartupValidator`, der die gescannten Assemblies nach
  `IIntegrationEvent`-Typen ohne `[Topic]` durchsucht. **Noch nicht umgesetzt.**
- **Fehlkonfiguration ist ungleich abgedeckt.** Für Commands und Queries gibt es Tests,
  die Mehrdeutigkeit und fehlende Handler mit Exceptions festnageln
  (`HandlerRegistrationTests`, `HandlerStartupValidationTests`, vier Fixture-Assemblies).
  Für Projektionen und Mapper existiert nur der Happy Path — bei Projektionen zu Recht,
  denn mehrere Handler pro Event und Events ganz ohne Projektion sind beide legitim.
- **Der gRPC-Vertrag liegt noch beim Service.** `VitalSync.Sample.StateStored.Contracts`
  ist die richtige Struktur, aber sobald der BFF ihn konsumiert, stellt sich die Frage
  nach einem geteilten Paket. Für das Integration Event ist sie noch ganz offen.

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
