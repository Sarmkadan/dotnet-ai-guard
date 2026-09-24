# Contributing

Thanks for helping make AI-facing .NET apps safer.

## Workflow

1. Open an issue first for new detection rules or PII categories so the rule id can be reserved.
2. Fork, branch from `main` (`feat/<topic>`, `fix/<topic>`).
3. `dotnet build -c Release && dotnet test` must pass locally. Warnings are errors.
4. Run `dotnet format` before committing.
5. Open a PR with a short description of what the rule catches and what it deliberately does not.

## Rules and fixtures

- Every new injection rule gets a stable id (`INJ-###`) and at least one positive and one negative test case.
- Every PII recognizer must include false-positive fixtures (e.g. order numbers that look like cards).
- Regexes must be ReDoS-safe: bounded quantifiers or `RegexOptions.NonBacktracking`.

## Style

Follow `.editorconfig`. Public API needs XML docs. Use Conventional Commits for messages.

## Security issues

Do not open public issues for vulnerabilities in the detector bypass class. Email the maintainer listed on the
GitHub profile instead.
