import hashlib
import importlib.util
from pathlib import Path
import tempfile
import unittest
import zipfile

SPEC = importlib.util.spec_from_file_location("release_assets", Path(__file__).resolve().parents[2] / ".github/scripts/release_assets.py")
assets = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(assets)


class ReleaseAssetsTests(unittest.TestCase):
    def test_zip_preserves_executable_and_companion_data_and_hashes_the_zip(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            source = root / "input.zip"
            with zipfile.ZipFile(source, "w") as archive:
                archive.writestr("App/App.exe", b"MZ test")
                archive.writestr("App/config.json", b"{}")
            result = root / "App-win-x64.zip"
            assets.prepare_archive(result, source)
            self.assertEqual(source.read_bytes(), result.read_bytes())
            checksum = result.with_name(result.name + ".sha256").read_bytes()
            self.assertEqual(checksum, (hashlib.sha256(result.read_bytes()).hexdigest() + "  App-win-x64.zip\n").encode())

    def test_single_executable_keeps_its_original_name(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            executable = root / "App.exe"
            executable.write_bytes(b"MZ test")
            assets.prepare_archive(root / "App-win-x64.zip", files=[executable])
            with zipfile.ZipFile(root / "App-win-x64.zip") as archive:
                self.assertEqual(archive.namelist(), ["App.exe"])

    def test_rejects_unsafe_or_nonexecutable_archives(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            for member in ["../App.exe", "C:/App.exe", "readme.txt"]:
                with self.subTest(member=member):
                    with zipfile.ZipFile(root / "input.zip", "w") as archive:
                        archive.writestr(member, b"test")
                    with self.assertRaises(ValueError):
                        assets.prepare_archive(root / "output.zip", root / "input.zip")

    def test_notes_use_version_specific_links_and_preserve_scan_block(self):
        template = "## Download\n{{DOWNLOAD_URL}}\n{{CHECKSUM_URL}}\n" + assets.START + "\n{{VIRUSTOTAL_CONTENT}}\n" + assets.END
        block = "## Security scan\n[VirusTotal results](https://example.test/result)"
        current = "Existing\n" + assets.START + "\n" + block + "\n" + assets.END + "\nUnrelated notes"
        result = assets.render_notes(template, "owner/repo", "v1.2.3", "App-win-x64.zip", current)
        self.assertIn("/download/v1.2.3/App-win-x64.zip.sha256", result)
        self.assertIn(block, result)
        self.assertNotIn("{{", result)

    def test_notes_reject_malformed_blocks_and_untranslated_text(self):
        for template in [assets.START, "日本語", "{{UNKNOWN}}"]:
            with self.subTest(template=template), self.assertRaises(ValueError):
                assets.render_notes(template, "owner/repo", "v1.2.3", "App-win-x64.zip")


if __name__ == "__main__":
    unittest.main()
