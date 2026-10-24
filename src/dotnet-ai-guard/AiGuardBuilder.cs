using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace AiGuard;

/// <summary>
/// Fluent registration helper returned by <c>AddAiGuard</c>. Swaps the default detector or redactor
/// and adds custom injection rules or PII patterns on top of the built-in sets.
/// </summary>
public sealed class AiGuardBuilder
{
    /// <summary>
    /// Creates a builder over <paramref name="services"/>.
    /// </summary>
    /// <param name="services">The service collection that AI Guard registrations are written to.</param>
    internal AiGuardBuilder(IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        Services = services;
    }

    /// <summary>The service collection that AI Guard registrations are written to.</summary>
    public IServiceCollection Services { get; }

    /// <summary>
    /// Replaces the registered <see cref="IPromptInjectionDetector"/> with <typeparamref name="TDetector"/>.
    /// </summary>
    /// <typeparam name="TDetector">Detector implementation, resolved by the container.</typeparam>
    /// <returns>This builder for chaining.</returns>
    public AiGuardBuilder UseDetector<TDetector>() where TDetector : class, IPromptInjectionDetector
    {
        Services.Replace(ServiceDescriptor.Singleton<IPromptInjectionDetector, TDetector>());
        return this;
    }

    /// <summary>
    /// Replaces the registered <see cref="IPiiRedactor"/> with <typeparamref name="TRedactor"/>.
    /// </summary>
    /// <typeparam name="TRedactor">Redactor implementation, resolved by the container.</typeparam>
    /// <returns>This builder for chaining.</returns>
    public AiGuardBuilder UseRedactor<TRedactor>() where TRedactor : class, IPiiRedactor
    {
        Services.Replace(ServiceDescriptor.Singleton<IPiiRedactor, TRedactor>());
        return this;
    }

    /// <summary>
    /// Adds a custom injection rule. It runs alongside <see cref="DefaultInjectionRules.All"/>.
    /// </summary>
    /// <param name="rule">The rule to add.</param>
    /// <returns>This builder for chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="rule"/> is <see langword="null"/>.</exception>
    public AiGuardBuilder AddRule(InjectionRule rule)
    {
        ArgumentNullException.ThrowIfNull(rule);

        Services.AddSingleton<InjectionRule>(rule);
        return this;
    }

    /// <summary>
    /// Adds a custom PII pattern. It runs alongside <see cref="DefaultPiiPatterns.All"/>.
    /// </summary>
    /// <param name="pattern">The pattern to add.</param>
    /// <returns>This builder for chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="pattern"/> is <see langword="null"/>.</exception>
    public AiGuardBuilder AddPiiPattern(PiiPattern pattern)
    {
        ArgumentNullException.ThrowIfNull(pattern);

        Services.AddSingleton<PiiPattern>(pattern);
        return this;
    }
}
