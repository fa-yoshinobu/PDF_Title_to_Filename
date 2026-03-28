@echo off
setlocal

cd /d "%~dp0"

set "PROJECT=PDF_Title_to_Filename\PDF_Title_to_Filename.csproj"
set "CONFIG=Release"
set "RUNTIME=win-x64"
set "OUTDIR=artifacts\publish"
set "DO_CLEAN=0"

if /I "%~1"=="clean" set "DO_CLEAN=1"
if /I "%~2"=="clean" set "DO_CLEAN=1"
if /I "%~3"=="clean" set "DO_CLEAN=1"
if not "%~1"=="" if /I not "%~1"=="clean" set "RUNTIME=%~1"
if not "%~2"=="" if /I not "%~2"=="clean" set "OUTDIR=%~2"

echo Runtime: "%RUNTIME%"
echo Output : "%OUTDIR%"
if "%DO_CLEAN%"=="1" (
  echo Clean  : enabled
) else (
  echo Clean  : disabled
)

if "%DO_CLEAN%"=="1" (
  echo [0/3] clean...
  dotnet clean "%PROJECT%" -c %CONFIG%
  if errorlevel 1 goto :error
)

echo [1/3] restore...
dotnet restore "%PROJECT%"
if errorlevel 1 goto :error

echo [2/3] publish single-file exe...
dotnet publish "%PROJECT%" ^
  -c %CONFIG% ^
  -r %RUNTIME% ^
  --self-contained true ^
  --no-restore ^
  /p:PublishSingleFile=true ^
  /p:EnableCompressionInSingleFile=true ^
  /p:IncludeNativeLibrariesForSelfExtract=true ^
  /p:DebugType=None ^
  /p:DebugSymbols=false ^
  -o "%OUTDIR%"
if errorlevel 1 goto :error

echo [3/3] done.
echo Output: "%OUTDIR%\PDF_Title_to_Filename.exe"
exit /b 0

:error
echo Build failed.
exit /b 1
