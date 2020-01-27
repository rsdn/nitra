SET Config=Release
SET MSBUILDENABLEALLPROPERTYFUNCTIONS=1
SET NemerleBinPathRoot=c:\RSDN\nemerle\bin\%Config%

msbuild Common\BootTasks.proj /t:BuildBoot 

.nuget\NuGet.exe restore Nitra-Stagt1.sln
msbuild Nitra-Stagt1.sln /t:Restore
msbuild Nitra-Stagt1.sln 

