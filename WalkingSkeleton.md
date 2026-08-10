# Walking Skeleton — Plan und Stand

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
