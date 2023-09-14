@echo off
echo Example usage:
echo * Just call build.bat to output to subdir .\ouput for all platforms and Release
echo * To change the output path (Can be a relative starting with .. or a full path):
echo   build.bat "output_path=..\MSPChallenge-Server\simulations"
echo * To build with the Debug configuration and only for platform alpine:
echo   build.bat "publish_targets[0]=alpine.3.17-x64" "configuration=Debug"
echo * To filter on multiple platforms:
echo   build.bat "publish_targets[0]=alpine.3.17-x64 publish_targets[1]=win-x64"
echo * To skip the Start? confirmation:
echo   build.bat "start=Y"
echo.

set ecopath_dir=..\Ecopath6_netstandard
set ecopath_source_dir="%ecopath_dir%\Sources"
if not exist "%ecopath_source_dir%" (
    echo Directory "%ecopath_source_dir%" does not exist. Please checkout the svn repo: https://sources.ecopath.org/svn/Ecopath/branches/Ecopath6_netstandard to "%ecopath_dir%"
    exit /b 1
)
set ewecore_dir="%ecopath_source_dir%\EwECore"
if not exist "%ewecore_dir%" (
    echo Directory "%ewecore_dir%" does not exist.
    exit /b 1
)
set eweutils_dir="%ecopath_source_dir%\EwEUtils"
if not exist "%eweutils_dir%" (
    echo Directory "%eweutils_dir%" does not exist.
    exit /b 1
)
set eweplugin_dir="%ecopath_source_dir%\EwEPlugin"
if not exist "%eweplugin_dir%" (
    echo Directory "%eweplugin_dir%" does not exist.
    exit /b 1
)
set ewemsplink_dir="%ecopath_source_dir%\EwECustomPlugins\EwEMSPTools\EwEMSPLink"
if not exist "%ewemsplink_dir%" (
    echo Directory "%ewemsplink_dir%" does not exist.
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
    set api_version=1.0.0
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
echo Using output path: %output_path%
echo Start? (Y/N)
if /i "%start%" neq "Y" (
    >nul choice /c YN /n
    if errorlevel 2 (
        exit /b 0
    )
)

rmdir /q /s "%output_path%\%api_version%" > nul 2> nul

rem prepare required dlls for MSW
call :build MSWSupport
IF %ERRORLEVEL% NEQ 0 (
    exit /b %ERRORLEVEL%
)
copy /y MSWSupport\MSWSupport\bin\%configuration%\%donetversion%\MSWSupport.dll DLLs\
rem prepare required dlls for SEL/REL
call :build SELRELBridge
IF %ERRORLEVEL% NEQ 0 (
    exit /b %ERRORLEVEL%
)
copy /y SELRELBridge\SELRELBridge\bin\%configuration%\%donetversion%\SELRELBridge.dll DLLs\
rem build referenced dlls, in right order
call :build %eweutils_dir%
call :build %eweplugin_dir%
call :build %ewecore_dir%
call :build %ewemsplink_dir%
rem prepare required dlls for MEL
copy /y %ewemsplink_dir%\bin\%configuration%\%donetversion%\EwEMSPLink.dll DLLs\
copy /y %ewemsplink_dir%\bin\%configuration%\%donetversion%\EwELicense.dll DLLs\
copy /y %ewecore_dir%\bin\%configuration%\%donetversion%\EwECore.dll DLLs\
copy /y %eweplugin_dir%\bin\%configuration%\%donetversion%\EwEPlugin.dll DLLs\
copy /y %eweutils_dir%\bin\%configuration%\%donetversion%\EwEUtils.dll DLLs\

cd %ewecore_dir%
call :publish .
IF %ERRORLEVEL% NEQ 0 (
    exit /b %ERRORLEVEL%
)
cd %eweutils_dir%
call :publish .
IF %ERRORLEVEL% NEQ 0 (
    exit /b %ERRORLEVEL%
)
cd %eweplugin_dir%
call :publish .
IF %ERRORLEVEL% NEQ 0 (
    exit /b %ERRORLEVEL%
)
cd %ewemsplink_dir%
call :publish .
IF %ERRORLEVEL% NEQ 0 (
    exit /b %ERRORLEVEL%
)

cd CEL
call :publish CEL
IF %ERRORLEVEL% NEQ 0 (
    exit /b %ERRORLEVEL%
)
cd MEL
call :publish MEL
IF %ERRORLEVEL% NEQ 0 (
    exit /b %ERRORLEVEL%
)
cd REL
call :publish REL
IF %ERRORLEVEL% NEQ 0 (
    exit /b %ERRORLEVEL%
)
cd SEL
call :publish SEL
IF %ERRORLEVEL% NEQ 0 (
    exit /b %ERRORLEVEL%
)
cd MSW
call :publish MSW
IF %ERRORLEVEL% NEQ 0 (
    exit /b %ERRORLEVEL%
)

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
dotnet build -c %configuration% -f %donetversion%
IF %ERRORLEVEL% NEQ 0 (
    exit /b %ERRORLEVEL%
)
cd "%cwd%"
exit /b 0

:publish
if not exist "%1" (
    echo Could not find "%1/"
    set ERRORLEVEL=1
    goto eof
)
call :publish_targets_loop_start %1 %2
IF %ERRORLEVEL% NEQ 0 (
    exit /b %ERRORLEVEL%
)
cd "%cwd%"
exit /b 0

:publish_targets_loop_start
set "x=0"
call :publish_targets_loop %1 %2
IF %ERRORLEVEL% NEQ 0 (
    exit /b %ERRORLEVEL%
)
exit /b 0

:publish_targets_loop

if not defined publish_targets[%x%] exit /b 0
call set target=%%publish_targets[%x%]%%
echo Publishing %target%...
dotnet publish -c %configuration% -r %target% -f %donetversion% --self-contained
IF %ERRORLEVEL% NEQ 0 (
    exit /b %ERRORLEVEL%
)
if "%publish_targets[1]%" == "" (
    set target_dir=%output_path%\
) else (
    set target_dir=%output_path%\%target%\
)
mkdir %target_dir% > nul 2> nul
echo %cd%
echo copy /y %1\bin\%configuration%\%donetversion%\%target%\publish\* %target_dir%
copy /y %1\bin\%configuration%\%donetversion%\%target%\publish\* %target_dir%
echo %api_version% > %target_dir%\version.txt
SET /a "x+=1"
goto :publish_targets_loop
