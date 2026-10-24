using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace AiGuard;

/// <summary>
/// Dependency injection and pipeline registration helpers for AI Guard.
/// </summary>
public static class AiGuardServiceCollectionExtensions
{
    /// <summary>
    /// Registers the default <see cref="IPromptInjectionDetector"/>, <see cref="IPiiRedactor"/>
    /// and <see cref="AiGuardOptions"/> in the container.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Optional delegate to tune <see cref="AiGuardOptions"/>.</param>
    /// <returns>The same <paramref name="services"/> for chaining.</returns>
    public static IServiceCollection AddAiGuard(this IServiceCollection services, Action<AiGuardOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        var optionsBuilder = services.AddOptions<AiGuardOptions>();
        if (configure is not null)
        {
            optionsBuilder.Configure(configure);
        }

        services.TryAddSingleton<IPromptInjectionDetector, RuleBasedPromptInjectionDetector>();
        services.TryAddSingleton<IPiiRedactor, RegexPiiRedactor>();
        return services;
    }

    /// <summary>
    /// Registers AI Guard and binds <see cref="AiGuardOptions"/> from the given configuration section.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">Configuration section (typically <c>AiGuard</c>).</param>
    /// <returns>The same <paramref name="services"/> for chaining.</returns>
    public static IServiceCollection AddAiGuard(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddOptions<AiGuardOptions>().Bind(configuration);
        services.TryAddSingleton<IPromptInjectionDetector, RuleBasedPromptInjectionDetector>();
        services.TryAddSingleton<IPiiRedactor, RegexPiiRedactor>();
        return services;
    }

    /// <summary>
    /// Adds <see cref="AiGuardMiddleware"/> to the request pipeline. Place it after authentication
    /// and before endpoint routing so blocked requests never reach handlers.
    /// </summary>
    /// <param name="app">The application builder.</param>
    /// <returns>The same <paramref name="app"/> for chaining.</returns>
    public static IApplicationBuilder UseAiGuard(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        return app.UseMiddleware<AiGuardMiddleware>();
    }
}
