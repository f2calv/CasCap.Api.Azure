# CasCap.Api.Azure.Auth.Tests

Unit tests for certificate-backed Azure token credential creation.

## Tests

| Test class | Method count | Expanded cases | Coverage |
| --- | ---: | ---: | --- |
| `AgentRuntimeAzureAuthConfigTests` | 1 | 1 | Combined PEM content creates an Agent Runtime client credential |
| `TokenCredentialBearerHandlerTests` | 2 | 2 | Bearer token acquisition and caller-owned Authorization preservation |
| `TokenCredentialExtensionsTests` | 5 | 5 | No source, workload identity, combined PEM, PFX, and conflicting sources |

## Traits

No traits are used; every test is an offline unit test.

## Skipped Tests

No tests are skipped.

## Layout

```text
Tests/
└── Unit/
    ├── AgentRuntimeAzureAuthConfigTests.cs
    ├── TokenCredentialBearerHandlerTests.cs
    └── TokenCredentialExtensionsTests.cs
```
