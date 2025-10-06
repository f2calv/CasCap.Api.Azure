namespace CasCap.Services;

public abstract class AcrServiceBase
{
    protected readonly ILogger _logger;

    protected AcrServiceBase(ILogger<AcrServiceBase> logger, Uri endpoint, TokenCredential credential)
    {
        ArgumentNullException.ThrowIfNull(endpoint);
        ArgumentNullException.ThrowIfNull(credential);
        _logger = logger;
        _client = new ContainerRegistryClient(endpoint, credential);
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
            _logger.LogInformation("{ClassName} starting {RepositoryName}", nameof(AcrServiceBase), repositoryName);

            var repo = _client.GetRepository(repositoryName);

            var manifests = repo.GetAllManifestPropertiesAsync(ArtifactManifestOrder.LastUpdatedOnDescending);
            await foreach (var manifest in manifests)
            {
                _logger.LogInformation("{ClassName} {RepositoryName} tags={Tags}", nameof(AcrServiceBase), manifest.RepositoryName, manifest.Tags);
            }
        }
    }
}
