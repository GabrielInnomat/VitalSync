# Documentation

How VitalSync works: its architecture, the patterns it applies, the technologies it is built on, and
the business domains it covers. Implementation detail deliberately stays out — that belongs in the
code, or in the [Thessera](./technologies.md) repository for anything the platform packages provide.

## The system

- **[Architecture](./architecture.md)** — how the system is put together: the tiers, the
  communication rules between them, the anatomy of a service, the database topology and the runtime
  composition.
- **[Patterns](./patterns.md)** — how a service is built inside: Domain-Driven Design, CQRS, the
  persistence strategies, the read side and integration events.
- **[Technologies](./technologies.md)** — what the system is built on and why each product was
  chosen.
- **[Testing](./testing.md)** — what is tested at which level, and the principles behind it.
- **[Design System](./design-system.md)** — the token architecture, the accessibility rules the
  colors are held to, and the conventions that no token can enforce on its own.

## The business

- **[Nutrition](./domains/nutrition.md)** — ingredients, recipes, meal plans, shopping lists and
  nutrient intake.
- **[Fitness](./domains/fitness.md)** — exercises, workout plans, workout sessions and energy
  expenditure.
- **[Health Analytics](./domains/health-analytics.md)** — insights derived from the other two
  domains.

Each domain document defines its own vocabulary, because a ubiquitous language belongs to its
bounded context.

## Reference

- **[Glossary](./glossary.md)** — the cross-cutting terms the documents above use.
- **[User stories](./userStories/)** — the requirements the domains are built from.
