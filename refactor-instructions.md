# refactor-instructions.md — PDF_Title_to_Filename

作成日: 2026-06-11。根拠: ソース・README.md・ci.bat・git履歴の実読。
本リポジトリ単体で完結する指示書。本ファイル自体は untracked のままにし、コミットに含めない。

---

## 1. Objective

ユーザーの実ファイルをリネームするアプリとして、**生成されるファイル名と設定JSON互換を1文字も変えずに**、
(1) ファイル名生成・サニタイズの中核ロジックを特性テストで固定し、(2) DI と手動 new の混在や
握りつぶし async void などの明確に安全な整理を行う。namespace リネームや既知バグの「修正」はしない。

## 2. Project Understanding

- Windows WPF アプリ (net8.0-windows, 単一EXE self-contained)。PDFメタデータからファイル名を生成しリネーム。MIT License。
- namespace は歴史的経緯で `PdfTitleRenamer`（プロジェクト名と不一致だが現状維持）。
- DI構成（`App.xaml.cs`）: `ILogService`/`IPdfProcessingService`/`ILanguageService` をシングルトン登録。
- 中核:
  - `Models/FileNameSettings.cs`: 設定の JSON 永続化（exe同フォルダ→LocalAppData→Documents→Temp の優先順位）と
    `GenerateFileName`（有効要素を順に評価し、空要素をスキップして Separator で結合）。
  - `Services/PdfProcessingService.cs`: PdfPig でメタデータ抽出、`SanitizeFileName`（NFKC正規化オプション・
    無効文字置換・予約語回避・240文字切詰）、`GetUniqueFilePath`（`(1)` 連番）。
  - `ViewModels/MainWindowViewModel.cs`（616行）: ファイル一覧・プレビュー・一括処理・進捗・ログ整形。
    状態は言語非依存の固定文字列定数（`STATUS_*`）で管理。
  - `Services/LanguageService.cs`: resx ベースの日英切替。`FileNameElement._languageService`（public static）経由で
    モデル層からも参照される。
- テスト: `PDF_Title_to_Filename.Tests`（xUnit + Moq）に `LanguageServiceTests.cs` のみ。
  csproj に `InternalsVisibleTo` 設定済み。
- 検証: `ci.bat` = restore → `dotnet test` → publish smoke test。GitHub Actions（static-analysis / release / VirusTotal）。
- csproj は `AnalysisMode=All` + `EnforceCodeStyleInBuild` と厳しめ。

## 3. Behaviors To Preserve

1. **ファイル名生成仕様**: 有効要素のみ・空要素スキップ・Separator 結合、全要素空なら空文字
   （=「メタデータなし」扱い）、要素順序は `Elements` の並び順。
2. **サニタイズ仕様**: NFKC は Title/Author/Subject/Keywords のみ適用（OriginalFileName/Prefix/Suffix 除外）、
   無効文字→`_`、連続空白→1個、Trim、空なら `Untitled`、Windows予約語は先頭 `_`、240文字切詰、
   重複時 `(1)(2)...` 連番。
3. **既知バグも現挙動として保持**: `SanitizeFileNameWithNFKCControl`（`MainWindowViewModel.cs:585-613`）は
   生成済みファイル名を Separator で再分割して有効要素と index 対応させるため、空要素スキップや
   メタデータ内の Separator 文字でずれる。**修正すると出力ファイル名が変わるため、修正せず現挙動のままテストで固定**
   し、修正案は提案として報告する（保守者判断待ち）。
4. **設定JSON互換**: `PDF_Title_to_Filename.json` のスキーマ・保存場所優先順位・読み込み失敗時の
   デフォルトfallback。既存ユーザーの設定ファイルが読めなくなる変更は不可。
5. 処理スキップ規則（RenameComplete / NoTitle / NoChangeNeeded はスキップ）、全メタデータ項目無効時の
   処理ボタン無効化と警告、ステータス表示の種類。
6. 日英 UI 文言・ログ文言と言語切替（システム言語自動検出・英語fallback・設定永続化）。
7. csproj の `<Version>` と publish 設定、`.github/workflows/`。

## 4. Non-Negotiables

- 開始前に `git status` 確認。本ファイル以外の差分があれば停止・報告。
- 編集前に `ci.bat` を実行し baseline（成否・テスト件数）を記録。失敗なら作業中止・報告。
- 1論点=1コミット。無関係な整形・ついでの変更をしない（AnalysisMode=All のため警告増に注意）。
- NuGet の追加・更新はしない（Tests プロジェクトに必要なものは揃っている）。
- テストはファイルシステムを汚さないこと（一時ディレクトリを使い後始末する）。

## 5. Stop And Ask Conditions

1. テストを書いたら現実装と README の仕様記述が矛盾した場合。
2. 変更が生成ファイル名・設定JSON・リネーム動作に影響しうる場合（§3-3 の修正を含む）。
3. csproj / `.github/workflows/` / resx リソースに触れたくなった場合。
4. DI 登録の削除対象（§7-P4）が実は使われている形跡を見つけた場合。

## 6. Baseline Commands

```bat
cd /d D:\refactor\PDF_Title_to_Filename
git status
ci.bat
```

## 7. Debt Map

凡例: ✅=実装可 / ⚠️=条件付き(指定フェーズ厳守) / ❌=提案・報告のみ

| # | 負債 | 根拠 | 改善案 | 可否 |
|---|---|---|---|---|
| P1 | **潜在バグ**: NFKC適用の index ずれ（§3-3 参照） | `MainWindowViewModel.cs:585-613` vs `FileNameSettings.cs:210-246` | 修正せず現挙動を特性テストで固定 + 既知問題コメント。修正案（GenerateFileName が要素単位でNFKC適用済み値を返す構造）は提案として報告 | ❌ |
| P2 | 中核ロジック未テスト: `GenerateFileName` / `SanitizeFileName` / `GetUniqueFilePath` / `FileNameSettings.Load/Save` | Tests に `LanguageServiceTests.cs` のみ | 特性テスト追加（要素順序・空スキップ・NFKC有無・予約語・240文字・連番・JSON round-trip）。P1 のずれ挙動も「現状」としてテスト化 | ✅ |
| P3 | `FileNameElement._languageService` が public static フィールドで `FileNameSettings` からも直接参照 | `FileNameSettings.cs:264` | static 維持のまま private + internal プロパティ化の最小整理のみ可。DI化は提案 | ⚠️ |
| P4 | DI 登録と手動 `new` の混在: `SettingsWindow`/`AboutWindow`/`SettingsWindowViewModel` は DI 登録済みだが実際は `new` で生成 | `App.xaml.cs:81-87` vs `MainWindowViewModel.cs:477,501-502` | Grep で未解決登録であることを再確認した上で、**未使用の DI 登録の削除のみ**可。生成経路は変えない | ⚠️ |
| P5 | namespace `PdfTitleRenamer` とプロジェクト名の不一致 | 全 .cs | 一括リネームは差分巨大・resx/XAML に波及。**やらない**（提案のみ） | ❌ |
| P6 | `FileNameSettings.Save` の `test_write.tmp` 権限プローブ・例外全黙殺 | `FileNameSettings.cs:153-176` | 挙動維持のため触らない。記録のみ | ❌ |
| P7 | `ProcessFilesAsync` の `LogText +=` 累積（毎回プロパティ経由で文字列再構築、O(n²)） | `MainWindowViewModel.cs:310-448` | P2 完了後、StringBuilder 化のみ可（最終表示文字列が同一であること） | ⚠️ |
| P8 | `UpdateAllFilePreviews` が async void で包括 try/catch 無し | `MainWindowViewModel.cs:552-555` | try/catch + `_logService.LogError` 追加（挙動はログ追加のみ） | ✅ |
| P9 | README が約1000行・日英重複・マーケティング文書化 | `README.md` | 整理は任意・提案のみ | ❌ |

## 8. Implementation Phases

1. **Phase 0 — baseline**: `git status` / `ci.bat` 実行・記録。失敗なら停止。
2. **Phase 1 — 安全網 (P2)**: 特性テスト追加。private メンバーが必要なら `internal` 化
   （`InternalsVisibleTo` は設定済み）。P1 のずれは現挙動のままテスト化し、テストコード内コメントで
   既知問題と明記。README と矛盾したら §5-1 で停止。
3. **Phase 2 — 安全な整理 (P8, P4)**: async void への try/catch 追加。未使用 DI 登録の削除
   （削除前に `GetRequiredService<Views.SettingsWindow>` 等の解決箇所が無いことを Grep で確認）。
4. **Phase 3 — 小改善 (P3, P7)**: static フィールドの最小カプセル化、LogText の StringBuilder 化
   （Phase 1 のテストと表示文字列の同一性で担保）。
5. **Phase 4 — 提案のみ (P1, P5, P6, P9)**: 修正案・リネーム案を最終レポートに記載。実装禁止。

## 9. Verification Requirements

- 各フェーズで `ci.bat` 完走（test + publish smoke test）。
- Phase 1 のテストは変更前コードで全件パスすることを確認してから先へ進む。
- Phase 2-3 の後で `dotnet test` の件数・成否が同一（追加分を除く）であること。
- 手動確認（Phase 2 以降）: アプリ起動 → 言語切替 → 設定ウィンドウ開閉 → About 表示。
  実PDFのリネームは行わなくてよい（テストでカバー）。

## 10. Reporting Format

1. 実施フェーズ / 追加・変更ファイル / コミット一覧
2. baseline と最終の `ci.bat` 結果対比（テスト件数 before → after）
3. 最後に実行した検証コマンドと生出力
4. Stop And Ask 該当事項一覧（新規発見含む）
5. Phase 4 の設計提案（特に P1 修正案。実装していないことを明記）
6. スキップした確認とその理由

## 11. Out-of-scope

- P1 バグの修正（提案のみ）、namespace 一括リネーム
- `.github/workflows/`、バージョン番号、publish/トリミング設定の変更
- resx リソース・UI/XAML・文言の変更、README の書き直し
- NuGet 追加・更新、新機能追加、MVVMライブラリ導入、網羅的整形

---

## 12. Implementation Status

実装済み（2026-06-12）。

実施済みフェーズ:

- Phase 0: baseline 確認済み。
- Phase 1: filename/settings/language 周辺の特性テストを追加済み。P1 の NFKC index ずれは現挙動として固定し、修正は提案対象のまま維持。
- Phase 2: `UpdateAllFilePreviews` の例外ログ追加、未使用 window DI 登録削除済み。
- Phase 3: `FileNameElement` language service の最小カプセル化、処理ログの `StringBuilder` 化済み。
- Phase 4: P1/P5/P6/P9 は提案のみで、実装対象外のまま維持。

実装コミット:

- `8d68a03` Add filename behavior characterization tests
- `9cf7485` Log preview refresh failures
- `7462021` Remove unused window DI registrations
- `34159e6` Encapsulate filename language service
- `c2e63aa` Use StringBuilder for processing log text

検証:

- `ci.bat` 完走。
- 最終テスト結果: 11 passed。
- publish smoke test passed。

補足:

- 実 PDF のリネーム手動確認は行っていない。中核のファイル名生成・設定 round-trip・重複名処理は特性テストで固定済み。
