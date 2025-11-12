namespace CasCap.Tests;

public abstract class TestBase
{
    protected string _connectionString;

    protected IAzBlobService _blobSvc;
    protected IAzQueueService _queueSvc;

    protected TestBase(/*ITestOutputHelper output*/)
    {
        var configuration = new ConfigurationBuilder()
            //.AddCasCapConfiguration()
            .AddJsonFile($"appsettings.Test.json", optional: false, reloadOnChange: false)
            .Build();

        //initiate ServiceCollection w/logging
        var services = new ServiceCollection()
            .AddSingleton<IConfiguration>(configuration)
            .AddLogging()
            //.AddXUnitLogging(output)
            ;

        var connectionString = configuration["ConnectionStrings:storage"];
        _connectionString = connectionString ?? throw new NullReferenceException(nameof(_connectionString));

        //add services
        services.AddTransient<IAzBlobService>(s => new AzBlobService(_connectionString));
        services.AddTransient<IAzQueueService>(s => new AzQueueService(_connectionString));

        //assign services to be tested
        var serviceProvider = services.BuildServiceProvider().AddStaticLogging();
        _blobSvc = serviceProvider.GetRequiredService<IAzBlobService>();
        _queueSvc = serviceProvider.GetRequiredService<IAzQueueService>();
    }
}
