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
container-backed tests. **No Aspire workload** — the AppHost is a `Microsoft.NET.Sdk` project that
references `Aspire.Hosting.AppHost` and `Aspire.Hosting.Dotnet`, registers every project with
`AddDotnetProject` by its path, and uses the Aspire CLI bundle, which the build resolves on its own.

`Directory.Build.props` applies solution-wide and puts the build under the strictest analysis
available, with warnings treated as errors — compiler and analyzer warnings through
`TreatWarningsAsErrors`, and warnings from MSBuild and from SDK or package targets through
`MSBuildTreatWarningsAsErrors`. A build failing on a warning is expected behavior, not a
reason to work around the rule.

Package versions are managed centrally in `Directory.Packages.props`: a `.csproj` carries
`<PackageReference Include="..." />` with no `Version`.

**Only LTS releases of .NET.** VitalSync moves to a new .NET version only when it is a Long Term
Support release; Standard Term Support releases are skipped. A package whose newer version requires
an STS release of .NET, or a newer compiler than the pinned SDK ships, stays on the latest version
that supports the current LTS release. Dependabot (`.github/dependabot.yml`) enforces this and keeps
package families together; the rules are described in `docs/technologies.md`.

Every push to `main` is built and tested on GitHub's own machines by `.github/workflows/build.yml`;
one that changes more than Markdown is then **deployed** by `.github/workflows/deploy.yml` on the
hosting computer — see `docs/hosting.md`. Only the deployment job runs there, and its steps run in Windows
PowerShell 5.1.

## Repository map

```text
VitalSync/
├── src/
│   ├── Aspire/
│   ├── Bff/
│   ├── Frontend/                   VitalSync.Web | VitalSync.DesignSystem (RCL: tokens + components)
│   └── Services/                   Nutrition | Fitness | HealthAnalytics, each Api + MigrationService
├── docs/                           architecture.md, patterns.md, technologies.md, testing.md,
│                                   design-system/, hosting.md, hosting-roadmap.md,
│                                   glossary.md, domains/
├── tools/                          build-time checks, hosting scripts and the operations guide,
│                                   not shipped
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
- **No comments, anywhere, of any kind** — no line, trailing, block or XML documentation comments.
  This covers every file: C#, Razor (`@* *@` and HTML comments), CSS, JavaScript, JSON, project and
  props files, workflow YAML, PowerShell, `.editorconfig`, `.gitignore`, and code examples in
  `*.md`. There is no exception for CSS. Knowledge a comment would carry belongs in a clearer name or
  in `docs/`. Only generated code is exempt, such as EF Core migrations. The rule is enforced: the
  analyzer in `tools/VitalSync.Analyzers` fails the build with `VSY0001` on comments in C#, Razor,
  CSS and JavaScript, and a repository test in `tests/VitalSync.Analyzers.Tests` checks every other
  file. A file type without known comment syntax fails that test until it is added to the scanner.
- **No FluentAssertions** — xUnit built-in asserts only.
- **Every HTML and CSS change must be WCAG 2.1 AA conformant.** Colour contrast is enforced by
  `tools/VitalSync.DesignTokens.Contrast`, but AA covers more than contrast: keyboard reachability,
  visible focus, semantic landmarks and headings, `aria-*` used correctly, and never colour as the
  only carrier of meaning (state, current-page, error, etc. all need a second, non-colour cue).
- **No "ß" in German text.** The interface is German for Switzerland (`de-CH`), which writes "ss"
  instead — "schliessen", "ausserhalb".

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
- **Never commit or push without asking.** Leave the changes in the working tree until a human
  agrees.
- **Never assume anything.** If you need more information, always **ask a human**.
- **Always ask in the chat, as plain prose.** Never open a dialog, prompt, or multiple-choice picker;
  do not use a question tool. Write the question and its options as normal text in your answer, then
  stop and wait.
- **Never answer with a table.** Not in chat, not in plan or notes documents you write. Use a heading
  with a short list underneath instead, and say what each item _means_ rather than only what it
  measures. Existing tables in the repository docs stay as they are.
