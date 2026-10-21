# CasCap.Api.Azure.ContainerRegistry

An Azure Container Registry diagnostic base class for .NET applications.
[`AcrServiceBase.ListRepos()`](https://github.com/f2calv/CasCap.Api.Azure/blob/eaaffef87e366fa62ddc5328dd6f71add9b6397c/src/CasCap.Api.Azure.ContainerRegistry/Services/AcrServiceBase.cs)
enumerates repository names and manifest tags and writes them to the supplied logger; it does not
return a repository collection or implement image push/pull.

## Installation

Targets `net8.0`, `net9.0` and `net10.0`.

```powershell
dotnet add package CasCap.Api.Azure.ContainerRegistry
```

## Usage

Derive a concrete type and supply a registry URI, token credential and logger:

```csharp
using Azure.Core;
using CasCap.Services;
using Microsoft.Extensions.Logging;

public sealed class RegistryInspector(
    ILogger<AcrServiceBase> logger, Uri endpoint, TokenCredential credential)
    : AcrServiceBase(logger, endpoint, credential)
{
}
```

```csharp
var inspector = new RegistryInspector(logger, registryEndpoint, credential);
await inspector.ListRepos();
```

Enable Information logging to see the results. Manifests are requested in descending last-updated
order. `ListRepos()` has no cancellation-token parameter.

## Authentication

Use a credential authorized to enumerate repositories and read manifest metadata. Registry role
requirements depend on its configured permissions mode; consult the
[Azure Container Registry roles guide](https://learn.microsoft.com/en-us/azure/container-registry/container-registry-roles).
Do not enable these diagnostic logs where repository names or tags would disclose private metadata.

There is no options model or DI registration helper; the consuming application supplies constructor
arguments and manages credential acquisition.

## Documentation

- [Registry implementation](https://github.com/f2calv/CasCap.Api.Azure/blob/main/src/CasCap.Api.Azure.ContainerRegistry/Services/AcrServiceBase.cs)
- [Package catalogue and license](https://github.com/f2calv/CasCap.Api.Azure#packages)
