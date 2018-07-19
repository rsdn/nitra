@echo off
title BuildBoot
IF "%VS150COMNTOOLS%"=="" (
    echo The VS150COMNTOOLS environment variable not set! Run this butch file from "Developer Command Prompt for VS 2017".
    pause
    EXIT /B 1
)
SET VsDevCmdPath=%VS150COMNTOOLS%VsDevCmd.bat
IF NOT EXIST "%VsDevCmdPath%" (
    echo The "%VsDevCmdPath%" butch file not exists! Try to run this butch file from "Developer Command Prompt for VS 2017".
    pause
    EXIT /B 1
)

call "%VsDevCmdPath%"
rem See https://github.com/Microsoft/msbuild/blob/master/documentation/wiki/MSBuild-Tips-&-Tricks.md
SET MSBUILDENABLEALLPROPERTYFUNCTIONS=1
MSBuild.exe %~dp0\Common\BootTasks.proj /t:BuildBoot /tv:4.0 /bl:%~dp0\Boot2\BootTasks.binlog;ProjectImports=Embed
pause
