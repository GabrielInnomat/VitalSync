# 0033. Typed keys are mapped explicitly, never discovered

- **Status:** Accepted
- **Date:** 2026-08-05

## Context

ADR-0005 introduced strongly typed identifiers and promised that persistence "requires mapping
between strongly typed identifiers and their underlying primitives (provided by a value converter
in the Persistence building block)". `ApplyEntityKeyConversions` delivered that promise, but it
did two jobs at once.

The first job is unavoidable: EF Core's value converters have to be attached to every property
whose CLR type implements `IEntityKey<T>`, because the provider cannot store the key type itself.

The second job was discovery. EF Core's property discovery never finds an `IEntityKey<T>`
property — it reports "not a supported primitive type" — so such a property is absent from
`entityType.GetProperties()`. To let a `DbContext` map a typed key by convention, the helper
scanned the CLR type with reflection and called `AddProperty` for every candidate the model did
not already know.

That second job is where the problems were. Because the scan added properties the model had never
been asked about, it silently overrode explicit modelling decisions: a property excluded with
`Ignore()` came back as a column, and a computed get-only property broke model creation outright
with "No backing field could be found". Guards for both cases were possible, and were tried, but
they only narrowed a mechanism whose premise no longer held.

The premise was that contexts map by convention. They do not. Every `DbContext` in this
repository — both sample write contexts, both sample read contexts, and every persistence test
fixture — configures its entities explicitly, because it needs column names, `IsRequired`,
`HasMaxLength`, `IsConcurrencyToken` and, for children, `OwnsMany` with `HasKey` (ADR-0031).
Explicit configuration means `FindProperty` always succeeds, which means the discovery branch was
never reached outside the two tests written to exercise it. It was dead code in production that
carried all of the risk.

## Decision

`ApplyEntityKeyConversions` attaches value converters and does nothing else. It iterates the
properties the model already has and assigns a converter to each one whose CLR type implements
`IEntityKey<T>`, leaving any property that already carries a converter untouched. There is no
reflection over the CLR type, no `AddProperty`, and consequently no guards.

A `DbContext` therefore **maps every typed key property explicitly**, the way it already maps
every other property. A typed key that is neither mapped nor ignored is not silently added: EF
Core fails when the model is built, naming the property and its type and stating both remedies.

## Consequences

- The helper cannot contradict the model, because it no longer writes to the model. `Ignore()`
  stays ignored and a computed property stays unmapped, by construction rather than by guard.
- A forgotten typed key is a loud model-creation failure instead of an unrequested column. The
  failure appears at host startup, which is where this repository puts its other structural
  checks.
- ADR-0005's promise is narrowed: a typed identifier still costs no domain ceremony and no
  hand-written conversion, but it does cost one line of mapping per property. That line was
  already being written everywhere.
- Owned types are unaffected. They are separate entity types whose properties are configured
  through `OwnsMany`, so they were always found by `FindProperty` and never took the discovery
  path.
- Value objects mapped as EF Core complex types remain out of scope. `ComplexProperty` is used
  nowhere in this repository; when the first one appears, covering it is an added loop over
  `GetComplexProperties()` against a real case rather than a speculative one.
- Iterating the model while mutating it is no longer possible, so the defensive copy the previous
  implementation needed around its entity-type loop is gone.

## Alternatives considered

- **Keep discovery and add guards.** Implemented first: skip a candidate that is a navigation, is
  `IsIgnored`, or has no setter. It fixed both observed symptoms and left the mechanism in place —
  a reflection scan that mutates the model, exercised only by tests that opt out of the explicit
  mapping every real context performs. Rejected as guarding dead code.
- **Keep discovery for read models only.** Read models are the flattest entities in the system and
  the least likely to need it; the split would have made the rule conditional without removing any
  of the mechanism.
- **Add a startup check that reports unmapped typed keys with a custom message.** It would have to
  reflect over CLR types again — the very thing being removed — to produce a message no better
  than EF Core's, which already names the property, the type and both remedies.

## Amendment (2026-08-10): complex types are covered after all

The consequence above deferred complex types until a real case appeared. The deferral is
withdrawn — not because a case appeared, but because the reasoning behind it did not survive
being written down.

The argument was "an added loop against a real case rather than a speculative one". That is the
right instinct for a *mechanism*, and it was the right call for the discovery branch this ADR
removed. It is the wrong call here, because the omission is not a missing feature — it is an
inconsistency in the rule this ADR states. The rule is "every property in the model whose CLR
type implements `IEntityKey<T>` gets a converter". A complex type's properties **are** in the
model; they are simply reached through `GetComplexProperties()` instead of `GetEntityTypes()`,
because a complex type is not an entity type. Leaving them out made the rule silently depend on
which mapping construct a property happened to sit under.

The cost of the deferral was also asymmetric. The fix is one recursive walk over
`IMutableTypeBase`, which `IMutableEntityType` and `IMutableComplexType` both implement, so
nesting costs nothing extra and there is no second code path to keep in step. The cost of *not*
having it lands on whoever writes the first `ComplexProperty` with a typed key inside: EF Core
fails with "not a supported primitive type", which names the property but not the reason, and
the reason — "this helper only walks entity types" — is invisible from the call site.

The test is therefore a genuine complex type in `EntityKeyConversionTests`, one level and one
nested level, on its own read-model-shaped fixture rather than on the existing aggregate-shaped
one. It is a constructed case, as the original consequence predicted; it is also exactly the
shape the first real one will have, because the read side is where inlined value objects belong
(the write side is closed to them by ADR-0025 and ADR-0031).

Two limits stay. A **collection** of complex types is out of scope: it maps to JSON and travels
through the JSON converters (ADR-0034), not through this helper. And explicit mapping still
applies inside a complex type — `ComplexProperty(..., builder => builder.Property(...))` — for
the same reason it applies everywhere else: this helper converts, it never discovers.
