namespace Microsoft.Extensions.DependencyInjection;

/// <summary>Extension methods for registering Log Analytics services with <see cref="Microsoft.Extensions.DependencyInjection.IServiceCollection" />.</summary>
public static class ServiceCollectionExtensions
{
    //public static void AddCasCapLogAnalyticsServices(this IServiceCollection services)
    //    => services.AddCasCapLogAnalyticsServices(_ => { });

    /// <summary>
    /// Registers the Log Analytics configuration options and the <see cref="CasCap.Abstractions.ILogAnalyticsQueryService"/>
    /// implementation with the dependency injection container.
    /// Options are bound from the <c>CasCap:LogAnalyticsOptions</c> configuration section.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the services to.</param>
    public static void AddCasCapLogAnalyticsServices(this IServiceCollection services/*,
            Action<LogAnalyticsOptions> LogAnalytics*/)
    {
        services.AddSingleton<IConfigureOptions<LogAnalyticsConfig>>(s =>
        {
            var configuration = s.GetRequiredService<IConfiguration>();
            return new ConfigureOptions<LogAnalyticsConfig>(options => configuration?.Bind(LogAnalyticsConfig.ConfigurationSectionName, options));
        });
        services.AddSingleton<IQueryService, QueryService>();
        //services.AddSingleton<ILogAnalyticsService, LogAnalyticsService>()
        //    .Configure<LogAnalyticsOptions>(configuration.GetSection(nameof(LogAnalyticsOptions)));
    }
}
