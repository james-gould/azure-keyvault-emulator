
$certSource = "C:\Users\James\keyvaultemulator\certs"
$volumeName = "kve-certs"

# Create certificates below, copy in scripts from /dotnet/ and /openssl/
#
#

docker volume create $volumeName | Out-Null

docker run --rm `
  -v "${certSource}:/from" `
  -v "${volumeName}:/to" `
  mcr.microsoft.com/windows/nanoserver `
  powershell -Command "Copy-Item -Path C:\from\* -Destination C:\to\ -Recurse"

docker run --rm -v "${volumeName}:/certs:ro" jamesgoulddev/azure-keyvault-emulator:latest
