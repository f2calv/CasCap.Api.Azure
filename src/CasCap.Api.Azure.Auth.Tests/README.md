# CasCap.Api.Azure.Auth.Tests

Offline xUnit v3 tests for Azure credential construction, HTTP bearer headers and authentication
registration. The executable uses Microsoft.Testing.Platform and targets .NET 8, .NET 9 and .NET 10.

## Tests

Counts below come from source declarations, per target framework, not an executed test run.

| Test class | Methods | Expanded cases | Scope |
| --- | ---: | ---: | --- |
| `AgentRuntimeAzureAuthConfigTests` | 1 | 1 | Combined PEM credential construction |
| `AgentRuntimeAzureAuthHttpClientBuilderExtensionsTests` | 2 | 3 | Enabled/disabled handler registration and missing identity validation |
| `TokenCredentialBearerHandlerTests` | 2 | 2 | Token scope and preservation of caller Authorization |
| `TokenCredentialExtensionsTests` | 5 | 5 | No source, PEM, PFX, conflicting sources and workload identity |
| **Total** | **10** | **11** | |

The registration theory expands through two `InlineData` rows. There are no trait categories.
Tests generate their own certificates and use fake credentials; no Azure account, live token
acquisition or service is required.

## Skipped Tests

No declared skips or conditional skip attributes.

## Running Tests

Use a stable .NET 10 SDK, with the target-framework runtimes installed. Run only after explicit
approval, from the repository root:

```powershell
dotnet test --project .\src\CasCap.Api.Azure.Auth.Tests\CasCap.Api.Azure.Auth.Tests.csproj --configuration Release
```

Release uses published dependencies; Debug additionally needs an adjacent
[CasCap.Common checkout](https://github.com/f2calv/CasCap.Common).

## Layout

```text
Tests/
└── Unit/
    ├── AgentRuntimeAzureAuthConfigTests.cs
    ├── AgentRuntimeAzureAuthHttpClientBuilderExtensionsTests.cs
    ├── TokenCredentialBearerHandlerTests.cs
    └── TokenCredentialExtensionsTests.cs
```

See the [Auth package README](../CasCap.Api.Azure.Auth/README.md) for the consumer contract.
