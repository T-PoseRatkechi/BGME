# Set Working Directory
Split-Path $MyInvocation.MyCommand.Path | Push-Location
[Environment]::CurrentDirectory = $PWD

./BGME.Framework/Publish.ps1 -ProjectPath "BGME.Framework/BGME.Framework.csproj" `
              -PackageName "BGME.Framework" `
			  -UseScriptDirectory false `
			  @args
Pop-Location