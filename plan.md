# Umsetzungsplan: Entitäten lösen Events über den Aggregate Root aus (Root-Callback)

**Datum:** 2026-08-04
**Status:** **Umgesetzt** (2026-08-04) — entschieden als
[ADR-0032](docs/architecture/decisions/0032-child-entities-raise-via-root.md); nicht committet.
**Bezug:** **ADR-0031** (Kindkollektionen als Owned Types), **ADR-0030** (deklarierte Namen,
Version am State), **ADR-0025/0026** in der Fassung nach Commit `859790e`.

## Ergebnis der Umsetzung

| Schritt                       | Ergebnis                                                                                                                                                                              |
| ----------------------------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| 0 Spike                       | **Positiv.** Ein Kind-State erbt von `EntityState<TSelf, TKey>`, ohne EF Cores Konstruktorermittlung für Owned Types zu verlieren; Modellvalidierung und typisierter Schlüssel bleiben intakt. |
| 1 ADR-0032                    | Geschrieben, im Index geführt, Amendments in ADR-0006/0010/0025 und (nach dem Hierarchie-Umbau) ADR-0008.                                                                             |
| 2 Domain-Basis                | `EntityState.cs`, `IDomainEventRaiser.cs`, `Entity<TKey, TState>`; `AggregateRoot` implementiert den Kanal **explizit**.                                                               |
| 3 Replay/Restore              | Abgedeckt: Kind-Event in der Root-Liste, Versionsfortschritt bei reiner Kindänderung, `Restore`/`LoadFromHistory` mit Kindern, Replay-Guard.                                          |
| 4 State-stored Sample         | `WidgetPart` → `WidgetPartState` + Hülle. **Keine Migration** (`has-pending-model-changes`: keine Änderungen), `EfCoreChildCollectionTests` grün, plus neuer Round-Trip-Test.          |
| 5 ES-Sample                   | `GadgetComponent` mit Command- und Replay-Pfad, zwei neue Events mit `[EventName]`, neuer Konventionstest in **beiden** Samples.                                                       |
| 6 Doku                        | `building-blocks-domain.md`, `glossary.md`, `testing-strategy.md`, `WalkingSkeleton.md`, `todo.md` (TODO-47), beide Instruktionsdateien.                                              |
| 7 Hierarchie-Umbau            | Nachgezogen: das zustandslose `Entity<TKey>` ist gelöscht, `Entity<TKey, TState>` hängt direkt an `EntityBase<TKey>` — zwei abstrakte Entitätsklassen statt drei.                     |

Abweichungen vom Plan, bewusst:

- **`IDomainEventRaiser` ist `public`, nicht `internal`.** Ein `internal` Interface darf nicht in
  der Signatur eines `protected` Konstruktors einer öffentlichen Basisklasse stehen (CS0051). Die
  Kapselung trägt stattdessen die **explizite** Implementierung am Root — Hauskonvention wie bei
  `IDomainEventOwner`/`IStateOwner`. Im ADR dokumentiert.
- **`GetCurrentState()` ist eine Methode, keine `State`-Property.** Sie wirft, wenn das Kind im selben
  Command entfernt wurde; eine werfende Property verbietet CA1065 bei `warnings as errors`.
- **Jede Entität hat einen State.** Der Plan ließ `Entity<TKey>` stehen; nach der Umsetzung hatte
  der Typ keinen einzigen Produktionsnutzer mehr (nur zwei Gleichheits-Testdoubles) und sein
  Leer-Guard war ohnehin in `AggregateRoot.ApplyEvent` und `IStateOwner.Restore` dupliziert. Er ist
  gelöscht, seine beiden Aufgaben sind in `Entity<TKey, TState>` gewandert.

## 0. Ausgangslage nach den letzten Commits (Review 2026-08-04)

Der Plan wurde gegen `2361fe0`, `859790e`, `0553a35` und `7be71ef` gereviewt. Vier Annahmen
des ursprünglichen Entwurfs sind überholt:

1. **Die Frage „wo leben die Child-States?" ist bereits entschieden** — nicht als Empfehlung,
   sondern als angenommene ADR-0031 (`2361fe0`): Kinder sind Bestandteil des Root-States und
   mappen als **Owned Types** mit eigenem, deklariertem, stark typisiertem Schlüssel;
   `AggregateStateGraph.Reconcile` gleicht den Owned-Graphen beim Commit **schlüsselbasiert und
   in jeder Tiefe** ab. Abschnitt 2.2 ist damit keine Wahl mehr, sondern eine Vorgabe, deren
   Autorenregeln dieser Plan einhalten **muss**.
2. **`Entity<TKey>` und `EntityTests.cs` existieren bereits** (Identität/Gleichheit nach
   ADR-0008). Der Plan darf sie nicht als „NEU" führen — die neue State-tragende Variante tritt
   **neben** die bestehende, ohne deren Gleichheitssemantik zu ändern.
3. **`IReconstitutable` ist gelöscht** (`859790e`). Rehydration läuft über einen privaten
   parameterlosen Konstruktor plus den internen `AggregateFactory`; die Konvention wird beim
   **Hoststart** durch `AddBuildingBlocks` geprüft. Alle Plan-Zeilen, die auf
   `IReconstitutable`/`CreateEmpty` zeigen, sind entsprechend umzuschreiben.
4. **Das state-stored Sample hat sein Kind schon** (`WidgetPart`, Tabelle `widget_parts`,
   Commands, Projektionen, Migrationen, Smoke-Tests). Dort ist nichts „einzuführen", sondern ein
   vorhandener reiner Record in eine verhaltenstragende Entität zu überführen — das ES-Sample
   (Gadget) hat dagegen tatsächlich noch kein Kind.

Zusätzlich gilt aus `0553a35` (ADR-0030): jedes neue Kind-Event braucht `[EventName("…-v1")]` und
muss über `AddDomainEventsFrom` erreichbar sein; die **Version bleibt allein am Root-State** und
wandert als Watermark auf `DomainEventMetadata`. Eine reine Kindänderung erhöht diese Version —
genau das macht sie für Projektionen und für den EF-Concurrency-Token sichtbar.

## 1. Ziel und Leitidee

Jede Entität innerhalb eines Aggregats soll eigene Zustandslogik besitzen und
Events auslösen können — **ohne** dass die Uncommitted-Events in die State-Objekte
wandern. Es gilt:

- **Der State bleibt unveränderlich und rein** (ADR-0010 bleibt gültig): `Apply` ist
  eine reine Funktion, keine Registrierung von Events im State.
- **Der Aggregate Root bleibt alleiniger Besitzer der Uncommitted-Events**
  (ADR-0006 bleibt gültig): eine private Liste, Reihenfolge global korrekt,
  Clearing bleibt O(1).
- **Kind-Entitäten erhalten einen eigenen immutablen State** und lösen Events über
  einen vom Root bereitgestellten Kanal aus (`Raise`-Callback / interne Schnittstelle).
- **Zwei Pfade bleiben erhalten:** Command-Pfad (`RaiseEvent` → apply + record beim
  Root) und Rehydrations-Pfad (`Apply` only, beim Replay/Restore).

```text
Command-Pfad                       Rehydrations-Pfad
────────────                       ─────────────────
Child.DoSomething()                Root.LoadFromHistory(stream) / IStateOwner.Restore(state)
  └─ raise(e)  ── Kanal ──►          └─ für jedes e:
       Root.RaiseEvent(e)                 State.Apply(e)   (Kindanteile inklusive)
         ├─ State.Apply(e)                 WithVersion(+1)
         │    └─ Kindanteile               (nichts wird registriert)
         ├─ WithVersion(+1)
         ├─ Identity-Guard
         └─ _domainEvents.Add(e)   ◄── einzige Event-Liste, nur am Root
```

Kein separates Routing in Kind-States: die Kindanteile sind Teil desselben Root-State-Records
(ADR-0031), also faltet `State.Apply(e)` sie ohnehin mit.

## 2. Neue / geänderte Bausteine — Code

### 2.1 `BuildingBlocks/src/BuildingBlocks.Domain/`

| Datei                                 | Aktion             | Beschreibung                                                                                                                                                                                                                                                                                                                    |
| ------------------------------------- | ------------------ | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `EntityState.cs`                      | **NEU**            | `abstract record EntityState<TSelf, TKey>` — analog zu `AggregateState`, aber **ohne** `Version` (die Version lebt nach ADR-0030 ausschließlich am Root-State). Trägt `Id` und `TSelf Apply(IDomainEvent)`. **Spike zuerst** (siehe 8): der Basis-Record darf die EF-Core-Konstruktorermittlung für Owned Types nicht brechen. |
| `Entity.cs`                           | **ÄNDERN**         | Datei enthielt `Entity<TKey> : EntityBase<TKey>` (ADR-0008, Identität + Gleichheit). **Ergebnis:** `Entity<TKey>` ist gelöscht, `abstract class Entity<TKey, TState> : EntityBase<TKey>` übernimmt dessen Identität und Leer-Guard und ergänzt `GetCurrentState()` (liest durch den Root) sowie `protected void RaiseEvent(IDomainEvent e)`, das an den Root-Kanal delegiert (kein eigenes Event-Verzeichnis). Gleichheitssemantik unverändert. |
| `IEventRaiser.cs` (o. ä., `internal`) | **NEU**            | Schmaler interner Kanal (`void Raise(IDomainEvent e)`), den der Root implementiert und an Kinder reicht. Nicht öffentlich — Domänen-Code außerhalb des Aggregats sieht ihn nie. Namenskonvention des Hauses für „privilegierte, explizit implementierte Sicht": `*Owner` (`IDomainEventOwner`, `IStateOwner`) — also eher `IDomainEventRaiser`/`IEventChannel` als ein neues Muster erfinden. |
| `AggregateRoot.cs`                    | **ÄNDERN**         | (a) implementiert den Kanal (explizit/intern); (b) `RaiseEvent` bleibt der einzige Registrierungspunkt — Reihenfolge, Identity-Guard und Clearing unverändert; (c) **kein** Zusatzrouting nötig: `ApplyEvent` faltet über `State.Apply(e).WithVersion(...)`, und die Kindanteile sind Teil desselben Records (2.2). |
| `AggregateState.cs`                   | **KEINE ÄNDERUNG** | Trägt `Id`, `Version` und das interne `WithVersion` (ADR-0030). `EntityState` spiegelt nur das `Apply`-Idiom, erbt aber **nicht** von `AggregateState` — sonst bekäme ein Kind eine zweite Version.                                                                                                                            |
| `EventSourcedAggregateRoot.cs`        | **KEINE ÄNDERUNG** | `LoadFromHistory` nutzt weiterhin `ApplyEvent`; Kindanteile werden beim Replay automatisch mitgefaltet, weil sie im Root-State liegen. Replay-Guard bleibt (eine Liste, ein Count).                                                                                                                                            |
| `IStateOwner.cs`                      | **KEINE ÄNDERUNG** | `StateType` / `State` / `Version` / `Restore(object)` bleiben wie sie sind: ein State-Dokument enthält die Kinder bereits. Ein separater Restore-Pfad pro Kind wird ausdrücklich **nicht** gebaut.                                                                                                                             |

### 2.2 Wo leben die Child-States? — entschieden durch ADR-0031

**Keine offene Frage mehr:** Child-States sind Bestandteil des Root-States (Komposition), z. B.
`WidgetState { …, IReadOnlyCollection<WidgetPart> Parts { get; init; } }`. Konsequenzen:

- `IStateOwner.State` / `Restore` / Snapshots funktionieren **unverändert** — ein
  State-Dokument enthält alles.
- Die Kind-**Entität** ist eine dünne Verhaltenshülle über „ihrem" Ausschnitt des Root-States
  (liest ihren State über den Root, schreibt nie direkt).
- Das Falten ist trivial: `State.Apply(e)` faltet auch die Kindanteile, weil sie Teil desselben
  Records sind. Ein zusätzliches Routing in `ApplyEvent` entfällt.

**Die Autorenregeln aus ADR-0031 sind bindend und gehen in jede neue Code-Zeile ein:**

- Die Kollektion ist eine `{ get; init; }`-Property, **kein** positionaler Record-Parameter —
  sonst findet EF Core „no suitable constructor".
- Sie wird mit `ToList()` / `new List<T>()` gebaut. **Kein `ImmutableList`, kein Collection
  Expression, kein `null`** — read-only, fixed-size und `null` fliegen mit
  `NotSupportedException` auf, in jeder Tiefe des Owned-Graphen. (Der ursprüngliche Entwurf
  schlug hier `ImmutableList<…>` vor; das würde beim ersten Commit knallen.)
- Jedes Kind hat einen **eigenen stark typisierten Schlüssel**, der im Write-Modell per `HasKey`
  deklariert ist; ohne ihn lehnt `AggregateStateModelStartupValidator` das Modell beim Hoststart ab.
- Ein State darf **nicht** auf einen unabhängigen Entity-Typ navigieren — ebenfalls beim
  Hoststart abgelehnt. Eine Kind-Entität mit Verhalten ist deshalb **kein** gemapptes Objekt,
  sondern eine Hülle über dem gemappten Kind-**State**.

Damit reduziert sich der Kern der Änderung auf: **Kind-Entitäten bekommen einen
`RaiseEvent`-Kanal und eine State-Sicht — die Persistenz- und Event-Mechanik des
Roots bleibt unangetastet.**

### 2.3 `BuildingBlocks/src/BuildingBlocks.Infrastructure/`

| Datei                                              | Aktion             | Beschreibung                                                                                                                                                                                                    |
| -------------------------------------------------- | ------------------ | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `Persistence/EfCoreAggregateTracker.cs`            | **KEINE ÄNDERUNG** | Sammlung/Clearing läuft weiter über `IDomainEventOwner` am Root.                                                                                                                                                |
| `Persistence/AggregateStateGraph.cs`               | **KEINE ÄNDERUNG** | Der schlüsselbasierte Owned-Graph-Abgleich (ADR-0031) deckt Kind-States bereits ab — unabhängig davon, ob ein Verhaltensobjekt darüber liegt. Nur **prüfen**, dass die Verhaltenshülle nie im Graphen landet.   |
| `Persistence/AggregateStateModelStartupValidator.cs` | **PRÜFEN**       | Bestehende drei Konventionen bleiben. Nur erweitern, falls die Verhaltenshülle eine neue Fehlform ermöglicht (z. B. `Entity<TKey,TState>` als Property eines States statt des reinen Kind-States).               |
| Unit of Work / Repositories                        | **KEINE ÄNDERUNG** | Ein State-Dokument pro Aggregat, eine Event-Liste pro Aggregat — Verträge (ADR-0026) bleiben stabil.                                                                                                            |
| Marten-Anbindung (ES)                              | **KEINE ÄNDERUNG** | Append-Reihenfolge = Raise-Reihenfolge, da nur eine Liste existiert. Kindanteile im State betreffen Marten nicht (Raw Event Store, kein Owned Mapping).                                                         |
| Startup-Validierung Aggregat-Konventionen          | **PRÜFEN**         | Der Scan in `AddBuildingBlocks` prüft heute privaten parameterlosen Konstruktor, `[AggregateName]`, `[EventName]`. Bei Komposition (2.2) werden Kinder **nicht** separat reconstituiert — voraussichtlich keine Erweiterung nötig; Entscheidung im ADR festhalten. |

## 3. Tests

| Testdatei                                                             | Aktion        | Inhalt                                                                                                                                                     |
| --------------------------------------------------------------------- | ------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| `BuildingBlocks.Domain.Tests/EntityStateTests.cs`                     | **NEU**       | Apply-Fold der neuen `EntityState`-Basis, Identitätsregeln, **kein** `Version`-Feld am Kind.                                                                |
| `BuildingBlocks.Domain.Tests/EntityTests.cs`                          | **ERWEITERN** | Datei existiert (Gleichheit/Identität nach ADR-0008) — ergänzen: Kind-Entität löst Event aus → landet in `Root.DomainEvents`; Reihenfolge Root ↔ Kind; Kind ohne Kanal raist nicht (Guard); Null-Guards. Bestehende Gleichheitstests bleiben. |
| `BuildingBlocks.Domain.Tests/AggregateRootTests.cs`                   | **ERWEITERN** | Kanal-Implementierung; `ClearDomainEvents` unverändert; Identity-Guard unverändert.                                                                         |
| `BuildingBlocks.Domain.Tests/AggregateVersionTests.cs`                | **ERWEITERN** | Eine **reine Kindänderung** erhöht die Root-Version um genau 1 (ADR-0030/0031).                                                                             |
| `BuildingBlocks.Domain.Tests/ReconstitutableTests.cs`                 | **ERWEITERN** | Achtung: `IReconstitutable` ist gelöscht (`859790e`) — die Datei testet heute `IStateOwner.Restore`. Ergänzen: `Restore` stellt Root-State inkl. Kindanteilen her, Kind-Sichten danach konsistent.       |
| `BuildingBlocks.Domain.Tests/EventSourcedAggregateRootTests.cs`       | **ERWEITERN** | Replay baut Kindanteile mit auf; Replay-Guard greift auch, wenn zuvor ein **Kind** ein Event ausgelöst hat.                                                 |
| `BuildingBlocks.Domain.Tests/TestDoubles/`                            | **ERWEITERN** | Test-Aggregat mit mindestens einer Kind-Entität (Command- und Replay-Pfad), nach Hausregel handgeschrieben statt gemockt.                                   |
| `BuildingBlocks.Infrastructure.Tests/EfCoreChildCollectionTests.cs`   | **ERWEITERN** | Regressionsschutz: der Owned-Graph-Abgleich bleibt korrekt, wenn über dem Kind-State eine Verhaltenshülle liegt (Testcontainers, mit `Skip`-Guard).         |
| `samples/StateStored/...Tests/AggregateConventionTests.cs`            | **ERWEITERN** | Konventionen für Kind-Entitäten (falls neue Attribute/Regeln entstehen); Scan analog zum bestehenden Konstruktor-Scan.                                      |
| `samples/StateStored/...Tests/WidgetTests.cs`                         | **ERWEITERN** | Vorhandene Part-Tests auf den Entitätspfad umstellen (Verhalten, nicht Mechanik: „Part ändert Menge → `WidgetPartQuantityChanged` am Root").                |
| `samples/EventSourced/...Tests/`                                      | **ERWEITERN** | Sample-Szenario mit Kind-Entität (siehe 5).                                                                                                                |

## 4. Dokumentation (`*.md`)

| Datei                                                                    | Aktion                | Beschreibung                                                                                                                                                                                                                                       |
| ------------------------------------------------------------------------ | --------------------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `docs/architecture/decisions/0032-child-entities-raise-via-root.md`      | **NEU (ADR)**         | Nächste freie Nummer ist **0032** (0031 ist seit `2361fe0` vergeben). Die Entscheidung: Kind-Entitäten mit eigenem State-Ausschnitt, Events via Root-Kanal; Uncommitted-Events bleiben am Root; verworfene Alternative (Events im State) mit Begründung (Ordering, Record-Equality, Snapshot-Sicherheit). |
| `docs/architecture/decisions/README.md`                                  | **ERWEITERN**         | Index-Zeile für ADR-0032.                                                                                                                                                                                                                          |
| `docs/architecture/decisions/0031-…-owned-types.md`                      | **VERWEIS**           | ADRs sind nach Annahme unveränderlich — kein Umschreiben. ADR-0032 verweist auf 0031 und erklärt, dass es die **Verhaltens**seite ergänzt, während 0031 die **Persistenz**seite regelt.                                                             |
| `docs/architecture/building-blocks-domain.md`                            | **ERWEITERN**         | Bausteintabelle (+`Entity<TKey, TState>`, +`EntityState<TSelf, TKey>`, +Kanal); Lifecycle-Diagramm um den Kindpfad ergänzen; Designregel: „Kind-Entitäten registrieren nie selbst — sie raisen über den Root".                                    |
| `docs/architecture/building-blocks-infrastructure.md`                    | **PRÜFEN**            | Nur, falls sich am Owned-Graph-Abschnitt (ADR-0031) etwas an der Beschreibung ändert.                                                                                                                                                              |
| `docs/architecture/decisions/0006-aggregate-owns-domain-events.md`       | **AMENDMENT (klein)** | Bestätigung + Hinweis: gilt auch für Events aus Kind-Entitäten; Verweis auf ADR-0032.                                                                                                                                                              |
| `docs/architecture/decisions/0010-aggregate-state-object.md`             | **AMENDMENT (klein)** | State-Objekt-Muster auf Entitäten ausgeweitet; `Apply` bleibt rein/einzig; Verweis auf ADR-0032.                                                                                                                                                   |
| `docs/architecture/decisions/0025-unified-state-fold-aggregate-model.md` | **AMENDMENT (klein)** | Fold-Modell umfasst Kindanteile im Root-State; `IStateOwner` unverändert.                                                                                                                                                                          |
| `docs/architecture/decisions/0026-single-repository-contract.md`         | **PRÜFEN**            | Keine Änderung erwartet — kurz vermerken statt anfassen.                                                                                                                                                                                          |
| `docs/architecture/testing-strategy.md`                                  | **PRÜFEN**            | Falls ein neues Fixture-Projekt unter `tests/ExternalAssemblies/` nötig wird (Assembly-Scan), dort dokumentieren.                                                                                                                                  |
| `docs/glossary.md`                                                       | **ERWEITERN**         | Neue Einträge: _Entity State_, _Root-Kanal / Raise-Callback_; bestehende Einträge (_RaiseEvent / LoadFromHistory_, _Rehydration_) um den Kindpfad ergänzen.                                                                                        |
| `.github/copilot-instructions.md` **und** `.claude/CLAUDE.md`            | **ERWEITERN**         | Beide Dateien werden in dieser Repo-Historie bei **jeder** Architekturänderung mitgeführt (`2361fe0`, `859790e`) und fehlten im ursprünglichen Plan. Neue Regel zum Kindpfad in die DDD-Konventionen aufnehmen — wortgleich in beiden Dateien.      |
| `todo.md`                                                                | **ERWEITERN**         | Neuen TODO-Eintrag mit Verweis auf diesen Plan anlegen; nach Umsetzung Resolution im bestehenden Format dokumentieren (Befund + „Belegt durch"). TODO-01 ist bereits **gelöst** und wird nicht wieder geöffnet.                                     |
| `Improvements.md` / `hacky.md` / `WalkingSkeleton.md`                    | **PRÜFEN**            | Werden in dieser Historie synchron gepflegt; offene Punkte zum Kindpfad dort vermerken oder abhaken.                                                                                                                                               |
| `docs/architecture/analysis/entity-state-with-uncommitted-events-impact.md` | **GESTRICHEN**     | Die Datei existiert im Repository nicht und wird nicht nachgereicht. Der Bezug entfällt; ADR-0032 zitiert sie nicht.                                                                                                                               |

## 5. Samples

| Sample                          | Aktion        | Beschreibung                                                                                                                                                                                                                                                            |
| ------------------------------- | ------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `samples/StateStored` (Widget)  | **UMBAUEN**   | Das Kind existiert seit `2361fe0` bereits: `WidgetPart` (`OwnsMany` → Tabelle `widget_parts`, eigener typisierter Schlüssel, Commands, Projektionen, Migrationen, Smoke-Tests). Zu tun ist **kein** Neubau, sondern die Überführung des reinen Records `WidgetPart` in einen Kind-**State** plus Verhaltenshülle, die `AddPart`/`ChangePartQuantity`/`RemovePart` aus `Widget` übernimmt. Regeln (`WidgetPartRules`) wandern mit. **Ziel: keine neue Migration** — Tabellenform bleibt gleich; das ist beim Umbau explizit zu verifizieren. |
| `samples/EventSourced` (Gadget) | **ERWEITERN** | Hier gibt es noch kein Kind. Kind-Entität einführen (z. B. `GadgetComponent`), die über den Root-Kanal Events auslöst — demonstriert Command- und Replay-Pfad. Kein Owned Mapping nötig (Marten als Raw Event Store), aber `[EventName]` für jedes neue Event.           |

## 6. Umsetzungsreihenfolge (Vorschlag, jeweils CI-grün)

0. **Spike (halber Tag, wegwerfbar):** Erbt ein Kind-State von `EntityState<TSelf, TKey>`, ohne
   dass EF Core die Konstruktorermittlung für Owned Types verliert? Fällt die Antwort negativ
   aus, bleibt der Kind-State ein flacher Record und die Basis entfällt — der Rest des Plans
   bleibt gültig. Ergebnis im ADR festhalten (Messen statt vermuten, wie bei ADR-0031).
1. **ADR-0032 schreiben** — Entscheidung, Abgrenzung zu ADR-0031 und verworfene Alternative.
2. **Domain-Basis**: `EntityState`, `Entity<TKey, TState>`, interner Raise-Kanal; `AggregateRoot`
   um die Kanal-Implementierung ergänzen (noch ohne Nutzer). Unit-Tests dazu.
3. **Replay-/Restore-Pfad** absichern: ES-Tests mit Kindanteilen, `Restore`-Tests,
   Versionsfortschritt bei reiner Kindänderung.
4. **State-stored Sample umbauen** (`WidgetPart` → Kind-State + Hülle) — mit dem Nachweis, dass
   `EfCoreChildCollectionTests` und die Widget-Smoke-Tests unverändert grün bleiben und **keine**
   neue Migration entsteht.
5. **ES-Sample erweitern** (Kind-Entität am Gadget) inkl. Konventionstests.
6. **Doku nachziehen**: `building-blocks-domain.md`, Glossar, Amendments zu ADR-0006/0010/0025,
   `.github/copilot-instructions.md` + `.claude/CLAUDE.md`, `todo.md`-Resolution.

**Committen ist nicht Teil dieses Plans** — die Hausregel ist ausdrücklich „NEVER commit
yourself"; jeder Schritt wird gebaut, getestet und dem Menschen zum Commit übergeben.

## 7. Explizit unverändert (Nicht-Ziele)

- Uncommitted-Events wandern **nicht** in State-Objekte.
- `IDomainEventOwner`, `EfCoreAggregateTracker`, Unit of Work, `AggregateStateGraph`,
  Marten-Append: **unverändert**.
- Kein zweiter `Apply`-Modus am State (`ApplyAndRecord` entfällt) — Registrierung
  bleibt exklusiv Sache von `Root.RaiseEvent`.
- Versionierung bleibt ausschließlich am Root (`AggregateState.Version`, ADR-0030); ein Kind
  bekommt **keine** eigene Version.
- Die Konventionen aus ADR-0031 (Owned Types, deklarierter Kindschlüssel, keine freie
  Navigation, schreibbare Kollektion) werden weder gelockert noch umgangen.
- `IRepository`-Vertrag (ADR-0026): **unverändert**; es gibt keinen Zugriff auf ein Kind ohne
  seinen Root.

## 8. Restrisiken

| Risiko                                                                              | Schwere | Mitigation                                                                                                                              |
| ----------------------------------------------------------------------------------- | ------- | ---------------------------------------------------------------------------------------------------------------------------------------- |
| Kanal-Leck: Kind-Entität wird ohne Root konstruiert und raist ins Leere             | 🟡      | Kanal im Konstruktor erzwingen (`ArgumentNullException`), kein öffentlicher parameterloser Konstruktor; Konventionstest.                 |
| Kind-Sicht und Root-State laufen auseinander (Stale View)                           | 🟡      | Kind hält **keinen** eigenen State-Snapshot, sondern liest immer durch den Root (Delegation statt Kopie).                                |
| `EntityState`-Basisrecord bricht EF Cores Konstruktorermittlung für Owned Types     | 🟡      | Spike in Schritt 0 **vor** dem ADR; Fallback ist der flache Kind-Record wie heute `WidgetPart`.                                          |
| Verhaltenshülle landet versehentlich im EF-Modell → Startup-Validator lehnt ab      | 🟡      | Hülle ist nie Property eines States; nur der Kind-**State** ist gemappt. Negativtest im Sample.                                          |
| Umbau des Samples erzwingt ungewollt eine Migration auf `widget_parts`              | 🟡      | Tabellenform beim Umbau unverändert lassen; `dotnet ef migrations has-pending-model-changes` bzw. leeres Migrations-Diff als Nachweis.   |
| Root-State wächst (viele Kinder) → Snapshot-Größe                                   | 🟢      | Bekannter Trade-off der Komposition; Snapshotting bleibt wie geplant „deferred".                                                         |
