@echo off
if exist %OutDir% rmdir %OutDir% /S /Q
mkdir %OutDir%
copy %~dp0\..\Grammars\Bin\%Configuration%\*.* %OutDir% /B /Z 1>nul
pushd .
cd %OutDir%
rem System.Xml.Linq implementation assembly is not seen by ncc implicit refs on Core host (see plan retarget-nitra-net10-plan, R4.3)
set "NETFW="
for /f "tokens=2 delims=[]" %%p in ('dotnet --list-runtimes ^| findstr /r /c:"Microsoft.NETCore.App"') do set "NETFW=%%p"
set "NETVER="
for /f "tokens=2" %%v in ('dotnet --list-runtimes ^| findstr /r /c:"Microsoft.NETCore.App"') do set "NETVER=%%v"
if not defined NETFW echo ERROR: cannot resolve Microsoft.NETCore.App shared framework directory & exit /b 1
if not defined NETVER echo ERROR: cannot resolve Microsoft.NETCore.App runtime version & exit /b 1
"%Nemerle%\Nemerle.Compiler.Test.exe" %Tests%\*.n %Tests%\*.cs %Tests%\*.nitra -output:%OutDir% -ref:System.Core -ref:%RuntimeDllPath%\Nitra.Runtime.dll -macro:%CompilerDllPath%\Nitra.Compiler.dll -ref:"%NETFW%\%NETVER%\System.Private.Xml.Linq.dll" -r dotnet -rp exec %TeamCityArgs%
popd