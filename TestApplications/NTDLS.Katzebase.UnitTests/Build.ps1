param(
    $buildConfig = "release"
)
write-host ("[Build] Current path: " + (pwd).path)


dotnet build --no-dependencies -c $buildConfig 

write-host (get-date)