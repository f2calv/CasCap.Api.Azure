using System.Security.Cryptography;

namespace CasCap.Tests.Unit;

/// <summary>Tests certificate-backed Agent Runtime caller configuration.</summary>
public sealed class AgentRuntimeAzureAuthConfigTests
{
    [Fact]
    public void TokenCredential_CombinedPemCreatesCredential()
    {
        using var rsa = RSA.Create(2048);
        var request = new CertificateRequest(
            "CN=agent-runtime-test",
            rsa,
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);
        using var certificate = request.CreateSelfSigned(
            DateTimeOffset.UtcNow.AddMinutes(-1),
            DateTimeOffset.UtcNow.AddMinutes(5));
        var combinedPem = certificate.ExportCertificatePem()
            + Environment.NewLine
            + rsa.ExportPkcs8PrivateKeyPem();
        var config = new AgentRuntimeAzureAuthConfig
        {
            Enabled = true,
            TenantId = Guid.NewGuid(),
            ClientId = Guid.NewGuid(),
            Certificate = combinedPem,
            Scope = "api://agent-runtime/.default",
        };

        var credential = config.TokenCredential;

        Assert.IsType<ClientCertificateCredential>(credential);
    }
}