# CasCap.Api.Azure.CognitiveServices

Azure Cognitive Services helpers for .NET applications that synthesize audio, transcribe recordings
or extract bounded PDF layout Markdown.
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

## Document Layout

The source checkout adds
[`AzDocumentLayoutClient`](https://github.com/f2calv/CasCap.Api.Azure/blob/main/src/CasCap.Api.Azure.CognitiveServices/Services/AzDocumentLayoutClient.cs),
with [`DocumentLayoutOptions`](https://github.com/f2calv/CasCap.Api.Azure/blob/main/src/CasCap.Api.Azure.CognitiveServices/Models/DocumentLayoutOptions.cs)
and [`DocumentLayoutPollResult`](https://github.com/f2calv/CasCap.Api.Azure/blob/main/src/CasCap.Api.Azure.CognitiveServices/Models/DocumentLayoutPollResult.cs).
These new source links and APIs require publication before they are available from the default branch
and a released package.

Supply a caller-owned `HttpClient` whose `BaseAddress` is a canonical, credential-free HTTPS root
ending in `/`, on port 443. Authentication belongs in its handler. **Disable redirects, POST retries,
hedging and authentication replay in the handler pipeline.** The transport makes one `SendAsync` call
per public method; it cannot override replay or logging added by a caller's handler.

```csharp
using CasCap.Models;
using CasCap.Services;

// httpClient is provided by the host, with authentication and no mutation replay.
var layout = new AzDocumentLayoutClient(httpClient, new DocumentLayoutOptions());
Uri receipt = await layout.SubmitAsync(pdfBytes, [1, 2], cancellationToken);
// Persist the receipt before scheduling subsequent work.
DocumentLayoutPollResult observation =
    await layout.GetResultAsync(receipt, [1, 2], cancellationToken);
```

`SubmitAsync` sends exactly one JSON `base64Source` POST for `prebuilt-layout`, API `2024-11-30`,
Markdown and UTF-16 code-unit indexing. Only HTTP 202 with one same-authority operation receipt is
accepted. The receipt must contain the exact model/result/GUID path and API-version query.
**Submission errors, timeouts and cancellation may leave an unknown remote outcome; do not blindly
resubmit.** Retain that ambiguity in the caller's durable state.

`GetResultAsync` performs one GET against a saved receipt, even on a newly constructed client.
It never submits, sleeps or loops. `notStarted` and `running` normalize to `Running`; `failed` and
`canceled` normalize to `Failed`. Only `Succeeded` includes complete Markdown and page projections.
The caller owns scheduling and any GET retry budget. `RetryAfter` is nullable; delta values are
preserved, and absolute dates use the configured clock and clamp past dates to zero.

Successful results require exact version/model/format metadata and the unique, exact requested page
set. Spans are bounded UTF-16 ranges that cannot split surrogate pairs, duplicate or overlap within
or across pages. Page fragments are concatenated in offset order without inserting text. Empty
Markdown and empty page-span arrays are permitted for blank pages. Malformed JSON, duplicate JSON
properties, unknown statuses and invalid receipts/results raise redacted `HttpRequestException`;
HTTP failures retain the status code but not the response body or transport exception details.
Caller input errors use argument exceptions; requested cancellation remains cancellation.

No PDF content, authorization headers, operation identifiers or service error bodies are logged by
this transport. Host HTTP diagnostics must independently redact sensitive data. It does not configure
credentials, deadlines, service quotas, quality policy or semantic acceptance of extracted text.

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

Layout options are direct constructor inputs, not part of `CognitiveServicesConfig`. Defaults and
maximum supported bounds are 25 MiB PDF bytes, 16 MiB response bytes, and 2000 selected pages/page
numbers. Each configured bound must be positive; callers can lower it. PDF signature and byte limits
are checked before submission, and result bytes are limited while streaming, including when no
`Content-Length` is present.

```csharp
var defaults = new DocumentLayoutOptions();
var bounded = new DocumentLayoutOptions
{
    MaximumPdfBytes = 25 * 1024 * 1024,
    MaximumResponseBytes = 16 * 1024 * 1024,
    MaximumPages = 2000,
    TimeProvider = TimeProvider.System,
};
```

Pass a controlled `TimeProvider` for deterministic absolute `Retry-After` interpretation. Supply a
cancellation token covering the complete response-body read; the host owns total deadlines.

## Documentation

- [Speech API contract](https://github.com/f2calv/CasCap.Api.Azure/blob/main/src/CasCap.Api.Azure.CognitiveServices/Abstractions/ISpeechService.cs)
- [Package catalogue and license](https://github.com/f2calv/CasCap.Api.Azure#packages)
- [Offline layout tests](https://github.com/f2calv/CasCap.Api.Azure/blob/main/src/CasCap.Api.Azure.CognitiveServices.Tests/README.md)
