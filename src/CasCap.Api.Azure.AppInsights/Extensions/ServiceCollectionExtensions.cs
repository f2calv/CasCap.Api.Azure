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
        services.AddSingleton<IConfigureOptions<AppInsightsOptions>>(s =>
        {
            var configuration = s.GetRequiredService<IConfiguration>();
            return new ConfigureOptions<AppInsightsOptions>(options => configuration?.Bind(AppInsightsOptions.ConfigurationSectionName, options));
        });
    }
}
