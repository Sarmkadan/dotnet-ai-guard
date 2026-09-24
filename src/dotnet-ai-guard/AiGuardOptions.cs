namespace AiGuard;

/// <summary>
/// Configuration for the AI Guard middleware and its detectors.
/// </summary>
public sealed class AiGuardOptions
{
    /// <summary>Configuration section name used when binding from <c>IConfiguration</c>.</summary>
    public const string SectionName = "AiGuard";

    /// <summary>
    /// Score in the range [0, 1] at or above which a request is treated as a prompt injection.
    /// Default is <c>0.7</c>.
    /// </summary>
    public double InjectionThreshold { get; set; } = 0.7d;

    /// <summary>
    /// What the middleware does when an injection is detected. Default is <see cref="GuardAction.Block"/>.
    /// </summary>
    public GuardAction OnInjection { get; set; } = GuardAction.Block;

    /// <summary>
    /// PII categories to redact from request bodies. Default is <see cref="PiiCategory.All"/>.
    /// Set to <see cref="PiiCategory.None"/> to disable redaction.
    /// </summary>
    public PiiCategory RedactCategories { get; set; } = PiiCategory.All;

    /// <summary>
    /// When <see langword="true"/>, PII in response bodies is redacted as well as request bodies.
    /// Default is <see langword="false"/>.
    /// </summary>
    public bool RedactResponses { get; set; }

    /// <summary>
    /// Request path prefixes the middleware applies to. Empty means every request.
    /// </summary>
    public IList<string> PathPrefixes { get; } = new List<string>();

    /// <summary>
    /// JSON property names (dot-separated paths) whose values are inspected. Empty means the whole body is scanned as text.
    /// </summary>
    public IList<string> InspectedFields { get; } = new List<string>();

    /// <summary>
    /// Maximum request body size in bytes that will be buffered for inspection. Larger bodies are passed through
    /// unmodified and a warning is logged. Default is 1 MiB.
    /// </summary>
    public long MaxBodyBytes { get; set; } = 1024 * 1024;

    /// <summary>HTTP status code returned when a request is blocked. Default is <c>400</c>.</summary>
    public int BlockedStatusCode { get; set; } = 400;
}

/// <summary>
/// Action taken by the middleware when a prompt injection is detected.
/// </summary>
public enum GuardAction
{
    /// <summary>Short-circuit the pipeline and return <see cref="AiGuardOptions.BlockedStatusCode"/>.</summary>
    Block,
    /// <summary>Let the request continue but record the verdict in <c>HttpContext.Items</c> and logs.</summary>
    Flag,
    /// <summary>Strip the offending spans from the input and continue.</summary>
    Sanitize,
}
