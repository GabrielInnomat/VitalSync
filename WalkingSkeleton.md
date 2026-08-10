# Walking Skeleton — Plan und Stand

## 9. Offene Fragen und Risiken

**Dieser Abschnitt führt nur noch offene Punkte.** Gelöste wurden am 2026-08-09 entfernt; ihre
Begründungen leben in den ADRs unter `docs/architecture/decisions/` und in den Tests weiter, die
Versionsgeschichte hat den vollen Wortlaut. Jeder Punkt ist in [todo.md](todo.md) mit einer
Priorität geführt.

| Nr.   | Titel                                                      | Herkunft     | TODO    |
| ----- | ---------------------------------------------------------- | ------------ | ------- |
| WS-07 | Der gRPC-Vertrag liegt noch beim Service                   | Etappe 1     | TODO-36 |

Nachzügler aus Commit `e44ae9b`: die drei produktiven MigrationService-Worker sind leere Hüllen
(`Host.CreateApplicationBuilder`, `Build()`, kein `Run()`), `WaitForCompletion` ist damit heute
eine Zusage ohne Inhalt. Bleibt bewusst offen, bis pro Kontext feststeht, wie dort gespeichert
wird — siehe [todo.md](todo.md), TODO-46. Seit ADR-0037 hängt daran ein zweiter Auftrag: dieser
Worker ist der einzige Host seines Kontexts, der provisionieren darf, also
`InfrastructureProvisioning.AtStartup` wählt.

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
