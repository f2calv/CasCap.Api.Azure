using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using Microsoft.Extensions.Logging;
namespace CasCap.Services;

/// <summary>
/// https://github.com/Azure-Samples/cognitive-services-speech-sdk/blob/master/samples/csharp/sharedcontent/console/speech_synthesis_samples.cs
/// </summary>
public interface IText2SpeechService
{
    Task CreateWAV(string soundbyte, string path);
}

public class Text2SpeechService : IText2SpeechService
{
    readonly ILogger _logger;

    readonly string _localPath;
    readonly SpeechConfig _speechConfig;

    public Text2SpeechService(ILogger<Text2SpeechService> logger, string localPath, string subscriptionKey, string region = "westeurope")
    {
        _logger = logger;
        _localPath = localPath ?? throw new FileNotFoundException("not found!", nameof(localPath));//but doesn't handle string.Empty
        if (string.IsNullOrWhiteSpace(localPath)) throw new ArgumentException("not supplied!", nameof(localPath));
        if (string.IsNullOrWhiteSpace(subscriptionKey)) throw new ArgumentException("not supplied!", nameof(subscriptionKey));
        _localPath = localPath;
        //note: WSL 2 w/Ubuntu 18.04 needs 'sudo apt-get update && sudo apt-get -y install libasound2'
        _speechConfig = SpeechConfig.FromSubscription(subscriptionKey, region);
        //_speechConfig.SetSpeechSynthesisOutputFormat(SpeechSynthesisOutputFormat.Riff16Khz16BitMonoPcm);
        //https://docs.microsoft.com/en-gb/azure/cognitive-services/speech-service/language-support
        //_speechConfig.SpeechSynthesisVoiceName = "en-GB-Susan-Apollo";
        //_speechConfig.SpeechSynthesisLanguage = "en-GB";
    }

    public async Task CreateWAV(string soundbyte, string path)
    {
        using var fileOutput = AudioConfig.FromWavFileOutput(path);
        using var synthesizer = new SpeechSynthesizer(_speechConfig, fileOutput);
        using var result = await synthesizer.SpeakTextAsync(soundbyte);
        if (result.Reason == ResultReason.SynthesizingAudioCompleted)
            _logger.LogDebug("Speech synthesized to speaker for text {soundbyte}", soundbyte);
        else if (result.Reason == ResultReason.Canceled)
        {
            var cancellation = SpeechSynthesisCancellationDetails.FromResult(result);
            _logger.LogWarning("CANCELED: Reason={reason}", cancellation.Reason);
            if (cancellation.Reason == CancellationReason.Error)
            {
                _logger.LogError("CANCELED: ErrorCode={ErrorCode}, ErrorDetails={ErrorDetails}, Did you update the subscription info?", cancellation.ErrorCode, cancellation.ErrorDetails);
            }
        }
    }
}
