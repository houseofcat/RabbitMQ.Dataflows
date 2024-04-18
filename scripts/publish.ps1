$scriptDirectory = Split-Path -Parent $MyInvocation.MyCommand.Path
$outputBasePath = Join-Path $scriptDirectory "../PublishedApp"

$projectPaths = @(
    "src\HouseofCat.Compression\HouseofCat.Compression.csproj",
    "src\HouseofCat.Data\HouseofCat.Data.csproj",
    "src\HouseofCat.Dataflows\HouseofCat.Dataflows.csproj",
    "src\HouseofCat.Encryption\HouseofCat.Encryption.csproj",
    "src\HouseofCat.Hashing\HouseofCat.Hashing.csproj",
    "src\HouseofCat.Metrics\HouseofCat.Metrics.csproj",
    "src\HouseofCat.RabbitMQ\HouseofCat.RabbitMQ.csproj",
    "src\HouseofCat.Serialization\HouseofCat.Serialization.csproj",
    "src\HouseofCat.Utilities\HouseofCat.Utilities.csproj"
)


if ((Test-Path $outputBasePath)) {
    Remove-Item -Recurse -Force $outputBasePath
}


New-Item -ItemType Directory -Path $outputBasePath | Out-Null


foreach ($relativeProjectPath in $projectPaths) {
    $projectPath = Join-Path $scriptDirectory "../$relativeProjectPath"
    $projectName = [System.IO.Path]::GetFileNameWithoutExtension($relativeProjectPath)
    $outputPath = Join-Path $outputBasePath $projectName

    Write-Host "Building and packing $projectName..."
    
    New-Item -ItemType Directory -Path $outputPath | Out-Null
    
    dotnet build $projectPath --configuration Release --output $outputPath
    
    Write-Host "Done packing $projectName!"
}

Write-Host "All projects packed!"