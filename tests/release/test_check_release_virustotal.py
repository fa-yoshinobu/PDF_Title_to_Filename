import hashlib
import importlib.util
import io
import json
from pathlib import Path
import subprocess
import tempfile
import unittest
from unittest import mock
import zipfile


SCRIPT = Path(__file__).resolve().parents[2] / ".github/scripts/check_release_virustotal.py"
SPEC = importlib.util.spec_from_file_location("check_release_virustotal", SCRIPT)
check = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(check)


def archive_bytes(entries):
    output = io.BytesIO()
    with zipfile.ZipFile(output, "w") as archive:
        for name, content in entries:
            archive.writestr(name, content)
    return output.getvalue()


class FakeGitHub:
    def __init__(self, output, files):
        self.work = output / "work"
        self.work.mkdir(parents=True)
        self.files = {index: content for index, (name, content) in enumerate(files, 1)}
        self.assets = [{"id": index, "name": name, "size": len(content)}
                       for index, (name, content) in enumerate(files, 1)]
        self.updates = []
        self.body = "Original release notes.\n"

    def release(self, tag):
        return {"id": 42, "draft": False, "published_at": "2026-01-01", "assets": self.assets, "body": self.body}

    def update_body(self, tag, release_id, content):
        self.updates.append(content)
        self.body = check.replace_block(self.body, content)

    def download(self, asset, destination):
        destination.write_bytes(self.files[asset["id"]])


class ReleaseVirusTotalTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.output = Path(self.temp.name)
        self.env = mock.patch.dict(check.os.environ, {"GH_TOKEN": "offline-test-token", "GITHUB_RUN_ID": "12345"})
        self.env.start()
        self.addCleanup(self.env.stop)
        self.addCleanup(self.temp.cleanup)

    def test_managed_block_preserves_original_and_concurrent_text(self):
        original = "User text before.\n" + check.START + "\nold result\n" + check.END + "\nUser text after.\n"
        updated = check.replace_block(original, "new result")
        self.assertEqual(updated, "User text before.\n" + check.START + "\nnew result\n" + check.END + "\nUser text after.\n")
        self.assertEqual(check.replace_block(updated, "new result"), updated)
        self.assertTrue(check.replace_block("untouched\n", "result").startswith("untouched\n"))

    def test_malformed_managed_block_is_not_overwritten(self):
        for body in [check.START, check.END, check.END + check.START, check.START * 2 + check.END]:
            with self.subTest(body=body), self.assertRaises(check.CheckError):
                check.replace_block(body, "new")

    def test_tag_is_url_quoted_and_draft_is_rejected(self):
        github = check.GitHub("owner/repo", self.output)
        with mock.patch.object(github, "api", return_value={"id": 1, "draft": False, "published_at": "now"}) as api:
            github.release("v 1/x?#")
            api.assert_called_once_with("repos/owner/repo/releases/tags/v%201%2Fx%3F%23")
        with mock.patch.object(github, "api", return_value={"id": 1, "draft": True, "published_at": "now"}):
            with self.assertRaises(check.CheckError):
                github.release("v1")

    def test_patch_uses_json_file_and_only_updates_body(self):
        github = check.GitHub("owner/repo", self.output)
        def invoke(args, **kwargs):
            self.assertIsInstance(args, list)
            self.assertNotIn("shell", kwargs)
            self.assertEqual(args[:3], ["gh", "api", "repos/owner/repo/releases/42"])
            self.assertEqual(args[3:6], ["--method", "PATCH", "--input"])
            payload = json.loads(Path(args[6]).read_text(encoding="utf-8"))
            self.assertEqual(payload, {"body": "line1\nline2 日本語"})
            return subprocess.CompletedProcess(args, 0, b"{}", b"")
        with mock.patch.object(check.subprocess, "run", side_effect=invoke):
            github.api("repos/owner/repo/releases/42", {"body": "line1\nline2 日本語"})

    def test_update_refetches_latest_body_and_rejects_replaced_release(self):
        github = check.GitHub("owner/repo", self.output)
        with mock.patch.object(github, "release", return_value={"id": 42, "body": "edit made during scan"}), mock.patch.object(github, "api") as api:
            github.update_body("v1", 42, "result")
            self.assertIn("edit made during scan", api.call_args.args[1]["body"])
        with mock.patch.object(github, "release", return_value={"id": 43, "body": "replacement"}), mock.patch.object(github, "api") as api:
            with self.assertRaises(check.CheckError):
                github.update_body("v1", 42, "result")
            api.assert_not_called()

    def test_api_errors_do_not_disclose_response_or_token(self):
        github = check.GitHub("owner/repo", self.output)
        with mock.patch.object(check.subprocess, "run", return_value=subprocess.CompletedProcess([], 1, b"private response", b"offline-test-token /private/path")):
            with self.assertRaises(check.CheckError) as context:
                github.api("repos/owner/repo")
        self.assertNotIn("private", str(context.exception))
        self.assertNotIn("token", str(context.exception))

    def test_bounded_copy_does_not_write_the_overflow_chunk(self):
        destination = io.BytesIO()
        with self.assertRaises(check.CheckError):
            check.copy_limited(io.BytesIO(b"123456"), destination, limit=5)
        self.assertLessEqual(len(destination.getvalue()), 5)

    def test_download_rejects_oversize_metadata_without_starting_gh(self):
        github = check.GitHub("owner/repo", self.output)
        with mock.patch.object(check.subprocess, "Popen") as popen:
            with self.assertRaises(check.CheckError):
                github.download({"id": 1, "size": check.MAX_BYTES + 1}, self.output / "file")
            popen.assert_not_called()

    def test_download_reads_asset_api_as_binary_and_checks_size(self):
        github = check.GitHub("owner/repo", self.output)
        process = mock.MagicMock()
        process.__enter__.return_value = process
        process.stdout = io.BytesIO(b"MZ123")
        process.wait.return_value = 0
        with mock.patch.object(check.subprocess, "Popen", return_value=process) as popen:
            github.download({"id": 7, "size": 5}, self.output / "file")
            self.assertEqual(popen.call_args.args[0], ["gh", "api", "repos/owner/repo/releases/assets/7", "--header", "Accept: application/octet-stream"])
            self.assertEqual((self.output / "file").read_bytes(), b"MZ123")
        process.stdout = io.BytesIO(b"short")
        with mock.patch.object(check.subprocess, "Popen", return_value=process):
            with self.assertRaises(check.CheckError):
                github.download({"id": 7, "size": 6}, self.output / "file")

    def test_zip_paths_are_never_extracted_and_non_executables_are_ignored(self):
        archive = archive_bytes([("../../escaped.exe", b"MZ exe"), ("project.psim", b"private project"), ("src/code.cs", b"source")])
        github = FakeGitHub(self.output, [("../bundle.zip", archive), ("project.psim", b"private")])
        files = list(check.executable_files(github, github.assets))
        self.assertEqual(len(files), 1)
        self.assertEqual(files[0][1].parent, github.work)
        self.assertEqual(files[0][1].name, "asset-1-member-0.exe")
        self.assertEqual(files[0][1].read_bytes(), b"MZ exe")
        self.assertFalse((self.output / "escaped.exe").exists())
        self.assertFalse((github.work / "project.psim").exists())

    def test_zip_executable_size_limit_is_checked_before_extraction(self):
        github = FakeGitHub(self.output, [("bundle.zip", archive_bytes([("program.exe", b"123456")]))])
        with mock.patch.object(check, "MAX_BYTES", 5):
            with self.assertRaises(check.CheckError):
                list(check.executable_files(github, github.assets))

    def test_duplicate_executables_scan_once_and_detections_do_not_block_publication(self):
        binary = b"MZ identical"
        github = FakeGitHub(self.output, [("PidSimulator.exe", binary), ("bundle.zip", archive_bytes([("PidSimulator.exe", binary), ("DEMO.psim", b"data")]))])
        def scan(path, digest, reports):
            self.assertEqual(path.read_bytes(), binary)
            return {"sha256": digest, "url": f"https://www.virustotal.com/gui/file/{digest}/detection", "complete": True,
                    "stats": {"malicious": 3, "suspicious": 1, "undetected": 60}, "error": ""}
        with mock.patch.object(check, "scan_file", side_effect=scan) as scanner:
            self.assertEqual(check.check_release("owner/repo", "v1", self.output, github), 0)
            self.assertEqual(scanner.call_count, 1)
        self.assertIn("scan in progress", github.updates[0])
        self.assertIn("[VirusTotal results", github.updates[-1])
        self.assertIn("/detection)", github.updates[-1])
        self.assertNotIn("malicious", github.body)
        self.assertNotIn("SHA-256", github.body)
        self.assertNotIn("actions/runs/12345", github.body)
        self.assertEqual(github.body.count(check.START), 1)
        self.assertTrue(github.body.startswith("Original release notes."))
        report = json.loads((self.output / "reports/release-virustotal.json").read_text(encoding="utf-8"))
        self.assertEqual(len(report["executables"]), 2)
        self.assertEqual(report["executables"][0]["stats"]["malicious"], 3)
        details = (self.output / "reports/release-virustotal.md").read_text(encoding="utf-8")
        self.assertIn("actions/runs/12345", details)
        self.assertIn("SHA-256", details)
        self.assertNotIn(str(self.output), json.dumps(report))
        self.assertTrue(all(path.suffix in (".json", ".md") for path in (self.output / "reports").rglob("*") if path.is_file()))

    def test_scan_failure_still_updates_body_and_returns_failure(self):
        github = FakeGitHub(self.output, [("program.exe", b"MZ")])
        with mock.patch.object(check, "scan_file", return_value={"sha256": "a" * 64, "url": "https://example.test", "complete": False, "stats": {}, "error": "incomplete"}):
            self.assertEqual(check.check_release("owner/repo", "v1", self.output, github), 1)
        self.assertIn("scan incomplete", github.updates[-1])
        self.assertTrue(github.body.startswith("Original release notes."))

    def test_release_asset_replacement_is_not_associated_with_old_completed_scan(self):
        github = FakeGitHub(self.output, [("program.exe", b"MZ")])
        def scan(path, digest, reports):
            github.assets[0]["updated_at"] = "2026-01-02T00:00:00Z"
            return {"sha256": digest, "url": f"https://www.virustotal.com/gui/file/{digest}/detection", "complete": True,
                    "stats": {"malicious": 0, "undetected": 60}, "checked_at": "2026-01-01T23:00:00+00:00"}
        with mock.patch.object(check, "scan_file", side_effect=scan):
            self.assertEqual(check.check_release("owner/repo", "v1", self.output, github), 1)
        self.assertIn("scan incomplete", github.updates[-1])
        self.assertNotIn("scan completed", github.updates[-1])
        self.assertNotIn("/detection", github.updates[-1])
        details = (self.output / "reports/release-virustotal.md").read_text(encoding="utf-8")
        self.assertIn("Rescan the changed release assets", details)
        self.assertIn("obsolete results", details)
        self.assertIn("Checked at (UTC): 2026-01-01T23:00:00+00:00", details)

    def test_download_failure_still_updates_incomplete_status(self):
        github = FakeGitHub(self.output, [("program.exe", b"MZ")])
        with mock.patch.object(github, "download", side_effect=check.CheckError("download failed")), mock.patch.object(check, "scan_file") as scanner:
            self.assertEqual(check.check_release("owner/repo", "v1", self.output, github), 1)
            scanner.assert_not_called()
        self.assertIn("scan incomplete", github.updates[-1])

    def test_no_executable_is_not_reported_as_scanned(self):
        github = FakeGitHub(self.output, [("docs.zip", archive_bytes([("readme.md", b"text")]))])
        self.assertEqual(check.check_release("owner/repo", "v1", self.output, github), 1)
        self.assertNotIn("scan completed", github.updates[-1])

    def test_scanner_invocation_retains_detection_results_and_validates_hash(self):
        binary = self.output / "program.exe"
        binary.write_bytes(b"MZ")
        digest = hashlib.sha256(b"MZ").hexdigest()
        reports = self.output / "reports"
        def run(args, **kwargs):
            folder = Path(args[args.index("--output-dir") + 1])
            self.assertEqual(Path(args[args.index("--file") + 1]), binary)
            self.assertEqual(args[0], check.sys.executable)
            self.assertNotIn("shell", kwargs)
            (folder / "virustotal.json").write_text(json.dumps({"sha256": digest, "status": "completed", "verdict": "completed", "checked_at": "2026-01-01T00:00:00+00:00", "stats": {"malicious": 2}}), encoding="utf-8")
            return subprocess.CompletedProcess(args, 0)
        with mock.patch.object(check.subprocess, "run", side_effect=run):
            result = check.scan_file(binary, digest, reports)
        self.assertTrue(result["complete"])
        self.assertEqual(result["stats"]["malicious"], 2)
        self.assertEqual(result["checked_at"], "2026-01-01T00:00:00+00:00")
        self.assertTrue(result["url"].endswith(digest + "/detection"))
        with mock.patch.object(check.subprocess, "run", side_effect=subprocess.TimeoutExpired("hidden-local-command", 1900)):
            result = check.scan_file(binary, digest, reports)
        self.assertFalse(result["complete"])
        self.assertNotIn("hidden-local-command", result["error"])

    def test_incomplete_scanner_report_without_hash_is_normal_failure(self):
        binary = self.output / "program.exe"
        binary.write_bytes(b"MZ")
        digest = hashlib.sha256(b"MZ").hexdigest()
        def run(args, **kwargs):
            folder = Path(args[args.index("--output-dir") + 1])
            (folder / "virustotal.json").write_text(json.dumps({"status": "incomplete", "verdict": "incomplete", "error": "missing key"}), encoding="utf-8")
            return subprocess.CompletedProcess(args, 1)
        with mock.patch.object(check.subprocess, "run", side_effect=run):
            result = check.scan_file(binary, digest, self.output / "reports")
        self.assertFalse(result["complete"])
        self.assertNotIn("SHA-256", result["error"])


if __name__ == "__main__":
    unittest.main()
