namespace CasCap.Tests;

/// <summary>Base class for storage integration tests.</summary>
public abstract class TestBase : IAsyncDisposable
{
    private readonly ConfigurationManager _configuration = new();
    private readonly ServiceProvider _serviceProvider;

    /// <summary>Gets the blob service under test.</summary>
    protected readonly IAzBlobService _blobSvc;

    /// <summary>Gets the queue service under test.</summary>
    protected readonly IAzQueueService _queueSvc;

    /// <summary>Initializes a new instance of <see cref="TestBase" />.</summary>
    protected TestBase(ITestOutputHelper output)
    {
        _configuration.AddStandardConfiguration("Tests", typeof(TestBase).Assembly);

        var connections = _configuration.GetSection(StorageTestConnectionStringsConfig.ConfigurationSectionName)
            .Get<StorageTestConnectionStringsConfig>();
        var connectionString = connections?.Storage;
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException("Configure ConnectionStrings:storage for the disposable test emulator.");

        var services = new ServiceCollection()
            .AddSingleton<IConfiguration>(_configuration)
            .AddLogging(logging => logging.AddProvider(new TestLogProvider(output)));

        services.AddTransient<IAzBlobService>(_ => new AzBlobService(connectionString, $"wibble{Environment.Version.Major}"));
        services.AddTransient<IAzQueueService>(_ => new AzQueueService(connectionString, $"wibble{Environment.Version.Major}"));

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
