namespace CasCap.Abstractions;

/// <summary>Abstraction for Azure Cognitive Services text-to-speech synthesis.</summary>
/// <remarks>See <see href="https://github.com/Azure-Samples/cognitive-services-speech-sdk/blob/master/samples/csharp/sharedcontent/console/speech_synthesis_samples.cs" />.</remarks>
public interface IText2SpeechService
{
    /// <summary>Synthesizes <paramref name="soundByte"/> as speech and writes the result to a WAV file at <paramref name="path"/>.</summary>
    Task CreateWAV(string soundByte, string path);
}
