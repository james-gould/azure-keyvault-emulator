#!/bin/bash

set -e

function dotnetCerts() {
  echo Creating .crt and .pfx to be used in emulator container

  dotnet dev-certs https -ep ./emulator.crt -p emulator -q

  echo Created emulator.crt with password emulator

  dotnet dev-certs https -ep ./emulator.pfx -p emulator -q

  installCerts
}

function opensslCerts() {
temp_config=$(mktemp)
cat > "$temp_config" <<EOF
[req]
distinguished_name = req_distinguished_name
x509_extensions = san
prompt = no

[req_distinguished_name]
CN = localhost

[san]
subjectAltName = DNS:localhost, DNS:emulator, DNS:localhost.vault.azure.net
EOF

openssl req \
  -x509 \
  -newkey rsa:4096 \
  -passin pass:emulator \
  -sha256 \
  -days 3560 \
  -nodes \
  -keyout emulator.key \
  -out emulator.crt \
  -extensions san \
  -config "$temp_config"

rm -f "$temp_config"


  echo "Exported emulator.key and emulator.crt. Now creating emulator.pfx"

  openssl pkcs12 -export \
    -out emulator.pfx \
    -inkey emulator.key \
    -in emulator.crt \
    -name "Azure Key Vault Emulator" \
    -passin pass:emulator \
    -passout pass:emulator \
    -name "Azure Key Vault Emulator"

  echo "PFX created with password: emulator"

  installCerts
}

function installCerts() {
  echo "Installing certificate to trusted root store"

  OS=$(uname)

  if [ "$OS" = "Linux" ]; then
    sudo cp emulator.crt /usr/local/share/ca-certificates/emulator.crt
    sudo update-ca-certificates
  elif [ "$OS" = "Darwin" ]; then
    sudo security add-trusted-cert -d -r trustRoot -k /Library/Keychains/System.keychain emulator.crt
  elif [[ "$OS" == MINGW64_NT* || "$OS" == MSYS_NT* || "$OS" == CYGWIN_NT* ]]; then
    certutil -user -addstore "Root" emulator.crt
  else
    echo "Unsupported OS for automatic trust store installation: $OS"
    exit 1
  fi

  echo "Certificate installed to trusted root store."
}

# Fix Git Bash/WSL2 changing C:/ to /c/ which breaks docker volume binds...
normalize_path() {
  local input_path="$1"
  if [[ "$(uname -s)" == MINGW* || "$(uname -s)" == MSYS* || "$(uname -s)" == CYGWIN* ]]; then
    cygpath -w "$input_path"
  else
    echo "$input_path"
  fi
}

# If running Linux, assert script execution as root
if [ "$(uname)" = "Linux" ] && [ "$(id -u)" -ne 0 ]; then
    echo 'This script must be run with sudo/as root.' >&2
    exit 1
fi


prompt="Choose an option:"

pHome=$(normalize_path "$HOME")

certSource="$pHome/certs"
volumeName="kve-certs"

# after all, why shouldn't i?
echo -e "  _______________________________________\n/ Welcome to the Azure Key Vault Emulator \\\\\n\\\\ setup guide, let's get started.         /\n  ---------------------------------------\n         \\   ^__^\n          \\  (oo)\\_______\n             (__)\\       )\\/\\\\\n                 ||----w |\n                 ||     ||\n\n"

echo -e "Which directory do you want to store the SSL certificates in?\n\n"
echo "1) $certSource"
echo "2) I want to customise it."
echo "3) Cancel"

echo ""

read -rp "$prompt [1-3]: " dirOptionSelected

echo ""

case "$dirOptionSelected" in
  1)
    echo "Certificates will be stored at $certSource"

    # create directory if it doesn't exist, only for default option.
    mkdir -p $certSource 
    ;;
  2)
    read -rp "Provide an absolute path to the directory you'd like to use: " certSource
    certSource=$(normalize_path "$certSource")
    ;;
  3)
    echo "User cancelled script run, exiting."
    exit 0
    ;;
  *)
    echo "Invalid option, exiting."
    exit 1
    ;;
esac

if [[ ! -d "$certSource" ]]; then
  echo "Provided path $certSource does not exist, script exiting."
  exit 1
fi

echo ""

echo -e "How do you want to generate the certificates?\n"
echo "1) openssl"
echo "2) dotnet dev-certs"
echo -e "3) Cancel\n"

echo ""

read -rp "$prompt [1-3]: " certGenOptionSelected

echo ""

# move to the directory so we can write the files
# bit lazy but saves passing variables around
cd $certSource

case "$certGenOptionSelected" in
  1)
    echo "Using openssl"
    opensslCerts
    ;;
  2)
    echo "Using dotnet dev-certs"
    dotnetCerts
    ;;
  3)
    echo "Cancelled."
    exit 0
    ;;
  *)
    echo "Invalid option, exiting."
    exit 1
    ;;
esac

echo ""

echo -e "Do you want to run the Azure Key Vault Emulator now?\n"
echo "1) Yes (with database)"
echo "2) Yes (no database)"
echo "3) No"

echo ""

read -rp "$prompt [1-3]: " certGenOptionSelected

echo ""

case "$certGenOptionSelected" in
  1)
    echo -e "Starting the Azure Key Vault Emulator in detached mode with a database.\n"

    echo -e "Running command: docker run -d -p 4997:4997 -v $certSource:/certs -e Persist=true jamesgoulddev/azure-keyvault-emulator:latest\n"

    docker run -d -p 4997:4997 -v $certSource:/certs -e Persist=true jamesgoulddev/azure-keyvault-emulator:latest
    ;;
  2)
    echo -e "Starting the Azure Key Vault Emulator in detached mode without a database.\n"

    echo -e "Running command: docker run -d -p 4997:4997 -v $certSource:/certs jamesgoulddev/azure-keyvault-emulator:latest\n
    "
    docker run -d -p 4997:4997 -v $certSource:/certs jamesgoulddev/azure-keyvault-emulator:latest
    ;;
  3)
    echo -e "Installation completed, you can run the Emulator using the following command:\n"

    echo -e "docker run -d -p 4997:4997 -v $certSource:/certs -e Persist=true jamesgoulddev/azure-keyvault-emulator:latest\n"

    echo -e "If you want to opt out of using a database, and destroy all data between sessions, omit -e Persist=true."
    ;;
  *)
    echo ""
    echo -e "  _________________________\n< Think you're funny, do ya? >\n  -------------------------\n         \\   ^__^\n          \\  (oo)\\_______\n             (__)\\       )\\/\\\\\n                 ||----w |\n                 ||     ||"
    exit 0
    ;;
esac