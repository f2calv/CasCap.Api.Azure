using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Options;

namespace CasCap.Tests.Unit;

/// <summary>Tests the Agent Runtime authentication registration for HTTP clients.</summary>
public sealed class AgentRuntimeAzureAuthHttpClientBuilderExtensionsTests
{
    private const string ClientName = "runtime";

    [Theory]
    [InlineData(false, 0)]
    [InlineData(true, 1)]
    public void AddAgentRuntimeAzureAuthentication_AddsHandlerOnlyWhenEnabled(bool enabled, int expectedHandlers)
    {
        var services = new ServiceCollection();
        var builder = services.AddHttpClient(ClientName);
        var baseline = CountHandlerActions(services);

        builder.AddAgentRuntimeAzureAuthentication(CreateConfiguration(enabled));

        Assert.Equal(baseline + expectedHandlers, CountHandlerActions(services));
    }

    [Fact]
    public void AddAgentRuntimeAzureAuthentication_EnabledWithoutIdentity_FailsValidation()
    {
        var configuration = CreateConfiguration(enabled: true);
        var services = new ServiceCollection().AddSingleton(configuration);
        services.AddHttpClient(ClientName).AddAgentRuntimeAzureAuthentication(configuration);
        using var provider = services.BuildServiceProvider();

        var exception = Assert.Throws<OptionsValidationException>(
            () => provider.GetRequiredService<IOptions<AgentRuntimeAzureAuthConfig>>().Value);

        Assert.Contains(exception.Failures, failure => failure.Contains(nameof(AgentRuntimeAzureAuthConfig.Scope), StringComparison.Ordinal));
    }

    private static IConfiguration CreateConfiguration(bool enabled) => new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?>
        {
            [$"{AgentRuntimeAzureAuthConfig.ConfigurationSectionName}:{nameof(AgentRuntimeAzureAuthConfig.Enabled)}"] = enabled.ToString(),
        })
        .Build();

    private static int CountHandlerActions(IServiceCollection services)
    {
        using var provider = services.BuildServiceProvider();
        return provider.GetRequiredService<IOptionsMonitor<HttpClientFactoryOptions>>()
            .Get(ClientName).HttpMessageHandlerBuilderActions.Count;
    }
}
