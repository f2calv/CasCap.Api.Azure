# CasCap.Api.Azure

A collection of .NET helper class libraries for interacting with Azure PaaS services. The repository contains 9 projects (8 libraries + 1 test project) targeting net8.0, net9.0, and net10.0.

**Solution Files:**

- `CasCap.Api.Azure.Release.slnx` (production builds, uses NuGet packages)
- `CasCap.Api.Azure.Debug.slnx` (development, references local CasCap.Common repo)

**Dependency:** Debug builds require [CasCap.Common](https://github.com/f2calv/CasCap.Common) cloned at the same directory level.

## Projects

| Project | Description | README |
| ------- | ----------- | ------ |
| **CasCap.Api.Azure.AppInsights** | Application Insights configuration & DI registration | [README](src/CasCap.Api.Azure.AppInsights/README.md) |
| **CasCap.Api.Azure.CognitiveServices** | Speech-to-text and text-to-speech via Azure Speech SDK | [README](src/CasCap.Api.Azure.CognitiveServices/README.md) |
| **CasCap.Api.Azure.ContainerRegistry** | Azure Container Registry repository/manifest listing | [README](src/CasCap.Api.Azure.ContainerRegistry/README.md) |
| **CasCap.Api.Azure.EventGrid** | Azure Event Grid messaging (placeholder) | [README](src/CasCap.Api.Azure.EventGrid/README.md) |
| **CasCap.Api.Azure.EventHub** | Event Hub publisher/subscriber with MessagePack serialization | [README](src/CasCap.Api.Azure.EventHub/README.md) |
| **CasCap.Api.Azure.LogAnalytics** | Log Analytics query service for Application Insights | [README](src/CasCap.Api.Azure.LogAnalytics/README.md) |
| **CasCap.Api.Azure.ServiceBus** | Service Bus queue and topic send/receive operations | [README](src/CasCap.Api.Azure.ServiceBus/README.md) |
| **CasCap.Api.Azure.Storage** | Blob, Queue, and Table storage base services | [README](src/CasCap.Api.Azure.Storage/README.md) |
| **CasCap.Api.Azure.Storage.Tests** | xUnit integration tests for Storage (requires Azurite) | [README](src/CasCap.Api.Azure.Storage.Tests/README.md) |

## Dependency Graph

### NuGet Package Dependencies

```mermaid
graph TD
    subgraph "CasCap.Common (external)"
        Logging["CasCap.Common.Logging"]
        Ext["CasCap.Common.Extensions"]
        SerJson["CasCap.Common.Serialization.Json"]
        SerMsgPack["CasCap.Common.Serialization.MessagePack"]
        Testing["CasCap.Common.Testing"]
    end

    subgraph "CasCap.Api.Azure Libraries"
        AI["AppInsights"]
        CS["CognitiveServices"]
        CR["ContainerRegistry"]
        EG["EventGrid"]
        EH["EventHub"]
        LA["LogAnalytics"]
        SB["ServiceBus"]
        ST["Storage"]
    end

    Tests["Storage.Tests"]

    AI --> Logging
    AI --> Ext

    CS --> Logging
    CS --> Ext

    CR --> Logging
    CR --> Ext

    EG --> Logging
    EG --> Ext

    EH --> Logging
    EH --> Ext
    EH --> SerMsgPack

    LA --> Logging
    LA --> Ext

    SB --> Logging
    SB --> Ext

    ST --> Logging
    ST --> Ext
    ST --> SerJson

    Tests --> ST
    Tests --> Logging
    Tests --> Testing
```

### Azure SDK Dependencies

```mermaid
graph LR
    subgraph "Azure SDKs"
        AzCore["Azure.Core"]
        AzIdentity["Azure.Identity"]
        AzBlobs["Azure.Storage.Blobs"]
        AzQueues["Azure.Storage.Queues"]
        AzTables["Azure.Data.Tables"]
        AzEG["Azure.Messaging.EventGrid"]
        AzEH["Azure.Messaging.EventHubs"]
        AzEHP["Azure.Messaging.EventHubs.Processor"]
        AzSB["Azure.Messaging.ServiceBus"]
        AzMQ["Azure.Monitor.Query"]
        AzACR["Azure.Containers.ContainerRegistry"]
        CogSpeech["Microsoft.CognitiveServices.Speech"]
    end

    AI["AppInsights"]
    CS["CognitiveServices"] --> CogSpeech
    CR["ContainerRegistry"] --> AzACR
    CR --> AzIdentity
    EG["EventGrid"] --> AzEG
    EH["EventHub"] --> AzEH
    EH --> AzEHP
    LA["LogAnalytics"] --> AzIdentity
    LA --> AzMQ
    SB["ServiceBus"] --> AzSB
    ST["Storage"] --> AzCore
    ST --> AzBlobs
    ST --> AzQueues
    ST --> AzTables
```

### Project Reference Graph

```mermaid
graph TD
    Tests["Storage.Tests"] --> ST["Storage"]
```

## Project Structure

```text
/
├── .github/
│   ├── workflows/
│   │   └── ci.yml              # Main CI pipeline (lint, version, build)
│   └── dependabot.yml          # Auto-updates for nuget, github-actions, devcontainers
├── src/
│   ├── CasCap.Api.Azure.AppInsights/          # Application Insights helpers
│   ├── CasCap.Api.Azure.CognitiveServices/    # Cognitive Services (e.g., Speech)
│   ├── CasCap.Api.Azure.ContainerRegistry/    # Azure Container Registry client
│   ├── CasCap.Api.Azure.EventGrid/            # Event Grid messaging
│   ├── CasCap.Api.Azure.EventHub/             # Event Hub streaming
│   ├── CasCap.Api.Azure.LogAnalytics/         # Log Analytics query service
│   ├── CasCap.Api.Azure.ServiceBus/           # Service Bus messaging
│   ├── CasCap.Api.Azure.Storage/              # Blob, Queue, Table storage
│   └── CasCap.Api.Azure.Storage.Tests/        # xUnit tests for Storage
├── Directory.Build.props       # Common MSBuild properties for all projects
├── Directory.Packages.props    # Centralized package version management (CPM)
├── .editorconfig               # Code style rules (4-space indent, LF line endings)
├── GitVersion.yml              # Semantic versioning configuration
├── global.json                 # .NET SDK version (allowPrerelease: false)
└── docker-compose.yml          # Azurite storage emulator setup
```

**Typical Project Structure:**

- `Abstractions/` - Interfaces
- `Services/` - Service implementations
- `Extensions/` - Dependency injection extensions
- `Models/` - DTOs and options classes
- `Usings.cs` - Global using statements

## Prerequisites

- **.NET SDK**: 10.0.x stable (see `global.json` — `allowPrerelease: false`)
- **Docker**: Required for Azurite storage emulator during testing

## Build & Validation Commands

### 1. Restore Dependencies (REQUIRED FIRST STEP)

```bash
dotnet restore CasCap.Api.Azure.Release.slnx
```

### 2. Build the Solution

```bash
dotnet build CasCap.Api.Azure.Release.slnx --configuration Release --no-restore
```

Builds all 9 projects for net8.0, net9.0, and net10.0 (27 DLLs total).

### 3. Clean Build Artifacts

```bash
dotnet clean CasCap.Api.Azure.Release.slnx
```

**Alternative:** PowerShell script `./clean.ps1` (removes all bin/obj folders recursively)

### 4. Format Code (REQUIRED BEFORE COMMIT)

```bash
dotnet format CasCap.Api.Azure.Release.slnx --no-restore
```

**Verify formatting:**

```bash
dotnet format CasCap.Api.Azure.Release.slnx --verify-no-changes --no-restore
```

CI will fail if code is not properly formatted.

### 5. Run Tests (REQUIRES AZURITE)

**Start Azurite storage emulator FIRST:**

```bash
docker compose up -d
```

**Ports:** 10000 (blob), 10001 (queue), 10002 (table)

**Run tests:**

```bash
dotnet test CasCap.Api.Azure.Release.slnx --configuration Release --no-build --verbosity normal
```

**Stop Azurite after testing:**

```bash
docker compose down
```

> **KNOWN ISSUE:** Tests currently fail with "API version 2026-02-06 is not supported by Azurite" errors. This is a known compatibility issue documented in `.github/workflows/ci.yml` (`execute-tests: false`). The CI explicitly skips tests.

### Quick Reference

**Full build from scratch:**

```bash
dotnet restore CasCap.Api.Azure.Release.slnx
dotnet build CasCap.Api.Azure.Release.slnx --configuration Release --no-restore
dotnet format CasCap.Api.Azure.Release.slnx --no-restore
```

**Build + Test (with Azurite):**

```bash
docker compose up -d
dotnet restore CasCap.Api.Azure.Release.slnx
dotnet build CasCap.Api.Azure.Release.slnx --configuration Release --no-restore
dotnet test CasCap.Api.Azure.Release.slnx --configuration Release --no-build
docker compose down
```

**Clean + Rebuild:**

```bash
dotnet clean CasCap.Api.Azure.Release.slnx
dotnet restore CasCap.Api.Azure.Release.slnx
dotnet build CasCap.Api.Azure.Release.slnx --configuration Release --no-restore
```

## CI/CD Pipeline (.github/workflows/ci.yml)

**Triggers:** push (except preview branches), pull_request to main, workflow_dispatch

**Jobs:**

1. **lint** - Uses reusable workflow `f2calv/gha-workflows/.github/workflows/lint.yml@v1`
2. **versioning** - GitVersion for semantic versioning (uses GitVersion.yml)
3. **build** - Ubuntu runner with:
   - Azurite service container (ports 10000-10002)
   - Reusable workflow `f2calv/gha-dotnet-nuget@v2`
   - Configuration: Release (default) or Debug (manual)
   - **Tests are disabled** (`execute-tests: false`) due to Azurite API version incompatibility
4. **release** - Creates GitHub releases (only on main or preview branches)

## Multi-Targeting Notes

All libraries target **net8.0, net9.0, and net10.0** simultaneously. When making changes:

- Test builds across all target frameworks
- Some packages (like `System.Linq.Async`) are only referenced for net8.0/net9.0
- Build output generates separate DLLs for each framework

## Packaging & Versioning

- **IsPackable:** Explicitly set per project (default is `false` in Directory.Build.props)
- **Versioning:** Automated via GitVersion (MainLine mode)
- **NuGet Push:** Handled by CI on main branch (requires NUGET_API_KEY secret)
- **Package metadata:** Defined in Directory.Build.props (author, license, icon, source link)

## Common Gotchas

1. **Debug vs Release solution:**
   - Debug solution references local `../CasCap.Common` repo (must be cloned)
   - Release solution uses NuGet packages
   - Always use Release solution unless actively developing CasCap.Common integration

2. **Central Package Management:**
   - Package versions are centralized in `Directory.Packages.props`
   - Never add version attributes to `<PackageReference>` in .csproj files
   - Update versions only in Directory.Packages.props

3. **Azurite Test Failures:**
   - Tests fail with API version errors - this is EXPECTED
   - Don't spend time trying to fix this unless specifically tasked
   - CI intentionally skips tests

4. **Docker Compose Command:**
   - Use `docker compose` (not `docker-compose`)
   - Older hyphenated command may not be available

5. **Formatting is Mandatory:**
   - CI will fail if code is not formatted
   - Always run `dotnet format` before committing
   - CI uses `--verify-no-changes` flag

6. **Pre-commit Hooks:**
   - Configured in `.pre-commit-config.yaml` but requires manual installation
   - Not automatically enforced in standard environments
   - Checks: YAML, JSON5, markdown linting, large files, whitespace
