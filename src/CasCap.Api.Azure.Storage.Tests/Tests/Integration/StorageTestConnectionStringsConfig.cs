namespace CasCap.Tests;

internal sealed class StorageTestConnectionStringsConfig
{
    public const string ConfigurationSectionName = "ConnectionStrings";

    public string? Storage { get; init; }
}
