@echo off
setlocal enabledelayedexpansion
echo Building PDF Title to Filename Ver1.00 (Single File)...
echo.

cd /d "%~dp0"

echo Restoring packages...
dotnet restore --verbosity minimal

echo Publishing optimized single file...
dotnet publish PDF_Title_to_Filename\PDF_Title_to_Filename.csproj --configuration Release --verbosity minimal --output publish

if %ERRORLEVEL% EQU 0 (
    echo.
    echo Build successful!
    echo.
    echo Single executable location:
    echo    %~dp0publish\PDF_Title_to_Filename.exe
    echo.
    if exist "%~dp0publish\PDF_Title_to_Filename.exe" (
        for %%i in ("%~dp0publish\PDF_Title_to_Filename.exe") do (
            set /a "size_mb=%%~zi/1024/1024"
            echo File information:
            echo    - Size: !size_mb! MB (%%~zi bytes)
            echo    - Compressed single file
            echo    - Self-contained (no .NET runtime required)
            echo    - Ready for distribution
        )
    )
    echo.
) else (
    echo Build failed!
)

pause
