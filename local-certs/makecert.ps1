write-host "Creating .crt and .pfx to be used in emulator container"

dotnet dev-certs https -ep local-certs/emulator.crt -p emulator -q

write-host "Created emulator.crt with password emulator"

dotnet dev-certs https -ep local-certs/emulator.pfx -p emulator -q

write-host "Created emulator.pfx with password emulator"