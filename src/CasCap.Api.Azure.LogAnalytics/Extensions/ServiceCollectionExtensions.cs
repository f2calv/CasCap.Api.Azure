namespace Microsoft.Extensions.DependencyInjection;

/// <summary>Extension methods for registering Log Analytics services with <see cref="Microsoft.Extensions.DependencyInjection.IServiceCollection" />.</summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the Log Analytics configuration options and the <see cref="IQueryService"/>
    /// implementation with the dependency injection container.
    /// Options are bound from the <c>CasCap:LogAnalyticsConfig</c> configuration section.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the services to.</param>
    public static void AddCasCapLogAnalyticsServices(this IServiceCollection services)
    {
        services.AddSingleton<IConfigureOptions<LogAnalyticsConfig>>(s =>
        {
            var configuration = s.GetRequiredService<IConfiguration>();
            return new ConfigureOptions<LogAnalyticsConfig>(options => configuration?.Bind(LogAnalyticsConfig.ConfigurationSectionName, options));
        });
        services.AddSingleton<IQueryService, QueryService>();
    }
}
