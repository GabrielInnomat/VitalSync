# 0038. Thessera is the product name; `GaWeCodes` stays the publisher prefix

- **Status:** Accepted
- **Date:** 2026-08-19

## Context

The reusable platform has now been renamed twice. It started as `BuildingBlocks.*`, became
`GaWeCodes.*`, and this ADR records the third and last move before publication: `GaWeCodes.Thessera.*`.

The second move was made under a decision recorded outside the ADR set (the packaging plan,
decision 0.11): **the publisher prefix would double as the product name.** A fantasy name — `Tessera`
was named explicitly — was considered and rejected, on the grounds that a fantasy name *promises a
product* while the goal was only "a presentable library published under my own name".

Two consequences followed from that choice, and both were recorded as deliberate:

- The package family had no name of its own. `GaWeCodes.Core`, `GaWeCodes.Domain` and
  `GaWeCodes.Wolverine` read as "some packages by GaWeCodes", not as parts of one thing.
- The composition entry points kept their old names. `AddBuildingBlocks`, `BuildingBlocksOptions`
  and `BuildingBlocksWiringSettings` were **explicitly not renamed**, and the stated reason was that
  a publisher prefix does not belong in a method name — Microsoft writes `AddLogging()`, not
  `AddMicrosoft()`. Under that premise the reasoning was correct, and the comparison with
  `AddMarten()` genuinely did not apply.

The forces that make this worth reopening now, before 1.0 and not after:

- **A publisher prefix cannot carry a family.** `GaWeCodes.` will eventually hold packages that have
  nothing to do with this platform. Once it does, nothing in the names says which packages belong
  together, and a consumer reading a `csproj` cannot tell a platform package from an unrelated one.
- **The runtime names had nowhere to point.** The `ActivitySource` was called `GaWeCodes` and the
  telemetry tags `gawecodes.*`, sitting in a dashboard beside `Npgsql`, `Wolverine` and `Marten` —
  all of them *product* names. The odd one out named a person.
- **The consumer-facing verb was stranded.** `AddBuildingBlocks` referred to a package family that
  no longer existed under that name in any identifier. It survived only because no better name was
  available: with no product name, every candidate (`AddFramework`, `AddWiring`, `AddFoundation`)
  was a common word a second library would plausibly choose, and `AddGaWeCodesComposition` tied a
  deliberately package-independent call to one package.
- **The cost is entirely front-loaded.** Nothing is published. After 1.0 the same change would be a
  major version for every package, and the message header would need dual operation because it
  travels between services.

## Decision

**The platform is called Thessera. Package IDs, assembly names and namespaces are
`GaWeCodes.Thessera.*` — publisher prefix, product name, then the package's own name.**

This supersedes the packaging plan's decision 0.11 in the single point of the product name. Everything
else 0.11 settled — MIT, `net10.0`, MinVer, nuget.org, the prefix reservation — stands unchanged, and
`GaWeCodes` remains the publisher prefix exactly as before.

### What the name reaches

| Layer | Before | After |
| ----- | ------ | ----- |
| Package ID / assembly / directory | `GaWeCodes.Core` | `GaWeCodes.Thessera.Core` |
| Namespace (ADR rule: namespace = package ID + folder path) | `GaWeCodes.Core.Dispatching` | `GaWeCodes.Thessera.Core.Dispatching` |
| Composition namespace (the one documented exception) | `GaWeCodes` | `GaWeCodes.Thessera` |
| Entry point | `AddBuildingBlocks(…)` | `AddThessera(…)` |
| Options / wiring types | `BuildingBlocksOptions`, `BuildingBlocksWiringSettings` | `ThesseraOptions`, `ThesseraWiringSettings` |
| Wolverine extension applied by the runtime | `ApplyBuildingBlocks*` (5 methods) | `ApplyThessera*` |
| `ActivitySource` | `"GaWeCodes"` | `"Thessera"` |
| Telemetry tags | `gawecodes.*` (11 tags) | `thessera.*` |
| Message header | `gawecodes.source-context` | `thessera.source-context` |
| Repository folder / solution | `BuildingBlocks/BuildingBlocks.slnx` | `Thessera/Thessera.slnx` |

### `AddThessera` is now the *correct* name, not a concession

The earlier decision not to rename the entry point rested on a premise this ADR removes. Its own
argument now points the other way: an extension method on `IHostApplicationBuilder` is effectively
globally visible after the `using`, so it must above all be **unlikely to collide** — and a product
name is the strongest guarantee of that. This is precisely why `AddMarten()` and `AddWolverine()`
work. The comparison was rejected in 0.11 for want of a product name; there is one now.

The method stays **package-independent**: satellite packages contribute to the same registration, and
it is called exactly once per host. That is why it is not `AddThesseraCore`.

### The three-segment rule is restated, not abandoned

The packaging plan set a "three segments" rule against segments that carry no meaning. With the
product segment inserted, the rule reads: **at most three segments after `GaWeCodes.Thessera.`**, and
the documented exception stands — `GaWeCodes.Thessera.Persistence.EfCore.Postgres` spends its three
on concern, technology family and vendor, mirroring `Npgsql.EntityFrameworkCore.PostgreSQL`.

### Type names keep their own vocabulary

`PostgresFaultTranslator`, `AggregateRoot`, `EntityKeyFormatter` and their kind are unaffected. The
product name replaces `BuildingBlocks` only where `BuildingBlocks` was standing in for the *family*.
Evans' generic term "building block" remains correct English in prose and stays in the domain docs.

## Consequences

- The family is legible from a `csproj` for the first time. `GaWeCodes.Thessera.*` says publisher
  and product; a later unrelated `GaWeCodes.*` package can no longer be mistaken for part of it.
- The prefix reservation for `GaWeCodes.` on nuget.org still covers every ID, so no new
  availability check is needed — but the reservation is now load-bearing for two purposes rather
  than one.
- Names got longer, and the most-typed one — `GaWeCodes.Thessera.Persistence.EfCore.Postgres` — is
  the longest. This was already accepted for the EF Core pair under the previous decision; the
  product segment makes it one word worse, in exchange for the family being nameable at all.
- **The runtime rename is a breaking change that no compiler catches.** Dashboards, alerts and
  saved queries built on `gawecodes.*` stop matching. Nothing is published, so today the blast
  radius is this repository. It would not be after 1.0, and the message header in particular would
  have required running both names side by side.
- Existing ADRs keep their original wording and their now-stale paths (`BuildingBlocks/src/…`,
  `BuildingBlocks.Infrastructure`). ADRs are immutable, and they record what was decided when it was
  decided. **This ADR is the map from those names to today's.**
- Three renames in one repository's history is two more than a published library gets. That is the
  argument for doing it now and the reason this is recorded as a decision rather than a chore: the
  next rename would cost a major version.

## Alternatives considered

- **Keep `GaWeCodes.*` as decided in 0.11.** Zero cost, and it was a defensible reading of the goal.
  Rejected because the goal itself shifted: a family meant to be adopted by strangers needs a name
  strangers can say, and the publisher prefix left the runtime names, the entry point and the family
  boundary each without an answer.
- **Rename only the packages, keep `AddBuildingBlocks`.** Halves the diff and keeps the one line
  every consumer has already written. Rejected because it leaves the platform with two names for
  itself — the exact condition that made `BuildingBlocks` a leftover after the previous rename, and
  the reason a test kept passing while asserting nothing.
- **`Tessera` (the conventional spelling).** Better recognised — a mosaic tile, and an apt image for
  composable pieces. Rejected in favour of `Thessera` as chosen by the author; the trade is
  searchability against uniqueness, and both names were free.
- **Drop `GaWeCodes` and publish as `Thessera.*`.** The shortest names, and what a product-first
  library would do. Rejected because the prefix reservation is what stops a third party publishing
  under the family name, and an unprefixed `Thessera.` reservation is far harder to obtain and
  defend for a single author.
- **Product segment only in the namespace, not in the package ID.** Would have kept IDs short.
  Rejected because it breaks the rule that namespace equals package ID plus folder path — the rule
  that makes `IDE0130` a real guard rather than a formality satisfied by construction.
