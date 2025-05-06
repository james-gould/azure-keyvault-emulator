#!/bin/sh
echo Creating certificates using openssl

CONFIG=$(cat <<EOF
[req]
distinguished_name = req_distinguished_name
x509_extensions = san
prompt = no

[req_distinguished_name]
CN = localhost

[san]
subjectAltName = DNS:localhost, DNS:emulator, DNS:localhost.vault.azure.net
EOF
)

openssl req \
-x509 \
-newkey rsa:4096 \
-passin pass:emulator \
-sha256 \
-days 3560 \
-nodes \
-keyout emulator.key \
-out emulator.crt \
-subj '/CN=localhost' \
-extensions san \
-config <( \
    echo -n "$CONFIG")

echo Exported emulator.key and emulator.crt. Now creating emulator.pfx

openssl pkcs12 -export -out emulator.pfx -inkey emulator.key -in emulator.crt -passin pass:emulator -passout pass:emulator -name "Azure Key Vault Emulator"

echo PFX created with password: emulator, you must now install these as a Trusted Root CA Authority.
