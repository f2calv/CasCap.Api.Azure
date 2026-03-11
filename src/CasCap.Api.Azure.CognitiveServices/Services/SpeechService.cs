namespace CasCap.Services;

/// <inheritdoc/>
public class SpeechService : ISpeechService
{
    private static readonly ILogger _logger = ApplicationLogging.CreateLogger(nameof(SpeechService));

    private readonly SpeechConfig _speechConfig;

    /// <summary>Initializes a new instance of <see cref="SpeechService"/> using a subscription key.</summary>
    public SpeechService(string subscriptionKey, string region = "westeurope")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(subscriptionKey);
        //note: WSL 2 w/Ubuntu 18.04 needs 'sudo apt-get update && sudo apt-get -y install libasound2'
        _speechConfig = SpeechConfig.FromSubscription(subscriptionKey, region);
        //_speechConfig.SetSpeechSynthesisOutputFormat(SpeechSynthesisOutputFormat.Riff16Khz16BitMonoPcm);
        //https://docs.microsoft.com/en-gb/azure/cognitive-services/speech-service/language-support
        //_speechConfig.SpeechSynthesisVoiceName = "en-GB-Susan-Apollo";
        //_speechConfig.SpeechSynthesisLanguage = "en-GB";
    }

    /// <summary>Initializes a new instance of <see cref="SpeechService"/> using a <see cref="TokenCredential"/>.</summary>
    public SpeechService(Uri endpoint, TokenCredential credential)
    {
        ArgumentNullException.ThrowIfNull(endpoint);
        ArgumentNullException.ThrowIfNull(credential);
        _speechConfig = SpeechConfig.FromEndpoint(endpoint, credential);
    }

    /// <inheritdoc/>
    public async Task CreateWAV(string soundByte, string path)
    {
        using var fileOutput = AudioConfig.FromWavFileOutput(path);
        using var synthesizer = new SpeechSynthesizer(_speechConfig, fileOutput);
        using var result = await synthesizer.SpeakTextAsync(soundByte);
        if (result.Reason == ResultReason.SynthesizingAudioCompleted)
            _logger.LogDebug("{ClassName} Speech synthesized to speaker for text {Soundbyte}", nameof(SpeechService), soundByte);
        else if (result.Reason == ResultReason.Canceled)
        {
            var cancellation = SpeechSynthesisCancellationDetails.FromResult(result);
            _logger.LogWarning("{ClassName} CANCELED: Reason={Reason}", nameof(SpeechService), cancellation.Reason);
            if (cancellation.Reason == CancellationReason.Error)
            {
                _logger.LogError("{ClassName} CANCELED: ErrorCode={ErrorCode}, ErrorDetails={ErrorDetails}, Did you update the subscription info?",
                    nameof(SpeechService), cancellation.ErrorCode, cancellation.ErrorDetails);
            }
        }
    }

    /// <inheritdoc/>
    public async Task<string?> RecognizeFromWAV(string path)
    {
        using var audioInput = AudioConfig.FromWavFileInput(path);
        using var recognizer = new SpeechRecognizer(_speechConfig, audioInput);
        var result = await recognizer.RecognizeOnceAsync();
        return HandleRecognitionResult(result);
    }

    /// <inheritdoc/>
    public async Task<string?> RecognizeFromMicrophone()
    {
        using var recognizer = new SpeechRecognizer(_speechConfig);
        var result = await recognizer.RecognizeOnceAsync();
        return HandleRecognitionResult(result);
    }

    private string? HandleRecognitionResult(SpeechRecognitionResult result)
    {
        if (result.Reason is ResultReason.RecognizedSpeech)
        {
            _logger.LogDebug("{ClassName} Recognized: {Text}", nameof(SpeechService), result.Text);
            return result.Text;
        }

        if (result.Reason is ResultReason.NoMatch)
        {
            _logger.LogWarning("{ClassName} Speech could not be recognized", nameof(SpeechService));
            return null;
        }

        if (result.Reason is ResultReason.Canceled)
        {
            var cancellation = CancellationDetails.FromResult(result);
            _logger.LogWarning("{ClassName} CANCELED: Reason={Reason}", nameof(SpeechService), cancellation.Reason);
            if (cancellation.Reason == CancellationReason.Error)
            {
                _logger.LogError("{ClassName} CANCELED: ErrorCode={ErrorCode}, ErrorDetails={ErrorDetails}, Did you update the subscription info?",
                    nameof(SpeechService), cancellation.ErrorCode, cancellation.ErrorDetails);
            }
        }

        return null;
    }
}
