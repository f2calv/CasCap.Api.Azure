# CasCap.Api.Azure.ContainerRegistry

Helper library for Azure Container Registry. Provides a base service class for listing repositories and manifests.

## Services / Extensions

| Type | Name | Description |
| ---- | ---- | ----------- |
| Service | `AcrServiceBase` | Abstract base class for ACR operations. Authenticates via `TokenCredential` and provides repository/manifest listing. |

### Key Methods

- `ListRepos()` — Lists all repositories and their manifests in the registry.

## Configuration

No configuration model. The service is constructed directly with a `Uri` endpoint and `TokenCredential`.

## Dependencies

### NuGet Packages

| Package | Description |
| ------- | ----------- |
| `Azure.Containers.ContainerRegistry` | Azure Container Registry client library |
| `Azure.Identity` | Azure identity and credential providers |
| `CasCap.Common.Logging` | Shared logging infrastructure |
| `CasCap.Common.Extensions` | Common extension methods |

### Project References

None.
