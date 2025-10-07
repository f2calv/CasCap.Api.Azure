namespace CasCap.Services;

public class Text2SpeechService : IText2SpeechService
{
    private static readonly ILogger _logger = ApplicationLogging.CreateLogger(nameof(Text2SpeechService));

    private readonly SpeechConfig _speechConfig;

    public Text2SpeechService(string subscriptionKey, string region = "westeurope")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(subscriptionKey);
        //note: WSL 2 w/Ubuntu 18.04 needs 'sudo apt-get update && sudo apt-get -y install libasound2'
        _speechConfig = SpeechConfig.FromSubscription(subscriptionKey, region);
        //_speechConfig.SetSpeechSynthesisOutputFormat(SpeechSynthesisOutputFormat.Riff16Khz16BitMonoPcm);
        //https://docs.microsoft.com/en-gb/azure/cognitive-services/speech-service/language-support
        //_speechConfig.SpeechSynthesisVoiceName = "en-GB-Susan-Apollo";
        //_speechConfig.SpeechSynthesisLanguage = "en-GB";
    }

    public Text2SpeechService(Uri endpoint, TokenCredential credential)
    {
        ArgumentNullException.ThrowIfNull(endpoint);
        ArgumentNullException.ThrowIfNull(credential);
        _speechConfig = SpeechConfig.FromEndpoint(endpoint, credential);
    }

    public async Task CreateWAV(string soundByte, string path)
    {
        using var fileOutput = AudioConfig.FromWavFileOutput(path);
        using var synthesizer = new SpeechSynthesizer(_speechConfig, fileOutput);
        using var result = await synthesizer.SpeakTextAsync(soundByte);
        if (result.Reason == ResultReason.SynthesizingAudioCompleted)
            _logger.LogDebug("{ClassName} Speech synthesized to speaker for text {Soundbyte}", nameof(Text2SpeechService), soundByte);
        else if (result.Reason == ResultReason.Canceled)
        {
            var cancellation = SpeechSynthesisCancellationDetails.FromResult(result);
            _logger.LogWarning("{ClassName} CANCELED: Reason={Reason}", nameof(Text2SpeechService), cancellation.Reason);
            if (cancellation.Reason == CancellationReason.Error)
            {
                _logger.LogError("{ClassName} CANCELED: ErrorCode={ErrorCode}, ErrorDetails={ErrorDetails}, Did you update the subscription info?",
                    nameof(Text2SpeechService), cancellation.ErrorCode, cancellation.ErrorDetails);
            }
        }
    }
}
