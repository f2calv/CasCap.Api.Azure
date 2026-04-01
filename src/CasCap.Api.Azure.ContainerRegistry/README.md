# CasCap.Api.Azure.ContainerRegistry

Helper library for Azure Container Registry. Provides a base service class for listing repositories and manifests.

## Services / Extensions

| Type | Name | Description |
| --- | --- | --- |
| Service | `AcrServiceBase` | Abstract base class for ACR operations. Authenticates via `TokenCredential` and provides repository/manifest listing. |

### Key Methods

- `ListRepos()` — Lists all repositories and their manifests in the registry.

## Configuration

No configuration model. The service is constructed directly with a `Uri` endpoint and `TokenCredential`.

## Dependencies

### NuGet Packages

| Package |
| --- |
| [Azure.Containers.ContainerRegistry](https://www.nuget.org/packages/azure.containers.containerregistry) |
| [Azure.Identity](https://www.nuget.org/packages/azure.identity) |
| [CasCap.Common.Logging](https://www.nuget.org/packages/cascap.common.logging) |
| [CasCap.Common.Extensions](https://www.nuget.org/packages/cascap.common.extensions) |

### Project References

None.
