# Set Working Directory
Split-Path $MyInvocation.MyCommand.Path | Push-Location
[Environment]::CurrentDirectory = $PWD

Remove-Item "$env:RELOADEDIIMODS/BGME.Framework.API/*" -Force -Recurse
dotnet publish "./BGME.Framework.API.csproj" -c Release -o "$env:RELOADEDIIMODS/BGME.Framework.API" /p:OutputPath="./bin/Release" /p:ReloadedILLink="true"

# Restore Working Directory
Pop-Location