@echo off
rem examples:
rem * Just call build.bat to output to subdir .\ouput for all platforms and Release
rem * To change the output path (Can be a relative starting with .. or a full path):
rem   build.bat "output_path=..\MSPChallenge-Server\simulations"
rem * To build with the Debug configuration and only for platform alpine:
rem   build.bat "publish_targets[0]=alpine.3.17-x64" "configuration=Debug"
rem * To filter on multiple platforms:
rem   build.bat "publish_targets[0]=alpine.3.17-x64 publish_targets[1]=win-x64"

set ecopathdir=..\Ecopath6_netstandard
if not exist "%ecopathdir%" (
    echo Directory "%ecopathdir%" does not exist. Please checkout the svn repo: https://sources.ecopath.org/svn/Ecopath/branches/Ecopath6_netstandard to "%ecopathdir%"
    exit /b 1
)
set ewemsplinkdir="%ecopathdir%\Sources\EwECustomPlugins\EwEMSPTools\EwEMSPLink"
if not exist "%ewemsplinkdir%" (
    echo Directory "%ewemsplinkdir%" does not exist.
    exit /b 1
)

@(
  setlocal
  for %%_ in (%*) do set "%%~_"
)

set cwd=%cd%
set donetversion=net6.0
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

rmdir /q /s "%output_path%\%api_version%" > nul 2> nul

call :build MSWSupport
copy /y MSWSupport\MSWSupport\bin\%configuration%\%donetversion%\MSWSupport.dll DLLs\
call :build SELRELBridge
copy /y SELRELBridge\SELRELBridge\bin\%configuration%\%donetversion%\SELRELBridge.dll DLLs\

call :build %ewemsplinkdir%
copy /y %ewemsplinkdir%\bin\%configuration%\%donetversion%\EwEMSPLink.dll DLLs\
copy /y %ewemsplinkdir%\bin\%configuration%\%donetversion%\EwECore.dll DLLs\
copy /y %ewemsplinkdir%\bin\%configuration%\%donetversion%\EwEPlugin.dll DLLs\
copy /y %ewemsplinkdir%\bin\%configuration%\%donetversion%\EwEUtils.dll DLLs\
cd %ewemsplinkdir%
call :publish .

cd CEL
call :publish CEL
cd MEL
call :publish MEL
cd REL
call :publish REL
cd SEL
call :publish SEL
cd MSW
call :publish MSW

:eof
cd "%cwd%"
endlocal
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
dotnet publish -c %configuration% -r %target% -f %donetversion% --self-contained
if "%publish_targets[1]%" == "" (
    set target_dir=%output_path%\%api_version%\
) else (
    set target_dir=%output_path%\%api_version%\%target%\
)
mkdir %target_dir% > nul 2> nul
echo %cd%
echo copy /y %1\bin\%configuration%\%donetversion%\%target%\publish\* %target_dir%
copy /y %1\bin\%configuration%\%donetversion%\%target%\publish\* %target_dir%
SET /a "x+=1"
goto :publish_targets_loop
