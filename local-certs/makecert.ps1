write-host "Creating .crt and .pfx to be used in emulator container"

dotnet dev-certs https -ep ./emulator.crt -p emulator

write-host "Created emulator.crt with password emulator"

dotnet dev-certs https -ep ./emulator.pfx -p emulator

write-host "Created emulator.pfx with password emulator"