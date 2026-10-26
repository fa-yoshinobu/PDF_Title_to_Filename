"""Submit a release executable to VirusTotal and save an advisory report."""

import argparse
from datetime import datetime, timezone
import hashlib
import html
from http.client import HTTPException
import json
import os
from pathlib import Path
import socket
import time
from urllib import error, parse, request
import uuid


API = "https://www.virustotal.com/api/v3"
UPLOAD_HOSTS = {"www.virustotal.com", "virustotal.com", "bigfiles.virustotal.com"}
MAX_FILE_SIZE = 650_000_000
DIRECT_UPLOAD_LIMIT = 32_000_000


class ScanError(Exception):
    pass


class NoRedirect(request.HTTPRedirectHandler):
    def redirect_request(self, req, fp, code, msg, headers, newurl):
        raise ScanError("VirusTotal returned an unexpected redirect.")


def sha256_file(path):
    with path.open("rb") as stream:
        return hashlib.file_digest(stream, "sha256").hexdigest()


def multipart_file(path, boundary):
    # Only the basename is transmitted. Do not include local paths in submissions.
    filename = path.name.replace('"', "_").replace("\r", "_").replace("\n", "_")
    header = (
        f"--{boundary}\r\nContent-Disposition: form-data; name=\"file\"; "
        f"filename=\"{filename}\"\r\nContent-Type: application/octet-stream\r\n\r\n"
    ).encode("utf-8")
    footer = f"\r\n--{boundary}--\r\n".encode("ascii")

    def chunks():
        yield header
        with path.open("rb") as stream:
            while block := stream.read(1024 * 1024):
                yield block
        yield footer

    return chunks(), len(header) + path.stat().st_size + len(footer)


def secure_upload_url(url):
    if not isinstance(url, str):
        raise ScanError("VirusTotal did not return an upload endpoint.")
    parsed = parse.urlsplit(url)
    # The official large-file example uses HTTP. Upgrade only the known upload
    # endpoint; the API key and executable must always travel over HTTPS.
    if (parsed.scheme == "http" and parsed.hostname in {"www.virustotal.com", "virustotal.com"}
            and not parsed.username and not parsed.password and parsed.port in {None, 80}
            and parsed.path.startswith("/_ah/upload/")):
        return parse.urlunsplit(("https", parsed.hostname, parsed.path, parsed.query, ""))
    return url


class VirusTotalClient:
    def __init__(self, api_key, timeout_seconds, *, opener=None, clock=time.monotonic, sleep=time.sleep):
        if not api_key.strip():
            raise ScanError("GitHub Actions Secret VT_API_KEY is missing.")
        self.api_key = api_key.strip()
        self.opener = opener or request.build_opener(NoRedirect())
        self.clock = clock
        self.sleep = sleep
        self.deadline = clock() + timeout_seconds
        self.next_request = clock()

    def pause(self, seconds):
        if self.clock() + seconds >= self.deadline:
            raise ScanError("VirusTotal analysis timed out; analysis is not complete.")
        self.sleep(max(0, seconds))

    def call(self, method, url, body=None, content_type=None, content_length=None):
        parsed = parse.urlsplit(url)
        if (parsed.scheme != "https" or parsed.hostname not in UPLOAD_HOSTS
                or parsed.username or parsed.password or parsed.port not in {None, 443}):
            host = "".join(char for char in (parsed.hostname or "missing") if char.isascii() and (char.isalnum() or char in ".-"))[:200]
            raise ScanError(f"VirusTotal returned an unsupported upload host ({host}).")
        for attempt in range(3):
            self.pause(max(0, self.next_request - self.clock()))
            # The public API permits four requests per minute. Also space retries.
            self.next_request = self.clock() + 16
            headers = {"x-apikey": self.api_key, "Accept": "application/json"}
            if content_type:
                headers["Content-Type"] = content_type
                headers["Content-Length"] = str(content_length)
            req = request.Request(url, data=body, headers=headers, method=method)
            try:
                with self.opener.open(req, timeout=min(120, self.deadline - self.clock())) as response:
                    payload = json.load(response)
                if not isinstance(payload, dict):
                    raise ScanError("VirusTotal returned an invalid response.")
                return payload
            except error.HTTPError as ex:
                status = ex.code
                ex.close()
                if method == "GET" and status in {429, 500, 502, 503, 504} and attempt < 2:
                    self.pause(60)
                    continue
                # Never print the response, API key, or one-time upload URL.
                raise ScanError(f"VirusTotal API request failed (HTTP {status}).") from None
            except (error.URLError, socket.timeout, TimeoutError, OSError, HTTPException):
                raise ScanError("VirusTotal API connection failed; analysis is not complete.") from None
            except (ValueError, UnicodeError):
                raise ScanError("VirusTotal returned an invalid JSON response.") from None
        raise ScanError("VirusTotal API request failed.")

    def scan(self, path, poll_seconds):
        size = path.stat().st_size
        if not 0 < size <= MAX_FILE_SIZE:
            raise ScanError("The executable must be between 1 byte and 650 MB.")
        digest = sha256_file(path)
        upload_url = API + "/files"
        if size > DIRECT_UPLOAD_LIMIT:
            upload_url = secure_upload_url(self.call("GET", API + "/files/upload_url").get("data"))
        boundary = "release-" + uuid.uuid4().hex
        body, length = multipart_file(path, boundary)
        receipt = self.call("POST", upload_url, body, f"multipart/form-data; boundary={boundary}", length)
        analysis_id = receipt.get("data", {}).get("id")
        if not isinstance(analysis_id, str) or not analysis_id or len(analysis_id) > 512:
            raise ScanError("VirusTotal did not return an analysis identifier.")
        if sha256_file(path) != digest:
            raise ScanError("The executable changed during submission.")
        while True:
            analysis = self.call("GET", API + "/analyses/" + parse.quote(analysis_id, safe=""))
            attributes = analysis.get("data", {}).get("attributes", {})
            status = attributes.get("status")
            if status == "completed":
                return completed_report(path.name, digest, analysis_id, analysis)
            if status not in {"queued", "in-progress"}:
                raise ScanError("VirusTotal returned an unknown analysis status.")
            self.pause(poll_seconds)


def completed_report(filename, digest, analysis_id, analysis):
    attributes = analysis["data"]["attributes"]
    stats = attributes.get("stats", {})
    required = ("malicious", "suspicious", "harmless", "undetected")
    if (not isinstance(stats, dict)
            or any(type(stats.get(key)) is not int or stats[key] < 0 for key in required)
            or any(type(value) is not int or value < 0 for value in stats.values())
            or sum(stats[key] for key in required) == 0):
        raise ScanError("VirusTotal completed without usable engine results.")
    reported_hash = analysis.get("meta", {}).get("file_info", {}).get("sha256")
    if reported_hash and reported_hash.lower() != digest:
        raise ScanError("VirusTotal analysis does not match the submitted executable.")
    detections = []
    results = attributes.get("results", {})
    if not isinstance(results, dict):
        raise ScanError("VirusTotal returned invalid engine results.")
    for name, engine in results.items():
        if not isinstance(engine, dict):
            raise ScanError("VirusTotal returned invalid engine results.")
        if engine.get("category") in {"malicious", "suspicious"}:
            detections.append({"engine": name, "category": engine["category"], "result": engine.get("result")})
    return {
        "filename": filename, "sha256": digest, "analysis_id": analysis_id,
        "url": f"https://www.virustotal.com/gui/file/{digest}/detection",
        "checked_at": datetime.now(timezone.utc).isoformat(timespec="seconds"),
        "status": "completed", "verdict": "completed", "stats": stats,
        "detections": sorted(detections, key=lambda item: item["engine"]),
    }


def markdown_text(value):
    return html.escape(str(value)).replace("|", "&#124;").replace("\r", " ").replace("\n", " ").replace("`", "&#96;")


def report_markdown(report):
    lines = [f"### {markdown_text(report['filename'])}", ""]
    if report["status"] != "completed":
        lines += ["**VirusTotal scan incomplete**", "", markdown_text(report["error"])]
    else:
        stats = report["stats"]
        lines += [
            "**VirusTotal scan completed**", "",
            f"[VirusTotal results]({report['url']})", "",
            f"Checked at (UTC): {report['checked_at']}", "",
            "| Engine category | Count |", "| --- | ---: |",
            f"| malicious | {stats['malicious']} |", f"| suspicious | {stats['suspicious']} |",
            f"| undetected | {stats['undetected']} |", f"| harmless | {stats['harmless']} |",
            f"| Timeout, failure or unsupported | {sum(value for key, value in stats.items() if key not in {'malicious', 'suspicious', 'undetected', 'harmless'})} |",
            "", f"SHA-256: `{report['sha256']}`", "",
            "These are point-in-time engine results. Review the linked details before use.",
        ]
        if report["detections"]:
            lines += ["", "| Reporting engine | Category | Detection |", "| --- | --- | --- |"]
            for item in report["detections"]:
                lines.append(f"| {markdown_text(item['engine'])} | {item['category']} | {markdown_text(item['result'] or '—')} |")
    return "\n".join(lines) + "\n"


def main(argv=None):
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--file", type=Path, required=True)
    parser.add_argument("--output-dir", type=Path, required=True)
    parser.add_argument("--timeout-seconds", type=int, default=1800)
    parser.add_argument("--poll-seconds", type=int, default=30)
    args = parser.parse_args(argv)
    report = {"filename": args.file.name, "status": "incomplete", "verdict": "incomplete"}
    try:
        if args.timeout_seconds <= 0 or args.poll_seconds < 16:
            raise ScanError("Use a positive timeout and a polling interval of at least 16 seconds.")
        if not args.file.is_file() or args.file.suffix.lower() != ".exe":
            raise ScanError("A release executable (.exe) is required.")
        client = VirusTotalClient(os.environ.get("VIRUSTOTAL_API_KEY", ""), args.timeout_seconds)
        report = client.scan(args.file, args.poll_seconds)
    except ScanError as ex:
        report["error"] = str(ex)
    except (OSError, KeyError, TypeError, AttributeError, ValueError):
        report["error"] = "Unable to scan the release executable or read the analysis response."
    args.output_dir.mkdir(parents=True, exist_ok=True)
    (args.output_dir / "virustotal.json").write_text(json.dumps(report, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    markdown = report_markdown(report)
    (args.output_dir / "virustotal.md").write_text(markdown, encoding="utf-8")
    if summary := os.environ.get("GITHUB_STEP_SUMMARY"):
        with Path(summary).open("a", encoding="utf-8") as stream:
            stream.write(markdown + "\n")
    # Detections are advisory: a completed analysis does not block the release.
    return 0 if report["status"] == "completed" else 1


if __name__ == "__main__":
    raise SystemExit(main())
