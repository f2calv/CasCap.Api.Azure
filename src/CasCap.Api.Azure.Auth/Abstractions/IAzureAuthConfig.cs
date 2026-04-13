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
    /// <summary>Short name of the Azure Key Vault (without the <c>.vault.azure.net</c> suffix).</summary>
    string KeyVaultName { get; }

    /// <summary>Full URI of the Azure Key Vault derived from <see cref="KeyVaultName"/>.</summary>
    Uri KeyVaultUri { get; }

    /// <summary>Azure managed identity client id for in-AKS workload identity.</summary>
    /// <remarks>Used when <see cref="TokenCredentialFactory.IsPodManagedIdentity"/> returns <see langword="true"/>.</remarks>
    Guid? AzureEntraPodManagedIdentityClientId { get; }

    /// <summary>Azure Entra tenant id for certificate-based authentication from the Edge.</summary>
    Guid? AzureEntraTenantId { get; }

    /// <summary>Azure Entra application (client) id for certificate-based authentication.</summary>
    /// <remarks>This should be stored in secrets.json.</remarks>
    Guid? AzureEntraApplicationId { get; }

    /// <summary>X.509 certificate thumbprint used to locate the certificate in the local store.</summary>
    /// <remarks>This should be stored in secrets.json or a Kubernetes secret.</remarks>
    string? AzureEntraCertThumbprint { get; }

    /// <summary>Path to a PFX file used for Edge/Docker certificate-based authentication.</summary>
    string? AzureEntraPfxPath { get; }

    /// <summary>Password protecting the PFX file at <see cref="AzureEntraPfxPath"/>.</summary>
    string? AzureEntraPfxPassword { get; }

    /// <summary>Lazily-resolved <see cref="TokenCredential"/> built from the certificate properties.</summary>
    TokenCredential? TokenCredential { get; }
}
