# Set Working Directory
Split-Path $MyInvocation.MyCommand.Path | Push-Location
[Environment]::CurrentDirectory = $PWD

./Publish.ps1 -ProjectPath "BGME.Framework/BGME.Framework.csproj" `
              -PackageName "BGME.Framework" `
              -PublishOutputDir "Publish/ToUpload/Framework" `

./Publish.ps1 -ProjectPath "BGME.Framework.P3R/BGME.Framework.P3R.csproj" `
              -PackageName "BGME.Framework.P3R" `
              -PublishOutputDir "Publish/ToUpload/Framework.P3R" `

./Publish.ps1 -ProjectPath "BGME.Framework.API/BGME.Framework.API.csproj" `
              -PackageName "BGME.Framework.API" `
              -PublishOutputDir "Publish/ToUpload/Framework.API" `

Pop-Location