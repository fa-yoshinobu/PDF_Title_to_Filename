# PowerShell Build Script for PDF Title to Filename
# 日本語対応版

Write-Host "Building PDF Title to Filename Ver1.00 (Single File)..." -ForegroundColor Green
Write-Host ""

# 現在のディレクトリに移動
Set-Location $PSScriptRoot

Write-Host "Restoring packages..." -ForegroundColor Yellow
dotnet restore --verbosity minimal

if ($LASTEXITCODE -eq 0) {
    Write-Host "Publishing optimized single file..." -ForegroundColor Yellow
    dotnet publish PDF_Title_to_Filename\PDF_Title_to_Filename.csproj --configuration Release --verbosity minimal --output publish

    if ($LASTEXITCODE -eq 0) {
        Write-Host ""
        Write-Host "Build successful!" -ForegroundColor Green
        Write-Host ""
        Write-Host "Single executable location:" -ForegroundColor Cyan
        Write-Host "   $PSScriptRoot\publish\PDF_Title_to_Filename.exe" -ForegroundColor White
        
        $exePath = "$PSScriptRoot\publish\PDF_Title_to_Filename.exe"
        if (Test-Path $exePath) {
            $fileInfo = Get-Item $exePath
            $sizeMB = [math]::Round($fileInfo.Length / 1MB, 2)
            
            Write-Host ""
            Write-Host "File information:" -ForegroundColor Cyan
            Write-Host "   - Size: $sizeMB MB ($($fileInfo.Length) bytes)" -ForegroundColor White
            Write-Host "   - Compressed single file" -ForegroundColor White
            Write-Host "   - Self-contained (no .NET runtime required)" -ForegroundColor White
            Write-Host "   - Ready for distribution" -ForegroundColor White
        }
        Write-Host ""
    } else {
        Write-Host "Build failed!" -ForegroundColor Red
    }
} else {
    Write-Host "Package restore failed!" -ForegroundColor Red
}

Write-Host "Press any key to continue..."
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
