@echo off
setlocal
title Chinh Nha Web Application
echo ====================================================
echo Khoi dong Ung dung Web Phan Bon Chinh Nha
echo ====================================================
echo.

pushd "%~dp0src\ChinhNha.Web"
if /I "%~1"=="--watch" (
	echo Dang chay server voi Hot Reload ^(dotnet watch^)...
	echo.
	dotnet watch run
) else (
	echo Dang chay server mot lan ^(dotnet run^)...
	echo.
	dotnet run
)
set "EXIT_CODE=%ERRORLEVEL%"
popd

exit /b %EXIT_CODE%
