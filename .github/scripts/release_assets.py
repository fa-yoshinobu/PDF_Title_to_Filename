"""Prepare fixed-name ZIP assets and English release notes without publishing."""

import argparse
import hashlib
from pathlib import Path, PurePosixPath
import re
import shutil
import stat
import zipfile

START = "<!-- virustotal:start -->"
END = "<!-- virustotal:end -->"


def render_notes(template, repository, tag, asset_name, current_body=None):
    if not re.fullmatch(r"[A-Za-z0-9_.-]+/[A-Za-z0-9_.-]+", repository):
        raise ValueError("Use an owner/repository name.")
    if not re.fullmatch(r"v\d+\.\d+\.\d+(?:-[A-Za-z0-9.-]+)?", tag):
        raise ValueError("Use a version tag such as v1.2.3.")
    block = "## Security scan\nVirusTotal scan pending."
    if current_body is not None and (START in current_body or END in current_body):
        if current_body.count(START) != 1 or current_body.count(END) != 1 or current_body.index(START) > current_body.index(END):
            raise ValueError("Invalid VirusTotal managed block.")
        block = current_body.split(START, 1)[1].split(END, 1)[0].strip()
    base = f"https://github.com/{repository}/releases/download/{tag}/{asset_name}"
    notes = template.replace("{{DOWNLOAD_URL}}", base).replace("{{CHECKSUM_URL}}", base + ".sha256")
    notes = notes.replace("{{RELEASE_TAG}}", tag).replace("{{VIRUSTOTAL_CONTENT}}", block)
    if re.search(r"\{\{[A-Z_]+\}\}", notes):
        raise ValueError("Unresolved release note placeholder.")
    if re.search(r"[\u3040-\u30ff\u3400-\u9fff]", notes):
        raise ValueError("Translate release notes into English; source documents may keep their language.")
    if notes.count(START) != 1 or notes.count(END) != 1 or notes.index(START) > notes.index(END):
        raise ValueError("The template must contain one VirusTotal managed block.")
    return notes.rstrip() + "\n"


def prepare_archive(destination, source_zip=None, files=()):
    if source_zip:
        source_zip = Path(source_zip)
        with zipfile.ZipFile(source_zip) as archive:
            has_executable = False
            for entry in archive.infolist():
                name = entry.filename.replace("\\", "/")
                path = PurePosixPath(name)
                if path.is_absolute() or ".." in path.parts or ":" in name or stat.S_ISLNK(entry.external_attr >> 16):
                    raise ValueError("Unsafe archive member.")
                has_executable |= not entry.is_dir() and name.lower().endswith(".exe")
            if not has_executable:
                raise ValueError("The distribution ZIP must contain an executable.")
            if archive.testzip() is not None:
                raise ValueError("Corrupt distribution ZIP.")
        if source_zip.resolve() != destination.resolve():
            shutil.copyfile(source_zip, destination)
    else:
        paths = [Path(p) for p in files]
        if not paths or not any(p.suffix.lower() == ".exe" for p in paths):
            raise ValueError("Include at least one executable.")
        if any(not p.is_file() for p in paths) or len({p.name.casefold() for p in paths}) != len(paths):
            raise ValueError("Missing input file or duplicate archive filename.")
        with zipfile.ZipFile(destination, "w", zipfile.ZIP_DEFLATED) as archive:
            for path in paths:
                archive.write(path, path.name)
    with destination.open("rb") as stream:
        digest = hashlib.file_digest(stream, "sha256").hexdigest()
    destination.with_name(destination.name + ".sha256").write_text(
        f"{digest}  {destination.name}\n", encoding="utf-8", newline="\n")


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("command", choices=["prepare", "notes"])
    parser.add_argument("--repository", required=True)
    parser.add_argument("--release-tag", required=True)
    parser.add_argument("--asset-name", required=True)
    parser.add_argument("--template", type=Path, default=Path(".github/release-notes.md"))
    parser.add_argument("--output-dir", type=Path, default=Path("artifacts/release"))
    parser.add_argument("--current-body", type=Path)
    source = parser.add_mutually_exclusive_group()
    source.add_argument("--zip", type=Path)
    source.add_argument("--file", nargs="+")
    args = parser.parse_args()
    if not re.fullmatch(r"[A-Za-z0-9_.-]+-win-(?:x64|arm64|anycpu)\.zip", args.asset_name):
        parser.error("Use a fixed AppName-win-x64.zip asset name without a version.")
    if args.command == "prepare" and not (args.zip or args.file):
        parser.error("Specify --zip or --file.")
    notes = render_notes(args.template.read_text(encoding="utf-8"), args.repository, args.release_tag,
                         args.asset_name, args.current_body.read_text(encoding="utf-8") if args.current_body else None)
    args.output_dir.mkdir(parents=True, exist_ok=True)
    if args.command == "prepare":
        prepare_archive(args.output_dir / args.asset_name, args.zip, args.file or ())
    (args.output_dir / "release-notes.md").write_text(notes, encoding="utf-8", newline="\n")


if __name__ == "__main__":
    main()
