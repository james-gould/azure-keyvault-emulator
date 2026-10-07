import { ContainerLifetime, createBuilder } from './.aspire/modules/aspire.mjs';

const builder = await createBuilder();

await builder.addAzureKeyVaultEmulator('direct');
await builder.addAzureKeyVaultEmulator('persistent', {
    lifetime: ContainerLifetime.Persistent,
    port: 4997,
    persist: true,
    useDotnetDevCerts: true,
});
await builder.addAzureKeyVault('redirected').runAsEmulator();
await builder.addAzureKeyVault('configured').runAsEmulator({
    options: {
        port: 4998,
        persist: false,
        localCertificatePath: 'certificates',
        loadCertificatesIntoTrustStore: false,
        shouldGenerateCertificates: false,
        useDotnetDevCerts: false,
    },
    configSectionName: 'emulator',
});

await builder.build().run();