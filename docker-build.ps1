param ($run, $push)

$tagName = "jamesgoulddev/azure-keyvault-emulator"
$version = "latest"

write-host "Executing docker build with tag: $tagName"

docker build --tag ${tagName}:${version} .

if($run -eq "y" -or $run -eq "Y")
{
    write-host "Running docker container, param run has value $run"

    docker run -p 80:8080 --name keyvault-emulator ${tagName}
} 
else if ($push -eq "y" -or $push -eq "Y") 
{
    write-host "Pushing to public docker registry: $tagName with version: $version"

    $error.clear()
    
    try { docker push $tagName:$version }
    catch { "Push failed" }
}
else 
{
    write-host "Build complete and ready, check Docker Desktop -> Images"
}

if($error) { "You do not have permissions to push to the registry, or are not logged in!" }