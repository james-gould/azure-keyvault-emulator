write-host "Creating .crt and .key for emulator.vault.azure.net"

openssl genpkey -algorithm RSA -out emulator.key -aes256

# openssl req `
#     -x509 `
#     -newkey rsa:4096 `
#     -sha256 `
#     -days 3560 `
#     -nodes `
#     -keyout emulator.key `
#     -out emulator.crt `
#     -subj '/CN=emulator.azure.vault.net' `
#     -extensions san `
#     -config config.txt

openssl req -new -key emulator.key -out emulator.csr -config config.txt

openssl x509 -req -in emulator.csr -signkey emulator.key -out emulator.crt -days 3650 -extensions v3_req -extfile config.txt



write-host "Creating PFX certificate to be used in Docker container"

write-host "You will be prompted to create a password, this is required inside the Dockerfile!"

# openssl pkcs12 -export -out emulator.pfx -inkey emulator.key -in emulator.crt

openssl pkcs12 -export -out emulator.pfx -inkey emulator.key -in emulator.crt