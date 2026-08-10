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

**Alle P1 und P2 sind erledigt.** Die verbliebenen P3 warten jeweils auf einen Auslöser: TODO-33
und TODO-36 auf den ersten echten Service, TODO-39 auf die erste Saga.

## Übersicht

| Nr.     | Titel                                                      | Prio   | Status    | Quellen           |
| ------- | ---------------------------------------------------------- | ------ | --------- | ----------------- |
| TODO-33 | Ein Assembly für alle Persistenz-Pakete                    | **P3** | offen     | IMP-19            |
| TODO-36 | Der gRPC-Vertrag liegt noch beim Service                    | **P3** | offen     | WS-07             |
| TODO-39 | Keine Saga- oder Process-Manager-Abstraktion               | **P3** | offen     | IMP-33            |

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
