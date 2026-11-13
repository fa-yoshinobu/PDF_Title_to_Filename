# Changelog

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/).

All notable changes are recorded from this point onward.

## [Unreleased]

### Added

- "Remove No Metadata" button that removes PDFs without usable metadata from the list in one click. Files on disk are not touched.

### Fixed

- Changing settings after a rename no longer resets renamed files to "No Metadata".
- Generated filenames are capped at 240 characters after the parts are joined, not only per part.
- The same file can no longer be added twice with different letter case in its path.
- The toolbar wraps and the progress bar stretches instead of being clipped in narrow windows.

### Changed

- Align local and CI executable, fixed-name ZIP and UTF-8/LF SHA-256 output under `artifacts/release/`; preserve required distribution files and require the VirusTotal API SHA-256 to match.


## [1.0.9] - 2026-11-10

### Changed

- Updated the bundled .NET runtime and Microsoft.Extensions libraries to 10.0.14 and PdfPig to 0.1.16.
- Replaced the application icon with a PDF page and filename field design, with 9 sizes from 16 to 256 px. The AI edition uses the same design with an AI badge.

## [1.0.8] - 2026-10-27

### Changed

- Standardized fixed-name Windows ZIP assets, ZIP SHA-256 checksums and English release notes.

### Fixed

- Unified VirusTotal completion checks and preserved unrelated release notes during rescans.
