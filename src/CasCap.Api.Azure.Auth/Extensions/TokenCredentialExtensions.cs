namespace CasCap;

/// <summary>
/// Factory methods for creating Azure <see cref="TokenCredential"/> instances
/// from <see cref="IAzureAuthConfig"/> properties.
/// </summary>
public static class TokenCredentialExtensions
{
    /// <summary>
    /// Checks whether the current pod is using Azure workload identity
    /// (managed identity with federated tokens).
    /// </summary>
    public static bool IsPodManagedIdentity =>
        Environment.GetEnvironmentVariable("AZURE_CLIENT_ID") is not null
        && Environment.GetEnvironmentVariable("AZURE_TENANT_ID") is not null
        && Environment.GetEnvironmentVariable("AZURE_FEDERATED_TOKEN_FILE") is not null
        && Environment.GetEnvironmentVariable("AZURE_AUTHORITY_HOST") is not null;

    /// <summary>
    /// Creates a <see cref="WorkloadIdentityCredential"/> from the injected Kubernetes environment,
    /// or a <see cref="ClientCertificateCredential"/> from the certificate properties in
    /// <paramref name="config"/>.
    /// </summary>
    /// <param name="config">Azure authentication configuration.</param>
    /// <returns>
    /// A <see cref="TokenCredential"/>, or <see langword="null"/> when neither workload identity nor
    /// a certificate source is available.
    /// </returns>
    public static TokenCredential? CreateTokenCredential(IAzureAuthConfig config)
    {
        if (IsPodManagedIdentity)
        {
            return new WorkloadIdentityCredential(
                new WorkloadIdentityCredentialOptions
                {
                    ClientId = config.AzureEntraPodManagedIdentityClientId?.ToString(),
                });
        }

        var certificateSourceCount = new[]
        {
            config.AzureEntraCertThumbprint,
            config.AzureEntraPemPath,
            config.AzureEntraPfxPath,
        }.Count(value => !string.IsNullOrWhiteSpace(value));

        if (certificateSourceCount > 1)
            throw new GenericException(
                $"Configure only one of {nameof(config.AzureEntraCertThumbprint)}, " +
                $"{nameof(config.AzureEntraPemPath)}, or {nameof(config.AzureEntraPfxPath)}.");

        X509Certificate2? certificate = null;
        if (!string.IsNullOrWhiteSpace(config.AzureEntraCertThumbprint))
        {
            using var store = new X509Store(StoreLocation.CurrentUser);
            store.Open(OpenFlags.ReadOnly);
            certificate = store.Certificates.Find(X509FindType.FindByThumbprint, config.AzureEntraCertThumbprint, false)
                .OfType<X509Certificate2>().SingleOrDefault();
            store.Close();
        }
        else if (!string.IsNullOrWhiteSpace(config.AzureEntraPemPath))
        {
            certificate = X509Certificate2.CreateFromPemFile(config.AzureEntraPemPath);
        }
        else if (!string.IsNullOrWhiteSpace(config.AzureEntraPfxPath))
        {
#if NET9_0_OR_GREATER
            certificate = X509CertificateLoader.LoadPkcs12FromFile(config.AzureEntraPfxPath, config.AzureEntraPfxPassword);
#else
            certificate = new X509Certificate2(config.AzureEntraPfxPath, config.AzureEntraPfxPassword);
#endif
        }
        if (certificate is null)
            return null;
        if (config.AzureEntraTenantId is null)
            throw new GenericException($"{nameof(config.AzureEntraTenantId)} is null");
        if (config.AzureEntraApplicationId is null)
            throw new GenericException($"{nameof(config.AzureEntraApplicationId)} is null");
        return new ClientCertificateCredential(config.AzureEntraTenantId.ToString(), config.AzureEntraApplicationId.ToString(), certificate);
    }
}
