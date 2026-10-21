namespace CasCap.Abstractions;

/// <summary>
/// Exposes Azure authentication configuration properties required for
/// Key Vault access and certificate-based <see cref="TokenCredential"/> creation.
/// </summary>
/// <remarks>
/// Implement on your application configuration record (e.g. <c>AppConfig</c>) so that
/// services needing only Azure authentication information can depend on this lightweight
/// abstraction instead of the full configuration type.
/// </remarks>
public interface IAzureAuthConfig
{
    /// <summary>Sentinel value for <see cref="KeyVaultName"/> that disables Key Vault integration.</summary>
    /// <remarks>Set <see cref="KeyVaultName"/> to this value to run without Azure Key Vault.</remarks>
    public const string SkipKeyVaultSentinel = "skip";

    /// <summary>Whether Key Vault integration is active.</summary>
    /// <remarks>Returns <see langword="false"/> when <see cref="KeyVaultName"/> equals <c>"skip"</c> (case-insensitive).</remarks>
    public bool IsKeyVaultEnabled => !string.Equals(KeyVaultName, SkipKeyVaultSentinel, StringComparison.OrdinalIgnoreCase);

    /// <summary>Short name of the Azure Key Vault (without the <c>.vault.azure.net</c> suffix).</summary>
    public string KeyVaultName { get; }

    /// <summary>Full URI of the Azure Key Vault derived from <see cref="KeyVaultName"/>.</summary>
    public Uri KeyVaultUri { get; }

    /// <summary>Optional Azure managed identity client ID for Kubernetes workload identity.</summary>
    /// <remarks>
    /// Overrides the injected <c>AZURE_CLIENT_ID</c> when
    /// <see cref="TokenCredentialExtensions.IsPodManagedIdentity"/> returns <see langword="true"/>.
    /// </remarks>
    public Guid? AzureEntraPodManagedIdentityClientId { get; }

    /// <summary>Azure Entra tenant id for certificate-based authentication from the Edge.</summary>
    public Guid? AzureEntraTenantId { get; }

    /// <summary>Azure Entra application (client) id for certificate-based authentication.</summary>
    /// <remarks>This should be stored in secrets.json.</remarks>
    public Guid? AzureEntraApplicationId { get; }

    /// <summary>X.509 certificate thumbprint used to locate the certificate in the local store.</summary>
    /// <remarks>This should be stored in secrets.json or a Kubernetes secret.</remarks>
    public string? AzureEntraCertThumbprint { get; }

    /// <summary>Path to a PFX file used for Edge/Docker certificate-based authentication.</summary>
    public string? AzureEntraPfxPath { get; }

    /// <summary>Password protecting the PFX file at <see cref="AzureEntraPfxPath"/>.</summary>
    public string? AzureEntraPfxPassword { get; }

    /// <summary>Path to a combined PEM file containing the certificate and private key.</summary>
    public string? AzureEntraPemPath { get; }

    /// <summary>Lazily-resolved <see cref="TokenCredential"/> built from the certificate properties.</summary>
    public TokenCredential? TokenCredential { get; }
}
