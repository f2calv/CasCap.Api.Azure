# CasCap.Api.Azure.EventGrid

An Azure Event Grid dependency package targeting `net8.0`, `net9.0` and `net10.0`.
It currently contains **no public service classes, interfaces, configuration or registration API**.

## Installation

```powershell
dotnet add package CasCap.Api.Azure.EventGrid
```

This package references `Azure.Messaging.EventGrid` and shared logging/extension dependencies.
Installation does not provide a CasCap publisher or subscriber. For an Event Grid implementation,
use the Azure SDK directly and follow its
[Event Grid client guidance](https://learn.microsoft.com/en-us/dotnet/api/overview/azure/messaging.eventgrid-readme).

## Documentation

- [Project contents](https://github.com/f2calv/CasCap.Api.Azure/tree/main/src/CasCap.Api.Azure.EventGrid)
- [Package catalogue and license](https://github.com/f2calv/CasCap.Api.Azure#packages)
