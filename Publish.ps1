# Set Working Directory
Split-Path $MyInvocation.MyCommand.Path | Push-Location
[Environment]::CurrentDirectory = $PWD

./BGME.Framework/Publish.ps1 -ProjectPath "BGME.Framework/BGME.Framework.csproj" `
              -PackageName "BGME.Framework" `
              -PublishOutputDir "Publish/ToUpload/Framework" `
			  @args
			  
./BGME.Framework.P3R/Publish.ps1 -ProjectPath "BGME.Framework.P3R/BGME.Framework.P3R.csproj" `
              -PackageName "BGME.Framework.P3R" `
              -PublishOutputDir "Publish/ToUpload/Framework.P3R" `
			  @args			  
./BGME.Framework.API/Publish.ps1 -ProjectPath "BGME.Framework.API/BGME.Framework.API.csproj" `
              -PackageName "BGME.Framework.API" `
              -PublishOutputDir "Publish/ToUpload/Framework.API" `
			  @args
Pop-Location