@echo off
title BuildBoot
SET MSBUILDENABLEALLPROPERTYFUNCTIONS=1
%WinDir%\Microsoft.NET\Framework\v4.0.30319\MSBuild.exe %~dp0\Common\BootTasks.proj /t:BuildBoot /tv:4.0
pause
