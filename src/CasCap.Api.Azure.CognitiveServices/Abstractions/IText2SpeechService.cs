namespace CasCap.Abstractions;

/// <summary>
/// https://github.com/Azure-Samples/cognitive-services-speech-sdk/blob/master/samples/csharp/sharedcontent/console/speech_synthesis_samples.cs
/// </summary>
public interface IText2SpeechService
{
    Task CreateWAV(string soundByte, string path);
}
