@echo off
title BuildBoot

IF "%PROCESSOR_ARCHITECTURE%"=="x86" (set WOW6432Node=) else (set WOW6432Node=WOW6432Node\)

SET MSBUILDENABLEALLPROPERTYFUNCTIONS=1
setlocal ENABLEEXTENSIONS
set KEY_NAME="HKEY_LOCAL_MACHINE\SOFTWARE\%WOW6432Node%Microsoft\VisualStudio\SxS\VS7"
set VALUE_NAME=15.0

for /f "tokens=2* skip=2" %%a in ('reg query %KEY_NAME% /V %VALUE_NAME% ') DO set Value=%%b

if defined Value (
call "%Value%Common7\Tools\VsDevCmd.bat"
set NoPause=true

%~dp0\ExternalTools\NuGet.exe restore %~dp0\System.Collections.Immutable.Light\System.Collections.Immutable.Light.csproj -PackagesDirectory %~dp0\packages

MSBuild.exe %~dp0\Common\BootTasks.proj /t:BuildBoot /bl:%~dp0\Boot2\BootTasks.binlog;ProjectImports=Embed
) else (
@echo %KEY_NAME%\%VALUE_NAME% not found.
)

pause
