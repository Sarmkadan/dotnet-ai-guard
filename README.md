# dotnet-ai-guard

[![CI](https://github.com/Sarmkadan/dotnet-ai-guard/actions/workflows/ci.yml/badge.svg)](https://github.com/Sarmkadan/dotnet-ai-guard/actions/workflows/ci.yml)
[![NuGet](https://img.shields.io/nuget/v/AiGuard.svg)](https://www.nuget.org/packages/AiGuard)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)

Prompt injection detector and PII redactor as ASP.NET Core middleware for AI-powered applications.

Drop it in front of any endpoint that forwards user input to an LLM. It scores the request body for
injection attempts (instruction overrides, role hijacking, system-prompt exfiltration, delimiter
smuggling) and masks personal data (emails, phones, cards, IBANs, IPs, national IDs) before the
text ever leaves your process.

## Features

- Rule-based prompt-injection scoring with per-rule signals and character spans
- Configurable action on detection: block, flag, or sanitize
- PII redaction with stable placeholders (`[EMAIL_1]`, `[PHONE_2]`) so you can re-hydrate responses
- Luhn validation for card numbers, checksum validation for IBANs
- Path and JSON-field scoping so only prompt fields are inspected
- Verdicts exposed via `HttpContext.Items` for downstream logging and telemetry
- No external calls, no model downloads; pure .NET, allocation-conscious
- Pluggable `IPromptInjectionDetector` / `IPiiRedactor` for custom or ML-backed implementations

## Quick start

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddAiGuard(options =>
    {
        options.InjectionThreshold = 0.7;
        options.OnInjection = GuardAction.Block;
        options.RedactCategories = PiiCategory.Email | PiiCategory.Phone | PiiCategory.CreditCard;
        options.PathPrefixes.Add("/api/chat");
        options.InspectedFields.Add("messages[*].content");
    })
    // Optional: extend the built-in rules and patterns without replacing them.
    .AddRule(new InjectionRule("INJ-900", "Internal jailbreak phrase", 0.8, @"\bopen\s+sesame\b"))
    .AddPiiPattern(new PiiPattern(PiiCategory.NationalId, @"\bemp-\d{6}\b"));

var app = builder.Build();

app.UseAuthentication();
app.UseAiGuard();
app.MapPost("/api/chat", async (ChatRequest req, HttpContext ctx, IChatClient llm) =>
{
    // req.Messages already redacted; verdict available if OnInjection == Flag
    var verdict = ctx.Items[AiGuardMiddleware.VerdictItemKey] as InjectionVerdict;
    return await llm.CompleteAsync(req.Messages);
});

app.Run();
```

Or bind from `appsettings.json`:

```json
{
  "AiGuard": {
    "InjectionThreshold": 0.7,
    "OnInjection": "Block",
    "RedactCategories": "Email, Phone, CreditCard",
    "PathPrefixes": ["/api/chat"]
  }
}
```

```csharp
builder.Services.AddAiGuard(builder.Configuration.GetSection(AiGuardOptions.SectionName));
```

## Installation

```bash
dotnet add package AiGuard
```

Requires .NET 9.0 or later.

## API overview

| Type | Purpose |
|------|---------|
| `IPromptInjectionDetector` | `DetectAsync(text)` -> `InjectionVerdict { IsInjection, Score, Signals }` |
| `IPiiRedactor` | `RedactAsync(text)` -> `RedactionResult { RedactedText, Entities }` |
| `AiGuardOptions` | Threshold, action, categories, path/field scoping, body size limit |
| `AiGuardMiddleware` | Buffers the body, runs both services, blocks/flags/rewrites |
| `AddAiGuard()` / `UseAiGuard()` | DI registration and pipeline wiring |
| `AiGuardBuilder` | Returned by `AddAiGuard()`: `UseDetector<T>()`, `UseRedactor<T>()`, `AddRule()`, `AddPiiPattern()` |

Use the services directly when you need them outside HTTP (background jobs, log sinks):

```csharp
var redactor = provider.GetRequiredService<IPiiRedactor>();
var result = await redactor.RedactAsync("Call me at +49 178 5729777 or john@example.com");
// result.RedactedText == "Call me at [PHONE_1] or [EMAIL_1]"
```

## Contributing

See [CONTRIBUTING.md](CONTRIBUTING.md).

## License

[MIT](LICENSE) - Copyright 2026 Vladyslav Zaiets
