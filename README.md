# PDF Title to Filename

![.NET](https://img.shields.io/badge/.NET-8.0-purple.svg)
![WPF](https://img.shields.io/badge/WPF-Windows-blue.svg)
![Single File](https://img.shields.io/badge/Distribution-Single%20File-green.svg)
![License](https://img.shields.io/badge/License-MIT-yellow.svg)

PDFファイルのメタデータからタイトルを抽出し、そのタイトルをファイル名として自動設定する高速・軽量なWindowsアプリケーションです。

## ✨ 特徴

- **🚀 高速処理** - 大幅なコード最適化により処理速度を向上
- **📦 単一実行ファイル** - .NET Runtime不要、1つのファイルで完結
- **📁 ドラッグ&ドロップ対応** - PDFファイルを簡単にドラッグして一括処理
- **📊 表形式表示** - ファイル名、新しい名前、ステータスを見やすく表示
- **⚡ 軽量設計** - 不要なコードを削除し、最小限の依存関係
- **🔄 非同期処理** - UIをブロックしない高速な処理
- **🎨 モダンUI** - Material Design風の美しいインターフェース
- **🔍 フォルダ再帰検索** - フォルダをドロップするとサブフォルダも自動検索

## 🎯 パフォーマンス最適化

### **単一実行ファイル配布**
- ✅ **.NET Runtime不要** - 自己完結型実行ファイル
- ✅ **圧縮最適化** - EnableCompressionInSingleFile による最小サイズ
- ✅ **コードトリミング** - 未使用コードの自動削除
- ✅ **配布簡単** - 1つのexeファイルのみで動作

## 🖼️ 機能

### PDF処理機能
- **メタデータ抽出**: PDFファイルのタイトル情報を自動抽出
- **文字正規化**: 全角英数字の半角変換（NFKC正規化）
- **ファイル名サニタイズ**: Windowsの無効文字を自動置換
- **重複回避**: 同名ファイルには自動で連番を付与
- **エラー詳細表示**: エラー理由を詳細に表示
- **プレビュー機能**: 処理前に新しいファイル名を事前確認
- **フォルダ再帰検索**: ドロップしたフォルダ内の全PDFファイルを自動検索

### ユーザーインターフェース
- **表形式表示**: 現在の名前、新しい名前、ステータスを一覧表示
- **プログレスバー**: 処理進捗のリアルタイム表示
- **ログビューア**: 処理結果の詳細確認
- **低解像度対応**: 小さな画面でも使いやすい設計
- **ドラッグ&ドロップ**: ファイルまたはフォルダを直接ドロップ
- **ステータスアイコン**: 視覚的に分かりやすい状態表示
- **レスポンシブデザイン**: ウィンドウサイズに応じた自動調整

### ステータス表示
- **📋 リネーム予定**: 処理待ちのファイル
- **⚙️ 処理中...**: 現在処理中のファイル
- **✅ リネーム完了**: 正常にリネームされたファイル
- **⚠️ タイトルなし**: PDFにタイトル情報がないファイル
- **ℹ️ 変更不要**: 既に適切なファイル名のファイル
- **❌ エラー**: 処理中にエラーが発生したファイル

## 🛠️ 技術仕様

### 開発環境
- **.NET 8.0** (最新LTS)
- **WPF** (Windows Presentation Foundation)
- **C# 12.0**

### 最適化された依存関係
- **UglyToad.PdfPig** - 高速PDF処理
- **Microsoft.Extensions.DependencyInjection** - 依存性注入
- **Microsoft.Extensions.Logging** - 最小構成ログ

### アーキテクチャ
- **MVVM パターン** - Model-View-ViewModel設計
- **依存性注入** - 疎結合な設計
- **非同期処理** - async/awaitパターン
- **リアクティブUI** - INotifyPropertyChangedによる双方向バインディング
- **コマンドパターン** - RelayCommandによるUI操作

### 詳細実装仕様

#### PDF処理サービス (PdfProcessingService)
- **非同期処理**: Task.Runによるバックグラウンド処理
- **エンコーディング対応**: CodePagesEncodingProviderによるShift_JIS対応
- **ファイル名正規化**: NFKC正規化による全角→半角変換
- **無効文字除去**: Windows無効文字の自動置換
- **予約語回避**: Windows予約語の自動回避
- **重複処理**: 連番付与による重複回避

#### ビューモデル (MainWindowViewModel)
- **リアクティブ更新**: PropertyChangedによるUI自動更新
- **コマンド管理**: RelayCommandによるUI操作制御
- **非同期処理**: async/awaitによる非同期ファイル処理
- **エラーハンドリング**: 包括的な例外処理
- **プレビュー機能**: 処理前のファイル名予測

#### UI実装 (MainWindow)
- **ドラッグ&ドロップ**: ファイル・フォルダの直接ドロップ対応
- **再帰検索**: フォルダ内の全PDFファイル自動検索
- **アクセス権限処理**: 権限エラーの適切な処理
- **レスポンシブ**: ウィンドウサイズに応じた自動調整

## 🚀 ビルド・実行方法

### 本番用（推奨）
```bash
# 最適化された単一実行ファイルを作成
build.bat
```
**生成場所**: `publish/PDF_Title_to_Filename.exe`（単一ファイル、.NET Runtime不要）

### 開発用
```bash
# 依存関係の復元
dotnet restore

# 開発用ビルド
dotnet build --configuration Release

# 最適化された公開
dotnet publish --configuration Release
```

## 📖 使用方法

### 基本操作
1. **アプリケーション起動**: `PDF_Title_to_Filename.exe`を実行（.NET Runtime不要）
2. **ファイル追加**: 
   - PDFファイルをテーブルエリアにドラッグ&ドロップ
   - または「ファイル選択」ボタンをクリック
   - フォルダをドロップすると、フォルダ内のPDFファイルを自動検索
3. **プレビュー確認**: 予想されるファイル名をテーブルで確認
4. **処理実行**: 「処理開始」ボタンをクリック
5. **結果確認**: ステータス列とログエリアで処理結果を確認

### 表示項目
- **現在の名前**: 元のファイル名
- **新しい名前**: リネーム後のファイル名
- **ステータス**: 処理状況（待機中、処理中、リネーム完了、エラーなど）
- **操作**: ファイル削除ボタン

## 🔧 処理仕様

### ファイル名正規化
1. **文字変換**: 全角英数字 → 半角英数字 (NFKC正規化)
2. **無効文字除去**: Windows無効文字 (`< > : " / \\ | ? *`) → `_`
3. **空白正規化**: 連続する空白 → 単一空白
4. **予約語回避**: Windows予約語の自動回避
5. **長さ制限**: 250文字制限での自動切り詰め

### 重複処理
```
元ファイル: document.pdf
重複時: document(1).pdf, document(2).pdf, ...
```

### スマート処理
- **完了済みスキップ**: 「リネーム完了」のファイルは処理をスキップ
- **エラー詳細**: エラー理由をマウスオーバーで詳細表示
- **高速処理**: 最適化により従来の数倍の処理速度
- **プレビュー機能**: 処理前に新しいファイル名を事前確認

### エラーハンドリング
- **PDF読み込みエラー**: 破損ファイルやアクセス権限エラー
- **ファイル名エラー**: 無効文字や予約語の自動回避
- **重複エラー**: 同名ファイルの自動連番付与
- **権限エラー**: 読み取り専用ファイルの処理スキップ
- **ドラッグ&ドロップエラー**: 無効なファイル形式の適切な処理

### フォルダ再帰検索
- **サブフォルダ検索**: 指定フォルダ内の全サブフォルダを再帰的に検索
- **アクセス権限処理**: アクセスできないフォルダは自動スキップ
- **PDFファイル自動検出**: 拡張子による自動判定

## 🧪 開発・テスト

### 開発環境セットアップ
```bash
# リポジトリのクローン
git clone https://github.com/yourusername/PDF_Title_to_Filename.git
cd PDF_Title_to_Filename

# 依存関係の復元
dotnet restore

# デバッグビルド
dotnet build

# 最適化された公開
dotnet publish --configuration Release
```

### プロジェクト構造
```
PDF_Title_to_Filename/
├── PDF_Title_to_Filename.sln          # ソリューションファイル
├── PDF_Title_to_Filename/             # プロジェクトフォルダ
│   ├── PDF_Title_to_Filename.csproj   # プロジェクトファイル
│   ├── app.manifest                   # アプリケーションマニフェスト
│   ├── Icons/
│   │   └── app.ico                    # アプリケーションアイコン
│   ├── Models/                        # データモデル
│   │   ├── FileItem.cs                # ファイル項目モデル
│   │   └── ProcessingResult.cs        # 処理結果モデル
│   ├── Services/                      # ビジネスロジック
│   │   ├── IPdfProcessingService.cs   # PDF処理インターフェース
│   │   ├── PdfProcessingService.cs    # PDF処理実装
│   │   ├── ILogService.cs             # ログサービスインターフェース
│   │   └── LogService.cs              # ログサービス実装
│   ├── ViewModels/                    # MVVM ViewModel
│   │   ├── MainWindowViewModel.cs     # メインウィンドウViewModel
│   │   └── RelayCommand.cs            # コマンド実装
│   ├── Views/                         # WPF Views
│   │   ├── MainWindow.xaml            # メインウィンドウUI
│   │   └── MainWindow.xaml.cs         # メインウィンドウコード
│   ├── App.xaml                       # アプリケーション設定
│   └── App.xaml.cs                    # アプリケーションコード
├── build.bat                          # ビルドスクリプト
├── README.md                          # プロジェクト説明
└── .gitignore                         # Git除外設定
```

### テスト方法
1. **単体テスト**: 各サービスクラスの個別テスト
2. **統合テスト**: PDFファイル処理の一連の流れ
3. **UIテスト**: ドラッグ&ドロップとボタン操作
4. **パフォーマンステスト**: 大量ファイル処理の速度確認
5. **エラーハンドリングテスト**: 各種エラーケースの確認

### コード品質
- **MVVMパターン**: 厳密なMVVM設計の実装
- **依存性注入**: Microsoft.Extensions.DependencyInjection使用
- **非同期処理**: async/awaitパターンの適切な実装
- **エラーハンドリング**: 包括的な例外処理
- **ログ出力**: 本番環境に最適化されたログ設定

## 📝 ライセンス

このプロジェクトはMITライセンスの下で公開されています。

```
MIT License

Copyright (c) 2024 PDF Title to Filename

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
```

## 🤝 貢献

プロジェクトへの貢献を歓迎します！

### 貢献方法
1. このリポジトリをフォーク
2. 機能ブランチを作成 (`git checkout -b feature/AmazingFeature`)
3. 変更をコミット (`git commit -m 'Add some AmazingFeature'`)
4. ブランチにプッシュ (`git push origin feature/AmazingFeature`)
5. プルリクエストを作成

### 開発ガイドライン
- コードはC# 12.0の最新機能を活用
- MVVMパターンに従った設計
- 非同期処理の適切な実装
- エラーハンドリングの徹底
- パフォーマンスを考慮した実装
- 単体テストの作成

## 📞 サポート

問題や質問がある場合は、GitHubのIssuesページでお知らせください。

## 🔄 更新履歴

### Ver1.00 (2025-08-10)
- 初回リリース
- PDFタイトル抽出機能
- ファイル名正規化機能
- ドラッグ&ドロップ対応
- フォルダ再帰検索機能
- モダンUI実装
- 単一実行ファイル配布
- 包括的なエラーハンドリング
- リアクティブUI実装

