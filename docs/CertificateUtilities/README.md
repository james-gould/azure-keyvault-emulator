# Certificate Utilities for the Azure Key Vault Emulator

> [!NOTE]
> You will only have to do this once and it should take around 3 minutes.

The Azure Key Vault Client SDK requires `HTTPS` and trusted SSL for valid connections - breaking either of these conditions will cause all requests to fail at runtime.

The `AzureKeyVaultEmulator.Aspire.Hosting` library will generate and install these certificates for you. If you're not using Aspire, or wish to provide your own certificates, follow the instructions below to get started using the Emulator.

# Automated Certificate Creation

The setup process can be fully automated by using the installation script:

```
bash <(curl -fsSL https://github.com/james-gould/azure-keyvault-emulator/blob/master/docs/setup.sh)
```

> [!IMPORTANT]
> If you're using **Windows**, use `Git Bash` to execute the setup script.

## Creating valid certificates

### With dotnet

If you have `.NET` installed you will have access to the CLI command `dotnet dev-certs` which creates valid, `localhost` SSL certificates. 

- Windows: [dotnet/create-certs.ps1](dotnet/create-certs.ps1)
- Linux/Mac: [dotnet/create-certs.sh](dotnet/create-certs.sh)

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

With `openssl` available on your command line, create `emulator.crt` you can now run [openssl/create-certs.sh](openssl/create-certs.sh).

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

You can now follow the [local system configuration](../CONFIG.md) to manually set these up for the Emulator. 

Don't worry, it's a very quick process.