@echo off
rem Core-line entry build without bootstrapping (assumes Boot2 outputs are up to date).
call %~dp0Build-Core.cmd "%~1" "%~2" skip-build-boot
