@echo off

set "publishDir=Output\net8.0"
set "win64=win-x64"
set "linux64=linux-x64"
set "arguments=--configuration Release --self-contained false -p:PublishSingleFile=true -p:PublishReadyToRun=false -p:DebugType=None -p:DebugSymbols=false --verbosity normal"

:TwitchChatToSubtitles
set "project=TwitchChatToSubtitles"
set "framework=--framework net8.0"
dotnet publish %project% --output "%publishDir%\%win64%" --runtime %win64% %framework% %arguments%
echo.
dotnet publish %project% --output "%publishDir%\%linux64%" --runtime %linux64% %framework% %arguments%
echo.

:TwitchChatToSubtitlesUI
set "project=TwitchChatToSubtitlesUI"
set "framework=--framework net8.0-windows"
dotnet publish %project% --output "%publishDir%\%win64%" --runtime %win64% %framework% %arguments%
echo.
dotnet publish %project% --output "%publishDir%\%linux64%" --runtime %linux64% %framework% %arguments%
echo.

:exit
:: pause
exit /b 0
