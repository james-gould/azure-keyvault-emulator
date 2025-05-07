param (
    [switch]$run,
    [switch]$push,
    [switch]$dev
)

function ThrowIfFailed($message) {
    if ($LASTEXITCODE -ne 0) {
        Write-Host $message
        exit 1
    }
}

$tagName = "jamesgoulddev/azure-keyvault-emulator"
$version = if ($dev -Or $run) { "dev-unstable" } else { "latest" }
$localName = "keyvault-emulator"
$localPath = "C:/users/James/keyvaultemulator/certs"

Write-Host "Executing docker build with tag: $tagName and version: $version"

docker build --tag "${tagName}:${version}" .
ThrowIfFailed "Build failed. Exiting."

Write-Host "Build succeeded."

if ($run) {
    Write-Host "Killing any existing container with name: $localName"
    docker kill $localName 2>$null
    docker rm $localName 2>$null

    Write-Host "Running docker container in detached mode with name: $localName"
    docker run -d -p 4997:4997 -v "${localPath}:/certs:ro" --name $localName "${tagName}:${version}"
    ThrowIfFailed "Container failed to start. Exiting."
}

if ($push) {
    if ($version -eq "latest") {
        $confirmation = Read-Host "You are about to push the 'latest' tag. Continue? (y/N)"
        if ($confirmation -notin @("y", "Y")) {
            Write-Host "Push cancelled by user."
            exit 0
        }
    }

    Write-Host "Pushing image to Docker Hub ${tagName}:${version}"
    docker push "${tagName}:${version}"
    ThrowIfFailed "Push failed. Check Docker login or repo permissions."
    Write-Host "Push succeeded."
}