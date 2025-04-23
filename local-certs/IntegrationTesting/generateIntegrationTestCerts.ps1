# write-host "Generating new .crt and .key for integration test certificates"

openssl req -x509 -sha256 -newkey rsa:2048 -keyout integration.key -out integration.crt

# write-host "Generate PKCS12 from integration host certificate inputs"

openssl pkcs12 -export -in integration.crt -inkey integration.key -passin pass:integration -out integration.pfx

# write-host "Exported integration.pfx with password: integration"