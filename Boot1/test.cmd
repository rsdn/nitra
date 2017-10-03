@echo off
title ShiftBoot
SET MSBUILDENABLEALLPROPERTYFUNCTIONS=1
%WinDir%\Microsoft.NET\Framework\v4.0.30319\MSBuild.exe test.nproj /tv:4.0
pause
