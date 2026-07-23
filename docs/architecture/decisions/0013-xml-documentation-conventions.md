# 0013. XML documentation conventions for Building Blocks

- **Status:** Accepted
- **Date:** 2026-07-23

## Context

The `BuildingBlocks` projects form the shared, VitalSync-independent foundation consumed by every
service. Their public surface is API for the rest of the codebase, so it must be documented
consistently and at a level of quality that explains not just *what* a member is, but *why it
exists* and *how and when to use it*.

Two problems motivated this decision:

1. **Inconsistent wording.** The same concept was documented differently across files (e.g. the
   key type parameter was variously "the type of the entity's identity key", "the type of the
   aggregate root's identifier", and "the type of the key"). This friction makes the API harder to
   read and review.
2. **Missing rationale.** Summaries described *what* a member is but rarely captured the *why*,
   *how*, or *when* — the insight a consumer actually needs.

We want a single, authoritative convention that both human developers and Copilot follow, without
imposing documentation ceremony on code where it adds no value (application/service code and test
projects).

## Decision

### Scope — where XML documentation is required

- XML documentation is **required only under `BuildingBlocks/src/*`**.
- It is **not required** for `BuildingBlocks/tests/*`, nor for any application/service code outside
  `BuildingBlocks` (`src/**`, `tests/**`).
- This is **enforced automatically** by the `.editorconfig` files under the `BuildingBlocks` source
  folders, which promote the missing-XML-comment diagnostic to a warning
  (`dotnet_diagnostic.CS1591.severity = warning`). The `CS1591` setting must **not** be copied into
  test or service projects.

Outside the required scope, prefer self-explanatory code; use ordinary comments only where they add
real value.

### The `<remarks>` tag — why / how / when

`<summary>` states *what* a member is, in a single sentence. `<remarks>` captures the insight the
summary cannot, covering any **useful subset** of:

- **A) Why it exists** — the rationale or the problem it solves.
- **B) How to use it** — usage guidance, call order, patterns, gotchas.
- **C) When / in which situation to use it** — the context or scenario it is meant for.

Not all three are required — include **only** the ones that bring real insight and skip filler. A
`<remarks>` that merely restates the `<summary>` is not acceptable. Use **at most one** `<remarks>`
per member.

Where `<remarks>` is required vs. optional:

| Member kind | `<remarks>` |
| --- | --- |
| Types (classes, interfaces, records, structs, enums) | **Required** — there is always a "why". |
| Methods and constructors | **Required, unless** none of A/B/C add anything beyond the summary (rare — omit only then). |
| Trivial properties (`Id`, `Message`, `Value`, `IsEmpty`, …) | **Optional** — add one only when it genuinely adds insight. |
| Equality / boilerplate members (`Equals`, `GetHashCode`, `==`, `!=`) | **Optional** — add one only when it genuinely adds insight. |
| Explicitly implemented members using `<inheritdoc/>` | **Exempt** — docs are inherited. |

### Formatting rules

- `<summary>` is a single sentence; additional context goes in `<remarks>`.
- Booleans in `<returns>` and boolean docs: `<c>true</c> if …; otherwise, <c>false</c>.`
- Null is always `<see langword="null"/>` — never the prose word "null" or "cannot be null".
- Cross-references use `<see cref="..."/>` for types/members and `<typeparamref name="..."/>` /
  `<paramref name="..."/>` for type parameters and parameters.
- Every thrown exception is documented with `<exception cref="...">Thrown when …</exception>`.
- Constructors: `Initializes a new instance of the <see cref="T"/> class …`.
- Use `<inheritdoc/>` when overriding or implementing an already-documented member.

### Canonical phrasings

The same concept is always described with the same wording:

| Concept | Canonical text |
| --- | --- |
| `TKey` type parameter | `The type of the identity key.` |
| `Id` property summary | `Gets the unique identifier of the {entity\|aggregate root}.` |
| `Equals(T)` summary | `Determines whether the specified {noun} is equal to the current {noun}.` |
| `Equals(object)` summary | `Determines whether the specified object is equal to the current {noun}.` |
| `GetHashCode` summary | `Returns a hash code for the current {noun}.` |
| `==` / `!=` summary | `Determines whether two {noun}s are {equal\|not equal}.` |
| `DomainEvents` summary | `Gets the read-only collection of domain events raised by the {noun}.` |
| Equality `<remarks>` | `Two {noun}s are considered equal when they are the same concrete type and share the same <see cref="Id"/>.` |

`{noun}` is `entity` or `aggregate root`, matching the declaring type.

## Consequences

- The `BuildingBlocks` public API reads consistently; reviewers can focus on substance rather than
  re-litigating wording.
- Consumers get actionable guidance (why/how/when), not just restated signatures.
- Documentation effort is deliberately bounded: none is imposed on tests or service code, and no
  filler `<remarks>` is forced onto trivial members.
- The convention is machine-enforceable at the "present or absent" level via `CS1591`; the *quality*
  of `<remarks>` (must add insight, must not restate the summary) remains a review responsibility.
- Copilot is steered toward the convention via `.github/copilot-instructions.md`, reducing rework.

## Alternatives considered

- **Require XML docs everywhere (including tests and services):** rejected — it produces low-value,
  ceremonial documentation and slows delivery in code that changes frequently.
- **Require `<remarks>` on every public/protected member unconditionally:** rejected — mandatory
  remarks on trivial properties and equality boilerplate degrade into filler that restates the
  summary, working against documentation quality.
- **Keep conventions only in a standalone `docs/*.md` style guide:** rejected as the authoritative
  home — a deliberate, durable convention fits the repository's immutable ADR model. The ADR is the
  source of truth; `copilot-instructions.md` links to it.
- **Enforce `<remarks>` content via analyzers:** not adopted — no reliable analyzer distinguishes an
  insightful remark from a restated summary; this stays a review concern.
