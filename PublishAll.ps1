# Set Working Directory
Split-Path $MyInvocation.MyCommand.Path | Push-Location
[Environment]::CurrentDirectory = $PWD

$env:RELOADEDIIMODS = "./Build1"
./Publish.ps1 -ProjectPath "BGME.Framework/BGME.Framework.csproj" `
              -PackageName "BGME.Framework" `
              -PublishOutputDir "Publish/ToUpload/Framework" `

$env:RELOADEDIIMODS = "./Build2"
./Publish.ps1 -ProjectPath "BGME.Framework.P3R/BGME.Framework.P3R.csproj" `
              -PackageName "BGME.Framework.P3R" `
              -PublishOutputDir "Publish/ToUpload/Framework.P3R" `

$env:RELOADEDIIMODS = "./Build3"
./Publish.ps1 -ProjectPath "BGME.Framework.API/BGME.Framework.API.csproj" `
              -PackageName "BGME.Framework.API" `
              -PublishOutputDir "Publish/ToUpload/Framework.API" `

Pop-Location