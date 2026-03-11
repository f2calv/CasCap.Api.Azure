namespace Microsoft.Extensions.DependencyInjection;

/// <summary>Extension methods for registering Application Insights services with <see cref="Microsoft.Extensions.DependencyInjection.IServiceCollection" />.</summary>
public static class ServiceCollectionExtensions
{
    /// <summary>Registers <see cref="CasCap.Models.AppInsightsOptions" /> configuration binding.</summary>
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
