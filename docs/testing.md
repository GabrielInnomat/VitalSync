# Testing

Automated tests cover the services and their integrations. This document describes what is tested
at which level and the principles behind it; the tools themselves are described in
[Technologies](./technologies.md).

## Test categories

- **Unit tests** — a single class or method in isolation.
- **Domain tests** — domain rules, invariants and event-raising on aggregates, value objects and
  domain events. They need no framework mocks, because the domain has no infrastructure.
- **Application-layer tests** — command and query handlers, the dispatcher, pipeline behaviors and
  `Result` semantics.
- **Persistence tests** — mapping, persistence and the collection of domain events on save.
- **Integration tests** — several components together against real infrastructure in containers.
- **Component communication tests** — gRPC contracts and the publishing and consuming of messages.

Test projects mirror the source structure one to one.

## Principles

- **The domain is highly testable**, because it has no infrastructure dependencies. If a domain test
  needs a mock, the domain has taken on a dependency it should not have.
- **Assert behavior, not implementation.** "Creating a recipe raises a `RecipeCreated` event" is a
  test worth having; asserting the shape of a private field is not.
- **Read-only event access is enforced and tested.** Outside layers must not be able to mutate an
  aggregate's domain events.
- **Fast feedback first.** Unit, domain, application and persistence tests run quickly and run
  always; container-backed tests are heavier and run behind them.
- **A negative assertion needs an anchor, not a deadline.** "Nothing was delivered" cannot be proven
  in a distributed system by waiting a fixed interval: too short and the test passes for the wrong
  reason, too long and it is slow, and on a loaded build agent the same code flips between the two.
  Anchor the assertion to something observable instead — a terminal state such as a message that has
  reached the dead-letter queue, or a sentinel message published afterwards whose arrival proves
  that anything sent earlier would already have arrived.
- **A race is staged, not awaited.** A test that needs two writers to collide must not produce the
  collision with sleeps or parallel tasks; the interleaving then depends on the machine, and the
  test passes for the wrong reason as often as it fails for one. Stage the conflict at a defined
  point inside the unit of work instead, so it is a certainty rather than a probability.
- **Where the compiler cannot be exhaustive, a test walks the set.** Adding an enum value does not
  break a `switch` over it, because a discard arm is always required and silently swallows anything
  added later. The same holds for any "every X has a matching Y" convention that no type constraint
  can express. Such a set is walked at run time by a test that names the offending value when it
  fails.
- **A guard test reproduces the failure it guards against.** Asserting that a service resolves to
  the type it was registered as only proves that dependency injection works. A useful guard rebuilds
  the situation the code exists to prevent, and shows that it no longer occurs.

## Design token contrast

The design system commits to WCAG 2.1 AA without exceptions, and its colors are declared once as CSS
custom properties. Contrast ratios written down by hand go stale the moment a color changes, so they
are computed instead of documented.

A rule document names the pairs that have to hold — a text color against the surface it is read on, a
form field border against the field it encloses, a chart series against the background it is drawn
over — together with the success criterion each pair answers to. The check resolves both sides
through the whole `var()` chain, separately for the light and the dark theme, and measures them. A
pair that names a token which no longer exists, or which resolves to something that is not a color,
fails just as loudly as one that is too faint: a rule that silently stops applying is worse than no
rule.

Where the system does not yet meet a rule, the shortfall is recorded as a waiver carrying the ratio
measured at the time and the reason it is still open. A waived pair does not break the build, but it
cannot drift either: if the ratio gets worse the check fails, and if the pair starts passing the
waiver is reported as stale and has to be removed. Debt stays visible and stays bounded, and the
strict mode reports every waiver as a failure for an honest picture of the true state.

Contrast alone is not the whole obligation. A chart also has to stay readable for someone who cannot
separate the hues it uses, and that property is invisible to a contrast ratio: uniformly darkening a
colour-blind-safe palette until every series clears the threshold can push two series onto the same
perceived colour, which passes the contrast rule while making the chart useless for the reader it was
chosen to serve. A second kind of rule therefore names a set of colours that have to stay tellable
apart and the smallest perceptual distance the set must keep. Each colour is run through a simulation
of protanopia, deuteranopia and tritanopia, every pair is measured in a perceptually uniform space,
and the closest pair under the worst of those conditions is what the rule is judged on. It reports
which pair collapsed and under which condition, because that is what tells you which colour to move.

The same check runs two ways. `dotnet run --project tools/VitalSync.DesignTokens.Contrast` prints a
readable report for whoever is changing a color, and the test project asserts the same result, so the
ordinary test run is the gate and no separate pipeline step is needed.

## Continuous integration

`.github/workflows/build.yml` runs on every push to `main`, on pull requests and on demand. It
builds in Release — which, with warnings as errors, is also the analyzer and style gate — and then
runs the tests.

Container-backed tests skip when Docker is unavailable, which keeps the suite usable locally. In CI
that would be dangerous: an agent without Docker would report success while entire test classes
never ran. An environment variable therefore turns a failed container start into a failed run
instead of a skip, and it is set in every pipeline.
