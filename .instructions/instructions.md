# VitalSync Development Instructions

## What VitalSync is

A cloud-native, distributed platform unifying **nutrition**, **fitness**, and **health analytics**
behind a single Blazor UI. Built as independent ASP.NET Core microservices using **DDD**, **CQRS**,
and **selective Event Sourcing**.

Technical and architectural decisions are mandatory. Business and domain details are refined
iteratively.

**Thessera is a separate product**, consumed as the `GaWeCodes.Thessera.*` packages and developed in
its own repository. Its internals are neither changed nor documented here.

## Build, test

```bash
dotnet build
dotnet test
```

Solution file: `VitalSync.slnx`. The SDK is pinned in `global.json`, and Docker is required for the
container-backed tests. **No Aspire workload** — the AppHost references `Aspire.AppHost.Sdk` as a
package.

`Directory.Build.props` applies solution-wide and puts the build under the strictest analysis
available, with warnings treated as errors. A build failing on a warning is expected behavior, not a
reason to work around the rule.

Package versions are managed centrally in `Directory.Packages.props`: a `.csproj` carries
`<PackageReference Include="..." />` with no `Version`.

## Repository map

```text
VitalSync/
├── src/
│   ├── Aspire/
│   ├── Bff/
│   ├── Frontend/                   VitalSync.Web | VitalSync.DesignSystem (RCL: tokens + components)
│   └── Services/                   Nutrition | Fitness | HealthAnalytics, each Api + MigrationService
├── docs/                           architecture.md, patterns.md, technologies.md, testing.md,
│                                   design-system.md, glossary.md, domains/
├── tools/                          build-time checks, not shipped
├── plan.md                         throwaway: what is left to build; deleted when it is
└── tests/
```

## Business domains

**Nutrition** covers ingredients, recipes, meal plans, shopping lists and nutrient intake.
**Fitness** covers exercises, workout plans, workout sessions and energy expenditure.
**Health Analytics** derives insights from both.

Bounded-context decomposition is iterative — see `docs/domains/`.

## Non-negotiable rules

- The Blazor frontend talks **exclusively** to the **BFF**. The BFF exposes **REST** to the frontend
  and **code-first gRPC** to the services.
- Microservices **never** call each other synchronously and
  **never** read another context's database.
- Layer separation is mandatory; dependencies point inward. A contract lives in the innermost layer
  that _consumes_ it.
- Each bounded context owns a **write + read database pair**, never shared, no cross-database FKs,
  joins or transactions.
- `AddThessera` is called **exactly once** per host.
- A new service host follows the form of an existing `Program.cs` exactly. Two things are not visible
  there: the connection names **are** the Aspire resource names, and `AddServiceDefaults()` already
  registers the OpenTelemetry sources `Thessera`, `Npgsql`, `Wolverine` and `Marten` — do not re-add
  them.
- **No comments** — not in `*.cs`, `*.csproj`, workflow YAML, or code examples in `*.md`.
- **CSS is the one exception, and a narrow one.** A comment is allowed only where the value would
  otherwise read as a mistake and be "fixed": a deliberate duplicate, a shade that must not be
  reused, a unit chosen to work around browser behavior. One line, stating the constraint. Never
  a value that a tool computes — a contrast ratio in a comment goes stale the moment a color
  changes, and the check in `tools/VitalSync.DesignTokens.Contrast` already owns those numbers.
  Rationale, derivations and conventions belong in `docs/`, not in the stylesheet.
- **No FluentAssertions** — xUnit built-in asserts only.

## Documentation

`docs/` describes **architecture, patterns and technologies** — how VitalSync works, not how it is
implemented. Implementation detail belongs in the code, and anything about the platform building
blocks belongs in the Thessera repository.

When a change affects architecture, update the affected document under `docs/`. Check the `*.md`
files your change affects — including this one, whenever you find a gap or an ambiguity in the
guidance here.

## Testing

The full strategy is in `docs/testing.md`. Beyond it: add or extend tests alongside **any**
behavioral change, and make them pass.

## When contributing

1. Respect the non-negotiable rules above.
2. Add or update tests, and make sure they pass.
3. Update the documentation your change affects.
4. Match existing style; respect `.editorconfig`, `Directory.Build.props`, `Directory.Packages.props`.

## How to behave

- **Always work on `main`** — never a separate branch, and never ask which branch to use.
- **Never commit.** Leave the changes in the working tree.
- **Never assume anything.** If you need more information, always **ask a human**.
- **Always ask in the chat, as plain prose.** Never open a dialog, prompt, or multiple-choice picker;
  do not use a question tool. Write the question and its options as normal text in your answer, then
  stop and wait.
- **Never answer with a table.** Not in chat, not in plan or notes documents you write. Use a heading
  with a short list underneath instead, and say what each item _means_ rather than only what it
  measures. Existing tables in the repository docs stay as they are.
