# Certificate Utilities for the Azure Key Vault Emulator

> [!NOTE]
> You will only have to do this once and it should take around 3 minutes.

The Azure Key Vault Client SDK requires `HTTPS` and trusted SSL for valid connections - breaking either of these conditions will cause all requests to fail at runtime.

To support this the `AzureKeyVaultEmulator.Aspire.Hosting` library will generate and install these certificates for you in the background, but that requires:

- Running your application with Administrator priviledges when you need the certificates installed.
- Allowing a 3rd party application to install Trusted Root CA certificates on your machine.

For obvious reasons one or both of these conditions may be unacceptable for you, your IT department, or anything else. Should that be the case you **are** still able to use the Emulator, but you will need to provide these certificates and have them as a Trusted Root CA on your local machine beforehand.

## Creating valid certificates

### With dotnet

If you have `.NET` installed you will have access to the CLI command `dotnet dev-certs` which creates valid, `localhost` SSL certificates. 

- Windows: [dotnet/create-certs.ps1](https://github.com/james-gould/azure-keyvault-emulator/blob/development/dotnet/create-certs.ps1)
- Linux/Mac: [dotnet/create-certs.sh](https://github.com/james-gould/azure-keyvault-emulator/blob/development/otnet/create-certs.sh)

If you don't want to run the script, and would rather execute the commands yourself, simply copy the (very brief) commands from the files into your local terminal.

### Without dotnet

You can generate self-signed SSL certificates with `openssl`, a free utility that handily comes packaged with `git`. 

If you're on Windows and `openssl` isn't on your `PATH`:

- Navigate to `C:\Program Files\Git\usr\bin` and verify that `openssl.exe` is present. 
    - Your local install directory may be different, but the `openssl.exe` will be under `user\bin\`
- Edit your environment variables and add the folder path to your `PATH` variable.
- Restart your terminal.

If you're on Linux/MacOS and `openssl` isn't available simply run:

`sudo apt-get install libssl-dev`.

With `openssl` available on your command line, create `emulator.crt` you can now run [openssl/create-certs.sh](https://github.com/james-gould/azure-keyvault-emulator/blob/development/openssl/create-certs.sh).

If you don't want to run the script, and would rather execute the commands yourself, simply copy the (very brief) commands from the files into your local terminal.

### Installation

Now you must install a certificate as a **Trusted Root CA**:

- On Windows install `emulator.pfx`.
    - Right click -> Install and follow the installation wizard.
- Otherwise install `emulator.crt`.
    - `Linux`: Run `cp emulator.crt /usr/local/share/ca-certificates/emulator.crt && update-ca-certificates`
    - MacOS: Run `sudo security add-trusted-cert -d -r trustRoot -k /Library/Keychains/System.keychain <path to your certificates>`

### Known limitations

A limitation of the Emulator is the inability to define the name and password of the SSL certificates. This is being investigated but currently they must **both** be `emulator`, ie you must generate `emulator.pfx` with the password `emulator` and `emulator.crt`. 

## Configure the Emulator to use your certificates

You can now follow the [local system configuration](https://github.com/james-gould/azure-keyvault-emulator/blob/development/CONFIG.md) to manually set these up for the Emulator. 

Don't worry, it's a very quick process.