# CasCap.Api.Azure.CognitiveServices.Tests

Offline synthetic tests for the Document Intelligence REST transport. All HTTP calls use fake
handlers; no Azure account, credentials, native Speech runtime or external service is required.

## Running

From the repository root with a stable .NET 10 SDK and the supported .NET runtimes installed:

```powershell
dotnet test --project .\src\CasCap.Api.Azure.CognitiveServices.Tests\CasCap.Api.Azure.CognitiveServices.Tests.csproj --configuration Debug
```

Debug references the adjacent shared-library checkout; use `--configuration Release` for published
dependencies. Targets are `net8.0`, `net9.0` and `net10.0`.

## Tests

| Class | Methods | Expanded cases per framework | Category |
| --- | --- | --- | --- |
| `AzDocumentLayoutClientTests` | 26 | 110 | `DocumentLayout` |

Coverage includes exact authenticated submission shape, single-attempt failures, redacted errors,
saved-receipt GETs, endpoint/receipt canonicalization, terminal and malformed statuses, retry timing,
model/version/format metadata, exact page sets, UTF-16 surrogate boundaries, duplicate/overlapping
spans, blank pages, streaming limits, cancellation and response stream disposal.

The maximum accepted source size (25 MiB), result size (16 MiB) and page selection (2000) are exercised
using synthetic data. These tests establish transport behavior, not live upstream conformance or
semantic extraction quality. A synthetic `%PDF-` payload is not a renderable PDF.

Observed Debug validation: 110 passed per target, **330 passed**, zero failed and zero skipped.

## Skipped Tests

None: zero skip reasons and zero skipped cases.

## Test Layout

```text
Tests/
└── Unit/
    └── AzDocumentLayoutClientTests.cs
```
