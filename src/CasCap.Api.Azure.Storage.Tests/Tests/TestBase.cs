namespace CasCap.Tests;

/// <summary>Base class for storage integration tests.</summary>
public abstract class TestBase
{
    /// <summary>Gets the Azure Storage connection string used by the tests.</summary>
    protected string _connectionString;

    /// <summary>Gets the blob service under test.</summary>
    protected IAzBlobService _blobSvc;

    /// <summary>Gets the queue service under test.</summary>
    protected IAzQueueService _queueSvc;

    /// <summary>Initializes a new instance of <see cref="TestBase" />.</summary>
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
        services.AddTransient<IAzBlobService>(s => new AzBlobService(_connectionString, $"wibble{Environment.Version.Major}"));
        services.AddTransient<IAzQueueService>(s => new AzQueueService(_connectionString, $"wibble{Environment.Version.Major}"));

        //assign services to be tested
        using var serviceProvider = services.BuildServiceProvider();
        serviceProvider.AddStaticLogging();
        _blobSvc = serviceProvider.GetRequiredService<IAzBlobService>();
        _queueSvc = serviceProvider.GetRequiredService<IAzQueueService>();
    }
}
