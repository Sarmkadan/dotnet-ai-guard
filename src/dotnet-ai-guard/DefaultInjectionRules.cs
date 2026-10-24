namespace AiGuard;

/// <summary>
/// Built-in regex rules for <see cref="IPromptInjectionDetector"/> implementations.
/// Rule ids are stable (<c>INJ-###</c>) and must never be renumbered.
/// All patterns are case-insensitive and tolerate arbitrary runs of whitespace.
/// </summary>
public static class DefaultInjectionRules
{
    /// <summary>
    /// All built-in injection rules, in ascending id order.
    /// </summary>
    public static IReadOnlyList<InjectionRule> All { get; } =
    [
        // Instruction override: "ignore all previous instructions", "ignore the above instructions".
        new("INJ-001", "Instruction override attempt", 0.9,
            @"\bignore\s+(all\s+)?(of\s+)?(the\s+)?(previous|prior|above|earlier)\s+instructions?\b"),

        // Role hijacking: "you are now ...", "act as ...", "pretend to be ...".
        new("INJ-002", "Role hijacking", 0.6,
            @"\b(you\s+are\s+now|act\s+as|pretend\s+(to\s+be|you\s+are))\b"),

        // Exfiltration: "reveal your system prompt", "print the system message".
        new("INJ-003", "System prompt exfiltration", 0.85,
            @"\b(reveal|print|show|display|output)\s+(me\s+)?(your|the)\s+(system\s+prompt|system\s+message|initial\s+instructions)\b"),

        // Disregarding policy: "disregard your rules", "ignore your guidelines".
        new("INJ-004", "Disregard of rules or guidelines", 0.8,
            @"\b(disregard|ignore)\s+(all\s+)?(of\s+)?your\s+(rules|guidelines)\b"),

        // Jailbreak personas. A bare "DAN" is deliberately not matched: it is also a common first name.
        new("INJ-005", "DAN/jailbreak/developer mode pattern", 0.7,
            @"\b(jailbreak(ed|ing)?|developer\s+mode|dan\s+mode|dan\s+prompt)\b"),

        // Fake chat-template delimiters: "### system", <|im_start|>, [INST], <<SYS>>.
        new("INJ-006", "Fake prompt delimiter", 0.75,
            @"(#{3,6}\s*system\b|<\|\s*im_(start|end)\s*\|>|\[\s*/?\s*INST\s*\]|<<\s*/?\s*SYS\s*>>)"),

        // Base64-looking blob. A single blob is weak evidence, so the weight is low.
        new("INJ-007", "Base64-encoded payload", 0.3,
            @"(?<![A-Za-z0-9+/=])[A-Za-z0-9+/]{61,}={0,2}(?![A-Za-z0-9+/=])"),

        // "Do anything now" persona phrase.
        new("INJ-008", "Do-anything-now phrase", 0.6,
            @"\bdo\s+anything\s+now\b"),

        // Fake context break: "the above was a test, new instructions:".
        new("INJ-009", "Fake test framing with new instructions", 0.7,
            @"\bthe\s+above\s+(was|is)\s+(just\s+)?a\s+test\b\W{0,5}new\s+instructions?\s*:"),

        // Verbatim dump of context: "repeat everything above verbatim", "translate all text above".
        new("INJ-010", "Verbatim repetition of preceding context", 0.5,
            @"\b(translate|repeat)\s+(everything|all(\s+of)?\s+(the\s+)?text)\s+(above|before)\b(\s+verbatim)?"),
    ];
}
