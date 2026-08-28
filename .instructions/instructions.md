# VitalSync Development Instructions

## What VitalSync is

A cloud-native, distributed platform unifying **nutrition**, **fitness**, and **health analytics**
behind a single Blazor UI. Built as independent ASP.NET Core microservices using **DDD**, **CQRS**,
and **selective Event Sourcing**.

> Technical/architectural decisions are mandatory. Business/domain details are refined iteratively.
> When a change affects architecture, add or update an ADR.

## Build, test

```bash
dotnet build
dotnet test
```

Solution file: `VitalSync.slnx`. SDK pinned in `global.json` (`10.0.302`, `rollForward:
latestFeature`).

`Directory.Build.props` applies solution-wide: `net10.0`, nullable + implicit usings enabled,
`LangVersion latest`, `AnalysisLevel latest-all`, `AnalysisMode All`, `EnableNETAnalyzers`,
`TreatWarningsAsErrors` and `CodeAnalysisTreatWarningsAsErrors` both true, `WarningLevel 9999`.

Package versions are managed centrally in `Directory.Packages.props`
(`ManagePackageVersionsCentrally`, `CentralPackageTransitivePinningEnabled`): a `.csproj` carries
`<PackageReference Include="..." />` with no `Version`.

## Prerequisites

the .NET SDK pinned in `global.json` (`10.0.302`, `rollForward: latestFeature`) and
Docker. **No Aspire workload** — the AppHosts reference `Aspire.AppHost.Sdk` as a package.

## Repository map

```text
VitalSync/
├── src/
│   ├── Aspire/
│   ├── Bff/
│   ├── Frontend/VitalSync.Web/
│   └── Services/                   Nutrition | Fitness | Analytics, each Api + MigrationService
├── docs/architecture/
└── tests/
```

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
- **Every service host wires the same defaults**:
  `builder.AddServiceDefaults()`, one `AddNpgSqlReadinessCheck` **per database the context owns**
  (`<context>-write` _and_ `<context>-read`), `AddRabbitMqReadinessCheck()`, `AddProblemDetails()` +
  `app.UseExceptionHandler()` (ADR-0017's thin global handler), `app.MapDefaultEndpoints()`, and
  `await app.RunAsync().ConfigureAwait(false)`. The connection names **are** the Aspire resource
  names. `AddServiceDefaults()` already registers the OpenTelemetry sources `GaWeCodes.Thessera`,
  `Npgsql`, `Wolverine` and `Marten` — do not re-add them.
- **No comments** — not in `*.cs`, `*.csproj`, workflow YAML, or code examples in `*.md`.
- **No FluentAssertions** — xUnit built-in asserts only (ADR-0014).

## Business domains

- **Nutrition** — ingredients & nutritional values, recipes, meal plans, shopping lists,
  nutrient-intake calculation.
- **Fitness** — exercises, workout plans, workout-session tracking, energy/calorie expenditure.
- **Analytics** — insights derived from nutrition and fitness data.

Bounded-context decomposition is iterative — see `docs/architecture/domain-model.md`.

## Testing & CI

Full strategy: `docs/architecture/testing-strategy.md`. What it will not tell you from the code:

- Test projects mirror source structure 1:1. Domain tests use hand-written test doubles
  (`TestDoubles/`), not mocks — the domain has no infrastructure to mock. NSubstitute is for
  application/persistence/messaging tests.
- Assert observable behavior, not internals ("creating a recipe raises a `RecipeCreated` event").
- Add or extend tests alongside **any** behavioral change, and make them pass.

## When contributing

1. Respect the non-negotiable rules above.
2. Add or update tests, and make sure they pass.
3. If a change affects architecture, add or update an ADR (template in `decisions/README.md`).
4. Check the `*.md` files your change affects and update them — including this file, whenever you
   find a gap or an ambiguity in the guidance here.
5. Match existing style; respect `.editorconfig`, `Directory.Build.props`, `Directory.Packages.props`.
6. **Always work on `main`** — never a separate branch, and never ask which branch to use.
7. Never assume anything. If you need more information always **ask a human**!
8. Ask **always** in the chat, as plain prose.\*\* Never open a dialog, prompt, or
   multiple-choice picker; do not use a question tool. Write the question and its options as normal
   text in your answer and then stop and wait.
9. **Never answer with a table.** Not in chat, not in plan or notes documents you write. Use a
   heading with a short list underneath instead, and say what each item _means_ rather than only
   what it measures. Existing tables in the repository docs stay as they are. 10. Never commit yourself.
