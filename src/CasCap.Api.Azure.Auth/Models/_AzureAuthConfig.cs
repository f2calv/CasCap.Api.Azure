namespace CasCap.Models;

/// <summary>
/// Lightweight projection of the <c>AppConfig</c> section that exposes only
/// <see cref="IAzureAuthConfig"/> properties. Used by test projects and services
/// that need Key Vault access without depending on the full <c>AppConfig</c> type.
/// </summary>
public record AzureAuthConfig : IAppConfig, IAzureAuthConfig
{
    /// <inheritdoc/>
    public static string ConfigurationSectionName => "AppConfig";

    /// <inheritdoc/>
    public bool IsKeyVaultEnabled =>
        !string.Equals(KeyVaultName, IAzureAuthConfig.SkipKeyVaultSentinel, StringComparison.OrdinalIgnoreCase);

    /// <inheritdoc/>
    [Required, MinLength(3)]
    public required string KeyVaultName { get; init; }

    /// <inheritdoc/>
    public Uri KeyVaultUri => new($"https://{KeyVaultName}.vault.azure.net/");

    /// <inheritdoc/>
    public Guid? AzureEntraPodManagedIdentityClientId { get; init; }

    /// <inheritdoc/>
    public Guid? AzureEntraTenantId { get; init; }

    /// <inheritdoc/>
    public Guid? AzureEntraApplicationId { get; init; }

    /// <inheritdoc/>
    public string? AzureEntraCertThumbprint { get; init; }

    /// <inheritdoc/>
    public string? AzureEntraPfxPath { get; init; }

    /// <inheritdoc/>
    public string? AzureEntraPfxPassword { get; init; }

    private TokenCredential? tokenCredential;

    /// <inheritdoc/>
    public TokenCredential? TokenCredential
    {
        get
        {
            tokenCredential ??= TokenCredentialExtensions.CreateTokenCredential(this);
            return tokenCredential;
        }
    }
}
