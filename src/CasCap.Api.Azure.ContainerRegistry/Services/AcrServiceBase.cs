namespace CasCap.Services;

/// <summary>Base class for Azure Container Registry operations.</summary>
public abstract class AcrServiceBase
{
    /// <summary>Logger instance for this class.</summary>
    protected readonly ILogger _logger;

    /// <summary>Initializes a new instance of <see cref="AcrServiceBase" /> using a service endpoint and token credential.</summary>
    protected AcrServiceBase(ILogger<AcrServiceBase> logger, Uri endpoint, TokenCredential credential)
    {
        ArgumentNullException.ThrowIfNull(endpoint);
        ArgumentNullException.ThrowIfNull(credential);
        _logger = logger;
        _client = new ContainerRegistryClient(endpoint, credential);
    }

    private readonly ContainerRegistryClient _client;

    /// <summary>Lists all repositories and their manifests in the registry.</summary>
    /// <remarks>
    /// See <see href="https://docs.microsoft.com/en-us/dotnet/api/overview/azure/containers.containerregistry-readme-pre" /> and
    /// <see href="https://azuresdkdocs.blob.core.windows.net/$web/dotnet/Azure.Containers.ContainerRegistry/1.0.0-beta.2/index.html#list-repositories-asynchronously" />.
    /// </remarks>
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
