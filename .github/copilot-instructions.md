# Copilot Instructions

## Shared Instructions

Shared Copilot instructions, skills and prompts are maintained centrally in the [.github](https://github.com/f2calv/.github) repository, under `.github/instructions/`, `.github/skills/` and `.github/prompts/`. They are deliberately not copied into this repository, so a change there takes effect everywhere without a pull request here.

To load them, clone that repository and either add it to this VS Code workspace, or link its folders into `~/.copilot/`. Its README explains both.

If those shared files are not visible, stop and tell the user rather than guessing the conventions — this repository depends on them.

Everything below is specific to this repository.

## Running the Tests

The storage tests run against [Azurite](https://github.com/azure/azurite), not a real Azure account, so start it before running them:

```powershell
docker compose --file docker-compose.yml up --detach
```

Without it every storage test fails on connection refused against ports 10000–10002. Nothing in the suite touches a live storage account.

## Debug and Release Reference Swap

Each project references `CasCap.Common.*` as a `ProjectReference` to an adjacent checkout in Debug, and as a `PackageReference` in Release. Only a Release build validates a published package version.

The configuration must be set on the **restore**, not just the build — `dotnet restore` defaults to `Configuration=Debug` and writes a `project.assets.json` containing no package entries, after which a `-c Release` build fails with `CS0234: the type or namespace name 'Common' does not exist`. Use `dotnet build -c Release` and let it restore, or pass `-p:Configuration=Release` to an explicit restore.

The same conditional defeats `dotnet list package --outdated`, which reports `Unable to read a package reference`. Compare versions against the nuget.org flat-container index instead.
