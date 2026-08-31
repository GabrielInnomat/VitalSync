# Health Analytics

The business domain that derives insights from the data the other domains produce.

**Status:** placeholder, and the least defined of the three. The bounded context is named but its
requirements are deliberately left open; the service exists as a skeleton without domain code.
Everything below is the intended scope, to be refined.

## Purpose

Nutrition knows what was eaten and Fitness knows what was trained. Neither can answer a question
that spans both. This context exists to combine them and turn the combination into something a user
can act on.

## Use cases

- Reporting across nutrition and fitness data.
- Analyses that relate intake to expenditure over time.
- Concrete analytical requirements, identified and extended as the project evolves.

## Core concepts

Not yet modeled. What this context owns depends entirely on which questions it is meant to answer,
and those are still open.

## Data sourcing

This is the defining constraint of the context and it is not negotiable: Health Analytics **never
reads another context's database and never calls another service synchronously**. Everything it
knows arrives as integration events from Nutrition and Fitness, and it maintains its own model from
them. See [Patterns](../patterns.md).

That makes it a pure downstream consumer today — it publishes nothing that the other two contexts
need — and the one context whose consumer idempotency rules genuinely matter in practice.

## Persistence strategy

**Not yet decided**, and dependent on what the context ends up owning. If it mostly maintains
derived views over foreign events, state storage is the natural fit.

## Open questions

- Which concrete analyses does the product actually promise?
- Which events does it need from Nutrition and Fitness to answer them?
- Does it own an aggregate of its own, or only read models fed by foreign events?
- Is it one context, or does reporting eventually separate from analysis?
