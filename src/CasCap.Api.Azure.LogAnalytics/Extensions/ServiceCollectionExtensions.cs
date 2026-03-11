namespace Microsoft.Extensions.DependencyInjection;

/// <summary>Extension methods for registering Log Analytics services with <see cref="Microsoft.Extensions.DependencyInjection.IServiceCollection" />.</summary>
public static class ServiceCollectionExtensions
{
    /// <summary>Registers <see cref="CasCap.Models.LogAnalyticsOptions" /> configuration binding and <see cref="CasCap.Abstractions.IQueryService" />.</summary>
    public static void AddCasCapLogAnalyticsServices(this IServiceCollection services/*,
            Action<LogAnalyticsOptions> LogAnalytics*/)
    {
        services.AddSingleton<IConfigureOptions<LogAnalyticsOptions>>(s =>
        {
            var configuration = s.GetRequiredService<IConfiguration>();
            return new ConfigureOptions<LogAnalyticsOptions>(options => configuration?.Bind(LogAnalyticsOptions.ConfigurationSectionName, options));
        });
        services.AddSingleton<IQueryService, QueryService>();
        //services.AddSingleton<ILogAnalyticsService, LogAnalyticsService>()
        //    .Configure<LogAnalyticsOptions>(configuration.GetSection(nameof(LogAnalyticsOptions)));
    }
}
