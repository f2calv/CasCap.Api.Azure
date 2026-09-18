namespace CasCap.Services;

/// <inheritdoc/>
public sealed class SpeechService : ISpeechService
{
    private static readonly ILogger _logger = ApplicationLogging.CreateLogger(nameof(SpeechService));

    private readonly SpeechConfig _speechConfig;

    //The fast transcription API is reached over its own endpoint rather than through SpeechConfig,
    //so the coordinates are kept for TranscribeAsync to build a client from.
    private readonly Uri? _endpoint;
    private readonly TokenCredential? _credential;

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
        _endpoint = endpoint;
        _credential = credential;
    }

    /// <inheritdoc/>
    public async Task<string?> TranscribeAsync(Stream audio, IReadOnlyList<string>? locales = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(audio);
        if (_endpoint is null || _credential is null)
        {
            throw new InvalidOperationException(
                $"{nameof(TranscribeAsync)} requires the endpoint and credential constructor; the " +
                "subscription-key constructor does not carry the resource endpoint the fast transcription API needs.");
        }

        //TODO: alternatives to the fast transcription API, should it prove unsuitable:
        //  - Continuous recognition via SpeechRecognizer. Works on the free tier, handles any length, but is
        //    event-driven and needs its own completion handling.
        //  - RecognizeOnceAsync, as used by RecognizeFromWAV. Simplest, but stops at the first utterance and so
        //    silently truncates anything beyond roughly 15 seconds.
        var client = new TranscriptionClient(_endpoint, _credential);
        var options = new TranscriptionOptions(audio);
        if (locales is { Count: > 0 })
        {
            foreach (var locale in locales)
                options.Locales.Add(locale);
        }

        var response = await client.TranscribeAsync(options, cancellationToken).ConfigureAwait(false);
        var text = response.Value.CombinedPhrases.FirstOrDefault()?.Text;
        _logger.LogDebug("{ClassName} transcribed {PhraseCount} phrase(s)", nameof(SpeechService),
            response.Value.Phrases.Count);
        return string.IsNullOrWhiteSpace(text) ? null : text;
    }

    /// <inheritdoc/>
    public async Task CreateWAV(string soundByte, string path)
    {
        using var fileOutput = AudioConfig.FromWavFileOutput(path);
        using var synthesizer = new SpeechSynthesizer(_speechConfig, fileOutput);
        using var result = await synthesizer.SpeakTextAsync(soundByte).ConfigureAwait(false);
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
        var result = await recognizer.RecognizeOnceAsync().ConfigureAwait(false);
        return HandleRecognitionResult(result);
    }

    /// <inheritdoc/>
    public async Task<string?> RecognizeFromMicrophone()
    {
        using var recognizer = new SpeechRecognizer(_speechConfig);
        var result = await recognizer.RecognizeOnceAsync().ConfigureAwait(false);
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
