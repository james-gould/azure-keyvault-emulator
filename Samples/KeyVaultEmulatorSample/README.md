# Overview

This is a sample application which exposes the Azure Key Vault Emulator to a WebAPI using `.NET Aspire`.

The solution includes:

- `AppHost` which orchestrates our dev-time infrastructure
- `ServiceDefaults` which configures `OpenTelemetry` and a bunch of useful features.
- `WebApiWithEmulator` which is a simple `.NET 9 WebApi` project that consumes and uses the emulated key vault resource.

No configuration is needed to run the solution, just clone it down and `F5`!