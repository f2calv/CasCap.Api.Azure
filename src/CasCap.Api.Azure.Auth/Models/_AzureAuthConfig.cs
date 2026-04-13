using Azure.Core;
using CasCap.Common.Abstractions;
using System.ComponentModel.DataAnnotations;

namespace CasCap.Api.Azure.Auth.Models;

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
    [Required]
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
            tokenCredential ??= TokenCredentialFactory.CreateTokenCredential(this);
            return tokenCredential;
        }
    }
}
