@echo off
setlocal

cd /d "%~dp0"

echo [1/4] restore...
dotnet restore PDF_Title_to_Filename.sln
if errorlevel 1 goto :error

echo [2/4] test...
dotnet test PDF_Title_to_Filename.sln -c Release --verbosity normal
if errorlevel 1 goto :error

echo [3/4] publish smoke test...
call build.bat win-x64 artifacts\ci
if errorlevel 1 goto :error
if not exist artifacts\ci\PDF_Title_to_Filename.exe goto :error

echo [4/4] done.
echo CI checks passed.
exit /b 0

:error
echo CI checks failed.
exit /b 1
