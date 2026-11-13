# Changelog

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/).

All notable changes are recorded from this point onward.

## [Unreleased]

## [1.1.0] - 2026-11-13

### Added

- "Remove No Metadata" button that removes PDFs without usable metadata from the list in one click. Files on disk are not touched.
- Status filter chips with per-status counts (All / Scheduled / No Metadata / Errors / Done).
- Edit the new filename directly in the list (double-click or F2); clearing it goes back to the generated name.
- Right-click menu: open file, open containing folder, copy new name, remove from list. Multi-select and the Delete key are supported.
- Confirmation before renaming, and "Undo" for the last rename batch (Ctrl+Z).
- Keyboard shortcuts: Ctrl+O (select files), Ctrl+Enter (run), F5 (refresh preview), Ctrl+Z (undo).
- The window position, size and log panel height are remembered.
- Error and status details are shown under the selected row.

### Changed

- Redesigned the main window: a unified, higher-contrast color palette, flat buttons with hover/disabled states, Segoe Fluent icons instead of emoji, a readable progress percentage and a resizable log panel that scrolls to the latest entry.
- The empty drop area can be clicked to select files, and the window highlights while dragging files over it.
- The language button shows "日本語" / "English".
- Updated the English and Japanese screenshots to show the redesigned interface.
- Align local and CI executable, fixed-name ZIP and UTF-8/LF SHA-256 output under `artifacts/release/`; preserve required distribution files and require the VirusTotal API SHA-256 to match.

### Fixed

- Changing settings after a rename no longer resets renamed files to "No Metadata".
- Generated filenames are capped at 240 characters after the parts are joined, not only per part.
- The same file can no longer be added twice with different letter case in its path.
- The toolbar wraps and the progress bar stretches instead of being clipped in narrow windows.
- Column headers in the file list were blank.
- The operation column now sizes to its translated header so the English label is not clipped.
- Switching the language no longer overwrites filename settings changed earlier in the same session.
- The "all metadata items disabled" warning showed resource keys instead of text.

## [1.0.9] - 2026-11-10

### Changed

- Updated the bundled .NET runtime and Microsoft.Extensions libraries to 10.0.14 and PdfPig to 0.1.16.
- Replaced the application icon with a PDF page and filename field design, with 9 sizes from 16 to 256 px. The AI edition uses the same design with an AI badge.

## [1.0.8] - 2026-10-27

### Changed

- Standardized fixed-name Windows ZIP assets, ZIP SHA-256 checksums and English release notes.

### Fixed

- Unified VirusTotal completion checks and preserved unrelated release notes during rescans.
