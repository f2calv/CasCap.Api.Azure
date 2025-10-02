namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
    //public static void AddCasCapAppInsightsServices(this IServiceCollection services)
    //    => services.AddCasCapAppInsightsServices(_ => { });

    public static void AddCasCapAppInsightsServices(this IServiceCollection services/*,
            Action<AppInsightsOptions> appInsights*/)
    {
        services.AddSingleton<IConfigureOptions<AppInsightsOptions>>(s =>
        {
            var configuration = s.GetRequiredService<IConfiguration>();
            return new ConfigureOptions<AppInsightsOptions>(options => configuration?.Bind(AppInsightsOptions.SectionKey, options));
        });
    }
}
