@echo off
title Chinh Nha Web Application
echo ====================================================
echo Khoi dong Ung dung Web Phan Bon Chinh Nha
echo ====================================================
echo.
echo Dang chay server voi Hot Reload (dotnet watch)...
echo.

pushd "%~dp0src\ChinhNha.Web"
dotnet watch run
popd

pause
