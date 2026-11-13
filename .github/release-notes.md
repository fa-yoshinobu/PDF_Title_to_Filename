## Download
- [Windows x64 ZIP]({{DOWNLOAD_URL}})
- [SHA-256 checksum]({{CHECKSUM_URL}})

## Getting started
Extract the ZIP file, then run `PDF_Title_to_Filename.exe`.

Requirements: Windows 10 or Windows 11 (x64). The .NET 10.0.14 runtime is included.

## Changes
### Added
- Remove PDFs without usable metadata from the list in one click, without deleting files.
- Filter files by status, edit new filenames directly, and use a right-click menu for common actions.
- Confirm rename operations and undo the last rename batch.
- Use keyboard shortcuts for file selection, renaming, preview refresh, and undo.
- Remember the window position, size, and log panel height.

### Changed
- Redesigned the main window with clearer controls, visible column headers, status counts, and a resizable log panel.
- Updated the English and Japanese screenshots in the README.
- Align local and CI executable, fixed-name distribution ZIP and SHA-256 output under `artifacts/release/`.
- Require the VirusTotal API SHA-256 to match every published executable before reporting a completed scan.

### Fixed
- Preserve renamed-file status when settings change and preserve filename settings when switching languages.
- Cap the complete generated filename at 240 characters and prevent duplicate file entries with differently cased paths.
- Keep the toolbar and progress display usable in narrow windows and show translated warning messages.
- Size the operation column to its translated header to prevent clipping in English.

<!-- virustotal:start -->
{{VIRUSTOTAL_CONTENT}}
<!-- virustotal:end -->
