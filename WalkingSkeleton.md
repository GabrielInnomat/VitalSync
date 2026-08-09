# Walking Skeleton — Plan und Stand

Arbeitsdokument zum vertikalen Durchstich unter `samples/`. Es hält fest, **warum** es
ihn gibt, **was** bereits erledigt ist, **was** als Nächstes kommt und **welche Fragen**
noch offen sind. Gedacht als Übergabe: wer hier weitermacht, soll ohne den
ursprünglichen Gesprächsverlauf auskommen.

Stand: 2026-08-01 (alle drei Etappen abgeschlossen)

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
| 2      | `EventSourced` — dieselbe Struktur auf Marten                             | **erledigt**  |
| 3      | Kreuzverkehr — Integration Event von Etappe 1 nach Etappe 2 über RabbitMQ | **erledigt**  |

```
samples/
├── VitalSync.Samples.AppHost/                    eigener Aspire-Host (nicht der produktive!)
├── VitalSync.Sample.Contracts/                   Integration-Event-Verträge mit mehr als einem Konsumenten
├── StateStored/                                  EF Core
│   ├── VitalSync.Sample.StateStored.Domain
│   ├── VitalSync.Sample.StateStored.Application
│   ├── VitalSync.Sample.StateStored.Infrastructure
│   ├── VitalSync.Sample.StateStored.Api               (gRPC, code-first)
│   ├── VitalSync.Sample.StateStored.MigrationService
│   ├── VitalSync.Sample.StateStored.Contracts       (gRPC-Vertrag, vom BFF referenzierbar)
│   └── VitalSync.Sample.StateStored.Tests
└── EventSourced/                                 Marten — dieselben sieben Projekte,
    └── … (Domain, Application, Infrastructure,       ein Aggregat `Gadget` statt `Widget`
           Api, MigrationService, Contracts, Tests)
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
| Routing-Topologie | ein Topic-Exchange für die ganze Plattform; der Name kommt seit ADR-0023 vom Host (`VitalSyncMessaging.IntegrationEventExchangeName` = `vitalsync.integration-events`) |
| Topic-Namen       | explizites `[Topic("<kontext>.<event>")]` in kebab-case, **ohne** Startup-Validator (inzwischen gelöst: `[IntegrationEventTopic]` mit Publish-Fail-Fast und Präfix-Guard gegen den eigenen Kontextnamen, siehe ADR-0023)     |
| Umfang Messaging  | zuerst nur Publish-Seite; die Subscribe-Seite kam in Etappe 3 und liegt seither ebenfalls in BuildingBlocks (ADR-0023-Amendment)             |
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
001fdea  Add the event-sourced sample's aggregate and CQRS slice
ad2275d  Add the read side and Marten wiring of the event-sourced sample
0d83f8c  Stop a redelivered create from undoing a rename in the read model
738d9fb  Complete the event-sourced sample with gRPC, migrations and Aspire
66a81b6  Record stage 2 of the walking skeleton and map the samples folder
abaeae3  Share the widget contract and mirror it into the event-sourced context
133864e  Let a subscribing handler dispatch commands with ISender
cab79bc  Subscribe the event-sourced service and prove the context crossing
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
startfähig; ein Aggregat war mit EF Core überhaupt nicht abbildbar (§5); und eine
redelivered Create-Projektion machte eine Umbenennung im Read-Modell wieder rückgängig
(§7).

**Keiner dieser Fehler war durch Build oder Testsuite sichtbar.** Das ist die
Rechtfertigung des Durchstichs in einem Satz.

---

## 4. Stand

Alle drei Etappen sind **abgeschlossen**, Arbeitsbaum sauber, **210 Tests grün**, Build
ohne Warnungen. Elf davon sind Smoke-Tests gegen ein laufendes System, die ohne gesetzte
`SAMPLE_*_API_URL` überspringen (siehe §11); mit laufenden Services laufen auch sie
grün. Die containergestützten Tests brauchen Docker und überspringen sonst — ausser
`VITALSYNC_REQUIRE_CONTAINERS=1` ist gesetzt.

Damit hat der Durchstich seine Aufgabe erfüllt: **jede** Behauptung, die ADR-0027 über
die Verdrahtung aufstellt, ist entweder belegt oder mit einem Amendment korrigiert, und
BuildingBlocks hat erstmals echte Konsumenten. Der Ordner `samples/` kann gelöscht
werden, sobald der erste echte Service steht — mit Ausnahme der Erkenntnisse in diesem
Dokument.

---

## 5. Das EF-Mapping-Problem und seine Lösung

Der wichtigste Befund des Durchstichs. Diese Analyse sollte erhalten bleiben.

### Das Problem

`AggregateRoot<TKey, TState>` deklariert:

```csharp
public sealed override TKey Id => State.Id;
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
  `IDomainEventOwner` (damals noch `IDomainEventsManager`). Domänencode sieht die Member
  nicht und kann den Event-Fold nicht per Hand-Restore umgehen.
- **`EfCoreRepository`** — lädt den State per `context.FindAsync(stateType, [id])` und
  rehydriert ein leeres Aggregat darum; `AddAsync` legt den State in den Context.
- **`EfCoreAggregateTracker`** (neu) — Gegenstück zum bestehenden
  `MartenAggregateTracker`. Nötig, weil EFs ChangeTracker nur noch States sieht.
  Beide Persistenzpfade sind damit erstmals symmetrisch.
- **`EfCoreUnitOfWork`** — kopiert vor dem Speichern per `CurrentValues.SetValues` den
  aktuellen State auf den getrackten Eintrag. **Das ist der Kern:** States sind
  unveränderlich, jedes Event ersetzt die Instanz, EF sähe sonst nichts zu speichern.
- **Nachtrag (2026-08-04, ADR-0031):** `SetValues` deckt nur Skalare ab. Seit
  `AggregateStateGraph.Reconcile` wird zusätzlich der **Owned-Graph** über den
  Schlüssel abgeglichen — in jeder Tiefe, weil blosses Zuweisen der Kollektion nur eine
  Ebene weit trägt —, und Kinder eines Aggregats mappen verbindlich als **Owned Types**.
  Das Sample ist dafür nicht mehr flach: `Widget` hat jetzt `Parts` (`widget_parts`),
  inkl. Domänen-, Modell-, Projektions- und Smoke-Tests.
- **Nachtrag (2026-08-04, ADR-0032):** Das Kind trägt inzwischen auch Verhalten.
  `WidgetPart` ist eine Hülle über `WidgetPartState` und löst seine Events über den Root
  aus (`IDomainEventRaiser`) — ohne Schemaänderung und ohne Migration, weil das Owned
  Mapping schon auf genau diesen Record zeigte. Im event-sourced Sample zeigt
  `GadgetComponent` denselben Pfad über Command und Replay.

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
- **Der Integration-Event-Vertrag lag vorläufig in Infrastructure.** Mit Etappe 3 kam
  ein Konsument hinzu, und er ist nach `VitalSync.Sample.Contracts` gewandert — erst
  dann hatte ADR-0024 überhaupt einen zweiten Konsumenten zu bewerten.

Werkzeug: `dotnet ef` muss in **Version 10** vorliegen — die global installierte 9.0.8
ist zu alt für die EF-10-Runtime. Das lokale Tool-Manifest, das das früher sicherstellte,
wurde in `7b22dc8` entfernt (siehe §7).

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

#### Nachtrag (2026-08-09): der Worker legt jetzt auch Schema und Topologie an

Mit ADR-0037 ist Provisionierung eine Rolle. Beide Sample-Worker rufen dieselbe
Kontext-Erweiterung wie ihr Service auf, nur mit
`InfrastructureProvisioning.AtStartup`, starten den Host (Marten-Schema,
Wolverine-Tabellen, Exchange und Queue entstehen dabei), fahren danach die
EF-Migrationen für Write- und Read-Kontext und stoppen wieder. Die Apis laufen mit
`Never` und scheitern beim Start, wenn etwas fehlt. Zwei Folgen für den AppHost:
`eventsourced-migrations` braucht jetzt `eventsourced-write` **und** `messaging`, und
`WaitForCompletion` ist auf beiden Pfaden inhaltlich gedeckt statt nur formal.
Nicht empirisch belegt: der Ende-zu-Ende-Lauf des Samples-AppHost gelang lokal nicht
(Aspire startete die Projektprozesse nicht) — abgesichert ist das derzeit allein durch CI.

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

> **Nachtrag 2026-08-03:** Kriterium 11 steht wieder — und schärfer als ursprünglich
> formuliert. Die Wolverine-Schranke gilt nur für **container-registrierte** Extensions,
> nicht für einen `UseWolverine`-Callback. Building Blocks setzt diesen Aufruf seit dem
> zweiten ADR-0027-Amendment selbst ab (`builder.AddBuildingBlocks(…)` auf
> `IHostApplicationBuilder`) und holt sich den Connection String aus der bereits
> getroffenen Auswahl. Der Host ruft jetzt **gar kein** `UseWolverine` mehr auf, und die
> Write-Datenbank wird genau einmal benannt.

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

## 7. Etappe 2 — EventSourced (Marten) — **erledigt**

Dieselbe Projektstruktur, dasselbe Vorgehen, ein Aggregat `Gadget` statt `Widget`.
Der Unterschied im Produktionscode ist **eine Zeile**: `UseMartenEventSourcing(...)`
statt `UseEfCorePersistence<TContext>(...)`.

Was zusätzlich anders ist, ist bewusst gewählt, nicht erzwungen:

- **Ein drittes Event `GadgetRetired` und eine Geschäftsregel.** Etappe 1 hatte nur eine
  Validierungsregel, damit war der Pfad `BusinessRuleViolationException → FailureCategory.BusinessRule`
  nie am laufenden System belegt. Jetzt ist er es (`FailedPrecondition` über gRPC).
  Zurückziehen ist zudem eine Zustandsänderung, kein Löschen — passend zu ADR-0026 und
  in einem Event Store ohnehin die einzige Option.
- **Nur der Read-Kontext hat Migrationen.** Der Migrations-Worker ist deshalb halb so
  groß wie sein Gegenstück, und die Write-Datenbank hat kein `__EFMigrationsHistory`.

### Was das Aggregat angeht: die ADR-0025-Behauptung hält

`Gadget` ist Zeile für Zeile so geschrieben wie ein state-stored Aggregat — dieselben
`RaiseEvent`-Aufrufe, derselbe State-Fold, dieselben Regeln. Die Basisklasse trägt
`Version` und `LoadFromHistory` und sonst nichts. Vom EF-Mapping-Problem (§5) ist die
Seite gar nicht betroffen: Marten speichert Events, es gibt keinen Schlüssel zu mappen.

Eine Nebenwirkung, die man kennen muss: weil `MartenEventSourcedRepository` die
Rehydrierung als „leeres Aggregat + Stream falten" umsetzt, ist der parameterlose
Konstruktor **Pflicht**, nicht Kosmetik. Er war dafür ursprünglich per `new()`-Constraint
öffentlich erzwungen — seit ADR-0025 ist er privat und die Hülle kommt über
die interne `AggregateFactory` (ADR-0025-Amendment 2026-08-04).

### Abnahmekriterien Etappe 2

Alle am **laufenden System** geprüft.

| #   | Nachweis                                                                            | Status     |
| --- | ----------------------------------------------------------------------------------- | ---------- |
| 1   | AppHost startet beide Services nebeneinander, alle Ressourcen healthy               | belegt     |
| 2   | Migrations-Worker migriert **nur** die Read-DB und beendet sich                     | belegt     |
| 3   | `CreateGadget` per gRPC liefert eine Id                                             | belegt     |
| 4   | Streams `Gadget/{guid}` in `eventsourced_write.mt_streams`, Events in `mt_events`   | belegt     |
| 5   | `wolverine_*`-Tabellen in **derselben** Datenbank wie der Event Store               | belegt     |
| 6   | Read-Modell in `eventsourced_read.gadgets`, ohne Warten auf Polling                 | belegt     |
| 7   | Zweimaliges Umbenennen: Stream wird geladen, gefaltet, auf Version 3 angehängt      | belegt     |
| 8   | Leerer Name → `InvalidArgument`, **kein** Stream angelegt                           | belegt     |
| 9   | Zweimaliges Zurückziehen → `FailedPrecondition` (Geschäftsregel, nicht Validierung) | belegt     |
| 10  | Integration Event **tatsächlich am Broker**, Routing Key `sample.gadget-retired`    | belegt     |
| 11  | `Program.cs` ohne Wolverine-Konfiguration außer `UseWolverine()`                    | **belegt** |

Kriterium 11 ist das in Etappe 1 gescheiterte Kriterium. Auf der Marten-Seite hält
ADR-0027 **vollständig**: der Host ruft `builder.Host.UseWolverine();` ohne Argument auf
und startet. Marten bringt seinen Message Store über `IntegrateWithWolverine` aus der
Service-Collection mit, also bleibt für den Host nichts übrig. Die Ausnahme im
ADR-0027-Amendment ist damit nachweislich auf den EF-Core-Pfad begrenzt und keine
allgemeine Schwäche der Verdrahtung.

Kriterium 10 geht über das entsprechende Kriterium 9 aus Etappe 1 hinaus: dort war nur
belegt, dass der Exchange **existiert**. Hier wurde eine Probe-Queue mit `sample.*`
gebunden, ein Retire ausgelöst und die Nachricht aus der Queue gelesen — mit Routing
Key, Correlation-Id und JSON-Payload. Das ist der erste echte Beleg, dass die
Publish-Hälfte durchgängig funktioniert. (Die Topic-Präfixe der beiden Samples heißen seit
ADR-0023 `sample-state-stored` und `sample-event-sourced`; hier stehen sie im Wortlaut des
damaligen Laufs.)

### Befunde

- **Event Sourcing löst die Reihenfolgefrage nicht.** Das war die Hoffnung aus §9: ein
  event-sourced Aggregat *hat* eine Version, also könnten Projektionen endlich
  ADR-0022s „per-aggregate order-aware" erfüllen. Empirisch falsch. Die Version steht in
  `mt_events.version`, aber der Projektionshandler bekommt ein nacktes `IDomainEvent` —
  im Event-JSON steht keine Sequenznummer. Die Sample-Projektionen behelfen sich mit
  derselben fachlichen Ordnungsgröße wie auf der State-Stored-Seite. **Die
  Architekturfrage bleibt offen und gilt für beide Persistenzstile gleichermaßen.**
  Verschärfend: `Version` ist explizit implementiert, das Aggregat sieht sie also selbst
  nicht und könnte sie nicht einmal freiwillig ins Event schreiben (nur über einen Cast
  auf `IEventSourcedAggregateRoot<TKey>`).
- **Ein echter Fehler, in beiden Samples.** Beim Schreiben der Projektionstests fiel auf,
  dass die Create-Projektion ihren Namen bei jeder Zustellung zurückschrieb: eine
  redelivered `WidgetCreated`/`GadgetCreated` machte damit eine bereits projizierte
  Umbenennung rückgängig. Behoben (`0d83f8c`), indem die Create-Projektion denselben
  fachlichen Ordinalwert prüft wie die Rename-Projektion. Mutationsprobe: Fix
  zurückgenommen → zwei Tests rot.
- **Wolverine-Tabellen liegen bei Marten im `public`-Schema** derselben Write-Datenbank
  (`wolverine_*`), nicht in einem eigenen `wolverine`-Schema wie im EF-Pfad. Die Warnung
  aus Etappe 1 („die EF-Migration darf das Wolverine-Schema nicht kennen") hat hier kein
  Gegenstück, weil es keine Write-Migration gibt.
- **Typisierte Schlüssel serialisieren ihre berechneten Member ins Event.** Im Store
  stand `"GadgetId": {"Value": "…", "IsEmpty": false}` — `IsEmpty` ist eine berechnete
  Eigenschaft und landete dauerhaft im Eventstrom. Harmlos beim Lesen, aber Events sind
  unveränderlich: was einmal drinsteht, bleibt drin. Inzwischen gelöst (
  ADR-0034): ein Schlüssel serialisiert als nackter Wert.
- **Feldnamen im Event sind abgeleitet.** Kein einziges `[JsonPropertyName]` im Repository:
  der JSON-Name eines Feldes war der CLR-Property-Name, ein Rename deserialisiert gespeicherte
  Events still auf `default`. Inzwischen gelöst (ADR-0035): ein eingecheckter
  Schema-Snapshot pro Service macht den Rename rot.
- **Integration Events gehen mit `delivery_mode: 1` an RabbitMQ**, also nicht persistent.
  Bis zur Übergabe schützt der Outbox; danach würde ein Broker-Neustart die Nachricht
  verlieren.
- **`.config/dotnet-tools.json` existiert nicht mehr** (in `7b22dc8` entfernt). Das
  global installierte `dotnet-ef` ist 9.0.8 und damit zu alt für die EF-10-Runtime; die
  Read-Migration wurde mit einem in ein temporäres Verzeichnis installierten
  `dotnet-ef 10.0.10` erzeugt (`--tool-path`). Wer regelmäßig Migrationen scaffoldet,
  sollte das Manifest wiederherstellen.

---

## 8. Etappe 3 — Kreuzverkehr — **erledigt**

```
StateStored.Api ── Command ──▶ EF-Commit (Aggregat + Outbox, eine Transaktion)
                          └──▶ Projection ──▶ statestored-read
                          └──▶ Integration Event ──▶ <Plattform-Exchange>
                                                              │  [Topic: sample-state-stored.widget-created]
                                                              ▼
EventSourced.Api ◀── eigene Queue, gebunden mit sample-state-stored.*
                 └──▶ Command via ISender ──▶ Marten-Append + Outbox
                                         └──▶ Projection ──▶ eventsourced-read
```

Ein im state-stored Kontext angelegtes Widget erscheint als gespiegeltes Gadget im
event-sourceten — ohne gemeinsame Datenbank und ohne synchronen Aufruf.

### Die Entscheidung, die diese Etappe erzwungen hat

`BuildingBlocksOptions` hat jetzt ein **`SubscribeToIntegrationEvents(...)`**. Der Weg
dahin war bewusst empirisch: erst wurde die Subscribe-Hälfte **im Service-Host**
verdrahtet, in Betrieb genommen und gemessen, dann verschoben.

Was Variante „Service verdrahtet selbst" gekostet hat:

- **12 Zeilen** im `Program.cs` jedes Konsumenten (Discovery, Queue, durable Inbox,
  Exchange-Binding).
- Den **Exchange-Namen als Literal**, weil BuildingBlocks seine Konstante `internal`
  hält — ein Konsument konnte den Wert, den er treffen muss, nicht referenzieren.
  (Seit ADR-0023 gibt es diese Konstante in BuildingBlocks gar nicht mehr: den Namen
  bestimmt der Host und reicht ihn an `UseWolverineMessaging` durch.)
- **Abnahmekriterium 11 aus Etappe 2** (blankes `UseWolverine()`) war wieder weg.
- Und der eigentliche Fehler (siehe unten) steckte trotzdem in BuildingBlocks, war für
  den Service also gar nicht behebbar.

Nach dem Verschieben nennt der Service nur noch Queue-Name, Consumer-Assembly und
Topic-Pattern; der Host steht wieder auf `builder.Host.UseWolverine();`. ADR-0023 hat
dazu ein Amendment (das alte „Publish-Hälfte only" ist damit abgelöst).

### Der Befund, der die Etappe fast versenkt hätte

Die ersten vier Integration Events über die Kontextgrenze gingen **spurlos verloren**.
Die Queue war korrekt gebunden, die Nachrichten kamen an, der Inbox-Status war
`Handled` — und es passierte nichts. Keine Exception beim Aufrufer, keine Dead Letter,
kein Retry.

Ursache: Wolverine konnte den Handler nicht generieren.

```
InvalidServiceLocationException: Found service locations while generating code for
Message Handler for WidgetCreatedIntegrationEvent, but ServiceLocationPolicy.NotAllowed
is in effect
```

`ISender` ist genau der Typ, den **jeder** Integration-Event-Konsument braucht (ADR-0023
Scope Note: der Handler ist ein dünner Adapter auf `ISender`), und seine Implementierung
nimmt einen `IServiceProvider` — für Wolverines Codegen ist das Service Location. Es ist
derselbe Fehlermodus, den Etappe 1 für `IDomainEventPublisher` und
`IIntegrationEventSinkFactory` gefunden hat, nur eine Ebene weiter außen. `ISender` ist
jetzt ebenfalls opt-in, in `ApplyBuildingBlocksDomainEventRouting`.

**Wie es gefunden wurde:** nicht im Debugger, sondern indem die beiden Services aus dem
Aspire-Host herausgenommen und von Hand gegen dieselben Container gestartet wurden —
Aspire schickt Service-Logs nur ins Dashboard, und genau die eine `fail:`-Zeile hing
daran. Das Vorgehen steht in §11.

### Abnahmekriterien Etappe 3

| #   | Nachweis                                                                          | Status |
| --- | --------------------------------------------------------------------------------- | ------ |
| 1   | Widget im StateStored-Kontext angelegt → Gadget-Stream im EventSourced-Kontext     | belegt |
| 2   | Beide Kontexte teilen **nur** den Identifikator, keine Datenbank, keinen Aufruf    | belegt |
| 3   | Read-Modell des Konsumenten zeigt den gespiegelten Namen                           | belegt |
| 4   | Umbenennung im Konsumenten reist **nicht** zurück (der Spiegel ist einseitig)      | belegt |
| 5   | Queue `eventsourced.integration-events`, gebunden mit `sample-state-stored.*` — von BuildingBlocks | belegt |
| 6   | `Program.cs` ohne Wolverine-Konfiguration außer `UseWolverine()`                   | belegt |
| 7   | Wiederholte Zustellung erzeugt kein zweites Gadget                                 | Test   |
| 8   | Ein dauerhaft scheiternder Konsument wird 3× wiederholt und dann dead-lettert      | Test   |

Kriterium 7 ist durch `MirrorWidgetTests` abgedeckt, nicht am laufenden System: eine
Redelivery lässt sich von außen nicht zuverlässig erzwingen. Die Idempotenz ruht auf
einer Entscheidung, nicht auf Bookkeeping — **das gespiegelte Aggregat übernimmt die
Identität des Originals**. Eine frisch erzeugte Id würde bei jeder Wiederholung ein
weiteres Gadget anlegen.

Kriterium 8 ist durch `DeadLetterTests` abgedeckt (Testcontainers: PostgreSQL **und**
RabbitMQ, echte Produktionsverdrahtung über `AddBuildingBlocks`). Ein Konsument, der
immer wirft, wird genau **viermal** aufgerufen — der erste Versuch plus die drei
Wiederholungen aus `ApplyBuildingBlocksMessagingDefaults` — und die Nachricht ist danach
aus der Dead-Letter-Queue lesbar. Doppelte Mutationsprobe: eine Wiederholung weniger →
`Expected: 4, Actual: 2`; `MoveToErrorQueue()` durch `Discard()` ersetzt → Timeout beim
Warten auf die Nachricht. Beide Hälften der Zusage sind damit einzeln festgenagelt.

**Dabei kam der eigentliche Befund heraus:** die Nachricht landet **nicht** in der
Tabelle `wolverine_dead_letters` der Write-Datenbank, sondern in Wolverines
`wolverine-dead-letter-queue` **auf dem Broker**. Der Test suchte zuerst in der
Datenbank und lief in einen Timeout, obwohl Retry und Dead-Lettering einwandfrei
funktionierten. Wer im Betrieb eine verschluckte Nachricht sucht, schaut sonst an der
falschen Stelle — die Tabelle existiert nämlich, sie bleibt nur leer.

### Weitere Befunde

- **Ein Kontext bekommt seine eigenen Integration Events zurück.** `sample.*` matchte zur
  Zeit dieser Etappe auch `sample.gadget-retired`, das der Konsument selbst publizierte.
  Damals folgenlos (kein Handler), aber wer unter einem Präfix publiziert **und**
  konsumiert, muss damit rechnen. **Inzwischen gelöst** (ADR-0023): Beide Samples
  haben eigene Kontextnamen (`sample-state-stored`, `sample-event-sourced`), jedes Event
  trägt seinen Absenderkontext im Header, ein eigenes Event wird vor dem Handler verworfen,
  und ein Handler auf den eigenen Kontext scheitert beim Start.
- **Die Consumer-Assembly muss explizit angegeben werden.** Naheliegend wäre, die
  Assemblies aus `AddHandlersFrom` wiederzuverwenden — das wäre ein Fehler: Wolverine
  erkennt Handler an der Namenskonvention und würde `CreateGadgetHandler` als
  Wolverine-Handler für `CreateGadget` registrieren, also am `ISender`-Pipeline vorbei
  dispatchen.
- **CA1711 und Wolverines Konvention kollidieren.** Wolverine sucht Typen auf `Handler`
  oder `Consumer`, CA1711 reserviert das Suffix `EventHandler` für Delegates. `Consumer`
  ist der einzige Name, der beides erfüllt.
- **Die Result/Exception-Grenze kippt am Transportrand.** Innerhalb des Services ist ein
  Fehlschlag ein Wert; im Wolverine-Handler bestätigt ein normales Return die Nachricht.
  Der Konsument muss werfen, sonst ist eine fehlgeschlagene Nachricht still weg.
- **ADR-0024 wurde zum ersten Mal wirklich angewandt.** `WidgetCreatedIntegrationEvent`
  ist nach `VitalSync.Sample.Contracts` gewandert, weil es jetzt einen zweiten
  Konsumenten hat. `GadgetRetiredIntegrationEvent` ist **bewusst** dort geblieben, wo es
  war — es hat keinen. Symmetrie wäre genau die Begründung, die ADR-0024 ablehnt.
- **Das geteilte Vertragspaket referenziert Wolverine**, wegen `[Topic]`. Ein
  publizierter Vertrag benennt damit seinen Transport. Preis dafür, dass der Routing Key
  Vertragsbestandteil ist statt aus dem CLR-Namespace abgeleitet. _(Inzwischen behoben:
  `[IntegrationEventTopic]` in `BuildingBlocks.Application` ersetzt Wolverines `[Topic]`,
  die WolverineFx-Referenz im Vertragspaket ist entfallen — ADR-0023-Amendment
  2026-08-03.)_

---

## 9. Offene Fragen und Risiken

**Dieser Abschnitt führt nur noch offene Punkte.** Gelöste wurden am 2026-08-09 entfernt; ihre
Begründungen leben in den ADRs unter `docs/architecture/decisions/` und in den Tests weiter, die
Versionsgeschichte hat den vollen Wortlaut. Jeder Punkt ist in [todo.md](todo.md) mit einer
Priorität geführt.

| Nr.   | Titel                                                      | Herkunft     | TODO    |
| ----- | ---------------------------------------------------------- | ------------ | ------- |
| WS-04 | `EntityFrameworkCore.Design` verträgt kein `PrivateAssets` | Schritt 3    | TODO-35 |
| WS-07 | Der gRPC-Vertrag liegt noch beim Service                   | Etappe 1     | TODO-36 |
| WS-15 | `ApplyEntityKeyConversions` erfasst keine Complex Types    | vorbestehend | TODO-19 |

Nachzügler aus Commit `e44ae9b`: die drei produktiven MigrationService-Worker sind leere Hüllen
(`Host.CreateApplicationBuilder`, `Build()`, kein `Run()`), `WaitForCompletion` ist damit heute
eine Zusage ohne Inhalt. Bleibt bewusst offen, bis pro Kontext feststeht, wie dort gespeichert
wird — siehe [todo.md](todo.md), TODO-46. Seit ADR-0037 hängt daran ein zweiter Auftrag: dieser
Worker ist der einzige Host seines Kontexts, der provisionieren darf, also
`InfrastructureProvisioning.AtStartup` wählt.

---
### WS-04, `EntityFrameworkCore.Design` verträgt kein `PrivateAssets`

Das Design-Paket steht ohne `PrivateAssets` in beiden Sample-Infrastructure-Projekten
([StateStored](samples/StateStored/VitalSync.Sample.StateStored.Infrastructure/VitalSync.Sample.StateStored.Infrastructure.csproj),
[EventSourced](samples/EventSourced/VitalSync.Sample.EventSourced.Infrastructure/VitalSync.Sample.EventSourced.Infrastructure.csproj)).
Mit `PrivateAssets="all"` kappt es die transitive Kante zu `EntityFrameworkCore.Relational`,
und Konsumenten scheitern zur Laufzeit mit `FileNotFoundException` — die Standardempfehlung
für Design-Pakete ist hier also aktiv falsch, was niemand erwartet.

#### Lösungsvorschlag

Die Design-Time-Factories dorthin verschieben, wo das Paket ohnehin hingehört, und den
MigrationService als Startup-Projekt scaffolden:

```bash
dotnet ef migrations add Xyz \
  --project    samples/StateStored/VitalSync.Sample.StateStored.Infrastructure \
  --startup-project samples/StateStored/VitalSync.Sample.StateStored.MigrationService
```

Damit verschwindet die Design-Referenz aus dem Infrastructure-Projekt, das von anderen
referenziert wird — die Ursache, nicht das Symptom.
---

### WS-07, Der gRPC-Vertrag liegt noch beim Service

`VitalSync.Sample.StateStored.Contracts` ist die richtige Struktur, aber der BFF konsumiert
heute noch nichts ([Program.cs](src/Bff/VitalSync.Bff/Program.cs) hat nur Controller).
Sobald er es tut, stellt sich die Frage nach einem geteilten Paket. Für das Integration
Event ist sie mit Etappe 3 beantwortet (`VitalSync.Sample.Contracts`), für den gRPC-Vertrag
nicht.

#### Lösungsvorschlag

Dieselbe Antwort wie beim Integration Event, sobald der zweite Konsument existiert: ein
eigenes Contracts-Projekt pro Bounded Context, das Service **und** BFF referenzieren.

```
src/Services/Nutrition/VitalSync.Nutrition.Contracts/   ← gRPC-Interfaces + DTOs
        ↑ referenziert von VitalSync.Nutrition.Api und von VitalSync.Bff
```

Nicht vorwegnehmen — die Entscheidung gehört an den Tag, an dem der BFF den ersten Service
aufruft, und dann in einen ADR (zusammen mit der bisher nur faktisch getroffenen
Bibliothekswahl `protobuf-net.Grpc`, siehe §2).
---

### WS-15, `ApplyEntityKeyConversions` erfasst keine Complex Types

Verifiziert: der Helper kennt **keine** Complex-Type-Behandlung, er läuft über
`Model.GetEntityTypes()`
([EntityKeyValueConverter.cs:66](BuildingBlocks/src/BuildingBlocks.Infrastructure/Persistence/EntityKeyValueConverter.cs)).
Ein typisierter Schlüssel innerhalb eines Complex Type bekäme keinen Konverter — und
scheiterte damit erst beim Migrieren gegen PostgreSQL, nicht beim Modellaufbau.

Der Scan hat unabhängig davon ein zweites Problem: er läuft über CLR- statt Model-Properties
und **legt dabei Properties im Modell an**, siehe [hacky.md Nr. 4](hacky.md). Beide Punkte
betreffen dieselbe Schleife und gehören in einen Fix.

#### Lösungsvorschlag

```csharp
foreach (var entityType in modelBuilder.Model.GetEntityTypes())
{
    Konvertiere(entityType.GetProperties());

    foreach (var complex in entityType.GetComplexProperties())
    {
        Konvertiere(complex.ComplexType.GetProperties());
    }
}
```

Zusammen mit hacky Nr. 4 umsetzen: dort wird die Schleife ohnehin von CLR- auf
Model-Properties umgestellt, und `GetComplexProperties()` ist genau dann verfügbar.

#### Nachtrag (2026-08-05): hacky Nr. 4 ist gelöst, WS-15 bleibt bewusst offen

Die Annahme „beide Punkte gehören in einen Fix" hat sich nicht gehalten — sie sind gegenläufig.
Der CLR-Scan wurde nicht auf Model-Properties umgestellt, sondern **ganz entfernt** (ADR-0033):
eine `IEntityKey<T>`-Property wird von EF Cores Discovery nie gefunden, also mappt jeder
`DbContext` seine typisierten Schlüssel explizit — was alle echten Kontexte ohnehin tun. Damit
kann der Helper dem Modell nicht mehr widersprechen, und ein vergessener Schlüssel scheitert laut
beim Modellaufbau.

WS-15 wird davon nicht mitgenommen, sondern kleiner: der Helper läuft jetzt ohnehin über das
Modell, `GetComplexProperties()` wäre eine zusätzliche Schleife. Offen bleibt es trotzdem, weil es
heute keinen Anwendungsfall gibt: `ComplexProperty` kommt im gesamten Repo nicht vor. Write-seitig
ist der Weg durch ADR-0031 (`OwnsMany`, `ToJson()`) und ADR-0025
(kein Complex Type für den State) belegt, und Owned Types sind eigene Entity-Types, also bereits
erfasst. Read-seitig sind die Modelle flach. Der Fix bleibt eine Zeile und gehört in den Moment,
in dem das erste eingebettete Value Object mit typisiertem Schlüssel auftaucht.
---

## 10. Konventionen, die beim Anlegen neuer Ordner beißen

Ordner-lokale `.editorconfig`-Regeln erbt ein neuer Top-Level-Ordner **nicht**:

- **CA1707** (Unterstriche in Methodennamen) — sprengt jeden Testnamen im
  `Given_When_Then`-Stil; Testordner brauchen eine eigene `.editorconfig` analog zu
  `BuildingBlocks/tests/.editorconfig` oder ein `NoWarn` im Test-`.csproj` (so gelöst in
  `tests/VitalSync.ServiceDefaults.Tests`).
- **CA2007** (fehlendes `ConfigureAwait`) — kollidiert in Testmethoden frontal mit
  **xUnit1030**, das `ConfigureAwait(false)` dort verbietet. Testprojekte schalten CA2007 daher
  ab, per `.editorconfig` (Sample-Tests) oder per `NoWarn` im `.csproj` (BuildingBlocks-Tests).

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

Beide Sample-Services starten (ein AppHost, zwei Services, ein Postgres-Server, ein
RabbitMQ):

```bash
dotnet run --project samples/VitalSync.Samples.AppHost
```

Die Smoke-Tests laufen gegen das laufende System und überspringen ohne URL. Jeder
Service hat seine **eigene** Variable — die Ports stehen in den `launchSettings.json`
der beiden Api-Projekte (54230 bzw. 54240):

```bash
SAMPLE_STATESTORED_API_URL=https://localhost:54230 SAMPLE_EVENTSOURCED_API_URL=https://localhost:54240 dotnet test samples
```

EF-Migrationen scaffolden braucht `dotnet-ef` **10.x**; die global installierte 9.0.8
reicht nicht und es gibt kein Tool-Manifest mehr. Ohne globalen Zustand anzufassen:

```bash
dotnet tool install dotnet-ef --version 10.0.10 --tool-path .tools
```

**Service-Logs sehen.** Aspire schickt die Ausgabe der Services nur ins Dashboard; auf
der Konsole und in den DCP-Logdateien steht sie nicht. Genau daran hing die eine
`fail:`-Zeile, die Etappe 3 erklärt hat. Wer eine Ursache sucht, nimmt die Services aus
dem AppHost heraus und startet sie von Hand gegen dieselben Container — der AppHost darf
dafür beendet werden, die Container laufen weiter:

```bash
docker inspect $(docker ps --format "{{.Names}}" | grep postgres) --format '{{range .Config.Env}}{{println .}}{{end}}' | grep POSTGRES_PASSWORD
```

Danach die Verbindungszeichenfolgen als `ConnectionStrings__<name>` setzen und
`dotnet run` auf das Api-Projekt. **Achtung:** `dotnet run` wendet `launchSettings.json`
an und überschreibt damit `ASPNETCORE_URLS` — die Services hören auf den dort
eingetragenen Ports (54230/54240), nicht auf einem selbst gewählten.

---

## 12. Arbeitsweise, die sich bewährt hat

- **Mutationsprobe statt Annahme.** Jeder neue Schutz wurde geprüft, indem der
  Produktionscode kurzzeitig kaputtgemacht wurde. Zweimal stellte sich dabei heraus,
  dass ein Test wertlos war, obwohl er grün lief.
- **Empirie statt Spekulation** bei Framework-Verhalten. Wolverines und EFs
  Fehlermeldungen sind aussagekräftiger als jede Herleitung.
- **Kleine, einzeln verifizierte Commits.** Jeder Commit sollte für sich bauen und
  grün sein — sonst ist er nicht zurücknehmbar.
- **Erst den unbequemen Test schreiben, dann die Projektion.** Beide echten Fehler in
  Etappe 2 kamen aus Tests, die eine Zustellung wiederholen oder vertauschen. Eine
  Projektion, die nur den Happy Path sieht, ist immer grün — auch wenn sie driftet.
- **Die unbequeme Variante erst bauen, dann verschieben.** Die Entscheidung über die
  Subscribe-Hälfte wurde nicht hergeleitet, sondern gemessen: Variante „Service
  verdrahtet selbst" lief zuerst, und erst danach stand fest, was sie kostet. Der
  Umbau danach war klein, die Begründung dafür belastbar.
- **Wenn nichts passiert und nichts weh tut, fehlen die Logs.** Eine still verworfene
  Nachricht sieht exakt aus wie eine, die nie gesendet wurde. In Etappe 3 kostete es
  drei Fehlversuche, bis klar war, dass nicht der Code das Problem war, sondern dass
  niemand die Service-Ausgabe sah.
