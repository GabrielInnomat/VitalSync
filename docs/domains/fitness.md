# Fitness

The business domain concerned with physical activity.

**Status:** placeholder. The bounded context is named but not yet detailed, and the service exists as
a skeleton without domain code. There are no user stories for it yet. Everything below is the
intended scope, to be refined.

## Purpose

A user manages the training side of their life here: which exercises exist, how they are combined
into plans, what was actually trained, and what that cost in energy.

## Use cases

- Manage exercises.
- Create workout plans.
- Track completed workout sessions.
- Determine energy expenditure and calories burned.

## Core concepts

### Exercise

A single trainable movement. Like the ingredient catalog in Nutrition, this is reference data.

### Workout plan

Composes exercises into a plan, with the sets, repetitions or durations intended for each.

### Workout session

One completed training session: started, exercises logged as they happen, completed. This is the
concept that makes the context interesting, because a session is a sequence of events by nature
rather than a row that gets updated.

## Persistence strategy

**Not yet decided.** The workout session is the strongest **Event Sourcing candidate** in the whole
system: it forms a natural event stream, and its full history has analytical value rather than
merely audit value.

Note that the choice is made for the whole context, not for the session alone — see the decision
rule in [Patterns](../patterns.md). If exercises and plans turn out to be poorly served by an event
store, that is an argument for cutting the context differently, not for mixing two strategies.

## Published integration events

None yet. A completed workout session is the obvious candidate for
[Health Analytics](./health-analytics.md) to consume.

## Open questions

- Does the context justify Event Sourcing as a whole, or only the session part?
- Is energy expenditure calculated here or derived in Health Analytics?
- How much of the exercise catalog is shared reference data versus user-owned?
