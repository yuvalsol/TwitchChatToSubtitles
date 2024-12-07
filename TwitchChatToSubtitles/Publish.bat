@echo off
set "arguments=--configuration Release --framework net8.0 --self-contained false -p:PublishSingleFile=true -p:PublishReadyToRun=false -p:DebugType=None -p:DebugSymbols=false"
set "publishDir=..\Output\net8.0"
dotnet publish --output "%publishDir%\win-x64" --runtime win-x64 %arguments%
echo.
dotnet publish --output "%publishDir%\linux-x64" --runtime linux-x64 %arguments%
echo.
::pause
