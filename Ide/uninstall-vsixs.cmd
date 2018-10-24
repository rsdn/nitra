@echo off
title BuildBoot

SET MSBUILDENABLEALLPROPERTYFUNCTIONS=1
setlocal ENABLEEXTENSIONS
set KEY_NAME="HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Microsoft\VisualStudio\SxS\VS7"
set VALUE_NAME=15.0

for /f "tokens=3" %%a in ('reg query %KEY_NAME% /V %VALUE_NAME%  ^|findstr /ri "REG_SZ"') DO set Value=%%a

if defined Value (
"%Value%Common7\IDE\VSIXInstaller.exe" /u:TdlLangVsPackage
"%Value%Common7\IDE\VSIXInstaller.exe" /u:NitraCSharpVsPackage
"%Value%Common7\IDE\VSIXInstaller.exe" /u:NitraLangVsPackage
"%Value%Common7\IDE\VSIXInstaller.exe" /u:NitraCommonVSIX

) else (
@echo %KEY_NAME%\%VALUE_NAME% not found.
)

pause

