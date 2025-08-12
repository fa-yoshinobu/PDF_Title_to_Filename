@echo off
echo ========================================
echo PDF Title to Filename - Debug Build
echo ========================================
echo.

echo [1/4] 依存関係の復元...
dotnet restore PDF_Title_to_Filename\PDF_Title_to_Filename.csproj
if %ERRORLEVEL% neq 0 (
    echo エラー: 依存関係の復元に失敗しました
    pause
    exit /b 1
)
echo ✓ 依存関係の復元完了
echo.

echo [2/4] Debugビルドの実行...
echo 詳細ログが有効なDebugビルドを実行します
dotnet build PDF_Title_to_Filename\PDF_Title_to_Filename.csproj --configuration Debug --verbosity minimal
if %ERRORLEVEL% neq 0 (
    echo エラー: Debugビルドに失敗しました
    pause
    exit /b 1
)
echo ✓ Debugビルド完了
echo.

echo [3/4] 実行ファイルの確認...
if exist "PDF_Title_to_Filename\bin\Debug\net8.0-windows\win-x64\PDF_Title_to_Filename.exe" (
    echo ✓ 実行ファイルが生成されました
    echo 場所: PDF_Title_to_Filename\bin\Debug\net8.0-windows\win-x64\PDF_Title_to_Filename.exe
) else (
    echo 警告: 実行ファイルが見つかりません
)
echo.

echo [4/4] ビルド情報の表示...
echo.
echo ========================================
echo ビルド情報
echo ========================================
echo ビルド設定: Debug
echo 詳細ログ: 有効 (ENABLE_DETAILED_LOGGING)
echo ターゲット: net8.0-windows
echo プラットフォーム: win-x64
echo 最適化: 無効 (デバッグ用)
echo デバッグシンボル: 有効
echo.

echo ========================================
echo Debugビルド完了！
echo ========================================
echo.
echo 使用方法:
echo 1. PDF_Title_to_Filename\bin\Debug\net8.0-windows\win-x64\PDF_Title_to_Filename.exe を実行
echo 2. 詳細なログがログエリアに表示されます
echo 3. 問題の特定やデバッグが容易になります
echo.
echo 注意: このビルドは本番配布用ではありません
echo 本番配布には build.bat を使用してください
echo.

pause
