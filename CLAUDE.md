# dotnet-ai-guard

Prompt injection detector and PII redactor as ASP.NET Core middleware. NuGet package id: `AiGuard`.

## Build and test

```bash
dotnet restore
dotnet build -c Release                       # TreatWarningsAsErrors is on; XML docs required on public API
dotnet test                                   # xunit, tests/dotnet-ai-guard.Tests
dotnet test --filter "FullyQualifiedName~Pii" # single area
dotnet pack src/dotnet-ai-guard -c Release -o artifacts
dotnet format --verify-no-changes             # style check, matches .editorconfig
```

Target framework: net9.0. SDK on this machine is 10.x and can target net9.0; do not bump the TFM.

## Architecture

Two independent services plus one middleware that composes them:

- `IPromptInjectionDetector` -> `InjectionVerdict` (score 0..1, list of `InjectionSignal` with rule id and `TextSpan`)
- `IPiiRedactor` -> `RedactionResult` (redacted text, list of `PiiEntity` with category, span, placeholder)
- `AiGuardMiddleware` buffers the request body (up to `MaxBodyBytes`), selects text via `InspectedFields`
  (JSON paths) or whole body, runs detector then redactor, applies `GuardAction` (Block / Flag / Sanitize),
  and rewrites `Request.Body` with the redacted payload. Verdict and redaction result go into `HttpContext.Items`.
- `AiGuardOptions` is bound via `IOptionsMonitor` so threshold and categories can change at runtime.
- `AiGuardServiceCollectionExtensions` exposes `AddAiGuard()` and `UseAiGuard()`.

Design constraints:
- No network calls, no ML model downloads in the default implementations. Regex + heuristics + checksums.
- Detectors must be safe against ReDoS: use `RegexOptions.NonBacktracking` or source-generated regexes with bounded quantifiers.
- Allocation-conscious: prefer `ReadOnlySpan<char>`, `ArrayPool`, `ValueTask`.
- Redaction placeholders are deterministic per request (`[EMAIL_1]`, `[EMAIL_2]`) so callers can re-hydrate LLM output.

## Key directories

```
src/dotnet-ai-guard/          library (namespace AiGuard)
  IPromptInjectionDetector.cs public contract + verdict records
  IPiiRedactor.cs             public contract + PiiCategory flags
  AiGuardOptions.cs           options + GuardAction enum
  AiGuardMiddleware.cs        pipeline component
  AiGuardServiceCollectionExtensions.cs  DI / UseAiGuard
tests/dotnet-ai-guard.Tests/  xunit tests
.github/workflows/ci.yml      build + test on push/PR
Directory.Build.props         shared MSBuild: Nullable, LangVersion latest, TreatWarningsAsErrors
```

Planned layout for implementations (create when implementing, keep public surface in the root namespace):
`src/dotnet-ai-guard/Detection/` (rules, scoring), `src/dotnet-ai-guard/Redaction/` (per-category recognizers),
`src/dotnet-ai-guard/Internal/` (body buffering, JSON path selection).

## Conventions

- Public types need XML docs; the build fails without them.
- One public type per file, file named after the type; small related records may share a file with their interface.
- Records for results, sealed classes for services, `readonly record struct` for small value types.
- Rule ids are stable strings: `INJ-###` for injection rules, `PII-<CATEGORY>` for recognizers. Never renumber.
- Tests: `MethodName_Scenario_Expected`, one assertion focus per test, use `[Theory]` with inline data for rule corpora.
- Every detection rule ships with at least one positive and one negative fixture.
- Commit messages: Conventional Commits (`feat:`, `fix:`, `test:`, `docs:`, `chore:`). No Co-Authored-By trailers.
- Do not add dependencies beyond the ASP.NET Core shared framework without a stated reason in the PR.
