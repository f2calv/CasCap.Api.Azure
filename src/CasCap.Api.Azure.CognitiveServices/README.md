# CasCap.Api.Azure.CognitiveServices

Helper library for Azure Cognitive Services. Provides text-to-speech synthesis and speech-to-text recognition via the Azure Speech SDK.

## Services / Extensions

| Type | Name | Description |
|------|------|-------------|
| Interface | `ISpeechService` | Abstraction for speech synthesis (TTS) and speech recognition (STT). |
| Service | `SpeechService` | Implements `ISpeechService` using `Microsoft.CognitiveServices.Speech`. Supports subscription key and `TokenCredential` authentication. |

### Key Methods

- `CreateWAV(string soundByte, string path)` — Synthesizes text to a WAV file.
- `RecognizeFromWAV(string path)` — Transcribes speech from a WAV file.
- `RecognizeFromMicrophone()` — Transcribes speech from the default microphone.

## Configuration

| Class | Section | Properties |
|-------|---------|------------|
| `CognitiveServicesConfig` | `CasCap:CognitiveServicesConfig` | `SubscriptionKey` (required) |

## Dependencies

### NuGet Packages

| Package | Description |
|---------|-------------|
| `Microsoft.CognitiveServices.Speech` | Azure Cognitive Services Speech SDK |
| `CasCap.Common.Logging` | Shared logging infrastructure |
| `CasCap.Common.Extensions` | Common extension methods |

### Project References

None.
