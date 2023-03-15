namespace CasCap.Tests;

public abstract class TestBase {

    protected string _connectionString;

    protected IAzBlobService _blobSvc;
    protected IAzQueueService _queueSvc;

    public TestBase(ITestOutputHelper output) {
        var configuration = new ConfigurationBuilder()
            //.AddCasCapConfiguration()
            .AddJsonFile($"appsettings.Test.json", optional: false, reloadOnChange: false)
            .Build();

        //initiate ServiceCollection w/logging
        var services = new ServiceCollection()
            .AddSingleton<IConfiguration>(configuration)
            .AddXUnitLogging(output);

        var loggerFactory = services.BuildServiceProvider().GetRequiredService<ILoggerFactory>();

        _connectionString = configuration["ConnectionStrings:storage"];
        //add services
        services.AddTransient<IAzBlobService>(s => new AzBlobService(loggerFactory.CreateLogger<AzBlobService>(), _connectionString));
        services.AddTransient<IAzQueueService>(s => new AzQueueService(loggerFactory.CreateLogger<AzQueueService>(), _connectionString));

        //assign services to be tested
        var serviceProvider = services.BuildServiceProvider();
        _blobSvc = serviceProvider.GetRequiredService<IAzBlobService>();
        _queueSvc = serviceProvider.GetRequiredService<IAzQueueService>();
    }
}