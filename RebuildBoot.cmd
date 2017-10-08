@echo off
title RebuildBoot
SET MSBUILDENABLEALLPROPERTYFUNCTIONS=1
rem %WinDir%\Microsoft.NET\Framework\v4.0.30319\MSBuild.exe %~dp0\Common\BootTasks.proj /t:BuildBoot /tv:4.0
call "%VSINSTALLDIR%\Common7\Tools\VsDevCmd.bat"
MSBuild.exe %~dp0\Common\BootTasks.proj /t:BuildBoot /tv:4.0 /bl:%~dp0\Boot2\BootTasks.binlog;ProjectImports=Embed /p:BuildTarget=Rebuild
pause

