namespace CasCap.Abstractions;

/// <summary>
/// Provides text-to-speech synthesis capabilities using Azure Cognitive Services.
/// </summary>
/// <seealso href="https://github.com/Azure-Samples/cognitive-services-speech-sdk/blob/master/samples/csharp/sharedcontent/console/speech_synthesis_samples.cs"/>
public interface IText2SpeechService
{
    /// <summary>
    /// Synthesizes the specified text to speech and writes the output to a WAV file.
    /// </summary>
    /// <param name="soundByte">The text to synthesize into speech.</param>
    /// <param name="path">The file system path where the WAV file will be written.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task CreateWAV(string soundByte, string path);
}
