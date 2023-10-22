# Set Working Directory
Split-Path $MyInvocation.MyCommand.Path | Push-Location
[Environment]::CurrentDirectory = $PWD

Remove-Item "$env:RELOADEDIIMODS/BGME.Framework/*" -Force -Recurse
dotnet publish "./BGME.Framework.csproj" -c Release -o "$env:RELOADEDIIMODS/BGME.Framework" /p:OutputPath="./bin/Release" /p:ReloadedILLink="true"

# Restore Working Directory
Pop-Location