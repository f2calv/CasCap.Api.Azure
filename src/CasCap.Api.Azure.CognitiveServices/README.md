# CasCap.Api.Azure.CognitiveServices

Helper library for Azure Cognitive Services. Provides text-to-speech synthesis and speech-to-text recognition via the Azure Speech SDK.

## Installation

```bash
dotnet add package CasCap.Api.Azure.CognitiveServices
```

## Services / Extensions

| Type | Name | Description |
| --- | --- | --- |
| Interface | `ISpeechService` | Abstraction for speech synthesis (TTS) and speech recognition (STT). |
| Service | `SpeechService` | Implements `ISpeechService` using `Microsoft.CognitiveServices.Speech` for synthesis and recognition, and `Azure.AI.Speech.Transcription` for fast transcription. Supports subscription key and `TokenCredential` authentication. |

### Key Methods

- `CreateWAV(string soundByte, string path)` — Synthesizes text to a WAV file.
- `SynthesizeAsync(string text, string? voice, CancellationToken)` — Synthesizes text and returns Ogg Opus audio, or `null` when synthesis produced nothing.
- `TranscribeAsync(Stream audio, IReadOnlyList<string>? locales, CancellationToken)` — Transcribes an audio stream through the fast transcription API.
- `RecognizeFromWAV(string path)` — Transcribes speech from a WAV file.
- `RecognizeFromMicrophone()` — Transcribes speech from the default microphone.

Prefer `SynthesizeAsync` over `CreateWAV` for synthesis. It returns the bytes, so no temporary file is
needed, and it emits Opus in an Ogg container, which is the format messaging clients expect for voice
notes. Pass `voice` as a full name such as `en-GB-SoniaNeural`; omit it to use the resource default.

Prefer `TranscribeAsync` for recorded audio. It takes a stream, so no temporary file is needed, and it
transcribes the whole recording, where the `Recognize*` methods stop at the first utterance and so
silently truncate anything longer than roughly fifteen seconds. It requires the endpoint and
credential constructor, because the subscription-key constructor does not carry the resource endpoint
that the fast transcription API needs, and the credential needs the `Cognitive Services Speech User`
role on the resource.

## Configuration

| Class | Section | Properties |
| --- | --- | --- |
| `CognitiveServicesConfig` | `CasCap:CognitiveServicesConfig` | `SubscriptionKey` (required) |

## Dependencies

### NuGet Packages

| Package |
| --- |
| [Azure.AI.Speech.Transcription](https://www.nuget.org/packages/azure.ai.speech.transcription) |
| [Microsoft.CognitiveServices.Speech](https://www.nuget.org/packages/microsoft.cognitiveservices.speech) |
| [CasCap.Common.Logging](https://www.nuget.org/packages/cascap.common.logging) |
| [CasCap.Common.Extensions](https://www.nuget.org/packages/cascap.common.extensions) |

### Project References

None.
