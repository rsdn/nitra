@echo off
title Uninstall Vsix

IF "%PROCESSOR_ARCHITECTURE%"=="x86" (set WOW6432Node=) else (set WOW6432Node=WOW6432Node\)

SET MSBUILDENABLEALLPROPERTYFUNCTIONS=1
setlocal ENABLEEXTENSIONS
set KEY_NAME="HKEY_LOCAL_MACHINE\SOFTWARE\%WOW6432Node%Microsoft\VisualStudio\SxS\VS7"
set VALUE_NAME=15.0

for /f "tokens=2* skip=2" %%a in ('reg query %KEY_NAME% /V %VALUE_NAME% ') DO set Value=%%b

if defined Value (
"%Value%Common7\IDE\VSIXInstaller.exe" /u:TdlLangVsPackage 	
"%Value%Common7\IDE\VSIXInstaller.exe" /u:NitraCSharpVsPackage 
"%Value%Common7\IDE\VSIXInstaller.exe" /u:NitraLangVsPackage 
"%Value%Common7\IDE\VSIXInstaller.exe" /u:NitraCommonVSIX 	

) else (
@echo %KEY_NAME%\%VALUE_NAME% not found.
)

