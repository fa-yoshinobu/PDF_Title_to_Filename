@echo off
setlocal enabledelayedexpansion
echo ========================================
echo PDF Title to Filename - Release Build
echo ========================================
echo 本番配布用の最適化されたビルドを実行します
echo 詳細ログは無効化されています
echo.

cd /d "%~dp0"

echo [1/3] 依存関係の復元...
dotnet restore --verbosity minimal

echo [2/3] Releaseビルドの実行...
echo 最適化された本番用ビルドを実行します
dotnet publish PDF_Title_to_Filename\PDF_Title_to_Filename.csproj --configuration Release --verbosity minimal --output publish

if %ERRORLEVEL% EQU 0 (
    echo ✓ Releaseビルド完了
    echo.
    echo [3/3] ビルド情報の表示...
    echo.
    echo ========================================
    echo ビルド情報
    echo ========================================
    echo ビルド設定: Release
    echo 詳細ログ: 無効 (最適化済み)
    echo ターゲット: net8.0-windows
    echo プラットフォーム: win-x64
    echo 最適化: 有効
    echo デバッグシンボル: 無効
    echo.
    echo 実行ファイル場所:
    echo    %~dp0publish\PDF_Title_to_Filename.exe
    echo.
    if exist "%~dp0publish\PDF_Title_to_Filename.exe" (
        for %%i in ("%~dp0publish\PDF_Title_to_Filename.exe") do (
            set /a "size_mb=%%~zi/1024/1024"
            echo ファイル情報:
            echo    - サイズ: !size_mb! MB (%%~zi bytes)
            echo    - 圧縮単一ファイル
            echo    - 自己完結型 (.NET Runtime不要)
            echo    - 配布準備完了
        )
    )
    echo.
    echo ========================================
    echo Releaseビルド完了！
    echo ========================================
    echo.
    echo 使用方法:
    echo 1. publish\PDF_Title_to_Filename.exe を配布
    echo 2. ユーザーは.NET Runtime不要で実行可能
    echo 3. 最適化されたパフォーマンス
    echo.
) else (
    echo エラー: Releaseビルドに失敗しました
)

pause
