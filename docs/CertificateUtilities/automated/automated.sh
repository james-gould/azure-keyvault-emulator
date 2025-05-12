#!/bin/bash

CERT_SOURCE="$HOME/certs"
VOLUME_NAME="kve-certs"

# Create certificates below, copy in scripts from /dotnet/ and /openssl/
#
#

docker volume create "$VOLUME_NAME" > /dev/null

docker run --rm \
  -v "$CERT_SOURCE":/from \
  -v "$VOLUME_NAME":/to \
  alpine sh -c "cp -r /from/* /to/"

docker run --rm -v "$VOLUME_NAME":/certs:ro jamesgoulddev/azure-keyvault-emulator:latest
