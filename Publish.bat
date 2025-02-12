@echo off

set publishDir="Output\net8.0"
set win64="win-x64"
set linux64="linux-x64"
set arguments="--configuration Release --self-contained false -p:PublishSingleFile=true -p:PublishReadyToRun=false -p:DebugType=None -p:DebugSymbols=false --verbosity normal"

:TwitchChatToSubtitles
call :Publish "TwitchChatToSubtitles", %publishDir%, %win64%, "net8.0", %arguments%
call :Publish "TwitchChatToSubtitles", %publishDir%, %linux64%, "net8.0", %arguments%

:TwitchChatToSubtitlesUI
call :Publish "TwitchChatToSubtitlesUI", %publishDir%, %win64%, "net8.0-windows", %arguments%
call :Publish "TwitchChatToSubtitlesUI", %publishDir%, %linux64%, "net8.0-windows", %arguments%

:exit
:: pause
exit /b 0

::----------------------------------------------------------------------

:Publish
:: Publish(project, publishDir, runtime, framework, arguments)
setlocal
set "project=%~1"
set "publishDir=%~2"
set "runtime=%~3"
set "framework=%~4"
set "arguments=%~5"

echo dotnet publish %project% --output "%publishDir%\%runtime%" --runtime %runtime% --framework %framework% %arguments%
echo.
call dotnet publish %project% --output "%publishDir%\%runtime%" --runtime %runtime% --framework %framework% %arguments%
echo.

endlocal

goto :eof
