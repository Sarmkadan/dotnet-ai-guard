using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace AiGuard.Tests;

public class ExtensionsGuardTests
{
    [Fact]
    public void AddAiGuard_NullServices_ThrowsWithServicesParamName()
    {
        IServiceCollection? services = null;

        var ex = Assert.Throws<ArgumentNullException>(() => services!.AddAiGuard());

        Assert.Equal("services", ex.ParamName);
    }

    [Fact]
    public void AddAiGuard_NullServicesWithConfigure_ThrowsWithServicesParamName()
    {
        IServiceCollection? services = null;

        var ex = Assert.Throws<ArgumentNullException>(() => services!.AddAiGuard(o => o.InjectionThreshold = 0.5));

        Assert.Equal("services", ex.ParamName);
    }

    [Fact]
    public void AddAiGuard_NullServicesWithConfiguration_ThrowsWithServicesParamName()
    {
        IServiceCollection? services = null;
        IConfiguration configuration = new ConfigurationBuilder().Build();

        var ex = Assert.Throws<ArgumentNullException>(() => services!.AddAiGuard(configuration));

        Assert.Equal("services", ex.ParamName);
    }

    [Fact]
    public void AddAiGuard_NullConfiguration_ThrowsWithConfigurationParamName()
    {
        var services = new ServiceCollection();

        var ex = Assert.Throws<ArgumentNullException>(() => services.AddAiGuard((IConfiguration)null!));

        Assert.Equal("configuration", ex.ParamName);
    }

    [Fact]
    public void UseAiGuard_NullApp_ThrowsWithAppParamName()
    {
        IApplicationBuilder? app = null;

        var ex = Assert.Throws<ArgumentNullException>(() => app!.UseAiGuard());

        Assert.Equal("app", ex.ParamName);
    }

    [Fact]
    public void InvokeAsync_NullContext_ThrowsSynchronouslyWithContextParamName()
    {
        var middleware = CreateMiddleware();

        // Discard the result inside an Action so Assert.Throws checks the synchronous throw
        // (a faulted Task would not satisfy it).
        var ex = Assert.Throws<ArgumentNullException>(() => { _ = middleware.InvokeAsync(null!); });

        Assert.Equal("context", ex.ParamName);
    }

    private static AiGuardMiddleware CreateMiddleware()
    {
        var monitor = new TestOptionsMonitor<AiGuardOptions>(new AiGuardOptions());
        return new AiGuardMiddleware(
            _ => Task.CompletedTask,
            new RuleBasedPromptInjectionDetector(monitor),
            new RegexPiiRedactor(monitor),
            monitor,
            NullLogger<AiGuardMiddleware>.Instance);
    }
}
