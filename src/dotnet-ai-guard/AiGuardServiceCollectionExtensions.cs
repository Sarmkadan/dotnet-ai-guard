using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

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
        throw new NotImplementedException();
    }

    /// <summary>
    /// Registers AI Guard and binds <see cref="AiGuardOptions"/> from the given configuration section.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">Configuration section (typically <c>AiGuard</c>).</param>
    /// <returns>The same <paramref name="services"/> for chaining.</returns>
    public static IServiceCollection AddAiGuard(this IServiceCollection services, IConfiguration configuration)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Adds <see cref="AiGuardMiddleware"/> to the request pipeline. Place it after authentication
    /// and before endpoint routing so blocked requests never reach handlers.
    /// </summary>
    /// <param name="app">The application builder.</param>
    /// <returns>The same <paramref name="app"/> for chaining.</returns>
    public static IApplicationBuilder UseAiGuard(this IApplicationBuilder app)
    {
        throw new NotImplementedException();
    }
}
