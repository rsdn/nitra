@echo off
title Uninstall Vsix

rem @echo on
setlocal enabledelayedexpansion

if "%VSWHERE%" == "" set "VSWHERE=%ProgramFiles(x86)%\Microsoft Visual Studio\Installer\vswhere.exe"

for /f "usebackq delims=" %%i in (`"%VSWHERE%" -latest -property installationPath`) do (
  if exist "%%i\Common7\Tools\VsDevCmd.bat" (
    rem call "%%i\Common7\Tools\VsDevCmd.bat"
	"%%i\Common7\IDE\VSIXInstaller.exe" /u:TdlLangVsPackage
	rem "%Value%Common7\IDE\VSIXInstaller.exe" /u:NitraCSharpVsPackage
	rem "%Value%Common7\IDE\VSIXInstaller.exe" /u:NitraLangVsPackage
	"%%i\Common7\IDE\VSIXInstaller.exe" /u:NitraCommonVsix22
    goto run_msbuild
  )
)

echo Instance or command prompt of latest Visual Studio not found
exit /b 2

:run_msbuild
