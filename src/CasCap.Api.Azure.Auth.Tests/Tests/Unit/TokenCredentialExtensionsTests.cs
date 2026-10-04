namespace CasCap;

/// <summary>Tests for certificate-backed Azure token credential creation.</summary>
public sealed class TokenCredentialExtensionsTests : IDisposable
{
    private readonly string tempDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
    private readonly Dictionary<string, string?> originalEnvironmentVariables = WorkloadIdentityEnvironmentVariables
        .ToDictionary(name => name, Environment.GetEnvironmentVariable);

    /// <summary>Creates the per-test temporary directory.</summary>
    public TokenCredentialExtensionsTests() => Directory.CreateDirectory(tempDirectory);

    /// <summary>Verifies an absent certificate source skips credential creation.</summary>
    [Fact]
    public void CreateTokenCredential_NoCertificateSource()
    {
        ClearWorkloadIdentityEnvironment();
        var config = CreateConfig();

        var credential = TokenCredentialExtensions.CreateTokenCredential(config);

        Assert.Null(credential);
    }

    /// <summary>Verifies a combined PEM certificate and private key creates a credential.</summary>
    [Fact]
    public void CreateTokenCredential_CombinedPem()
    {
        ClearWorkloadIdentityEnvironment();
        var pemPath = Path.Combine(tempDirectory, "client-certificate.pem");
        using var certificate = CreateCertificate();
        File.WriteAllText(
            pemPath,
            string.Concat(
                certificate.ExportCertificatePem(),
                Environment.NewLine,
                certificate.GetRSAPrivateKey()!.ExportPkcs8PrivateKeyPem()));
        var config = CreateConfig() with { AzureEntraPemPath = pemPath };

        var credential = TokenCredentialExtensions.CreateTokenCredential(config);

        Assert.NotNull(credential);
    }

    /// <summary>Verifies a PFX certificate remains supported.</summary>
    [Fact]
    public void CreateTokenCredential_Pfx()
    {
        ClearWorkloadIdentityEnvironment();
        var pfxPath = Path.Combine(tempDirectory, "client-certificate.pfx");
        using var certificate = CreateCertificate();
        File.WriteAllBytes(pfxPath, certificate.Export(X509ContentType.Pkcs12));
        var config = CreateConfig() with { AzureEntraPfxPath = pfxPath };

        var credential = TokenCredentialExtensions.CreateTokenCredential(config);

        Assert.NotNull(credential);
    }

    /// <summary>Verifies ambiguous certificate source configuration is rejected.</summary>
    [Fact]
    public void CreateTokenCredential_MultipleCertificateSources()
    {
        ClearWorkloadIdentityEnvironment();
        var config = CreateConfig() with
        {
            AzureEntraPemPath = "client-certificate.pem",
            AzureEntraPfxPath = "client-certificate.pfx",
        };

        var exception = Assert.Throws<GenericException>(
            () => TokenCredentialExtensions.CreateTokenCredential(config));

        Assert.Contains(nameof(IAzureAuthConfig.AzureEntraPemPath), exception.Message);
        Assert.Contains(nameof(IAzureAuthConfig.AzureEntraPfxPath), exception.Message);
    }

    /// <summary>Verifies the injected Kubernetes workload identity takes priority over certificate sources.</summary>
    [Fact]
    public void CreateTokenCredential_WorkloadIdentity()
    {
        SetWorkloadIdentityEnvironment();
        var config = CreateConfig() with
        {
            AzureEntraPodManagedIdentityClientId = Guid.NewGuid(),
            AzureEntraPemPath = "unused-client-certificate.pem",
        };

        var credential = TokenCredentialExtensions.CreateTokenCredential(config);

        Assert.IsType<WorkloadIdentityCredential>(credential);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        foreach (var (name, value) in originalEnvironmentVariables)
            Environment.SetEnvironmentVariable(name, value);

        Directory.Delete(tempDirectory, recursive: true);
        GC.SuppressFinalize(this);
    }

    private static readonly string[] WorkloadIdentityEnvironmentVariables =
    [
        "AZURE_AUTHORITY_HOST",
        "AZURE_CLIENT_ID",
        "AZURE_FEDERATED_TOKEN_FILE",
        "AZURE_TENANT_ID",
    ];

    private static void ClearWorkloadIdentityEnvironment()
    {
        foreach (var name in WorkloadIdentityEnvironmentVariables)
            Environment.SetEnvironmentVariable(name, null);
    }

    private static AzureAuthConfig CreateConfig() =>
        new()
        {
            KeyVaultName = "example-key-vault",
            AzureEntraTenantId = Guid.NewGuid(),
            AzureEntraApplicationId = Guid.NewGuid(),
        };

    private static X509Certificate2 CreateCertificate()
    {
        using var rsa = RSA.Create(2048);
        var request = new CertificateRequest(
            "CN=example-client-certificate",
            rsa,
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);
        return request.CreateSelfSigned(
            DateTimeOffset.UtcNow.AddMinutes(-1),
            DateTimeOffset.UtcNow.AddHours(1));
    }

    private void SetWorkloadIdentityEnvironment()
    {
        Environment.SetEnvironmentVariable("AZURE_AUTHORITY_HOST", "https://login.microsoftonline.com/");
        Environment.SetEnvironmentVariable("AZURE_CLIENT_ID", Guid.NewGuid().ToString());
        Environment.SetEnvironmentVariable("AZURE_FEDERATED_TOKEN_FILE", Path.Combine(tempDirectory, "federated-token"));
        Environment.SetEnvironmentVariable("AZURE_TENANT_ID", Guid.NewGuid().ToString());
    }
}
