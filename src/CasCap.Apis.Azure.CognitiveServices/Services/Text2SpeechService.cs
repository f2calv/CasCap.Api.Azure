using CasCap.Apis.AzCognitiveServices;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using Microsoft.Extensions.Logging;
using NAudio.Wave;
namespace CasCap.Services;

/// <summary>
/// Because of reference to NAudio dependency inject this manually at start of relevant process,
/// to prevent .NET Standard warnings.
/// https://github.com/Azure-Samples/cognitive-services-speech-sdk/blob/master/samples/csharp/sharedcontent/console/speech_synthesis_samples.cs
/// </summary>
public interface IText2SpeechService
{
    Task playSound(string soundbyte);
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

    public async Task playSound(string soundbyte)
    {
        soundbyte = soundbyte.Replace("_", " ");//clean up any possible incoming enum.ToString()
        var IsWindowsMedia = true;
        var fileName = string.Empty;
        fileName = soundbyte switch
        {
            SoundBytes.start => "chimes.wav",
            SoundBytes.end => "tada.wav",
            SoundBytes.alert => "ding.wav",
            SoundBytes.error => "Windows Hardware Fail.wav",
            SoundBytes.connect => "Windows Hardware Insert.wav",
            SoundBytes.disconnect => "Windows Hardware Remove.wav",
            _ => ((Func<string>)(() =>
            {
                IsWindowsMedia = false;
                return $"{soundbyte}.wav";
            }))()
        };
        var dir = Path.Combine(_localPath, $"temp/soundbytes/");
        if (!File.Exists(dir))
            Directory.CreateDirectory(dir);
        var path = Path.Combine(dir, fileName);
        if (IsWindowsMedia) path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "media", fileName);
        if (!File.Exists(path))
            await CreateWAV(soundbyte, path);

        using var audioFile = new AudioFileReader(path);
        using var outputDevice = new WaveOutEvent();
        outputDevice.Init(audioFile);
        outputDevice.Play();
        while (outputDevice.PlaybackState == PlaybackState.Playing)
            await Task.Delay(100);

        //none of the below appear to work - either due to lack of a PC speaker or 'No Sounds' scheme
        //Console.Beep();
        //SystemSounds.Beep.Play();
        //SystemSounds.Asterisk.Play();
        //SystemSounds.Beep.Play();
        //SystemSounds.Exclamation.Play();
        //SystemSounds.Hand.Play();
        //SystemSounds.Question.Play();
    }

    async Task CreateWAV(string soundbyte, string path)
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
