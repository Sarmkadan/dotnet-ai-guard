using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AiGuard;

/// <summary>
/// ASP.NET Core middleware that inspects incoming request bodies for prompt injection
/// and redacts PII before the request reaches downstream handlers.
/// </summary>
public sealed partial class AiGuardMiddleware
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

    [LoggerMessage(Level = LogLevel.Warning, Message = "Request body exceeds MaxBodyBytes ({MaxBodyBytes}), passing through unmodified")]
    private partial void LogBodyTooLarge(long maxBodyBytes);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Prompt injection detected (score={Score}): {Signals}")]
    private partial void LogInjectionDetected(double score, string signals);

    [LoggerMessage(Level = LogLevel.Information, Message = "Redacted {Count} PII entities from request body")]
    private partial void LogPiiRedacted(int count);

    [LoggerMessage(Level = LogLevel.Information, Message = "Redacted {Count} PII entities from response body")]
    private partial void LogResponsePiiRedacted(int count);

    /// <summary>
    /// Creates a new instance of the middleware.
    /// </summary>
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
    public async Task InvokeAsync(HttpContext context)
    {
        var opts = _options.CurrentValue;

        // Only process requests whose method has a body (POST, PUT, PATCH)
        if (!HttpMethods.IsPost(context.Request.Method) &&
            !HttpMethods.IsPut(context.Request.Method) &&
            !HttpMethods.IsPatch(context.Request.Method))
        {
            await _next(context);
            return;
        }

        // PathPrefixes filter: if configured, only apply to matching paths
        if (opts.PathPrefixes.Count > 0)
        {
            var path = context.Request.Path.Value ?? string.Empty;
            var matches = false;
            foreach (var prefix in opts.PathPrefixes)
            {
                if (path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    matches = true;
                    break;
                }
            }

            if (!matches)
            {
                await _next(context);
                return;
            }
        }

        // Buffer the request body
        context.Request.EnableBuffering();
        using var reader = new StreamReader(context.Request.Body, Encoding.UTF8, leaveOpen: true);
        var body = await reader.ReadToEndAsync();

        if (string.IsNullOrEmpty(body))
        {
            context.Request.Body.Position = 0;
            await _next(context);
            return;
        }

        // Check body size limit
        if (Encoding.UTF8.GetByteCount(body) > opts.MaxBodyBytes)
        {
            LogBodyTooLarge(opts.MaxBodyBytes);
            context.Request.Body.Position = 0;
            await _next(context);
            return;
        }

        // Prompt injection detection
        var verdict = await _detector.DetectAsync(body, context.RequestAborted);

        if (verdict.IsInjection)
        {
            LogInjectionDetected(verdict.Score, string.Join(", ", verdict.Signals.Select(s => s.RuleId)));

            switch (opts.OnInjection)
            {
                case GuardAction.Block:
                    context.Response.StatusCode = opts.BlockedStatusCode;
                    context.Response.ContentType = "application/json";
                    await context.Response.WriteAsync("{\"error\":\"prompt_injection_detected\"}");
                    return;

                case GuardAction.Flag:
                    context.Items[VerdictItemKey] = verdict;
                    break;

                case GuardAction.Sanitize:
                    // Strip matched spans from body (from end to start to preserve indices)
                    var sb = new StringBuilder(body);
                    foreach (var signal in verdict.Signals.OrderByDescending(s => s.Span?.Start ?? 0))
                    {
                        if (signal.Span is { } span)
                        {
                            sb.Remove(span.Start, span.Length);
                        }
                    }
                    body = sb.ToString();
                    context.Items[VerdictItemKey] = verdict;
                    break;
            }
        }

        // PII redaction
        if (opts.RedactCategories != PiiCategory.None)
        {
            var redaction = await _redactor.RedactAsync(body, context.RequestAborted);
            if (redaction.HasRedactions)
            {
                body = redaction.RedactedText;
                context.Items[RedactionItemKey] = redaction;
                LogPiiRedacted(redaction.Entities.Count);
            }
        }

        // Replace the request body with the processed version
        var newBody = Encoding.UTF8.GetBytes(body);
        context.Request.Body = new MemoryStream(newBody);
        context.Request.ContentLength = newBody.Length;

        if (opts.RedactResponses && opts.RedactCategories != PiiCategory.None)
        {
            await InvokeWithResponseRedactionAsync(context, opts);
        }
        else
        {
            await _next(context);
        }
    }

    /// <summary>
    /// Invokes the next middleware while intercepting the response body to redact PII.
    /// When the response Content-Type starts with <c>application/json</c> or <c>text/</c>,
    /// the body is read as UTF-8, redacted via <see cref="IPiiRedactor"/>, and rewritten
    /// with an updated Content-Length. Other content types are copied through unchanged.
    /// </summary>
    private async Task InvokeWithResponseRedactionAsync(HttpContext context, AiGuardOptions options)
    {
        var originalBody = context.Response.Body;
        using var buffer = new MemoryStream();
        context.Response.Body = buffer;

        try
        {
            await _next(context);

            buffer.Position = 0;

            var contentType = context.Response.ContentType;
            var shouldRedact = contentType is not null &&
                (contentType.StartsWith("application/json", StringComparison.OrdinalIgnoreCase) ||
                 contentType.StartsWith("text/", StringComparison.OrdinalIgnoreCase));

            if (shouldRedact)
            {
                var responseText = await new StreamReader(buffer, Encoding.UTF8).ReadToEndAsync();
                var redaction = await _redactor.RedactAsync(responseText, context.RequestAborted);

                if (redaction.HasRedactions)
                {
                    LogResponsePiiRedacted(redaction.Entities.Count);
                    var redactedBytes = Encoding.UTF8.GetBytes(redaction.RedactedText);
                    context.Response.ContentLength = redactedBytes.Length;
                    await originalBody.WriteAsync(redactedBytes);
                }
                else
                {
                    context.Response.ContentLength = buffer.Length;
                    buffer.Position = 0;
                    await buffer.CopyToAsync(originalBody);
                }
            }
            else
            {
                buffer.Position = 0;
                await buffer.CopyToAsync(originalBody);
            }
        }
        finally
        {
            context.Response.Body = originalBody;
        }
    }
}
