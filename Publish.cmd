@echo off

set configuration="Release"
set publishDir="Publish"
set net10="net10.0"
set net10Win="net10.0-windows"
set arguments="--self-contained false -p:PublishSingleFile=true -p:PublishReadyToRun=false -p:DebugType=None -p:DebugSymbols=false"
set verbosity="normal"

set project="TwitchChatToSubtitles"
call :Publish %project%, %configuration%, %publishDir%, %net10%, "linux-arm64", %arguments%, %verbosity%
call :Publish %project%, %configuration%, %publishDir%, %net10%, "linux-x64", %arguments%, %verbosity%
call :Publish %project%, %configuration%, %publishDir%, %net10%, "win-x64", %arguments%, %verbosity%

set project="TwitchChatToSubtitlesUI"
call :Publish %project%, %configuration%, %publishDir%, %net10Win%, "win-x64", %arguments%, %verbosity%

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

set output="%publishDir%\%framework%\%runtime%"

echo publish %project% %framework% %runtime%
echo.
call dotnet publish %project% --configuration %configuration% --output %output% --framework %framework% --runtime %runtime% %arguments% --verbosity %verbosity%
echo.
endlocal
goto :eof
