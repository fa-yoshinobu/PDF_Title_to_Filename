@echo off
setlocal enabledelayedexpansion
echo ========================================
echo PDF Title to Filename - Release Build
echo ========================================
echo Executing optimized build for production distribution
echo Detailed logging is disabled
echo.

cd /d "%~dp0"

echo [1/3] Restoring dependencies...
dotnet restore --verbosity minimal

echo [2/3] Executing Release build...
echo Executing optimized production build
dotnet publish PDF_Title_to_Filename\PDF_Title_to_Filename.csproj --configuration Release --verbosity minimal --output publish

if %ERRORLEVEL% EQU 0 (
    echo Release build completed
    echo.
    echo [3/3] Displaying build information...
    echo.
    echo ========================================
    echo Build Information
    echo ========================================
    echo Build Configuration: Release
    echo Detailed Logging: Disabled (Optimized)
    echo Target: net8.0-windows
    echo Platform: win-x64
    echo Optimization: Enabled
    echo Debug Symbols: Disabled
    echo.
    echo Executable Location:
    echo    %~dp0publish\PDF_Title_to_Filename.exe
    echo.
    if exist "%~dp0publish\PDF_Title_to_Filename.exe" (
        for %%i in ("%~dp0publish\PDF_Title_to_Filename.exe") do (
            set /a "size_mb=%%~zi/1024/1024"
            echo File Information:
            echo    - Size: !size_mb! MB (%%~zi bytes)
            echo    - Compressed single file
            echo    - Self-contained (.NET Runtime not required)
            echo    - Ready for distribution
        )
    )
    echo.
    echo ========================================
    echo Release build completed!
    echo ========================================
    echo.
    echo Usage:
    echo 1. Distribute publish\PDF_Title_to_Filename.exe
    echo 2. Users can run without .NET Runtime
    echo 3. Optimized performance
    echo.
) else (
    echo Error: Release build failed
)

pause
