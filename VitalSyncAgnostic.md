# Building Blocks — Agnostik- und API-Review

Reviewumfang: `BuildingBlocks/src/BuildingBlocks.Domain`, `BuildingBlocks.Application`,
`BuildingBlocks.Infrastructure`. Leitfrage: **Würden diese drei Projekte als NuGet-Paket
ausserhalb von VitalSync funktionieren?**

## Kurzantwort

**Domain und Application: ja** — bis auf zwei Formalien (fehlendes `Directory.Build.props`,
`net10.0`-only). Beide haben keinerlei VitalSync-Bezug, keine Fremdabhängigkeit ausser der
BCL, und der Namensraum ist sauber geschnitten.

**Infrastructure: nein, nicht sinnvoll.** Nicht weil VitalSync darin vorkommt — der Name
kommt tatsächlich nirgends vor — sondern weil das Projekt nicht "Infrastructure" ist,
sondern *eine* konkrete Infrastruktur: PostgreSQL + EF Core + Marten + Wolverine + RabbitMQ,
alle zwölf Pakete als Pflichtabhängigkeit, und diese Entscheidung ist bis in die öffentliche
API und bis in ein Dispatching-Behavior durchgeschlagen. Ein Fremdkonsument mit SQL Server
oder Azure Service Bus kann nichts davon benutzen und auch nichts davon ersetzen, weil die
Erweiterungspunkte `internal` sind. Die Unabhängigkeit von *VitalSync* ist erreicht; die
Unabhängigkeit vom *Stack* wurde nie angestrebt — nur liest sich der Projektname, als wäre
sie es.

## Übersicht

| Nr. | Titel                                                                     | Prio | Status |
| --- | ------------------------------------------------------------------------- | ---- | ------ |
| 1   | `Directory.Build.props` ist Kompilierungsvoraussetzung, nicht Politur      | P1   | Offen  |
| 2   | Infrastructure ist ein Stack-Monolith mit zwölf Pflichtabhängigkeiten      | P1   | Offen  |
| 3   | Öffentliche API von Infrastructure hängt an Wolverine-Typen                | P1   | Offen  |
| 4   | `UnitOfWorkBehavior` kennt PostgreSQL-Fehlercodes                          | P1   | Offen  |
| 5   | `AggregateStateGraph` baut EF Cores ChangeTracker nach                     | P1   | Offen  |
| 6   | Stream-Key und `AggregateId` sind abgeleitet, nicht deklariert             | P1   | Offen  |
| 7   | Fehlermeldungen zitieren ADR-Nummern und VitalSync-Prozesse                | P2   | Offen  |
| 8   | Kein Erweiterungspunkt: `internal`-first ohne Gegenstück                   | P2   | Offen  |
| 9   | `DeadLetterHealthCheck` liest Wolverines interne Tabelle per rohem SQL     | P2   | Offen  |
| 10  | Zwei unabhängige Wege zum selben `DbContext`                               | P2   | Offen  |
| 11  | `net10.0`-only, kein Multi-Targeting                                       | P2   | Offen  |
| 12  | Reflection/`Activator`/Expression-Trees ohne Trimming-Annotationen         | P2   | Offen  |
| 13  | Repository-Registrierung ist nicht typgeprüft                              | P2   | Offen  |
| 14  | `Result<T>`: implizite Operatoren, die werfen; kein `null` transportierbar | P2   | Offen  |
| 15  | Composition Root erzwingt HealthChecks und eine `NpgsqlDataSource`         | P2   | Offen  |
| 16  | `AggregateState`: Double-Cast, unerzwungenes F-Bound, `Apply`-`null`       | P2   | Offen  |
| 17  | `LoadFromHistory` ist nicht gegen doppelten Aufruf geschützt               | P3   | Offen  |
| 18  | `WolverineWiringSettings` trägt einen Vendornamen für Nicht-Vendor-Zustand | P3   | Offen  |
| 19  | `KebabCase` ist public aus Verlegenheit                                    | P3   | Offen  |
| 20  | `RuleChecker`-Überladungen sind bei leerem Aufruf mehrdeutig               | P3   | Offen  |
| 21  | `DomainEventEnvelope.Payload` wird doppelt serialisiert                    | P3   | Offen  |
| 22  | `InternalsVisibleTo` auf die Testassembly wird mit ausgeliefert            | P3   | Offen  |
| 23  | `FailureCategory` ist ein geschlossenes Enum ohne Erweiterungsweg          | P3   | Offen  |
| 24  | Statische, prozessweite Typ-Caches                                         | P3   | Offen  |
| 25  | `Entity<TKey, TState>` bindet sich per Closure an seinen Root              | P3   | Offen  |

---

## 1 — `Directory.Build.props` ist Kompilierungsvoraussetzung, nicht Politur

**Problem.** Die drei `.csproj` enthalten ausser `TargetFramework` und den Referenzen nichts.
`Nullable`, `ImplicitUsings`, `LangVersion`, `AnalysisLevel` und `TreatWarningsAsErrors`
kommen ausschliesslich aus `Directory.Build.props` auf Repository-Ebene. Bei `Nullable` wäre
das kosmetisch (nur Warnungen), bei `ImplicitUsings` nicht: keine einzige Datei deklariert
`using System;` oder `using System.Linq;`. `AggregateRoot.cs` verwendet `ArgumentNullException`,
`Type` und `List<>` ohne jedes `using`. Löst man die drei Projekte in ein eigenes Repository
oder eine eigene Solution heraus — genau der Schritt, den ein NuGet-Paket bedeutet —
kompilieren sie **nicht**. Dazu fehlen sämtliche Paketmetadaten: kein `PackageId`, keine
`Description`, kein `Authors`, keine `RepositoryUrl`, kein `PackageLicenseExpression`, keine
Versionierung, kein SourceLink, keine XML-Dokumentation.

**Lösungsvorschlag.** Ein eigenes `BuildingBlocks/Directory.Build.props` einziehen, das die
sprachrelevanten Eigenschaften (`Nullable`, `ImplicitUsings`, `LangVersion`, `TargetFramework`)
und die Paketmetadaten trägt und über `<Import Project="$([MSBuild]::GetPathOfFileAbove(...))" />`
das Repo-Root-Props für die Analyzer-Politik dazunimmt. Damit ist der Ordner in sich
geschlossen: er kompiliert im Repo *und* herausgelöst. Ergänzend `GenerateDocumentationFile`
und `Microsoft.SourceLink.GitHub`, weil ein Konsument ohne Repo-Zugriff sonst weder
IntelliSense-Text noch Debugging bekommt. Das ist der billigste Punkt der Liste und
gleichzeitig der einzige, der die Frage "würde das brechen?" mit einem harten Ja beantwortet.

## 2 — Infrastructure ist ein Stack-Monolith mit zwölf Pflichtabhängigkeiten

**Problem.** `BuildingBlocks.Infrastructure.csproj` referenziert Marten, EF Core, Npgsql,
WolverineFx.RabbitMQ, .Marten, .EntityFrameworkCore, .Postgresql und .RuntimeCompilation —
alle ohne Bedingung. Wer nur den handgeschriebenen Dispatcher will (`ISender`,
`RequestSender`, `LoggingBehavior`, `ExceptionToResultBehavior` — also den MediatR-Ersatz,
der das konzeptionell attraktivste Stück des Pakets ist), zieht damit einen Event Store, einen
ORM, einen PostgreSQL-Treiber und einen Message-Bus mit. Für ein Paket, dessen erklärter Zweck
Wiederverwendbarkeit ist, ist das die teuerste Eigenschaft überhaupt: die Abhängigkeiten sind
transitiv sichtbar, versionsgekoppelt und nicht abwählbar. Verstärkend kommt hinzu, dass die
Persistenz-Auswahl (`PersistenceChoice`) zur Registrierungszeit exklusiv ist — ein Host lädt
also *immer* mindestens einen kompletten Persistenz-Stack, den er nachweislich nicht benutzt.

**Lösungsvorschlag.** Aufspalten entlang der Linien, die im Ordnerbaum bereits gezogen sind:
`BuildingBlocks.Infrastructure` behält Dispatching, Telemetry, Time, den
`BuildingBlocksComposition`-Kern und die Erweiterungspunkte — Abhängigkeiten nur auf
`Microsoft.Extensions.*.Abstractions`. Darauf `BuildingBlocks.Persistence.EfCore`,
`BuildingBlocks.EventSourcing.Marten` und `BuildingBlocks.Messaging.Wolverine`, jedes mit
seinem eigenen `Use…`-Erweiterungspunkt auf `BuildingBlocksOptions`. Die vorhandene
Registrar-Struktur (`PersistenceRegistrar`, `MessagingRegistrar`) ist genau die Naht dafür und
müsste in die jeweiligen Pakete wandern, statt vom Kern aufgerufen zu werden. Der Aufwand ist
nicht trivial, weil `WolverineWiringSettings` heute quer über alle drei Bereiche liegt (siehe
Punkt 18) — aber ohne diesen Schnitt ist "als NuGet ausserhalb nutzbar" nur für Domain und
Application wahr.

## 3 — Öffentliche API von Infrastructure hängt an Wolverine-Typen

**Problem.** Die Dokumentation begründet mehrere `public`-Typen damit, dass Wolverine C# in
eine andere Assembly generiert und sie beim Namen nennt: `DomainEventEnvelope`,
`DomainEventEnvelopeHandler`, `DomainEventEnvelopeSerializer`, `DomainEventTypeRegistry`,
`ProjectionEnvelope`, `IIntegrationEventSinkFactory`, `IntegrationEventSourceContext`,
`OwnContextIntegrationEventFilter`. Damit wird das öffentliche API-Surface eines
wiederverwendbaren Pakets von einem Implementierungsdetail eines Transports bestimmt. Der
gravierendste Fall ist `IIntegrationEventSinkFactory`: die Schnittstelle ist die einzige
dokumentierte Stelle, an der ein Host einen eigenen Sink einhängen kann, und ihre Signatur ist
`IIntegrationEventSink Create(IMessageContext context)` — `IMessageContext` ist ein
Wolverine-Typ. Ein Konsument mit einem anderen Bus kann diesen Erweiterungspunkt also nicht
bedienen, obwohl er genau dafür public ist. `IntegrationEventMapperCheck` prüft zusätzlich
gegen den konkreten Typ `NullIntegrationEventSinkFactory`, also gegen eine
Implementierungsidentität statt gegen ein Verhalten.

**Lösungsvorschlag.** Den Erweiterungspunkt vom Transport befreien: `IIntegrationEventSink`
liegt bereits richtig in `Application` und kennt Wolverine nicht. Die Factory sollte dieselbe
Eigenschaft haben — etwa `IIntegrationEventSinkFactory` mit einem transport-neutralen
Kontextträger (`object`-Handle ist schlecht; besser ist es, den Sink direkt scoped auflösbar
zu machen und die Wolverine-Bindung im Wolverine-Paket über einen scoped
`IMessageContext`-Wrapper herzustellen). Die Envelope-Typen gehören zusammen mit ihren
Handlern ins Messaging-Paket aus Punkt 2 — dort dürfen sie public sein, weil dort Wolverine
eine deklarierte Abhängigkeit ist, und der Kern bleibt sauber. Für den Mapper-Check gilt:
gegen die *Wirkung* prüfen (ist ein realer Sink registriert?) statt gegen den Nulltyp, wie es
`UnitOfWorkPresenceCheck` bei `NullUnitOfWork` ebenfalls tut — dieselbe Schwäche, dieselbe
Korrektur.

## 4 — `UnitOfWorkBehavior` kennt PostgreSQL-Fehlercodes

**Problem.** `UnitOfWorkBehavior<TRequest, TResponse>` liegt in `Dispatching/` — dem
persistenzneutralsten Ordner des Projekts — und fängt `JasperFx.ConcurrencyException`,
`DbUpdateConcurrencyException`, `DbUpdateException` mit Inner-`PostgresException` sowie
`PostgresException` mit `SqlState == PostgresErrorCodes.UniqueViolation`. Das Behavior kennt
damit drei Persistenztechnologien und einen konkreten SQL-Dialekt. Es ist zudem der einzige
Ort, an dem entschieden wird, welcher Speicherfehler zu welcher `FailureCategory` wird — eine
Entscheidung, die je Speicher unterschiedlich ausfällt und deshalb nicht im Dispatcher liegen
kann. Praktisch heisst das: das gesamte CQRS-Dispatching ist nicht ohne PostgreSQL nutzbar.

**Lösungsvorschlag.** Einen Übersetzungspunkt einziehen, etwa
`IPersistenceFaultTranslator { bool TryTranslate(Exception exception, out Failure failure); }`,
registriert als Enumerable. Das Behavior fängt dann `Exception`, fragt die registrierten
Übersetzer der Reihe nach und wirft weiter, wenn keiner zuständig ist — Verhalten wie heute,
aber ohne Wissen über den Speicher. Jede Persistenzstrategie registriert ihren eigenen
Übersetzer in ihrem eigenen Paket (`EfCorePostgresFaultTranslator`, `MartenFaultTranslator`).
Die beiden Konstanten `ConcurrencyConflictCode` und `UniqueViolationCode` wandern mit; dass
sie heute `public const` auf einem `internal`-Typ sind, ist ohnehin folgenlos.

## 5 — `AggregateStateGraph` baut EF Cores ChangeTracker nach

**Problem.** `AggregateStateGraph` reconciled den Objektgraphen eines ersetzten State gegen
den getrackten Graphen: `CurrentValues.SetValues`, Rekursion über `entry.Navigations`,
Abgleich der Kinder per Primärschlüssel, `collection.Metadata.GetCollectionAccessor()!` mit
`accessor.Add(owner, child, forMaterialization: false)` und `accessor.Remove(...)`. Das ist
eine handgeschriebene Teilimplementierung dessen, was EF Cores `ChangeTracker` intern tut, auf
Basis von APIs (`IClrCollectionAccessor` über `GetCollectionAccessor`,
`IProperty.GetGetter()`), die zwar erreichbar, aber ausdrücklich nicht als stabile öffentliche
Vertragsfläche für Anwendungscode gedacht sind. In einem Anwendungsrepo ist das ein
vertretbares Risiko, das ein EF-Core-Minor-Upgrade sichtbar macht. In einem NuGet-Paket ist es
ein Risiko, das *Konsumenten* tragen, ohne es zu kennen — und der `null!`-Zugriff auf den
Accessor sowie das `IsWritable`-Probing per gecachtem `PropertyInfo` auf
`ICollection<>.IsReadOnly` sind die sichtbaren Symptome davon. Der ganze Mechanismus existiert
nur, weil das Design unveränderliche State-Records auf einen mutationsbasierten
ChangeTracker abbildet.

**Lösungsvorschlag.** Kurzfristig: die Abhängigkeit auf EF-Core-Interna in einer einzigen
Adapterklasse mit einem Vertragstest je verwendeter Metadaten-API kapseln, damit ein Upgrade
zur Buildzeit auffällt statt zur Laufzeit — und die EF-Core-Version im Paket als
`[10.0,11.0)` begrenzen, statt sie offen zu lassen. Mittelfristig die Ursache angehen: Kinder
mit eigener Identität nicht als Teil eines ersetzten State-Records mappen, sondern den
Kind-Record beim Commit über einen expliziten, vom Aggregat gelieferten Änderungs-Satz
(hinzugefügt/geändert/entfernt) an den Kontext geben. Das ist mehr Arbeit im Domainmodell,
ersetzt aber ~180 Zeilen Reflection-Reconciler durch gewöhnliche EF-Core-Aufrufe. Die
Entscheidung gehört in eine ADR, weil sie ADR-0031 berührt.

## 6 — Stream-Key und `AggregateId` sind abgeleitet, nicht deklariert

**Problem.** ADR-0030 verbietet abgeleitete persistierte Namen mit der Begründung, ein
CLR-Rename dürfe niemals gespeicherte Daten anfassen — und erzwingt dafür `[EventName]` und
`[AggregateName]`. Eine Ebene tiefer gilt das nicht: `EntityKeyFormatter.GetKeyValue` bildet
den Schlüsselwert mit `string.Create(CultureInfo.InvariantCulture, $"{value}")` ab, also über
`ToString()` des zugrundeliegenden Werts, und `GetStreamKey` setzt daraus
`"{aggregateName}/{keyValue}"` zusammen. Dieser String ist der Marten-Stream-Key, also der
härteste persistierte Vertrag im ganzen System, und er ist die einzige Form, in der die
Aggregat-Identität auch auf `DomainEventEnvelope`, `DomainEventMetadata` und damit in jeder
Projektion und jedem Read Model landet. Für `Guid` ist das heute stabil; für einen
`long`-, `decimal`-, `DateOnly`- oder Enum-basierten Schlüssel hängt die Datenrepräsentation
an einem BCL-Formatierungsverhalten. Zusätzlich ist die Abbildung einseitig: es gibt keinen
Weg von `AggregateId` zurück zum typisierten Schlüssel, was jede spätere
Wiederherstellung aus Metadaten ausschliesst. Und im Gegensatz zu den Event-Feldnamen (ADR-0035,
`EventSchema.approved.txt`) gibt es dafür keinen Snapshot-Test.

**Lösungsvorschlag.** Die Schlüsselformatierung explizit machen statt implizit: eine
`IEntityKeyStringFormat`-Konvention, die pro Werttyp eine deklarierte Formatierung
(`"D"` für `Guid`, `"O"` für Datumstypen, invariant-dezimal für Zahlen) und einen passenden
Parser festlegt, mit einer Ausnahme bei unbekanntem Werttyp statt eines stillen `ToString()`.
Zusätzlich den Stream-Key-Aufbau in den bestehenden `PersistedSchema`-Snapshot aufnehmen, so
dass ein geänderter Schlüsseltyp oder eine geänderte Formatierung dieselbe rote Prüfung
auslöst wie ein umbenanntes Eventfeld. Das schliesst die letzte Lücke in der ansonsten sehr
konsequenten "persistierte Namen sind deklariert"-Regel.

## 7 — Fehlermeldungen zitieren ADR-Nummern und VitalSync-Prozesse

**Problem.** Der Suchlauf nach `vitalsync` in `BuildingBlocks/src` liefert null Treffer — das
Versprechen aus ADR-0018 hält wörtlich. Die Fehlermeldungen halten es nicht: elf Dateien
zitieren ADR-Nummern ("ADR-0031", "ADR-0037", "ADR-0022/0023"), mehrere verweisen auf
VitalSync-Betriebsprozesse ("Run the context's migration worker — the one host that selects
`ProvisionInfrastructure(...)`"), und `IntegrationEventTopicAttribute` sowie
`MessagingRegistrar` verwenden `nutrition` als Beispiel. Für einen Fremdkonsumenten ist
"ADR-0031" eine Referenz auf ein Dokument, das er nicht hat, und "migration worker" eine
Rolle, die es in seinem System vielleicht nicht gibt. Inhaltlich sind die Meldungen exzellent
— sie erklären konsequent das *Warum*, nicht nur das *Was* — nur ist der Zeiger am Ende ins
Leere gerichtet. Bemerkenswert ist, dass hier ein selbstgesetzter Standard ("write no
comments") die Erklärungen in die Fehlermeldungen verlagert hat, wo sie ausgeliefert werden.

**Lösungsvorschlag.** Die Meldungen behalten, die Referenzen entkoppeln. Konkret: ADR-Nummern
entfernen und stattdessen einen stabilen Regelbezeichner mitgeben, der auch ohne das Repo
funktioniert (z. B. `[BB0031]` mit einer im Paket-README verlinkten Regelliste); die
generischen Sätze bleiben, die VitalSync-spezifische Handlungsanweisung wird generisch
formuliert ("the host that provisions this context's infrastructure"). Die Beispiele
`nutrition` durch neutrale Platzhalter (`orders`, `billing`) ersetzen. Das ist reine
Textarbeit, betrifft aber die Stelle, an der ein Fremdkonsument das Paket zuerst als
"aus einem fremden Projekt herausgeschnitten" erkennt.

## 8 — Kein Erweiterungspunkt: `internal`-first ohne Gegenstück

**Problem.** Die Regel "`internal` ist der Default in Infrastructure" ist für ein
Anwendungsrepo richtig und wird konsequent durchgehalten. Für ein NuGet dreht sie ihr
Vorzeichen um: `IStartupCheck` und `StartupPhase` sind internal — ein Konsument kann keinen
eigenen Startprüfer in denselben Mechanismus einhängen, obwohl der Mechanismus (Phasen,
`IHostedLifecycleService`, fail-fast) genau das ist, was man aus dem Paket haben will.
Dasselbe gilt für `IPersistenceFaultTranslator` (existiert nicht, siehe Punkt 4),
`ITrackedAggregate`, `AggregateTracker<TEntry>`, `DomainEventEnvelopeFactory` und
`EntityKeyJsonOptions`. Übrig bleiben als Erweiterungsflächen `AddPipelineBehavior` und
`AddHandlersFrom` — beides innerhalb des vorgesehenen Rahmens. Wer davon abweichen will, kann
nur forken. Verstärkend: `AddBuildingBlocks` darf genau einmal aufgerufen werden und wirft
sonst, was in einem Anwendungsrepo Fehler verhindert, in einem Bibliothekskontext aber
Komposition durch mehrere Bibliotheken ausschliesst.

**Lösungsvorschlag.** Eine bewusste, kleine öffentliche Erweiterungsfläche definieren statt
sie sich ergeben zu lassen: `IStartupCheck` + `StartupPhase` public (der Mechanismus ist
generisch und hat keinen Vendorbezug), der Faultübersetzer aus Punkt 4 public, und
`EntityKeyJsonOptions.Apply` public, weil ein Konsument dieselben Konverter für seine eigenen
Serialisierungspfade braucht. Alles andere bleibt internal. Der bestehende
`PublicSurfaceTests`-Ansatz ist dafür genau das richtige Werkzeug — er zwingt zur bewussten
Entscheidung. Für ein echtes Paket wäre zusätzlich `Microsoft.CodeAnalysis.PublicApiAnalyzers`
(`PublicAPI.Shipped.txt` / `.Unshipped.txt`) das etabliertere Mittel, weil es Änderungen zur
Buildzeit statt im Test meldet.

## 9 — `DeadLetterHealthCheck` liest Wolverines interne Tabelle per rohem SQL

**Problem.** `DeadLetterInspector` öffnet eine eigene `NpgsqlDataSource` und führt
`select count(*) from (select 1 from wolverine_dead_letters limit 1000) as capped` aus. Der
Tabellenname ist ein Implementierungsdetail von Wolverine, kein Vertrag, und die Abfrage ist
ohne Schema-Qualifizierung geschrieben — Wolverine erlaubt aber ein konfigurierbares
Schema (`SchemaName`). Wird eines gesetzt, greift der `42P01`-Zweig, der Check meldet dauerhaft
`Degraded` mit der Begründung "die Tabelle existiert nicht", und das ist genau der Zustand, den
niemand untersucht, weil er wie ein Provisionierungsproblem aussieht. Die Absicht — ein
verlorenes Projection-Envelope sichtbar machen, ohne den Host aus der Readiness zu kippen — ist
richtig und gut begründet; der Weg dorthin umgeht die Bibliothek, die die Information besitzt.

**Lösungsvorschlag.** Wolverines eigene Verwaltungs-API benutzen (`IMessageStore` →
`DeadLetters` / `Admin`), die den Zähler ohne Kenntnis von Tabellennamen und Schema liefert und
zugleich die zweite Datenbankverbindung samt eigenem Connection-Pool überflüssig macht. Falls
die API den benötigten Zähler nicht bietet, wenigstens den Schema-Namen aus derselben Quelle
beziehen, aus der Wolverine ihn bezieht, und das SQL entsprechend qualifizieren. Im Zuge der
Aufteilung aus Punkt 2 gehört dieser Check ohnehin in das Wolverine-Paket, nicht in den Kern
(siehe auch Punkt 15).

## 10 — Zwei unabhängige Wege zum selben `DbContext`

**Problem.** `EfCoreRepository` bekommt den Kontext über `WriteDbContextAccessor`, der ihn aus
`provider.GetRequiredService<TContext>()` bezieht. `EfCoreUnitOfWork<TContext>` arbeitet auf
`outbox.DbContext`, also auf der Instanz, die Wolverines `IDbContextOutbox<TContext>` auflöst.
Dass beide dieselbe Instanz sind, ist eine Annahme über zwei fremde
Registrierungsmechanismen — sie stimmt heute, weil beide scoped sind und Wolverine den Kontext
aus demselben Scope zieht. Stimmt sie einmal nicht, ist der Effekt so leise wie er nur sein
kann: `outbox.DbContext.Entry(entry.PersistedState)` erzeugt im fremden ChangeTracker einen
`Detached`-Eintrag, `Reconcile` schreibt korrekt in ein Objekt, das niemand speichert, und der
Commit meldet Erfolg. Die Codebasis hat für weit weniger stille Fehler explizite Startprüfungen
— hier gibt es keine.

**Lösungsvorschlag.** Die Annahme auf einen Weg reduzieren: `EfCoreUnitOfWork` sollte den
Kontext ebenfalls aus dem `WriteDbContextAccessor` nehmen und den Outbox nur zum Publizieren
und für `SaveChangesAndFlushMessagesAsync` verwenden — oder umgekehrt der Accessor aus dem
Outbox befüllt werden. Wo das nicht geht, ist ein einzeiliger Vergleich
(`ReferenceEquals(outbox.DbContext, accessor.Context)`) im Commit oder als Startprüfung die
billigste Absicherung. Dass sie fehlt, fällt vor allem deshalb auf, weil `WriteDbContextAccessor`
selbst mit genau dieser Argumentation eingeführt wurde: den unqualifizierten Zugriff auf "den"
DbContext zu verhindern.

## 11 — `net10.0`-only, kein Multi-Targeting

**Problem.** Alle drei Projekte zielen ausschliesslich auf `net10.0`, angebunden an ein per
`global.json` gepinntes SDK. Als NuGet veröffentlicht bedeutet das: nutzbar nur für
Konsumenten, die bereits vollständig auf .NET 10 sind. Für Domain und Application ist das
besonders schade, weil deren Code — abgesehen von Collection Expressions und
`ArgumentException.ThrowIfNullOrWhiteSpace` — problemlos auf .NET 8 liefe und dort das
grösste potenzielle Publikum hätte.

**Lösungsvorschlag.** `Domain` und `Application` auf `net8.0;net10.0` multi-targeten; die
wenigen neueren BCL-Aufrufe sind entweder in .NET 8 vorhanden oder über einen kleinen
`#if`-freien Helfer ersetzbar. `Infrastructure` folgt der niedrigsten Version, die EF Core,
Marten und Wolverine gemeinsam unterstützen, und bleibt sonst bei `net10.0`. Nebeneffekt:
Multi-Targeting deckt versehentliche Abhängigkeiten von Preview-Verhalten auf, die man
einzielig nie bemerkt.

## 12 — Reflection/`Activator`/Expression-Trees ohne Trimming-Annotationen

**Problem.** Das Paket lebt von Laufzeit-Metaprogrammierung: `RequestSender`, `ProjectionRunner`
und `MapperRunner` erzeugen Dispatcher über `Activator.CreateInstance(...MakeGenericType(...))`,
`AggregateFactory` ruft private parameterlose Konstruktoren per `ConstructorInfo.Invoke`,
`EntityKeyActivator` und `EntityKeyFormatter` kompilieren Expression-Trees, und mehrere
Startprüfungen scannen `assembly.GetTypes()`. Das ist alles bewusst gewählt und hat gute
Gründe. Es ist aber unvereinbar mit `PublishTrimmed`, `PublishAot` und teilweise mit
`EnableSingleFileAnalysis` — und nichts davon ist annotiert. Ein Konsument, der trimmt, bekommt
weder eine Warnung beim Publish noch einen Fehler beim Start, sondern eine
`MissingMethodException` beim ersten Command.

**Lösungsvorschlag.** Ehrlich deklarieren statt lösen: `[RequiresUnreferencedCode]` auf den
Einstiegspunkten (`AddBuildingBlocks`, `AddHandlersFrom`, `AddDomainEventsFrom`,
`ISender.SendAsync`-Implementierung), `IsTrimmable=false` im Paket, und ein Satz im README.
Damit erhält der Konsument die Warnung zur Buildzeit, die er heute nicht bekommt. Ein
quellcodegenerierter Dispatcher wäre die technisch bessere Antwort, ist aber ein eigenes
Vorhaben und für die aktuelle Zielgruppe unverhältnismässig.

## 13 — Repository-Registrierung ist nicht typgeprüft

**Problem.** `PersistenceRegistrar.UseMarten` registriert
`services.TryAddScoped(typeof(IRepository<,>), typeof(MartenEventSourcedRepository<,>))`. Die
Implementierung verlangt `where TAggregate : class, IEventSourcedAggregateRoot<TKey>`, das
Vertragsinterface aber nur `IAggregateRoot<TKey>`. Fordert ein Handler in einem
Marten-Kontext ein `IRepository<Widget, WidgetId>` für ein nicht-ereignisquellbasiertes
Aggregat an, bricht die Auflösung erst zur Laufzeit beim ersten Request, mit einer
DI-Meldung über verletzte generische Constraints, die weder das Aggregat noch die Ursache
benennt. Das ist der einzige Fall dieser Art im Paket — für praktisch jede vergleichbare
Fehlklasse existiert eine erklärende Startprüfung (`HandlerRegistrationCheck`,
`UnitOfWorkPresenceCheck`, `IntegrationEventMapperCheck`, `AggregateStateModelCheck`).

**Lösungsvorschlag.** Eine Startprüfung ergänzen, die die gescannten Assemblies nach
`IAggregateRoot<>`-Implementierungen durchgeht und für die gewählte Persistenzstrategie
prüft, ob jedes gefundene Aggregat das jeweils erforderliche Interface erfüllt
(`IEventSourcedAggregateRoot<>` bei Marten, `IStateOwner` bei EF Core) — mit einer Meldung im
Stil der übrigen Prüfungen. Die Infrastruktur dafür steht komplett; es fehlt nur die Prüfung.

## 14 — `Result<T>`: implizite Operatoren, die werfen; kein `null` transportierbar

**Problem.** Drei zusammenhängende API-Kanten. Erstens wirft `Result<TResult>.Success` bei
`null`, und `implicit operator Result<TResult>(TResult value)` ruft es auf — eine implizite
Konvertierung, die eine Ausnahme wirft, ist eine Falle, weil sie an einer Stelle ohne
sichtbaren Aufruf zuschlägt. Zweitens kann `Result<T>` dadurch grundsätzlich kein `null`
tragen; `Result<string?>` ist nicht ausdrückbar, obwohl eine Query legitim "gefunden, Wert
leer" liefern kann. Drittens sind bei `TResult == Failure` die beiden impliziten Operatoren
(`Result<Failure>(Failure)` als Wert und als Fehler) mehrdeutig — ein seltener, aber realer
Compilerfehler beim Konsumenten, den nur ein Paketnutzer trifft, nie das eigene Repo.

**Lösungsvorschlag.** Den impliziten Wert-Operator streichen und `Result.Success(value)`
verlangen — der Gewinn an Kürze wiegt die versteckte Ausnahme nicht auf; der
`Failure`-Operator kann bleiben, weil er keinen Wert konstruiert. Die `null`-Prüfung von
`Success` auf `where TResult : notnull` heben, damit der Compiler statt der Laufzeit meckert,
und für den bewussten Leerfall die Nutzung eines eigenen Ergebnistyps oder `Failure.NotFound`
dokumentieren. Beides sind Breaking Changes und gehören vor eine Paketveröffentlichung, nicht
danach.

## 15 — Composition Root erzwingt HealthChecks und eine `NpgsqlDataSource`

**Problem.** `BuildingBlocksComposition.RegisterCore` ruft unbedingt
`RegisterDeadLetterHealthCheck`, sobald irgendein Write-Connection-String vorliegt. Das ruft
`services.AddHealthChecks()` — also eine Infrastrukturentscheidung, die ein reiner
Worker-Host nicht getroffen hat — und registriert eine Factory, die eine eigene
`NpgsqlDataSource` mit `ApplicationName = "building-blocks-dead-letter-check"` baut. Damit
enthält der Kern-Kompositionspfad eine harte PostgreSQL-Annahme und eine harte
HealthChecks-Annahme, beide ohne Abwahlmöglichkeit. Konsistent wäre es nicht: für die
Persistenz gibt es mit `UseNoPersistence()` eine ausdrückliche Wahl, für diesen Nebenweg gar
keine.

**Lösungsvorschlag.** Die Registrierung dorthin verschieben, wo ihre Voraussetzungen
deklariert sind — in das Wolverine/Postgres-Paket aus Punkt 2, angehängt an
`UseEfCorePersistence`/`UseMartenEventSourcing`. Solange die Aufteilung nicht erfolgt ist,
zumindest an die Bedingung koppeln, dass Messaging überhaupt gewählt wurde (ohne Transport gibt
es keine Dead Letter aus Broker-Zustellungen), und die zweite Datenquelle durch die
Wolverine-API aus Punkt 9 ersetzen, womit auch der separate Connection-Pool entfällt.

## 16 — `AggregateState`: Double-Cast, unerzwungenes F-Bound, `Apply`-`null`

**Problem.** Drei kleine Kanten am sonst überzeugendsten Stück des Domainmodells.
`WithVersion` castet mit `(TSelf)(object)(this with { Version = version })` — die Dokumentation
nennt das bewusst "einen ungeprüften Cast, einmal, in Building Blocks", und das ist vertretbar.
Nur ist die F-Bound-Zusicherung `where TSelf : AggregateState<TSelf, TKey>` nicht
selbstverifizierend: `record Wrong : AggregateState<Other, OtherId>` compiliert, und der Cast
schlägt dann zur Laufzeit mit einer `InvalidCastException` ohne Kontext fehl. Zweitens darf
`State.Apply(domainEvent)` `null` zurückgeben — `AggregateRoot.ApplyEvent` ruft direkt
`.WithVersion(...)` darauf, was in eine nackte `NullReferenceException` läuft, während direkt
darunter die leere Identität eine erklärende `DomainValidationException` bekommt.

**Lösungsvorschlag.** Für das F-Bound eine Startprüfung analog zu
`EntityKeyConstraintTests`/`AggregateConventionTests`: beim Scannen der Domain-Event-Assemblies
prüfen, dass jeder `AggregateState<TSelf, TKey>`-Erbe sich selbst als `TSelf` einsetzt — dieselbe
fail-fast-Latte wie bei `[EventName]` und dem privaten Konstruktor. Für `Apply` einen
`ArgumentNullException`-artigen Guard mit derselben erklärenden Meldung wie beim leeren
Identitätsfall; drei Zeilen, und der häufigste Anfängerfehler beim Schreiben eines
`Apply`-`switch` (fehlender Default-Arm, der `null` liefert) wird benennbar.

## 17 — `LoadFromHistory` ist nicht gegen doppelten Aufruf geschützt

**Problem.** `EventSourcedAggregateRoot.LoadFromHistory` prüft nur, ob bereits Domain Events
erhoben wurden. Ein zweiter Aufruf auf derselben Hülle faltet die Historie erneut auf den
bereits gefalteten Zustand und verdoppelt die Version — was beim Commit zu einem falschen
`Expected Stream Version` und damit zu einem Concurrency-Fehler führt, dessen Ursache nirgends
steht. Heute rufen nur `MartenEventSourcedRepository` und `EventSourcedReadModelRebuildRunner`
auf, beide genau einmal; die Methode ist über die explizite Interface-Implementierung aber
erreichbar.

**Lösungsvorschlag.** Zusätzlich gegen die aktuelle Version prüfen: ist sie ungleich null,
war die Hülle bereits geladen — mit derselben erklärenden `InvalidOperationException` wie beim
bestehenden Fall. Eine Zeile, und die Methode wird von "nur korrekt aufrufbar" zu
"falsch aufrufbar mit Meldung".

## 18 — `WolverineWiringSettings` trägt einen Vendornamen für Nicht-Vendor-Zustand

**Problem.** Die Regel "benenne nie einen Ordner oder Typ nach einem Vendor" wird im Paket
ausdrücklich formuliert und begründet (`RequestSender` statt `ISender`, `TopicResolver` statt
`IntegrationEventTopic`, `StateStored`/`EventSourced` statt `Marten`). Der zentralste
Zustandstyp des Kompositionspfads bricht sie: `WolverineWiringSettings` hält die
Persistenzwahl, die Messaging-Einstellungen, das Abonnement *und* die Provisionierungswahl —
drei davon haben mit Wolverine nichts zu tun, und `PersistenceChoice` bzw.
`InfrastructureProvisioning` würden auch ohne jeden Message-Bus existieren. Praktisch wird
dieser Typ zur grössten Hürde bei der Aufteilung aus Punkt 2, weil er alle Bereiche
zusammenbindet.

**Lösungsvorschlag.** In `BuildingBlocksWiringSettings` umbenennen (der Typ ist internal, die
Umbenennung kostet nichts) und beim Aufteilen entlang der bereits vorhandenen Eigenschaften
zerlegen: `PersistenceSelection`, `MessagingSelection`, `ProvisioningSelection`, jede im
zugehörigen Paket, zusammengehalten von einem schmalen Kern-Typ. Der Name ist der kleinere Teil;
er zeigt aber genau die Naht, an der die Aufteilung ansetzen muss.

## 19 — `KebabCase` ist public aus Verlegenheit

**Problem.** `BuildingBlocks.Domain.Naming.KebabCase` ist erklärtermassen "public purely
because Infrastructure needs it". In einem Anwendungsrepo ist das eine harmlose
Sichtbarkeitsentscheidung. In einem NuGet ist jede public Klasse ein Vertrag: sie erscheint in
IntelliSense, wird benutzt und muss ab dann semver-stabil bleiben — für einen
Validierungshelfer, der eigentlich ein Implementierungsdetail ist. Die Schwester-Klasse
`ContractName` ist konsequenterweise internal.

**Lösungsvorschlag.** `KebabCase` internal machen und die Sichtbarkeit für Infrastructure über
`InternalsVisibleTo` in `BuildingBlocks.Domain` herstellen — derselbe Mechanismus, den
Infrastructure bereits für seine Testassembly nutzt. Falls die Validierung tatsächlich Teil des
öffentlichen Vertrags sein soll (etwa weil Konsumenten eigene Namensattribute schreiben), dann
bewusst und dokumentiert public lassen, aber nicht mit der Begründung "sonst kommt Infrastructure
nicht dran".

## 20 — `RuleChecker`-Überladungen sind bei leerem Aufruf mehrdeutig

**Problem.** `RuleChecker` bietet je zwei Überladungen pro Regelart: eine mit einem einzelnen
Argument und eine mit `params`. `Check(rule)` bindet korrekt an die Einzelvariante,
`Check(a, b)` an die `params`-Variante. `Check()` und `Check(null)` sind dagegen zwischen
`params IBusinessRule[]` und `params IDomainValidationRule[]` mehrdeutig und erzeugen beim
Konsumenten CS0121 mit einer Meldung, die die Ursache nicht erklärt. Das ist kein Fehler im
Verhalten, sondern eine Kante in der Signaturmenge.

**Lösungsvorschlag.** Getrennte Namen für die beiden Regelarten (`CheckRules` / `Validate`,
oder `Check` / `CheckValidation`) beseitigen die Mehrdeutigkeit vollständig und machen an der
Aufrufstelle zusätzlich sichtbar, welche Fehlerklasse entsteht — was ohnehin die wichtigere
Information ist, da die beiden Arten laut Konvention nie in einem Aufruf gemischt werden
dürfen. Alternativ, minimal-invasiv: die `params`-Überladungen auf ein verpflichtendes erstes
Argument umstellen (`Check(IBusinessRule first, params IBusinessRule[] rest)`), was den leeren
Aufruf unmöglich macht.

## 21 — `DomainEventEnvelope.Payload` wird doppelt serialisiert

**Problem.** `DomainEventEnvelopeSerializer.Wrap` serialisiert das Domain Event nach `string`
und legt diesen String als `Payload` in den Envelope, der anschliessend von Wolverine selbst
nach JSON serialisiert wird. Der Payload wird dabei vollständig escaped und landet als
JSON-String-in-JSON in der Outbox-Zeile. Effekt: rund doppelter Speicherbedarf pro Zeile, eine
zusätzliche UTF-16/UTF-8-Konvertierung pro Richtung, und ein in der Datenbank unlesbarer,
weil escapeter Payload — was bei genau der Tabelle stört, in die man bei einem Zwischenfall
zuerst schaut. Der Grund für die String-Zwischenstufe (Typauflösung über `EventName` statt
über einen CLR-Typnamen) ist richtig und muss bleiben; die String-Repräsentation folgt daraus
nicht.

**Lösungsvorschlag.** `Payload` als `JsonElement` (oder `byte[]` mit UTF-8) führen. `JsonElement`
wird von System.Text.Json ohne Escaping eingebettet, bleibt in der Outbox-Zeile lesbar und
erspart beide Konvertierungen; `Unwrap` deserialisiert direkt aus dem Element in den über
`EventName` aufgelösten Typ. Der Envelope ist ein Infrastrukturtyp ohne eigenen
Schema-Snapshot, die Änderung ist also nur für in-flight-Outbox-Zeilen relevant und braucht ein
Drain-Fenster beim Deployment.

## 22 — `InternalsVisibleTo` auf die Testassembly wird mit ausgeliefert

**Problem.** `BuildingBlocks.Infrastructure.csproj` enthält
`<InternalsVisibleTo Include="BuildingBlocks.Infrastructure.Tests" />`. Das Attribut landet im
kompilierten Assembly und damit im Paket. Da die Assembly nicht signiert ist, kann jeder
Konsument eine Assembly namens `BuildingBlocks.Infrastructure.Tests` bauen und erhält vollen
Zugriff auf sämtliche internals — also auf praktisch das ganze Projekt, weil `internal` hier
der Default ist. Sicherheitsrelevant ist das kaum, aber es hebt die in Punkt 8 beschriebene
Kapselung faktisch auf und liefert einen Testartefakt an Produktionskonsumenten aus.

**Lösungsvorschlag.** Für ein veröffentlichtes Paket den Zugriff über eine
Konfigurationsbedingung nur im internen Build setzen (`Condition="'$(IsPackaging)' != 'true'"`)
oder die Assembly signieren und den `PublicKey` im Attribut angeben. Solange nicht
veröffentlicht wird, ist der Punkt rein hygienisch.

## 23 — `FailureCategory` ist ein geschlossenes Enum ohne Erweiterungsweg

**Problem.** Die fünf Werte (`Validation`, `BusinessRule`, `NotFound`, `Conflict`, `Forbidden`)
sind bewusst gewählt, und die Begründungen für die Nicht-Werte (`Unexpected`, `Unauthorized`)
sind überzeugend. Für VitalSync ist das die richtige Entscheidung. Für ein Paket bedeutet es,
dass ein Konsument mit einer sechsten, legitimen Kategorie (z. B. `RateLimited`,
`PreconditionFailed`) keinen Weg hat ausser einem Fork — `Failure` prüft im Konstruktor
`Enum.IsDefined`, verschliesst die Tür also aktiv. Zugleich ist im Repo dokumentiert, dass ein
neuer Wert am Transport-Rand nicht compilergeprüft werden *kann* und deshalb durch zwei
Laufzeitprüfungen abgesichert wird — dieselbe Absicherung stünde einem Konsumenten nicht zur
Verfügung.

**Lösungsvorschlag.** Zwei gangbare Wege. Entweder bewusst geschlossen lassen und im README
als Designentscheidung dokumentieren ("das Paket definiert ein festes Fehlervokabular; wer ein
anderes braucht, benutzt `Failure.Code`") — das ist legitim und die kleinere Änderung. Oder
`FailureCategory` von einem `enum` auf einen `readonly record struct` mit statischen
vordefinierten Werten umstellen, wodurch die vorhandenen fünf unverändert benutzbar bleiben,
die Transport-Abbildung weiterhin einen Default-Zweig braucht (wie heute) und ein Konsument
einen eigenen Wert ergänzen kann. Zweiteres ist eine Breaking Change und gehört vor eine
Veröffentlichung.

## 24 — Statische, prozessweite Typ-Caches

**Problem.** `RequestSender`, `ProjectionRunner`, `MapperRunner`, `TopicResolver`,
`EntityKeyFormatter`, `AggregateFactory`, `EntityKeyModelBuilderExtensions` und
`AggregateStateGraph` halten alle `static readonly ConcurrentDictionary<Type, …>`. Die Inhalte
sind zustandslos, der Ansatz ist korrekt und schnell. Zwei Konsequenzen sind für ein
Bibliothekspaket aber erwähnenswert: die Caches werden über alle Hosts eines Prozesses geteilt
(bei mehreren Testhosts unkritisch, weil die Werte identisch sind, aber die Speicherbelegung
wächst monoton), und sie halten `Type`-Referenzen fest, was das Entladen eines
`AssemblyLoadContext` verhindert — relevant für Plugin-Szenarien, in denen ein Paket wie dieses
durchaus landen kann.

**Lösungsvorschlag.** Für die Mehrheit der Fälle nichts tun, aber die Entscheidung
dokumentieren. Wo Aufwand vertretbar ist, die Caches an ein Singleton hängen (der Container hat
bereits einen — `DomainEventTypeRegistry` ist genau so gebaut), womit der Lebenszyklus dem Host
folgt statt dem Prozess. Für `AggregateFactory` und `TopicResolver` ist das die kleinste
Änderung mit dem grössten Effekt, weil beide bereits über einen Startpfad laufen, der ein
Singleton besitzt.

## 25 — `Entity<TKey, TState>` bindet sich per Closure an seinen Root

**Problem.** Die Kind-Entität bekommt im Konstruktor einen `IDomainEventRaiser` und einen
`Func<TKey, TState?> stateLookup`. Der Root reicht damit zwei Closures über sich selbst in
die Hülle; `GetCurrentState()` ruft die Lambda auf und wirft, wenn sie `null` liefert. Das
funktioniert und ist konsistent zur ADR-0032-Erzählung ("ein Kind erhebt über seinen Root").
Als API ist es dennoch die schwächste Stelle im Domainmodell: der Lebenszyklus der Hülle hängt
an einer anonymen Funktion, deren Herkunft im Debugger nicht ablesbar ist, das Kind kann seinen
Root nicht benennen (was jede Fehlermeldung generisch hält, siehe die Meldung "is no longer part
of its aggregate", die den Root nicht nennt), und ein versehentlich mitgefangener veralteter
State im Closure wäre praktisch nicht zu finden.

**Lösungsvorschlag.** Beide Closures durch eine einzige Schnittstelle ersetzen, die der Root
explizit implementiert — etwa `IChildStateHost<TKey, TState>` mit `TState? Find(TKey id)` und
`void Raise(IDomainEvent)`. Das Kind hält dann eine benannte Referenz statt zweier Lambdas, die
Fehlermeldung kann den Root-Typ und dessen Id nennen, und der Debugger zeigt eine echte
Objektbeziehung. Der Aufwand liegt bei einer Schnittstelle und einer geänderten
Konstruktorsignatur; die Semantik bleibt identisch.
