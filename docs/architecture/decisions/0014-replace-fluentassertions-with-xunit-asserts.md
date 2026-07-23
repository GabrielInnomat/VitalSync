# 0014. Replace FluentAssertions with standard xUnit asserts

- **Status:** Accepted
- **Date:** 2026-07-23

## Context

The documented testing toolchain listed **FluentAssertions** as the assertion
library for both the Building Blocks and the microservices (see the README
technology stack, `docs/architecture/testing-strategy.md`, and the Copilot
instructions).

FluentAssertions is no longer free to use: starting with version 8, the library
moved to a commercial license that requires a paid license for most usage. Since
the project does not want to take on a paid dependency for something as
fundamental and cross-cutting as test assertions, the toolchain must change.

Because the assertion library is a documented, cross-cutting standard for the
repository, changing it affects architecture and therefore warrants an ADR.

## Decision

Remove **FluentAssertions** from the VitalSync testing toolchain and use the
**assertions built into xUnit** (`Assert.*`) instead.

- No FluentAssertions package reference is added to any test project.
- Tests express expectations with standard xUnit assertions
  (e.g. `Assert.Equal`, `Assert.True`, `Assert.Throws`, `Assert.Raises`, etc.).
- The behavior-over-implementation testing principles remain unchanged; only the
  assertion syntax differs.

The remaining tooling (**xUnit**, **NSubstitute**, **EF Core InMemory**) is
unaffected.

## Consequences

- **Easier:** No paid or restrictively licensed dependency; one fewer package to
  track, update, and vet for licensing. Assertions stay aligned with the test
  framework already in use.
- **Harder:** xUnit's built-in assertions are less expressive/fluent than
  FluentAssertions, so some assertions become more verbose. Contributors must
  avoid reintroducing FluentAssertions.

## Alternatives considered

- **Keep FluentAssertions** — rejected: version 8+ is commercially licensed and
  not free for this project's usage.
- **Shouldly (MIT)** — a viable free, expressive alternative, but adds another
  third-party dependency; rejected in favor of keeping the toolchain minimal.
- **AwesomeAssertions (MIT fork of FluentAssertions)** — API-compatible drop-in,
  but still an extra dependency and tied to a fork's longevity; rejected in
  favor of the framework-native option.
