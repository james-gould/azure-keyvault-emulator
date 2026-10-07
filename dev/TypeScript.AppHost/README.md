# TypeScript hosting integration smoke test

This AppHost checks the emulator integration's generated TypeScript bindings.
Its `aspire.config.json` references the hosting `.csproj` by assembly name so
Aspire builds the source project without publishing a NuGet package.

The fixture targets Aspire SDK/CLI 13.6.0 and covers `addAzureKeyVaultEmulator`,
`runAsEmulator`, and all seven exported configuration fields. The hosting
library's framework and dependency versions are unchanged.

## Validate

With Aspire CLI 13.6.0, .NET 10 SDK, and a supported Node.js version installed,
run these commands from the repository root:

```bash
aspire restore --apphost dev/TypeScript.AppHost/apphost.mts --non-interactive
cd dev/TypeScript.AppHost
npx --no-install tsc --noEmit -p tsconfig.apphost.json
```

`aspire restore` builds the hosting project, generates the bindings, and
installs the npm dependencies. Do not edit or commit `.aspire/modules`.

To exercise the exports through the TypeScript-to-.NET runtime without
starting containers, run this command from the repository root:

```bash
aspire publish --apphost dev/TypeScript.AppHost/apphost.mts --list-steps --non-interactive
```

This executes the AppHost in publish mode and lists pipeline steps. It does
not deploy resources or publish a package.

The C# regression tests cover options conversion, export metadata,
configuration-section lookup, validation, and publish-mode resource behavior:

```bash
dotnet test test/AzureKeyVaultEmulator.Aspire.Hosting.Tests --configuration Release
```

## Running containers

Binding generation and the publish-mode preview do not require Docker or
certificate installation. Running the fixture's containers additionally
requires Docker and valid, trusted certificates, including an `emulator.pfx`
and `emulator.crt` in the AppHost's `certificates` directory for the
custom-certificate example. Normal emulator certificate-generation and
trust-store behavior applies to the other resources.
