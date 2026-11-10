@echo off
rem Build-Core.cmd - Core-line entry build of nitra (requires no .NET Framework targeting packs).
rem Usage: Build-Core.cmd [Config] [Target] [skip-build-boot|build-boot-only]
rem   Config - Debug (default) or Release
rem   Target - Build (default) or Rebuild
rem MSBuild is located via vswhere (VS2019+ / VS18), no VS2017 registry pin, no VsDevCmd.
rem NemerleBinPathRoot: env var wins, else sibling drop ..\Nemerle\bin\<Config> (no machine paths committed).

SET Name=Nitra
SET Config=%~1
IF "%Config%"=="" SET Config=Debug
SET Target=%~2
IF "%Target%"=="" SET Target=Build
SET Mode=%~3
SET MSBUILDENABLEALLPROPERTYFUNCTIONS=1

SET NemerleBinPathRootLocal=%NemerleBinPathRoot%
IF "%NemerleBinPathRootLocal%"=="" SET NemerleBinPathRootLocal=%~dp0..\Nemerle\bin\%Config%

SET VSWHERE=%ProgramFiles(x86)%\Microsoft Visual Studio\Installer\vswhere.exe
IF NOT EXIST "%VSWHERE%" SET VSWHERE=%ProgramFiles%\Microsoft Visual Studio\Installer\vswhere.exe
IF NOT EXIST "%VSWHERE%" (
  @echo vswhere.exe not found. Visual Studio ^(2019+^) with Microsoft.Component.MSBuild is required.
  EXIT /B -1
)

FOR /F "usebackq delims=" %%i IN (`"%VSWHERE%" -latest -products * -requires Microsoft.Component.MSBuild -find MSBuild\**\Bin\MSBuild.exe`) DO IF NOT DEFINED MSB SET MSB=%%i

IF "%MSB%"=="" (
  @echo MSBuild.exe not found via vswhere.
  EXIT /B -1
)

@echo [Build-Core] MSBuild=%MSB%
@echo [Build-Core] Config=%Config% Target=%Target% NemerleBinPathRoot=%NemerleBinPathRootLocal%

IF NOT "%Mode%"=="skip-build-boot" (
  @echo [Build-Core] Building boot ^(BuildBoot^)...
  "%MSB%" %~dp0Common\BootTasks.proj /t:BuildBoot /p:NemerleBinPathRoot=%NemerleBinPathRootLocal%
  IF ERRORLEVEL 1 EXIT /B 1
)

IF NOT "%Mode%"=="build-boot-only" (
  @echo [Build-Core] Restoring Nitra-Core.slnf...
  "%MSB%" %~dp0Nitra-Core.slnf /t:Restore /p:NemerleBinPathRoot=%NemerleBinPathRootLocal%
  IF ERRORLEVEL 1 EXIT /B 1
  @echo [Build-Core] %Target%ing Nitra-Core.slnf...
  "%MSB%" %~dp0Nitra-Core.slnf /t:%Target% /p:NemerleBinPathRoot=%NemerleBinPathRootLocal%
  IF ERRORLEVEL 1 EXIT /B 1
)

@echo [Build-Core] Done.
