echo Creating certificates using openssl

openssl req \
-x509 \
-newkey rsa:4096 \
-sha256 \
-days 3560 \
-nodes \
-keyout emulator.key \
-out emulator.crt \
-subj '/CN=localhost' \
-extensions san \
-config <( \
    echo '[req]'; \
    echo 'distinguished_name=req'; \
    echo '[san]'; \
    echo 'subjectAltName=DNS.1:localhost,DNS.2:emulator,DNS.3:localhost.vault.azure.net')

echo Exported emulator.key and emulator.crt. Now creating emulator.pfx

openssl pkcs12 -export -out emulator>.pfx \
-inkey emulator>.key \
-in emulator>.crt

echo PFX create, you must now install these as a Trusted Root CA Authority.