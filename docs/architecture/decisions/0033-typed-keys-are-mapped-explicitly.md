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
