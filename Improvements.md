# Improvements — BuildingBlocks

Befunde aus der Code-Analyse von `BuildingBlocks/src/BuildingBlocks.{Domain,Application,Infrastructure}`.

**Dieses Dokument führt nur noch offene Befunde.** Gelöste wurden am 2026-08-09 entfernt; die
Begründungen leben in den ADRs unter `docs/architecture/decisions/`, in den Instruktionsdateien
und in den Tests weiter. Die Versionsgeschichte hat den vollen Wortlaut.

Jeder Punkt ist in [todo.md](todo.md) mit einer Priorität geführt; dort steht auch, was ihn
auslöst.

## Status

| Nr.    | Titel                                                          | Status    | TODO    |
| ------ | -------------------------------------------------------------- | --------- | ------- |
| IMP-19 | Ein Assembly für EF Core, Marten, Wolverine und RabbitMQ       | offen     | TODO-33 |
| IMP-33 | Keine Saga- oder Process-Manager-Abstraktion                   | offen     | TODO-39 |

---

# IMP-19, Ein Assembly für EF Core, Marten, Wolverine und RabbitMQ

`BuildingBlocks.Infrastructure` referenziert unverändert 11 Pakete, darunter Marten, EF Core, Npgsql
und fünf WolverineFx-Pakete
([BuildingBlocks.Infrastructure.csproj](BuildingBlocks/src/BuildingBlocks.Infrastructure/BuildingBlocks.Infrastructure.csproj)).
Ein rein state-stored Service zieht Marten mit, ein event-sourced Service EF Core — und jedes
Major-Upgrade eines der Pakete betrifft alle Services gleichzeitig.

## Lösungsvorschlag

Aufteilen entlang der Abhängigkeitsgrenzen, sobald der zweite echte Service existiert:

```
BuildingBlocks.Infrastructure            → Dispatching, Events, DI-Kern (keine Store-Pakete)
BuildingBlocks.Infrastructure.EfCore     → EfCoreRepository/UnitOfWork/Tracker, EntityKey-Konverter
BuildingBlocks.Infrastructure.Marten     → Marten-Repository/UnitOfWork/Tracker, EntityKeyFormatter
BuildingBlocks.Infrastructure.Wolverine  → Envelope, Handler, Sink, Wolverine-Extensions
```

Der Schnitt ist heute schon durch die Ordnerstruktur vorgezeichnet und wäre eine reine
Projektverschiebung. Vorher lohnt er nicht — mit einem Sample-Paar ist der Aufwand höher als der
Gewinn. Der Auslöser für die Umsetzung: der erste Produktions-Service, der nur eine der beiden
Persistenzwelten braucht.

---

# IMP-33, Keine Saga- oder Process-Manager-Abstraktion

Abgedeckt sind: eingehender Command, eingehende Query, eingetroffenes Event. Nicht abgedeckt: alles mit
Zustand über Zeit und Zeitsteuerung — „erinnere nach 3 Tagen ohne Eintrag", „warte, bis Nutrition
**und** Fitness gemeldet haben". Der letzte Fall ist für Analytics absehbar relevant.

## Lösungsvorschlag

Nicht selbst bauen — Wolverine bringt Sagas und `ScheduleAsync` bereits mit, und beides läuft über
dieselbe durable Message-Infrastruktur, die hier ohnehin schon konfiguriert ist:

```csharp
public sealed class DailyAnalyticsSaga : Saga
{
    public string Id { get; init; }
    public bool NutritionReported { get; set; }
    public bool FitnessReported { get; set; }

    public void Handle(NutritionDayClosed e) { NutritionReported = true; }
    public void Handle(FitnessDayClosed e)   { FitnessReported = true; }
}
```

Die Entscheidung, die eine ADR braucht: ob Wolverine damit vom reinen Transport (ADR-0015/0023) zum
Prozess-Host aufgewertet wird. Das ist eine bewusste Aufweichung der bisherigen Abgrenzung — vor der
ersten Saga klären, nicht danach.
