# Hacky — Fundstellen aus dem Codebase-Scan

**Dieses Dokument führt nur noch offene Fundstellen.** Gelöste wurden am 2026-08-09 entfernt; ihre
Begründungen leben in den ADRs unter `docs/architecture/decisions/` und in den Tests weiter, die
Versionsgeschichte hat den vollen Wortlaut.

## Status

| Nr. | Titel                                  | Status | TODO    |
| --- | -------------------------------------- | ------ | ------- |
| 13  | Global sequentielle Domain-Event-Queue | offen  | TODO-20 |

---

# 13, Global sequentielle Domain-Event-Queue

`options.PublishMessage<DomainEventEnvelope>().ToLocalQueue(...).Sequential()` serialisiert
_sämtliche_ Domain Events des gesamten Service über eine Queue, um eine _pro-Aggregat_-Garantie
zu kaufen (ADR-0022). Global serialisieren für eine lokale Zusage — kein Fehler, aber eine
Durchsatzobergrenze von einem Event zur Zeit pro Service.

`BuildingBlocks/src/BuildingBlocks.Infrastructure/DependencyInjection/Wiring/WolverineOptionsExtensions.cs`

## Lösungsvorschlag

Nach Aggregat-Id partitionieren statt global zu serialisieren: gleiche Ordnungsgarantie, aber
parallel über verschiedene Aggregate. Der `DomainEventEnvelope` führt `AggregateName`/`AggregateId`
seit ADR-0030 mit, die Voraussetzung dafür ist also erfüllt.

Vor der ersten Lastmessung nicht anfassen. Hier festgehalten, damit die Entscheidung bewusst
getroffen wird und nicht als Default stehen bleibt.
