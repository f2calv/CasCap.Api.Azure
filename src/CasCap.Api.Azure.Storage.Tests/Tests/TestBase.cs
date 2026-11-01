namespace CasCap.Tests;

/// <summary>Base class for storage integration tests.</summary>
public abstract class TestBase : IAsyncDisposable
{
    private readonly ConfigurationManager _configuration = new();
    private readonly ServiceProvider _serviceProvider;
    /// <summary>Gets the Azure Storage connection string used by the tests.</summary>
    protected string _connectionString;

    /// <summary>Gets the blob service under test.</summary>
    protected IAzBlobService _blobSvc;

    /// <summary>Gets the queue service under test.</summary>
    protected IAzQueueService _queueSvc;

    /// <summary>Initializes a new instance of <see cref="TestBase" />.</summary>
    protected TestBase(/*ITestOutputHelper output*/)
    {
        _configuration.AddStandardConfiguration("Test", typeof(TestBase).Assembly);

        //initiate ServiceCollection w/logging
        var services = new ServiceCollection()
            .AddSingleton<IConfiguration>(_configuration)
            .AddLogging()
            //.AddXUnitLogging(output)
            ;

        _connectionString = _configuration.GetConnectionString("storage")
            ?? throw new InvalidOperationException("Configure ConnectionStrings:storage for the disposable test emulator.");

        //add services
        services.AddTransient<IAzBlobService>(s => new AzBlobService(_connectionString, $"wibble{Environment.Version.Major}"));
        services.AddTransient<IAzQueueService>(s => new AzQueueService(_connectionString, $"wibble{Environment.Version.Major}"));

        //assign services to be tested
        _serviceProvider = services.BuildServiceProvider();
        _serviceProvider.AddStaticLogging();
        _blobSvc = _serviceProvider.GetRequiredService<IAzBlobService>();
        _queueSvc = _serviceProvider.GetRequiredService<IAzQueueService>();
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        await _serviceProvider.DisposeAsync();
        _configuration.Dispose();
        GC.SuppressFinalize(this);
    }
}
