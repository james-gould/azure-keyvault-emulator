write-host "Creating .crt and .key for emulator.vault.azure.net"

openssl req `
    -x509 `
    -newkey `
    rsa:4096 `
    -sha256 `
    -days 3560 `
    -nodes `
    -keyout emulator.key `
    -out emulator.crt `
    -subj '/CN=emulator.vault.azure.net' `
    -extensions san `
    -config config.txt

write-host "Creating PFX certificate to be used in Docker container"

write-host "You will be prompted to create a password, this is required inside the Dockerfile!"

openssl pkcs12 -export -out emulator.pfx -inkey emulator.key -in emulator.crt