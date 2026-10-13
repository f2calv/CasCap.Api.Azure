using CasCap;
using CasCap.Common.Extensions;
using CasCap.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>Adds Agent Runtime bearer authentication to an HTTP client.</summary>
public static class AgentRuntimeAzureAuthHttpClientBuilderExtensions
{
    /// <summary>
    /// Binds <see cref="AgentRuntimeAzureAuthConfig"/> and, when it is enabled, adds a
    /// <see cref="TokenCredentialBearerHandler"/> for its scope to the client.
    /// </summary>
    /// <param name="builder">The Agent Runtime HTTP client builder.</param>
    /// <param name="configuration">The application configuration, read once to decide whether to add the handler.</param>
    /// <returns>The same <see cref="IHttpClientBuilder"/> for chaining.</returns>
    public static IHttpClientBuilder AddAgentRuntimeAzureAuthentication(this IHttpClientBuilder builder, IConfiguration configuration)
    {
        builder.Services.AddCasCapConfiguration<AgentRuntimeAzureAuthConfig>();
        var config = configuration.GetSection(AgentRuntimeAzureAuthConfig.ConfigurationSectionName).Get<AgentRuntimeAzureAuthConfig>();
        return config is not { Enabled: true }
            ? builder
            : builder.AddHttpMessageHandler(serviceProvider =>
            {
                var auth = serviceProvider.GetRequiredService<IOptions<AgentRuntimeAzureAuthConfig>>().Value;
                return new TokenCredentialBearerHandler(auth.GetTokenCredential(), auth.Scope!);
            });
    }
}
