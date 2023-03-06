namespace CasCap.Models;

public class CognitiveServicesOptions
{
    public const string SectionKey = $"{nameof(CasCap)}:{nameof(CognitiveServicesOptions)}";
    public string SubscriptionKey { get; set; } = string.Empty!;
}