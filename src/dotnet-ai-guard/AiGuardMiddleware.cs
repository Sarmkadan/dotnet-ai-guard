using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AiGuard;

/// <summary>
/// ASP.NET Core middleware that inspects incoming request bodies for prompt injection
/// and redacts PII before the request reaches downstream handlers.
/// </summary>
public sealed class AiGuardMiddleware
{
    /// <summary>Key under which the <see cref="InjectionVerdict"/> is stored in <see cref="HttpContext.Items"/>.</summary>
    public const string VerdictItemKey = "AiGuard.InjectionVerdict";

    /// <summary>Key under which the <see cref="RedactionResult"/> is stored in <see cref="HttpContext.Items"/>.</summary>
    public const string RedactionItemKey = "AiGuard.RedactionResult";

    private readonly RequestDelegate _next;
    private readonly IPromptInjectionDetector _detector;
    private readonly IPiiRedactor _redactor;
    private readonly IOptionsMonitor<AiGuardOptions> _options;
    private readonly ILogger<AiGuardMiddleware> _logger;

    /// <summary>
    /// Creates a new instance of the middleware.
    /// </summary>
    /// <param name="next">Next delegate in the pipeline.</param>
    /// <param name="detector">Prompt-injection detector.</param>
    /// <param name="redactor">PII redactor.</param>
    /// <param name="options">Live options.</param>
    /// <param name="logger">Logger.</param>
    public AiGuardMiddleware(
        RequestDelegate next,
        IPromptInjectionDetector detector,
        IPiiRedactor redactor,
        IOptionsMonitor<AiGuardOptions> options,
        ILogger<AiGuardMiddleware> logger)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _detector = detector ?? throw new ArgumentNullException(nameof(detector));
        _redactor = redactor ?? throw new ArgumentNullException(nameof(redactor));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Processes the request: buffers the body, runs detection and redaction, then either blocks,
    /// flags or rewrites the request according to <see cref="AiGuardOptions.OnInjection"/>.
    /// </summary>
    /// <param name="context">The current HTTP context.</param>
    public Task InvokeAsync(HttpContext context)
    {
        throw new NotImplementedException();
    }
}
