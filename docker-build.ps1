# ./docker-build.ps1 run push

param ($run, $push, $dev)

start powershell -wait {./local-certs/makecert.ps1}

$tagName = "jamesgoulddev/azure-keyvault-emulator"
$version = "latest"

if($dev)
{
    $version = "dev-unstable"
}

write-host "Executing docker build with tag: $tagName and version: $version"

try { docker build --tag ${tagName}:${version} . }
catch { "Build failed" }

exit

if($run -and !$error)
{
    write-host "Running docker container, param run has value $run"

    docker run -p 80:8080 --name keyvault-emulator ${tagName}
}
else 
{
    if($error)
    {
        write-host "Failed to build image, exiting script."
        exit
    }
    
    write-host "Build complete, ready and available in your local container images."
}

if ($push -and !$error) 
{
    write-host "Pushing to public docker registry: $tagName with version: $version"

    $error.clear()
    
    try { docker push ${tagName}:${version} }
    catch { "Push failed" }
}

if($error) { "You do not have permissions to push to the registry, or are not logged in!" }