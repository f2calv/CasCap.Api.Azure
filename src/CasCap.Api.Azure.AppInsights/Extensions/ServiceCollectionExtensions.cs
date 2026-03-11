namespace Microsoft.Extensions.DependencyInjection;

/// <summary>Extension methods for registering Application Insights services with <see cref="Microsoft.Extensions.DependencyInjection.IServiceCollection" />.</summary>
public static class ServiceCollectionExtensions
{
    //public static void AddCasCapAppInsightsServices(this IServiceCollection services)
    //    => services.AddCasCapAppInsightsServices(_ => { });

    /// <summary>
    /// Registers the Application Insights configuration options with the dependency injection container.
    /// Options are bound from the <c>CasCap:AppInsightsOptions</c> configuration section.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the services to.</param>
    public static void AddCasCapAppInsightsServices(this IServiceCollection services/*,
            Action<AppInsightsOptions> appInsights*/)
    {
        services.AddSingleton<IConfigureOptions<AppInsightsConfig>>(s =>
        {
            var configuration = s.GetRequiredService<IConfiguration>();
            return new ConfigureOptions<AppInsightsConfig>(options => configuration?.Bind(AppInsightsConfig.ConfigurationSectionName, options));
        });
    }
}
