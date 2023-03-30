namespace Microsoft.Extensions.DependencyInjection;

public static class DI
{
    //public static void AddCasCapLogAnalyticsServices(this IServiceCollection services)
    //    => services.AddCasCapLogAnalyticsServices(_ => { });

    public static void AddCasCapLogAnalyticsServices(this IServiceCollection services/*,
            Action<LogAnalyticsOptions> LogAnalytics*/)
    {
        services.AddSingleton<IConfigureOptions<LogAnalyticsOptions>>(s =>
        {
            var configuration = s.GetRequiredService<IConfiguration>();
            return new ConfigureOptions<LogAnalyticsOptions>(options => configuration?.Bind(LogAnalyticsOptions.SectionKey, options));
        });
        services.AddSingleton<ILogAnalyticsQueryService, LogAnalyticsQueryService>();
        //services.AddSingleton<ILogAnalyticsService, LogAnalyticsService>()
        //    .Configure<LogAnalyticsOptions>(configuration.GetSection(nameof(LogAnalyticsOptions)));
    }
}
