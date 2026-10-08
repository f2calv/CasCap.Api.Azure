namespace CasCap.Models;

/// <summary>Certificate identity and OAuth scope used to call the Agent Runtime.</summary>
public sealed record AgentRuntimeAzureAuthConfig : IAppConfig, IValidatableObject
{
    private TokenCredential? _tokenCredential;

    /// <inheritdoc/>
    public static string ConfigurationSectionName => $"{nameof(CasCap)}:{nameof(AgentRuntimeAzureAuthConfig)}";

    /// <summary>Gets whether Agent Runtime bearer authentication is enabled.</summary>
    public bool Enabled { get; init; }

    /// <summary>Gets the Microsoft Entra tenant identifier.</summary>
    public Guid? TenantId { get; init; }

    /// <summary>Gets the caller application identifier.</summary>
    public Guid? ClientId { get; init; }

    /// <summary>Gets the combined PEM certificate and private key loaded from private configuration.</summary>
    public string? Certificate { get; init; }

    /// <summary>Gets the Agent Runtime OAuth scope, normally its application ID URI plus <c>/.default</c>.</summary>
    public string? Scope { get; init; }

    /// <summary>Gets the lazily constructed certificate credential.</summary>
    public TokenCredential TokenCredential => _tokenCredential ??= CreateTokenCredential();

    /// <inheritdoc/>
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!Enabled)
            yield break;
        if (TenantId is null)
            yield return new ValidationResult($"{nameof(TenantId)} is required when authentication is enabled.", [nameof(TenantId)]);
        if (ClientId is null)
            yield return new ValidationResult($"{nameof(ClientId)} is required when authentication is enabled.", [nameof(ClientId)]);
        if (string.IsNullOrWhiteSpace(Certificate))
            yield return new ValidationResult(
                $"{nameof(Certificate)} is required when authentication is enabled.",
                [nameof(Certificate)]);
        if (string.IsNullOrWhiteSpace(Scope))
            yield return new ValidationResult($"{nameof(Scope)} is required when authentication is enabled.", [nameof(Scope)]);
    }

    private ClientCertificateCredential CreateTokenCredential() =>
        TenantId is not { } tenantId
            ? throw new InvalidOperationException($"{nameof(TenantId)} is required.")
            : ClientId is not { } clientId
            ? throw new InvalidOperationException($"{nameof(ClientId)} is required.")
            : new ClientCertificateCredential(
                tenantId.ToString(),
                clientId.ToString(),
                X509Certificate2.CreateFromPem(Certificate!, Certificate!));
}