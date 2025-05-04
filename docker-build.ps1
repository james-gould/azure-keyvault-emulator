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
$version = if ($dev) { "dev-unstable" } else { "latest" }

Write-Host "Executing docker build with tag: $tagName and version: $version"

docker build --tag "${tagName}:${version}" .
ThrowIfFailed "Build failed. Exiting."

Write-Host "Build succeeded."

if ($run) {
    Write-Host "Running docker container..."
    docker run -p 80:8080 --name keyvault-emulator "${tagName}:${version}"
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