namespace CasCap.Abstractions;

/// <summary>Abstraction for Azure Cognitive Services text-to-speech synthesis and speech-to-text recognition.</summary>
/// <remarks>See <see href="https://github.com/Azure-Samples/cognitive-services-speech-sdk/blob/master/samples/csharp/sharedcontent/console/speech_synthesis_samples.cs" />.</remarks>
public interface ISpeechService
{
    /// <summary>Synthesizes <paramref name="soundByte"/> as speech and writes the result to a WAV file at <paramref name="path"/>.</summary>
    Task CreateWAV(string soundByte, string path);

    /// <summary>Recognizes speech from a WAV file at <paramref name="path"/> and returns the transcribed text.</summary>
    /// <returns>The recognized text, or <see langword="null"/> if recognition failed or no speech was detected.</returns>
    Task<string?> RecognizeFromWAV(string path);

    /// <summary>Transcribes an audio stream through the fast transcription API and returns the transcribed text.</summary>
    /// <param name="audio">The audio to transcribe, in any format the service accepts. Read from the current position.</param>
    /// <param name="locales">
    /// Candidate locales such as <c>en-GB</c>. Supply one when it is known, to improve accuracy and latency.
    /// When <see langword="null"/> or empty the multilingual model identifies the language itself.
    /// </param>
    /// <param name="cancellationToken">Cancels the request.</param>
    /// <returns>The transcribed text, or <see langword="null"/> when no speech was recognized.</returns>
    /// <remarks>
    /// Unlike <see cref="RecognizeFromWAV"/> this accepts a stream, so no temporary file is required, and it
    /// transcribes the whole recording rather than stopping at the first utterance.
    /// </remarks>
    Task<string?> TranscribeAsync(Stream audio, IReadOnlyList<string>? locales = null,
        CancellationToken cancellationToken = default);

    /// <summary>Recognizes speech from the default microphone input and returns the transcribed text.</summary>
    /// <returns>The recognized text, or <see langword="null"/> if recognition failed or no speech was detected.</returns>
    Task<string?> RecognizeFromMicrophone();
}
