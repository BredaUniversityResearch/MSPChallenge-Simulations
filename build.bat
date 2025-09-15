@echo off
echo Example usage:
echo * Just call build.bat to output to subdir .\output for all platforms and Release
echo * To change the output path (Can be a relative starting with .. or a full path):
echo   build.bat "output_path=..\MSPChallenge-Server\simulations"
echo * To build with the Debug configuration and only for platform alpine:
echo   build.bat "publish_targets[0]=debian.11-x64" "configuration=Debug"
echo * To filter on multiple platforms:
echo   build.bat "publish_targets[0]=debian.11-x64 publish_targets[1]=win-x64"
echo * To skip the Start? confirmation:
echo   build.bat "start=Y"
echo.

set ecopath_dir=..\Ecopath6_multitarget
set ecopath_source_dir="%ecopath_dir%\Sources"
if not exist "%ecopath_source_dir%" (
    echo Directory "%ecopath_source_dir%" does not exist. Please checkout the svn repo: https://sources.ecopath.org/svn/Ecopath/branches/Ecopath6_multitarget to "%ecopath_dir%"
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
set ewemsplink_dir="%ecopath_source_dir%\EwECustomPlugins\EwEMSPChallengeIntegration\EwEMSPLink"
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
    set api_version=2.0.0
)
if "%publish_targets[0]%" == "" (
    set publish_targets[0]=debian.11-x64
rem    set publish_targets[1]=win-x64
)
if "%output_path%" == "" (
    set output_path=%cwd%\output
)
if "%output_path:~0,2%" == ".." (
    set output_path=%cwd%\%output_path%
)
echo Output paths:
call :show_output
echo Start? (Y/N)
if /i "%start%" neq "Y" (
    >nul choice /c YN /n
    if errorlevel 2 (
        exit /b 0
    )
)

call :cleanup

rem prepare required dlls for MSW
call :build MSWSupport
IF %ERRORLEVEL% NEQ 0 (
    exit /b %ERRORLEVEL%
)
copy /y MSWSupport\MSWSupport\bin\%configuration%\%donetversion%\*.dll DLLs\
copy /y MSWSupport\MSWSupport\bin\%configuration%\%donetversion%\*.pdb DLLs\
rem prepare required dlls for SEL/REL
call :build SELRELBridge
IF %ERRORLEVEL% NEQ 0 (
    exit /b %ERRORLEVEL%
)
copy /y SELRELBridge\SELRELBridge\bin\%configuration%\%donetversion%\SELRELBridge.dll DLLs\
copy /y SELRELBridge\SELRELBridge\bin\%configuration%\%donetversion%\*.pdb DLLs\
rem build referenced dlls, in right order
call :build %eweutils_dir%
call :build %eweplugin_dir%
call :build %ewecore_dir%
call :build %ewemsplink_dir%
rem prepare required dlls for MEL
copy /y %ewemsplink_dir%\bin\%configuration%\%donetversion%\EwEMSPLink.dll DLLs\
copy /y %ewemsplink_dir%\bin\%configuration%\%donetversion%\EwELicense.dll DLLs\
copy /y %ewemsplink_dir%\bin\%configuration%\%donetversion%\*.pdb DLLs\
copy /y %ewecore_dir%\bin\%configuration%\%donetversion%\EwECore.dll DLLs\
copy /y %ewecore_dir%\bin\%configuration%\%donetversion%\*.pdb DLLs\
copy /y %eweplugin_dir%\bin\%configuration%\%donetversion%\EwEPlugin.dll DLLs\
copy /y %eweplugin_dir%\bin\%configuration%\%donetversion%\*.pdb DLLs\
copy /y %eweutils_dir%\bin\%configuration%\%donetversion%\EwEUtils.dll DLLs\
copy /y %eweutils_dir%\bin\%configuration%\%donetversion%\*.pdb DLLs\

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
echo Build stopped
exit /b 0

rem ======= all functions below =======

:build
if not exist "%1" (
    echo Could not find "%1/"
    set ERRORLEVEL=1
    goto eof
)
cd "%1"
for %%A in (%1) do set "project_basename=%%~nxA"
set vbproj_file=%project_basename%_%donetversion%.vbproj
if exist "%vbproj_file%" (
    dotnet build %vbproj_file% -c %configuration% -f %donetversion%
) else (
    dotnet build -c %configuration% -f %donetversion%
)
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
set target_dir=%output_path%\%target%
set source_dir=%1\bin\%configuration%\%donetversion%\%target%\publish
set target_data_dir=%target_dir%\%1data
set source_data_dir=%source_dir%\%1data

echo Publishing to %target%...
for %%A in (%cd%) do set "project_basename=%%~nxA"
set vbproj_file=%project_basename%_%donetversion%.vbproj
if exist "%vbproj_file%" (
    dotnet publish %vbproj_file% -c %configuration% -r %target% -f %donetversion% --self-contained
) else (
    dotnet publish -c %configuration% -r %target% -f %donetversion% --self-contained
)

IF %ERRORLEVEL% NEQ 0 (
    exit /b %ERRORLEVEL%
)
mkdir %target_dir% > nul 2> nul
echo %cd%
echo copy /y %source_dir%\* %target_dir%
copy /y %source_dir%\* %target_dir%
if "%1" NEQ "." (
	echo %target_data_dir%
	mkdir %target_data_dir% > nul 2> nul
	echo %api_version% > %target_data_dir%\version.txt
	if exist "%source_data_dir%" (
		copy /y %source_data_dir%\* %target_data_dir%
	)
)
SET /a "x+=1"
goto :publish_targets_loop

:cleanup

call :cleanup_targets_loop_start
IF %ERRORLEVEL% NEQ 0 (
    exit /b %ERRORLEVEL%
)
cd "%cwd%"
exit /b 0

:cleanup_targets_loop_start

set "x=0"
call :cleanup_targets_loop
IF %ERRORLEVEL% NEQ 0 (
    exit /b %ERRORLEVEL%
)
exit /b 0

:cleanup_targets_loop

if not defined publish_targets[%x%] exit /b 0
call set target=%%publish_targets[%x%]%%
set target_dir=%output_path%\%target%\
echo Removing: %target_dir%
rmdir /q /s "%target_dir%" > nul 2> nul
SET /a "x+=1"
goto :cleanup_targets_loop

:show_output

call :show_output_targets_loop_start
IF %ERRORLEVEL% NEQ 0 (
    exit /b %ERRORLEVEL%
)
cd "%cwd%"
exit /b 0

:show_output_targets_loop_start

set "x=0"
call :show_output_targets_loop
IF %ERRORLEVEL% NEQ 0 (
    exit /b %ERRORLEVEL%
)
exit /b 0

:show_output_targets_loop

if not defined publish_targets[%x%] exit /b 0
call set target=%%publish_targets[%x%]%%
set target_dir=%output_path%\%target%\
echo %target_dir%
SET /a "x+=1"
goto :show_output_targets_loop

