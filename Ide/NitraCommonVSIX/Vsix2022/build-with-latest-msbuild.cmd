@echo on
setlocal enabledelayedexpansion

rem Run VsDevCmd.bat from latest VS to set environment variables. 
rem Without it, the environment variables from VS 2017 will be used, resulting in a failure when validating vsix.
for /f "usebackq delims=" %%i in (`vswhere.exe -latest -property installationPath`) do (
  if exist "%%i\Common7\Tools\VsDevCmd.bat" (
    call "%%i\Common7\Tools\VsDevCmd.bat"
    goto run_msbuild
  )
)

echo Instance or command prompt of latest Visual Studio not found
exit /b 2

:run_msbuild

rem latest MSBuild.exe
for /f "usebackq tokens=*" %%i in (`vswhere -latest -requires Microsoft.Component.MSBuild -find MSBuild\**\Bin\MSBuild.exe`) do (
  "%%i" %*
  exit /b !errorlevel!
)
