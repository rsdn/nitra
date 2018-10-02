@echo off
title BuildBoot

SET MSBUILDENABLEALLPROPERTYFUNCTIONS=1
setlocal ENABLEEXTENSIONS
set KEY_NAME="HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Microsoft\VisualStudio\SxS\VS7"
set VALUE_NAME=15.0

for /f "tokens=3" %%a in ('reg query %KEY_NAME% /V %VALUE_NAME%  ^|findstr /ri "REG_SZ"') DO set Value=%%a

if defined Value (
call "%Value%Common7\Tools\VsDevCmd.bat"
set NoPause=true

MSBuild.exe %~dp0\Common\BootTasks.proj /t:BuildBoot /tv:4.0 /bl:%~dp0\Boot2\BootTasks.binlog;ProjectImports=Embed

IF %errorlevel% == 0 call Reg-bins-VS2015.cmd
) else (
@echo %KEY_NAME%\%VALUE_NAME% not found.
)

pause
