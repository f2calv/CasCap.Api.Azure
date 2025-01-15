namespace CasCap.Services;

public abstract class AcrServiceBase
{
    protected /*readonly*/ ILogger _logger;

    public AcrServiceBase(ILogger<AcrServiceBase> logger, string Endpoint)
    {
        _logger = logger;

        // Get the service endpoint from the environment
        Uri endpoint = new(Endpoint);

        // Create a new ContainerRegistryClient
        _client = new ContainerRegistryClient(endpoint, new DefaultAzureCredential());
    }

    private readonly ContainerRegistryClient _client;

    //https://docs.microsoft.com/en-us/dotnet/api/overview/azure/containers.containerregistry-readme-pre
    //https://azuresdkdocs.blob.core.windows.net/$web/dotnet/Azure.Containers.ContainerRegistry/1.0.0-beta.2/index.html#list-repositories-asynchronously
    public async Task ListRepos()
    {
        // Perform an operation
        AsyncPageable<string> repositories = _client.GetRepositoryNamesAsync();
        await foreach (var repositoryName in repositories)
        {
            _logger.LogInformation("starting {RepositoryName}", repositoryName);

            var repo = _client.GetRepository(repositoryName);

            var manifests = repo.GetAllManifestPropertiesAsync(ArtifactManifestOrder.LastUpdatedOnDescending);
            await foreach (var manifest in manifests)
            {
                _logger.LogInformation("{RepositoryName} tags={tags}", manifest.RepositoryName, manifest.Tags);
            }
        }
    }
}
