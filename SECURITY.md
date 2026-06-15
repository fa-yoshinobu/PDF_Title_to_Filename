# Security Policy

## Supported Versions

Only the latest public release is supported for security fixes.

Older releases, forks, and locally modified builds may not receive security updates. If you are using a self-contained EXE, update to the latest released build to receive runtime and dependency security fixes.

## Reporting a Vulnerability

Please do not disclose security vulnerabilities in public issues, discussions, or pull requests before a fix is available.

Use GitHub's private vulnerability reporting or a GitHub Security Advisory for this repository when available. If private reporting is not available, open a minimal public issue asking for a private contact channel, without including exploit details.

When reporting, please include:

- Affected application version and operating system
- Steps to reproduce
- Expected and actual impact
- Proof-of-concept details, logs, or sample files if safe to share
- Any known affected dependency or advisory ID

Reports are handled on a best-effort basis. There is no paid bug bounty program.

## Scope

In scope:

- Vulnerabilities in this application code
- Unsafe file handling, command execution, configuration handling, or privilege handling
- Vulnerable direct or transitive dependencies used by released builds

Out of scope:

- Vulnerabilities in Windows, .NET, NuGet, or other third-party platforms that should be reported upstream
- Issues that require a malicious local administrator
- Social engineering, phishing, or physical access scenarios
- Denial-of-service cases that only affect the reporter's own local files

## Disclosure

Please allow reasonable time for investigation and release preparation before public disclosure. Security fixes may be released as a patch version with a brief changelog entry.

---

# セキュリティポリシー

## サポート対象バージョン

セキュリティ修正の対象は、原則として最新の公開リリースのみです。

古いリリース、fork、ローカルで変更されたビルドには、セキュリティ更新が提供されない場合があります。自己完結型 EXE を使用している場合、ランタイムや依存関係のセキュリティ修正を受けるには最新リリースへ更新してください。

## 脆弱性の報告

修正が提供される前に、公開 issue、discussion、pull request へ脆弱性の詳細を書かないでください。

利用可能な場合は、このリポジトリの GitHub private vulnerability reporting または GitHub Security Advisory を使用してください。非公開報告が利用できない場合は、攻撃手順や詳細を含めず、非公開連絡手段を求める最小限の公開 issue を作成してください。

報告には可能な範囲で次の情報を含めてください。

- 影響を受けるアプリのバージョンと OS
- 再現手順
- 想定される影響と実際の影響
- 安全に共有できる PoC、ログ、サンプルファイル
- 関連する依存関係や advisory ID

報告への対応はベストエフォートです。有償のバグバウンティ制度はありません。

## 対象範囲

対象:

- このアプリケーションコードの脆弱性
- ファイル処理、コマンド実行、設定処理、権限処理に関する問題
- リリースビルドで使用する直接・推移依存関係の脆弱性

対象外:

- Windows、.NET、NuGet、その他の第三者プラットフォーム自体の脆弱性
- 悪意あるローカル管理者権限が必要な問題
- ソーシャルエンジニアリング、フィッシング、物理アクセスが前提の問題
- 報告者自身のローカルファイルにのみ影響する DoS

## 公開

調査とリリース準備のため、公開前に合理的な猶予をお願いします。セキュリティ修正は patch version と簡潔な changelog として公開される場合があります。
