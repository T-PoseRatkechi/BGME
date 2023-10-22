# Set Working Directory
Split-Path $MyInvocation.MyCommand.Path | Push-Location
[Environment]::CurrentDirectory = $PWD

Remove-Item "$env:RELOADEDIIMODS/BGME.Reloaded/*" -Force -Recurse
dotnet publish "./BGME.Reloaded.csproj" -c Release -o "$env:RELOADEDIIMODS/BGME.Reloaded" /p:OutputPath="./bin/Release" /p:ReloadedILLink="true"

# Restore Working Directory
Pop-Location