@echo off
setlocal
set cwd=%cd%
set api_version=v1
set publish_targets[0]=win-x64
set publish_targets[1]=ubuntu.22.04-x64
set publish_targets[2]=alpine.3.17-x64

call :build MSWSupport
copy /y MSWSupport\MSWSupport\bin\Release\net6.0\MSWSupport.dll DLLs\

call :build SELRELBridge
copy /y SELRELBridge\SELRELBridge\bin\Release\net6.0\SELRELBridge.dll DLLs\

rmdir /q /s output > nul 2> nul

call :publish CEL %api_version%\
call :publish MEL %api_version%\
call :publish REL %api_version%\
call :publish SEL %api_version%\
call :publish MSW

:eof
cd "%cwd%"
endlocal
pause
IF %ERRORLEVEL% NEQ 0 (
    exit /b %ERRORLEVEL%
)
exit /b 0

rem ======= all functions below =======

:build
if not exist "%1" (
    echo Could not find "%1/"
    set ERRORLEVEL=1
    goto eof
)
cd "%1"
dotnet build -c Release
cd "%cwd%"
exit /b 0

:publish
if not exist "%1" (
    echo Could not find "%1/"
    set ERRORLEVEL=1
    goto eof
)
cd "%1"
call :publish_targets_loop_start %1 %2
cd "%cwd%"
exit /b 0

:publish_targets_loop_start
set "x=0"
call :publish_targets_loop %1 %2
exit /b 0

:publish_targets_loop
if not defined publish_targets[%x%] exit /b 0
call set target=%%publish_targets[%x%]%%
echo Publishing %target%...
dotnet publish -c Release -r %target% --self-contained
set target_dir=%cwd%\output\%target%\%1\%2
echo mkdir %target_dir% > nul 2> nul
mkdir %target_dir% > nul 2> nul
echo copy %1\bin\Release\net6.0\%target%\publish\* %target_dir%
copy /y %1\bin\Release\net6.0\%target%\publish\* %target_dir%
SET /a "x+=1"
goto :publish_targets_loop
