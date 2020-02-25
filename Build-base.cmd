@echo off
rem echo on

IF "%~1"=="" (
@echo The parameter 1 [Config] is empty. Use Debug or Release.
EXIT /B -1
)

IF "%~2"=="" (
@echo The parameter 2 [Target] is empty. Use Build or Rebuild.
EXIT /B -1
)

SET Name=Nitra
SET Config=%~1
SET Target=%~2
SET Verbosity=m
SET MSBUILDENABLEALLPROPERTYFUNCTIONS=1
SET NemerleInstallDir=%~dp0..\Nemerle\bin\%Config%
SET KEY_NAME="HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Microsoft\VisualStudio\SxS\VS7"
SET VALUE_NAME=15.0
setlocal ENABLEEXTENSIONS

@echo Starting build %Name% Target=%Target% Config=%Config% NemerleInstallDir="%NemerleInstallDir%"

for /f "tokens=2,*" %%a in ('reg query %KEY_NAME% /V %VALUE_NAME%  ^|findstr /ri "REG_SZ"') DO set Value=%%b

IF defined Value (
call "%Value%Common7\Tools\VsDevCmd.bat"

IF NOT "%~3"=="skip-build-boot" msbuild %~dp0Common\BootTasks.proj /t:BuildBoot /p:NemerleBinPathRoot=%NemerleInstallDir%

IF NOT "%~3"=="build-boot-only" (
msbuild %~dp0Nitra-Stagt1.sln /p:NemerleBinPathRoot=%NemerleInstallDir% /t:Restore
msbuild %~dp0Nitra-Stagt1.sln /p:NemerleBinPathRoot=%NemerleInstallDir% /t:%Target%
)

) else (
@echo %KEY_NAME%\%VALUE_NAME% not found in Windows Registry. Possibly no Visual Studio 2017 installed.
)
rem pause

