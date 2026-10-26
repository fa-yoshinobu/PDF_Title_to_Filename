import io
from http.client import IncompleteRead
import json
import os
from pathlib import Path
import sys
import tempfile
import unittest
from unittest.mock import patch
from urllib.error import HTTPError

sys.path.insert(0, str(Path(__file__).resolve().parents[2] / ".github" / "scripts"))
import scan_virustotal as scanner


def analysis(malicious=0, suspicious=0, status="completed"):
    return {"data": {"attributes": {
        "status": status,
        "stats": {"malicious": malicious, "suspicious": suspicious, "undetected": 60, "harmless": 0,
                  "failure": 1, "timeout": 0, "confirmed-timeout": 0, "type-unsupported": 2},
        "results": {"Example AV": {"category": "malicious", "result": "Test.Detection"}} if malicious else {},
    }}}


class FakeClock:
    def __init__(self):
        self.now = 0

    def clock(self):
        return self.now

    def sleep(self, seconds):
        self.now += seconds


class FakeOpener:
    def __init__(self, clock, responses):
        self.clock = clock
        self.responses = iter(responses)
        self.calls = []

    def open(self, req, timeout):
        body = b"".join(req.data) if req.data is not None else None
        self.calls.append((self.clock.now, req, body))
        response = next(self.responses)
        if isinstance(response, Exception):
            raise response
        return io.BytesIO(json.dumps(response).encode())


class VirusTotalTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.directory = Path(self.temp.name)
        self.exe = self.directory / "PidSimulator.exe"
        self.exe.write_bytes(b"MZ-release-test-only")

    def client(self, responses, timeout=300):
        clock = FakeClock()
        opener = FakeOpener(clock, responses)
        client = scanner.VirusTotalClient("secret-test-value", timeout, opener=opener, clock=clock.clock, sleep=clock.sleep)
        return client, opener

    def test_upload_poll_and_report_match_exact_executable(self):
        client, opener = self.client([
            {"data": {"id": "analysis/id=="}}, analysis(status="queued"), analysis(status="in-progress"), analysis(),
        ])
        report = client.scan(self.exe, 30)
        self.assertEqual("completed", report["status"])
        self.assertEqual(scanner.sha256_file(self.exe), report["sha256"])
        self.assertEqual(f"https://www.virustotal.com/gui/file/{report['sha256']}/detection", report["url"])
        self.assertEqual(scanner.API + "/files", opener.calls[0][1].full_url)
        self.assertIn("analysis%2Fid%3D%3D", opener.calls[1][1].full_url)
        _, req, upload = opener.calls[0]
        self.assertEqual(int(req.get_header("Content-length")), len(upload))
        self.assertIn(self.exe.read_bytes(), upload)
        self.assertIn(b'filename="PidSimulator.exe"', upload)
        self.assertNotIn(str(self.directory).encode(), upload)
        self.assertTrue(all(later[0] - earlier[0] >= 16 for earlier, later in zip(opener.calls, opener.calls[1:])))

    def test_large_file_uses_one_time_upload_url(self):
        endpoint = "https://bigfiles.virustotal.com/upload/example"
        client, opener = self.client([{"data": endpoint}, {"data": {"id": "123"}}, analysis()])
        with patch.object(scanner, "DIRECT_UPLOAD_LIMIT", 1):
            client.scan(self.exe, 30)
        self.assertEqual(scanner.API + "/files/upload_url", opener.calls[0][1].full_url)
        self.assertEqual(endpoint, opener.calls[1][1].full_url)

    def test_untrusted_upload_url_never_receives_key_or_bytes(self):
        for endpoint in ("http://www.virustotal.com/upload", "https://evil.example/upload", "https://www.virustotal.com@evil.example/upload"):
            with self.subTest(endpoint=endpoint):
                client, opener = self.client([{"data": endpoint}])
                with patch.object(scanner, "DIRECT_UPLOAD_LIMIT", 1), self.assertRaises(scanner.ScanError):
                    client.scan(self.exe, 30)
                self.assertEqual(1, len(opener.calls))

    def test_legacy_upload_endpoint_is_upgraded_before_sending(self):
        endpoint = "http://www.virustotal.com/_ah/upload/example"
        client, opener = self.client([{"data": endpoint}, {"data": {"id": "123"}}, analysis()])
        with patch.object(scanner, "DIRECT_UPLOAD_LIMIT", 1):
            client.scan(self.exe, 30)
        self.assertEqual("https://www.virustotal.com/_ah/upload/example", opener.calls[1][1].full_url)

    def test_truncated_http_response_is_reported_without_raw_error(self):
        client, _ = self.client([IncompleteRead(b"secret-test-value")])
        with self.assertRaises(scanner.ScanError) as result:
            client.scan(self.exe, 30)
        self.assertNotIn("secret-test-value", str(result.exception))

    def test_detected_result_is_advisory_and_cli_succeeds(self):
        completed = scanner.completed_report(self.exe.name, scanner.sha256_file(self.exe), "123", analysis(2, 1))
        output = self.directory / "report"
        with patch.dict(os.environ, {"VIRUSTOTAL_API_KEY": "secret-test-value"}), \
                patch.object(scanner.VirusTotalClient, "scan", return_value=completed):
            code = scanner.main(["--file", str(self.exe), "--output-dir", str(output)])
        self.assertEqual(0, code)
        report = json.loads((output / "virustotal.json").read_text(encoding="utf-8"))
        self.assertEqual(2, report["stats"]["malicious"])
        markdown = (output / "virustotal.md").read_text(encoding="utf-8")
        self.assertIn(completed["url"], markdown)
        self.assertIn("| malicious | 2 |", markdown)
        self.assertIn("Example AV", markdown)
        self.assertNotIn("secret-test-value", markdown)
        self.assertNotIn(str(self.directory), markdown)

    def test_missing_secret_is_unfinished_not_checked(self):
        output = self.directory / "report"
        with patch.dict(os.environ, {"VIRUSTOTAL_API_KEY": ""}), patch.object(scanner.request, "build_opener") as connect:
            code = scanner.main(["--file", str(self.exe), "--output-dir", str(output)])
        self.assertEqual(1, code)
        connect.assert_not_called()
        markdown = (output / "virustotal.md").read_text(encoding="utf-8")
        self.assertIn("scan incomplete", markdown)
        self.assertNotIn("scan completed", markdown)

    def test_timeout_does_not_produce_completed_result(self):
        client, _ = self.client([{"data": {"id": "123"}}, analysis(status="queued")], timeout=40)
        with self.assertRaisesRegex(scanner.ScanError, "timed out"):
            client.scan(self.exe, 30)

    def test_rate_limit_get_retries_without_reupload(self):
        throttled = HTTPError(scanner.API, 429, "limited", {}, io.BytesIO(b"secret-test-value"))
        client, opener = self.client([{"data": {"id": "123"}}, throttled, analysis()])
        report = client.scan(self.exe, 30)
        self.assertEqual("completed", report["status"])
        self.assertEqual(1, sum(req.method == "POST" for _, req, _ in opener.calls))
        self.assertGreaterEqual(opener.calls[2][0] - opener.calls[1][0], 60)

    def test_failed_upload_is_not_repeated_and_does_not_expose_credentials(self):
        failed = HTTPError("https://www.virustotal.com/secret-url", 403, "secret-test-value", {}, io.BytesIO(b"secret-test-value"))
        client, opener = self.client([failed])
        with self.assertRaises(scanner.ScanError) as result:
            client.scan(self.exe, 30)
        self.assertEqual(1, len(opener.calls))
        self.assertNotIn("secret-test-value", str(result.exception))
        self.assertNotIn("secret-url", str(result.exception))

    def test_empty_negative_or_missing_engine_counts_are_not_checked(self):
        for stats in ({}, {"malicious": 0, "suspicious": 0, "undetected": 0, "harmless": 0},
                      {"malicious": -1, "suspicious": 0, "undetected": 60, "harmless": 0},
                      {"malicious": False, "suspicious": 0, "undetected": 60, "harmless": 0}):
            with self.subTest(stats=stats):
                response = analysis()
                response["data"]["attributes"]["stats"] = stats
                with self.assertRaises(scanner.ScanError):
                    scanner.completed_report(self.exe.name, "abc", "123", response)

    def test_mismatched_hash_rejected(self):
        response = analysis()
        response["meta"] = {"file_info": {"sha256": "different"}}
        with self.assertRaises(scanner.ScanError):
            scanner.completed_report(self.exe.name, scanner.sha256_file(self.exe), "123", response)

    def test_engine_markdown_cannot_inject_html_or_table_rows(self):
        response = analysis(1)
        response["data"]["attributes"]["results"] = {"<img> | AV\nrow": {"category": "malicious", "result": "<script>"}}
        report = scanner.completed_report(self.exe.name, "abc", "123", response)
        markdown = scanner.report_markdown(report)
        self.assertNotIn("<img>", markdown)
        self.assertNotIn("<script>", markdown)
        self.assertIn("&#124;", markdown)


if __name__ == "__main__":
    unittest.main()
