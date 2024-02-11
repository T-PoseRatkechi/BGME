# Set Working Directory
Split-Path $MyInvocation.MyCommand.Path | Push-Location
[Environment]::CurrentDirectory = $PWD

Remove-Item "$env:RELOADEDIIMODS/BGME.Framework.P3R/*" -Force -Recurse
dotnet publish "./BGME.Framework.P3R.csproj" -c Release -o "$env:RELOADEDIIMODS/BGME.Framework.P3R" /p:OutputPath="./bin/Release" /p:ReloadedILLink="true"

# Restore Working Directory
Pop-Location