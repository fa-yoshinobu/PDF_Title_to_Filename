@echo off
echo ========================================
echo PDF Title to Filename - Debug Build
echo ========================================
echo.

echo [1/4] Restoring dependencies...
dotnet restore PDF_Title_to_Filename\PDF_Title_to_Filename.csproj
if %ERRORLEVEL% neq 0 (
    echo Error: Failed to restore dependencies
    pause
    exit /b 1
)
echo Dependencies restored successfully
echo.

echo [2/4] Executing Debug build...
echo Executing Debug build with detailed logging enabled
dotnet build PDF_Title_to_Filename\PDF_Title_to_Filename.csproj --configuration Debug --verbosity minimal
if %ERRORLEVEL% neq 0 (
    echo Error: Debug build failed
    pause
    exit /b 1
)
echo Debug build completed
echo.

echo [3/4] Verifying executable file...
if exist "PDF_Title_to_Filename\bin\Debug\net8.0-windows\win-x64\PDF_Title_to_Filename.exe" (
    echo Executable file generated
    echo Location: PDF_Title_to_Filename\bin\Debug\net8.0-windows\win-x64\PDF_Title_to_Filename.exe
) else (
    echo Warning: Executable file not found
)
echo.

echo [4/4] Displaying build information...
echo.
echo ========================================
echo Build Information
echo ========================================
echo Build Configuration: Debug
echo Detailed Logging: Enabled (ENABLE_DETAILED_LOGGING)
echo Target: net8.0-windows
echo Platform: win-x64
echo Optimization: Disabled (for debugging)
echo Debug Symbols: Enabled
echo.

echo ========================================
echo Debug build completed!
echo ========================================
echo.
echo Usage:
echo 1. Run PDF_Title_to_Filename\bin\Debug\net8.0-windows\win-x64\PDF_Title_to_Filename.exe
echo 2. Detailed logs will be displayed in the log area
echo 3. Easy to identify issues and debug
echo.
echo Note: This build is not for production distribution
echo Use build.bat for production distribution
echo.

pause
