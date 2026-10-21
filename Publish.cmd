@echo off

set configuration="Release"
set publishDir="Publish"
set net10="net10.0"
set net10Win="net10.0-windows"
set win64="win-x64"
set linux64="linux-x64"
set arguments="--self-contained false -p:PublishSingleFile=true -p:PublishReadyToRun=false -p:DebugType=None -p:DebugSymbols=false"
set verbosity="normal"

call :Publish "TwitchChatToSubtitles", %configuration%, %publishDir%, %net10%, %win64%, %arguments%, %verbosity%
call :Publish "TwitchChatToSubtitles", %configuration%, %publishDir%, %net10%, %linux64%, %arguments%, %verbosity%
call :Publish "TwitchChatToSubtitlesUI", %configuration%, %publishDir%, %net10Win%, %win64%, %arguments%, %verbosity%

:exit
:: pause
exit /b 0

::----------------------------------------------------------------------

:Publish
:: Publish(project, configuration, publishDir, framework, runtime, arguments, verbosity)
setlocal
set "project=%~1"
set "configuration=%~2"
set "publishDir=%~3"
set "framework=%~4"
set "runtime=%~5"
set "arguments=%~6"
set "verbosity=%~7"

echo publish %project% %framework% %runtime%
echo.
call dotnet publish %project% --configuration %configuration% --output "%publishDir%\%framework%\%runtime%" --framework %framework% --runtime %runtime% %arguments% --verbosity %verbosity%
echo.
endlocal
goto :eof
