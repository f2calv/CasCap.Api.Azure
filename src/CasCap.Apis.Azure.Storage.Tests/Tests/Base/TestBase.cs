using CasCap.Services;
using Xunit.Abstractions;
namespace CasCap.Apis.AzStorage.Tests;

public abstract class TestBase
{
    //protected IAzBlobStorageBase _blobSvc;
    protected AzBlobService _blobSvc;
    protected AzQueueService _queueSvc;
    protected AzTableService _tableSvc;
    protected ILogger _logger;

    readonly static string connectionString
        = "DefaultEndpointsProtocol=http;AccountName=devstoreaccount1;AccountKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==;BlobEndpoint=http://127.0.0.1:10000/devstoreaccount1;QueueEndpoint=http://127.0.0.1:10001/devstoreaccount1;TableEndpoint=http://127.0.0.1:10002/devstoreaccount1;";

    readonly static string containerName = "testcontainer";
    readonly static string queueName = "testqueue";
    public TestBase(ITestOutputHelper output)
    {
        var configuration = new ConfigurationBuilder()
            //.AddCasCapConfiguration()
            //.AddJsonFile($"appsettings.Test.json", optional: false, reloadOnChange: false)
            .Build();

        //initiate ServiceCollection w/logging
        var services = new ServiceCollection()
            .AddSingleton<IConfiguration>(configuration)
            .AddXUnitLogging(output);

        var serviceProvider = services.BuildServiceProvider();
        var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();

        //add services
        services.AddTransient(s => new AzBlobService(loggerFactory.CreateLogger<AzBlobService>(), connectionString, containerName));
        services.AddTransient(s => new AzQueueService(loggerFactory.CreateLogger<AzQueueService>(), connectionString, queueName));
        services.AddTransient(s => new AzTableService(loggerFactory.CreateLogger<AzTableService>(), connectionString));

        //assign services to be tested
        serviceProvider = services.BuildServiceProvider();
        _blobSvc = serviceProvider.GetRequiredService<AzBlobService>();
        _queueSvc = serviceProvider.GetRequiredService<AzQueueService>();
        _tableSvc = serviceProvider.GetRequiredService<AzTableService>();
        _logger = loggerFactory.CreateLogger<TestBase>();
    }

    protected static readonly byte[] fileBytes =
    {
        0x1E, 0x00, 0x00, 0x00, 0x0E, 0x04, 0x47, 0x00, 0x00, 0x00, 0x00, 0x00,
        0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01, 0xFF, 0x18, 0x0B, 0x0E, 0xFF,
        0x12, 0x03, 0x00, 0x00, 0x0E, 0x6D, 0x15, 0x34, 0x15, 0x20, 0x12, 0x10,
        0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xEC, 0x16, 0x1F, 0x00,
        0x00, 0x00, 0x04, 0x29, 0x92, 0x11, 0x00, 0x00, 0x04, 0xA9, 0x0B, 0x16,
        0x00, 0x00, 0x00, 0xB7, 0x16, 0xC1, 0x80, 0x40, 0xFD, 0x1B, 0x01, 0x8E,
        0x00, 0x00, 0x81, 0x40, 0xFD, 0x1A, 0x00, 0x1F, 0x00, 0x00, 0x00, 0x00,
        0x21, 0x00, 0x00, 0x00, 0xCE, 0x00, 0xED, 0xEB, 0x15, 0x00, 0x00, 0x00,
        0xCE, 0x40, 0x84, 0x15, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xCE, 0x80,
        0x15, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x0F, 0x80, 0x40, 0xFD, 0x61,
        0x04, 0x68, 0x4F, 0x4F, 0x68, 0x08, 0x00, 0x72, 0x72, 0x16, 0x41, 0x00
    };
}