echo Creating .crt and .pfx to be used in emulator container

dotnet dev-certs https -ep ./emulator.crt -p emulator -q

echo Created emulator.crt with password emulator

dotnet dev-certs https -ep ./emulator.pfx -p emulator -q

echo Created emulator.pfx with password emulator. You must now install these certificates locally as a Trusted Root CA Authority.