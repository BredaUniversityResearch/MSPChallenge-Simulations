@(
  setlocal
  echo off
  for %%_ in (%*) do set "%%~_"
)

set cwd=%cd%
set donetversion=net5.0
if "%configuration%" == "" (
    set configuration=Release
)
if "%api_version%" == "" (
    set api_version=v1
)
if "%publish_targets[0]%" == "" (
    set publish_targets[0]=alpine.3.17-x64
    set publish_targets[1]=win-x64
    rem set publish_targets[2]=ubuntu.22.04-x64
)
if "%output_path%" == "" (
    set output_path=%cwd%\output
)
if "%output_path:~0,2%" == ".." (
    set output_path=%cwd%\%output_path%
)

echo using output path: %output_path%

call :build MSWSupport
copy /y MSWSupport\MSWSupport\bin\%configuration%\%donetversion%\MSWSupport.dll DLLs\

call :build SELRELBridge
copy /y SELRELBridge\SELRELBridge\bin\%configuration%\%donetversion%\SELRELBridge.dll DLLs\

call :build EwEShell
copy /y EwEShell\EwEShell\bin\%configuration%\%donetversion%\EwEShell.dll DLLs\

rmdir /q /s "%output_path%" > nul 2> nul

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
dotnet build -c %configuration%
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
dotnet publish -c %configuration% -r %target% --self-contained

if "%publish_targets[1]%" == "" (
    set target_dir=%output_path%\%api_version%\
) else (
    set target_dir=%output_path%\%target%\%api_version%\
)

mkdir %target_dir% > nul 2> nul
copy /y %1\bin\%configuration%\%donetversion%\%target%\publish\* %target_dir%
SET /a "x+=1"
goto :publish_targets_loop
