# CasCap.Api.Azure.CognitiveServices

Azure Speech helpers for .NET applications that synthesize audio or transcribe recordings.
`SpeechService` implements
[`ISpeechService`](https://github.com/f2calv/CasCap.Api.Azure/blob/eaaffef87e366fa62ddc5328dd6f71add9b6397c/src/CasCap.Api.Azure.CognitiveServices/Abstractions/ISpeechService.cs)
using the Azure Speech SDK and fast transcription client.

## Installation

Targets `net8.0`, `net9.0` and `net10.0`.

```powershell
dotnet add package CasCap.Api.Azure.CognitiveServices
```

## Usage

Provide a Speech resource endpoint and an Azure `TokenCredential` with resource access:

```csharp
using CasCap.Services;

var speech = new SpeechService(speechEndpoint, credential);
byte[]? audio = await speech.SynthesizeAsync(
    "Hello world", voice: "en-GB-SoniaNeural", cancellationToken: cancellationToken);
```

`SynthesizeAsync` returns Ogg Opus bytes (`Ogg48Khz16BitMonoOpus`), or `null` if synthesis produces no
audio. Omit `voice` to use the SDK default. The cancellation parameter is not forwarded to the
underlying synthesis call, so do not assume it cancels speech generation.

For a recording, prefer the whole-file fast transcription path:

```csharp
using var recording = File.OpenRead(audioPath);
string? transcript = await speech.TranscribeAsync(
    recording, locales: ["en-GB"], cancellationToken: cancellationToken);
```

`TranscribeAsync` requires the endpoint-and-credential constructor. A subscription-key constructor
also exists (`new SpeechService(subscriptionKey, region)`), with region defaulting to `westeurope`,
but it cannot be used for fast transcription. The wrapper returns the first combined phrase's text,
or `null` for empty text; it does not expose the service's full phrase/channel metadata.

`CreateWAV(text, path)` writes a WAV file. `RecognizeFromWAV(path)` and
`RecognizeFromMicrophone()` recognize a **single utterance**, not an entire recording; microphone
recognition also requires a working audio input device.

## Service Requirements

Use an Azure Speech resource, a supported region and audio format, and the native prerequisites for
the Speech SDK on your platform. With Microsoft Entra authentication, assign the caller the
`Cognitive Services Speech User` role as appropriate.

Fast transcription availability, pricing tiers and file limits are controlled by Azure, not this
wrapper. Check the [fast transcription guide](https://learn.microsoft.com/en-us/azure/ai-services/speech-service/fast-transcription-create)
and [Speech SDK installation requirements](https://learn.microsoft.com/en-us/azure/ai-services/speech-service/quickstarts/setup-platform).
Service guidance checked **20 October 2026**; no live service call is implied.

## Configuration Examples

`CognitiveServicesConfig` exposes a required subscription key under
`CasCap:CognitiveServicesConfig`. There is no registration helper that binds it or constructs
`SpeechService`; the consuming application owns both steps.

```json
{
  "CasCap": {
    "CognitiveServicesConfig": {
      "SubscriptionKey": "your-subscription-key"
    }
  }
}
```

Store the actual key in user secrets or a secret-backed configuration provider. Endpoint, credential
and region are constructor inputs, not properties of this configuration model.

## Documentation

- [Speech API contract](https://github.com/f2calv/CasCap.Api.Azure/blob/main/src/CasCap.Api.Azure.CognitiveServices/Abstractions/ISpeechService.cs)
- [Package catalogue and license](https://github.com/f2calv/CasCap.Api.Azure#packages)
