@echo off
echo Example usage:
echo * Just call build.bat to output to .\output\linux-x64 with Release
echo * To change the output path (Can be a relative starting with .. or a full path):
echo   build.bat "output_path=..\MSPChallenge-Server\simulations"
echo * To build with the Debug configuration:
echo   build.bat "configuration=Debug"
echo * To skip the Start? confirmation:
echo   build.bat "start=Y"
echo.

@(
  setlocal
  for %%_ in (%*) do set "%%~_"
)

set cwd=%cd%
set donetversion=net10.0
if "%configuration%" == "" (
    set configuration=Release
)
if "%api_version%" == "" (
    set api_version=2.0.0
)
set publish_target=linux-x64
if "%output_path%" == "" (
    set output_path=%cwd%\output
)
if "%output_path:~0,2%" == ".." (
    set output_path=%cwd%\%output_path%
)
echo Output paths:
call :show_output
echo Configuration: %configuration%
echo Target framework: %donetversion%
echo Start? (Y/N)
if /i "%start%" neq "Y" (
    >nul choice /c YN /n
    if errorlevel 2 (
        exit /b 0
    )
)

call :cleanup

rem build entire solution
echo.
echo ===== Building solution =====
call :run_dotnet_checked build "%cwd%\MSPChallenge-Simulations.sln" -c %configuration% -f %donetversion%
IF %ERRORLEVEL% NEQ 0 (
    goto :eof
)

rem publish each project with its data files
cd CEL\CEL
call :publish .
IF %ERRORLEVEL% NEQ 0 (
    goto :eof
)
cd %cwd%\MEL\MEL
call :publish .
IF %ERRORLEVEL% NEQ 0 (
    goto :eof
)
cd %cwd%\REL\REL
call :publish .
IF %ERRORLEVEL% NEQ 0 (
    goto :eof
)
cd %cwd%\SEL\SEL
call :publish .
IF %ERRORLEVEL% NEQ 0 (
    goto :eof
)
cd %cwd%\MSW\MSW
call :publish .
IF %ERRORLEVEL% NEQ 0 (
    goto :eof
)

cd "%cwd%"
call :docker_build_all
IF %ERRORLEVEL% NEQ 0 (
    goto :eof
)

:eof
set "_final_exit=%ERRORLEVEL%"
cd /d "%cwd%"
echo Build stopped
endlocal & exit /b %_final_exit%

rem ======= all functions below =======


:publish

set "target=%publish_target%"
set target_dir=%output_path%\%target%

rem Extract project name from current directory if %1 is "."
if "%1" == "." (
    for %%A in (%cd%) do set "project_name=%%~nxA"
) else (
    set "project_name=%1"
)

set source_dir=%cd%\bin\%configuration%\%donetversion%\%target%\publish
set target_data_dir=%target_dir%\%project_name%data
set source_data_dir=%source_dir%\%project_name%data

echo Publishing to %target%...
for %%A in (%cd%) do set "project_basename=%%~nxA"
set vbproj_file=%project_basename%_dotnet.vbproj
if exist "%vbproj_file%" (
    call :run_dotnet_checked publish %vbproj_file% -c %configuration% -r %target% -f %donetversion% --self-contained
) else (
    call :run_dotnet_checked publish -c %configuration% -r %target% -f %donetversion% --self-contained
)

IF %ERRORLEVEL% NEQ 0 (
    exit /b %ERRORLEVEL%
)
mkdir %target_dir% > nul 2> nul
echo %cd%
echo copy /y %source_dir%\* %target_dir%
copy /y %source_dir%\* %target_dir%
echo %target_data_dir%
mkdir %target_data_dir% > nul 2> nul
echo %api_version% > %target_data_dir%\version.txt
if exist "%source_data_dir%" (
	copy /y %source_data_dir%\* %target_data_dir%
)
cd "%cwd%"
exit /b 0

:cleanup

set "target=%publish_target%"
set target_dir=%output_path%\%target%\
echo Removing: %target_dir%
rmdir /q /s "%target_dir%" > nul 2> nul
cd "%cwd%"
exit /b 0

:show_output

set "target=%publish_target%"
set target_dir=%output_path%\%target%\
echo %target_dir%
cd "%cwd%"
exit /b 0

rem ── Builds the Docker image for the fixed linux-x64 publish target ──
:docker_build_all

for /f "delims=" %%B in ('git -C "%cwd%" branch --show-current 2^>nul') do set "_git_branch=%%B"
if "%_git_branch%" == "" (
    echo Warning: could not detect git branch, defaulting Docker tag to "dev".
    set "_git_branch=other"
)
if /i "%_git_branch%" == "main" (
    set "_docker_tag=main"
) else (
    set "_docker_tag=dev"
)
echo.
echo ===== Docker build ^(tag: %_docker_tag%^) =====

set "target=%publish_target%"
set "_docker_dir=%output_path%\%target%"

if not exist "%_docker_dir%\Dockerfile" (
    echo Skipping Docker build for %target%: no Dockerfile found in %_docker_dir%
    exit /b 0
)

echo.
echo Building Docker image for target %target% from %_docker_dir% ...
pushd "%_docker_dir%"
docker build -t docker-hub.mspchallenge.info/cradlewebmaster/msp-challenge-simulations:%_docker_tag% -f Dockerfile .
set "_docker_exit=%ERRORLEVEL%"
popd

IF %_docker_exit% NEQ 0 (
    echo Docker build failed for target %target% with exit code %_docker_exit%.
    exit /b %_docker_exit%
)
echo Docker image built successfully: docker-hub.mspchallenge.info/cradlewebmaster/msp-challenge-simulations:%_docker_tag%

cd "%cwd%"
exit /b 0

rem ── Runs a dotnet command, captures output, detects 401 and fixes credentials ──
rem    Usage: call :run_dotnet_checked <dotnet args...>
:run_dotnet_checked

set "_dotnet_log=%TEMP%\msp_dotnet_%RANDOM%_%RANDOM%.log"
set "_dotnet_retried=0"

:run_dotnet_checked_exec
dotnet %* > "%_dotnet_log%" 2>&1
set "_dotnet_exit=%ERRORLEVEL%"
type "%_dotnet_log%"

if %_dotnet_exit% EQU 0 (
    del /q "%_dotnet_log%" > nul 2> nul
    exit /b 0
)

findstr /i /c:"NU1301" /c:"401 (Unauthorized)" /c:"NU1100" "%_dotnet_log%" > nul
if %ERRORLEVEL% NEQ 0 (
    rem Not a 401 — pass the error through
    del /q "%_dotnet_log%" > nul 2> nul
    exit /b %_dotnet_exit%
)

if "%_dotnet_retried%" == "1" (
    echo Still getting 401 after credential update. Aborting.
    del /q "%_dotnet_log%" > nul 2> nul
    exit /b %_dotnet_exit%
)

echo.
echo Detected 401 Unauthorized. Launching credential fix...
powershell -NoProfile -ExecutionPolicy Bypass -File "%cwd%\fix-nuget-auth.ps1" -ErrorLog "%_dotnet_log%"
if %ERRORLEVEL% NEQ 0 (
    echo Credential fix failed or was cancelled.
    del /q "%_dotnet_log%" > nul 2> nul
    exit /b %_dotnet_exit%
)

set "_dotnet_retried=1"
echo.
echo Retrying dotnet command...
goto :run_dotnet_checked_exec

