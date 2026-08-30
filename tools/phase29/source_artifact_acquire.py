#!/usr/bin/env python3

from __future__ import annotations

import argparse
import hashlib
import html
import json
import re
import shutil
import subprocess
import sys
import time
from datetime import datetime, timezone
from pathlib import Path
from typing import Any
from urllib.parse import urlparse, quote


ROOT = Path.cwd().resolve()

STATE_ROOT = (
    ROOT /
    ".phase29-source-rebuild"
)

RUN_STATE = (
    STATE_ROOT /
    "run.json"
)

AUDIT_PATH = (
    STATE_ROOT /
    "reports" /
    "architecture-audit.json"
)

SOURCE_LOCK_PATH = (
    STATE_ROOT /
    "source-lock.json"
)

CACHE_ROOT = (
    STATE_ROOT /
    "cache" /
    "source-artifacts"
)

SOURCE_STATE_ROOT = (
    STATE_ROOT /
    "sources"
)

REPORT_PATH = (
    STATE_ROOT /
    "reports" /
    "source-license-acquisition.json"
)


def now() -> str:
    return (
        datetime.now(timezone.utc)
        .isoformat()
    )


def read_json(
    path: Path,
) -> Any:
    return json.loads(
        path.read_text(
            encoding="utf-8"
        )
    )


def write_json(
    path: Path,
    obj: Any,
) -> None:
    path.parent.mkdir(
        parents=True,
        exist_ok=True,
    )

    path.write_text(
        json.dumps(
            obj,
            ensure_ascii=False,
            indent=2,
        ) + "\n",
        encoding="utf-8",
    )


def sha256_bytes(
    data: bytes,
) -> str:
    return hashlib.sha256(
        data
    ).hexdigest()


def sha256_file(
    path: Path,
) -> str:
    return sha256_bytes(
        path.read_bytes()
    )


def url_key(
    url: str,
) -> str:
    return hashlib.sha256(
        url.encode(
            "utf-8"
        )
    ).hexdigest()


def normal_host(
    url: str,
) -> str:
    host = (
        urlparse(url)
        .hostname
        or ""
    ).lower()

    if host.startswith(
        "www."
    ):
        host = host[4:]

    return host


def source_state_path(
    url: str,
) -> Path:
    return (
        SOURCE_STATE_ROOT /
        f"{url_key(url)}.json"
    )


def artifact_path(
    url: str,
) -> Path:
    return (
        CACHE_ROOT /
        f"{url_key(url)}.bin"
    )


def text_path(
    url: str,
) -> Path:
    return (
        CACHE_ROOT /
        f"{url_key(url)}.txt"
    )


def headers_path(
    url: str,
) -> Path:
    return (
        CACHE_ROOT /
        f"{url_key(url)}.headers"
    )


def checkpoint_valid(
    url: str,
) -> bool:
    state_path = source_state_path(
        url
    )

    artifact = artifact_path(
        url
    )

    if (
        not state_path.exists()
        or
        not artifact.exists()
    ):
        return False

    try:
        state = read_json(
            state_path
        )
    except Exception:
        return False

    if state.get(
        "url"
    ) != url:
        return False

    expected = state.get(
        "artifactSha256"
    )

    if not expected:
        return False

    if sha256_file(
        artifact
    ) != expected:
        return False

    if state.get(
        "classification"
    ) not in {
        "APPROVED",
        "BLOCKED_LICENSE",
        "BLOCKED_PRODUCT_POLICY",
        "BLOCKED_UNVERIFIED",
    }:
        return False

    return True


def fetch(
    url: str,
) -> None:
    artifact = artifact_path(
        url
    )

    headers = headers_path(
        url
    )

    tmp_artifact = artifact.with_suffix(
        ".tmp"
    )

    tmp_headers = headers.with_suffix(
        ".tmp"
    )

    for tmp in (
        tmp_artifact,
        tmp_headers,
    ):
        if tmp.exists():
            tmp.unlink()

    def cleanup() -> None:
        for tmp in (
            tmp_artifact,
            tmp_headers,
        ):
            if tmp.exists():
                tmp.unlink()

    def valid_download() -> bool:
        return (
            tmp_artifact.exists()
            and
            tmp_artifact.stat().st_size > 0
        )

    def run_command(
        command: list[str],
        label: str,
    ) -> bool:
        cleanup()

        print(
            f"    transport={label}"
        )

        result = subprocess.run(
            command,
            check=False,
        )

        if (
            result.returncode == 0
            and
            valid_download()
        ):
            return True

        cleanup()

        return False

    common_curl = [
        "curl",
        "-fL",
        "--silent",
        "--show-error",
        "--retry",
        "3",
        "--retry-delay",
        "2",
        "--connect-timeout",
        "20",
        "--max-time",
        "180",
        "-A",
        (
            "Mozilla/5.0 "
            "Edulytics-SourceAudit/1.0"
        ),
        "-D",
        str(
            tmp_headers
        ),
        "-o",
        str(
            tmp_artifact
        ),
    ]

    # Attempt 1:
    # Normal HTTPS transport.
    ok = run_command(
        common_curl + [
            url
        ],
        "curl-default",
    )

    host = normal_host(
        url
    )

    # Attempt 2:
    # MVP's legacy HTTPS endpoint can fail
    # negotiation with newer OpenSSL builds.
    # Keep HTTPS and TLS >= 1.2, but force HTTP/1.1.
    if (
        not ok
        and
        host == "mathematicsvisionproject.org"
    ):
        ok = run_command(
            common_curl[:-1]
            if False
            else [
                "curl",
                "-fL",
                "--silent",
                "--show-error",
                "--retry",
                "3",
                "--retry-delay",
                "2",
                "--connect-timeout",
                "20",
                "--max-time",
                "180",
                "--http1.1",
                "--tlsv1.2",
                "--tls-max",
                "1.2",
                "-A",
                (
                    "Mozilla/5.0 "
                    "Edulytics-SourceAudit/1.0"
                ),
                "-D",
                str(
                    tmp_headers
                ),
                "-o",
                str(
                    tmp_artifact
                ),
                url,
            ],
            "curl-mvp-tls12-http11",
        )

    # Attempt 3:
    # wget may use a different TLS implementation
    # on the same Linux installation.
    if (
        not ok
        and
        host == "mathematicsvisionproject.org"
        and
        shutil.which("wget")
    ):
        cleanup()

        print(
            "    transport=wget-mvp-tls12"
        )

        result = subprocess.run(
            [
                "wget",
                "--quiet",
                "--server-response",
                "--https-only",
                "--secure-protocol=TLSv1_2",
                "--timeout=30",
                "--tries=3",
                "--user-agent="
                "Mozilla/5.0 Edulytics-SourceAudit/1.0",
                "-O",
                str(
                    tmp_artifact
                ),
                url,
            ],
            check=False,
            stderr=subprocess.PIPE,
            text=True,
        )

        if (
            result.returncode == 0
            and
            valid_download()
        ):
            # Preserve wget response metadata
            # in the same headers artifact slot.
            tmp_headers.write_text(
                result.stderr or "",
                encoding="utf-8",
            )

            ok = True

        else:
            cleanup()

    # Attempt 4:
    # If the original MVP host is temporarily
    # unavailable, retrieve the SAME historical
    # source artifact through Internet Archive.
    #
    # The original publisher URL remains the
    # canonical source URL. The archive URL is
    # recorded only as the retrieval channel.
    if (
        not ok
        and
        host == "mathematicsvisionproject.org"
    ):
        print(
            "    transport=internet-archive"
        )

        archive_candidates = []

        for candidate in [
            url,
            url.replace(
                "https://www.",
                "http://www.",
                1,
            ),
            url.replace(
                "https://www.",
                "https://",
                1,
            ),
            url.replace(
                "https://www.",
                "http://",
                1,
            ),
        ]:
            if (
                candidate
                not in archive_candidates
            ):
                archive_candidates.append(
                    candidate
                )

        for original_candidate in archive_candidates:
            availability_url = (
                "https://archive.org/"
                "wayback/available?url="
                + quote(
                    original_candidate,
                    safe="",
                )
            )

            availability = subprocess.run(
                [
                    "curl",
                    "-fsSL",
                    "--retry",
                    "3",
                    "--retry-delay",
                    "2",
                    "--connect-timeout",
                    "20",
                    "--max-time",
                    "60",
                    "-A",
                    (
                        "Mozilla/5.0 "
                        "Edulytics-SourceAudit/1.0"
                    ),
                    availability_url,
                ],
                check=False,
                stdout=subprocess.PIPE,
                stderr=subprocess.PIPE,
                text=True,
            )

            if availability.returncode != 0:
                continue

            try:
                payload = json.loads(
                    availability.stdout
                )
            except Exception:
                continue

            snapshot = (
                payload
                .get(
                    "archived_snapshots",
                    {}
                )
                .get(
                    "closest",
                    {}
                )
            )

            if not snapshot.get(
                "available"
            ):
                continue

            if str(
                snapshot.get(
                    "status",
                    ""
                )
            ) != "200":
                continue

            snapshot_url = snapshot.get(
                "url"
            )

            timestamp = snapshot.get(
                "timestamp"
            )

            if (
                not snapshot_url
                or
                not timestamp
            ):
                continue

            snapshot_url = (
                snapshot_url
                .replace(
                    "http://web.archive.org",
                    "https://web.archive.org",
                    1,
                )
            )

            normal_marker = (
                f"/web/{timestamp}/"
            )

            raw_marker = (
                f"/web/{timestamp}id_/"
            )

            if (
                normal_marker
                in snapshot_url
            ):
                raw_url = (
                    snapshot_url
                    .replace(
                        normal_marker,
                        raw_marker,
                        1,
                    )
                )
            elif (
                raw_marker
                in snapshot_url
            ):
                raw_url = snapshot_url
            else:
                continue

            cleanup()

            print(
                "    archive snapshot="
                f"{timestamp}"
            )

            archive_result = subprocess.run(
                [
                    "curl",
                    "-fL",
                    "--silent",
                    "--show-error",
                    "--retry",
                    "4",
                    "--retry-delay",
                    "2",
                    "--connect-timeout",
                    "20",
                    "--max-time",
                    "180",
                    "-A",
                    (
                        "Mozilla/5.0 "
                        "Edulytics-SourceAudit/1.0"
                    ),
                    "-D",
                    str(
                        tmp_headers
                    ),
                    "-o",
                    str(
                        tmp_artifact
                    ),
                    raw_url,
                ],
                check=False,
            )

            if (
                archive_result.returncode
                != 0
                or
                not valid_download()
            ):
                cleanup()
                continue

            # These MVP URLs are expected PDFs.
            # Never accept an archived HTML error
            # page in place of the source file.
            if (
                url.lower()
                .split(
                    "?",
                    1,
                )[0]
                .endswith(
                    ".pdf"
                )
            ):
                head = (
                    tmp_artifact
                    .read_bytes()[:5]
                )

                if head != b"%PDF-":
                    cleanup()
                    continue

            # Persist retrieval provenance.
            with tmp_headers.open(
                "a",
                encoding="utf-8",
            ) as fh:
                fh.write(
                    "\n"
                    "X-Edulytics-Retrieval-Channel: "
                    "INTERNET_ARCHIVE\n"
                )

                fh.write(
                    "X-Edulytics-Retrieval-URL: "
                    f"{raw_url}\n"
                )

                fh.write(
                    "X-Edulytics-Retrieval-Timestamp: "
                    f"{timestamp}\n"
                )

                fh.write(
                    "X-Edulytics-Original-URL: "
                    f"{url}\n"
                )

            ok = True
            break

    if not ok:
        raise RuntimeError(
            "source fetch failed after all "
            f"HTTPS transport fallbacks: {url}"
        )

    # Integrity sanity checks.
    size = tmp_artifact.stat().st_size

    if size < 100:
        cleanup()

        raise RuntimeError(
            f"downloaded artifact suspiciously small "
            f"({size} bytes): {url}"
        )

    # MVP URLs here are PDFs.
    # Prevent accidentally caching an HTML error page
    # as a successful source artifact.
    if (
        host == "mathematicsvisionproject.org"
        and
        url.lower()
        .split("?", 1)[0]
        .endswith(".pdf")
    ):
        head = tmp_artifact.read_bytes()[:5]

        if head != b"%PDF-":
            cleanup()

            raise RuntimeError(
                "MVP PDF URL returned non-PDF content: "
                f"{url}"
            )

    tmp_artifact.replace(
        artifact
    )

    if tmp_headers.exists():
        tmp_headers.replace(
            headers
        )

def content_type(
    url: str,
) -> str:
    path = headers_path(
        url
    )

    if not path.exists():
        return ""

    text = path.read_text(
        encoding="utf-8",
        errors="ignore",
    )

    matches = re.findall(
        r"(?im)^content-type:\s*([^\r\n]+)",
        text,
    )

    if not matches:
        return ""

    return (
        matches[-1]
        .split(
            ";",
            1,
        )[0]
        .strip()
        .lower()
    )



def retrieval_metadata(
    url: str,
) -> dict[str, str]:
    path = headers_path(
        url
    )

    result = {
        "retrievalChannel":
            "ORIGIN",

        "retrievalUrl":
            url,

        "retrievalTimestamp":
            "",
    }

    if not path.exists():
        return result

    text = path.read_text(
        encoding="utf-8",
        errors="ignore",
    )

    patterns = {
        "retrievalChannel":
            (
                r"(?im)^"
                r"X-Edulytics-Retrieval-Channel:"
                r"\s*(.+?)\s*$"
            ),

        "retrievalUrl":
            (
                r"(?im)^"
                r"X-Edulytics-Retrieval-URL:"
                r"\s*(.+?)\s*$"
            ),

        "retrievalTimestamp":
            (
                r"(?im)^"
                r"X-Edulytics-Retrieval-Timestamp:"
                r"\s*(.+?)\s*$"
            ),
    }

    for key, pattern in patterns.items():
        match = re.search(
            pattern,
            text,
        )

        if match:
            result[key] = (
                match
                .group(1)
                .strip()
            )

    return result

def html_to_text(
    raw: str,
) -> str:
    raw = re.sub(
        r"(?is)<script.*?</script>",
        " ",
        raw,
    )

    raw = re.sub(
        r"(?is)<style.*?</style>",
        " ",
        raw,
    )

    raw = re.sub(
        r"(?s)<[^>]+>",
        " ",
        raw,
    )

    raw = html.unescape(
        raw
    )

    raw = re.sub(
        r"[ \t]+",
        " ",
        raw,
    )

    raw = re.sub(
        r"\n\s*\n+",
        "\n",
        raw,
    )

    return raw.strip()


def extract_text(
    url: str,
) -> str:
    artifact = artifact_path(
        url
    )

    ctype = content_type(
        url
    )

    data = artifact.read_bytes()

    is_pdf = (
        data.startswith(
            b"%PDF"
        )
        or
        ctype == "application/pdf"
        or
        url.lower().split(
            "?",
            1,
        )[0].endswith(
            ".pdf"
        )
    )

    output = text_path(
        url
    )

    if is_pdf:
        pdftotext = shutil.which(
            "pdftotext"
        )

        if pdftotext:
            result = subprocess.run(
                [
                    pdftotext,
                    "-layout",
                    str(
                        artifact
                    ),
                    str(
                        output
                    ),
                ],
                check=False,
                stdout=subprocess.DEVNULL,
                stderr=subprocess.DEVNULL,
            )

            if (
                result.returncode == 0
                and
                output.exists()
            ):
                return output.read_text(
                    encoding="utf-8",
                    errors="ignore",
                )

        # Fail closed. Never infer a PDF
        # license from unreadable binary data.
        output.write_text(
            "",
            encoding="utf-8",
        )

        return ""

    decoded = data.decode(
        "utf-8",
        errors="ignore",
    )

    text = html_to_text(
        decoded
    )

    output.write_text(
        text,
        encoding="utf-8",
    )

    return text


def classify_im_kendall(
    url: str,
) -> dict[str, Any]:
    # The official IM Terms explicitly identify
    # im.kendallhunt.com as the 2019–2021
    # IM K–12 Math First Edition OER and state
    # that it is CC BY 4.0 including commercial use.
    return {
        "classification":
            "APPROVED",

        "sourceFamily":
            "IM_K12_FIRST_EDITION",

        "license":
            "CC BY 4.0",

        "commercialReuse":
            True,

        "requiresAttribution":
            True,

        "licenseEvidence":
            (
                "https://illustrativemathematics.org/"
                "terms-of-use/"
            ),

        "licenseEvidenceRule":
            (
                "Official IM Terms §7.1 explicitly "
                "identify im.kendallhunt.com as "
                "IM K–12 Math First Edition "
                "(2019–2021), licensed CC BY 4.0 "
                "for commercial and non-commercial use."
            ),

        "assetRestrictions": [
            "Do not import IM logos.",
            "Do not use IM trademarks as Edulytics branding.",
            (
                "Third-party assets require their "
                "own compatible rights."
            ),
        ],
    }


def classify_mvp(
    text: str,
) -> dict[str, Any]:
    normalized = (
        text
        .replace(
            "\u00a0",
            " "
        )
        .lower()
    )

    has_by4 = any(
        marker in normalized
        for marker in [
            (
                "licensed under the creative commons "
                "attribution cc by 4.0"
            ),
            (
                "creative commons attribution 4.0"
            ),
            "cc by 4.0",
        ]
    )

    has_nc = any(
        marker in normalized
        for marker in [
            "cc by-nc",
            "noncommercial",
            "non-commercial",
        ]
    )

    if (
        has_by4
        and
        not has_nc
    ):
        return {
            "classification":
                "APPROVED",

            "sourceFamily":
                "MVP_EXACT_CC_BY_4",

            "license":
                "CC BY 4.0",

            "commercialReuse":
                True,

            "requiresAttribution":
                True,

            "licenseEvidence":
                "EXACT_ARTIFACT_TEXT",

            "licenseEvidenceRule":
                (
                    "The downloaded exact MVP artifact "
                    "contains an explicit CC BY 4.0 "
                    "license statement."
                ),
        }

    if has_nc:
        return {
            "classification":
                "BLOCKED_LICENSE",

            "sourceFamily":
                "MVP_NONCOMMERCIAL",

            "commercialReuse":
                False,

            "reason":
                (
                    "Exact artifact contains "
                    "NonCommercial licensing."
                ),
        }

    return {
        "classification":
            "BLOCKED_UNVERIFIED",

        "sourceFamily":
            "MVP_UNVERIFIED",

        "commercialReuse":
            False,

        "reason":
            (
                "Exact artifact did not yield an "
                "explicit commercially compatible "
                "license statement. Fail closed."
            ),
    }


def classify_libretexts(
    text: str,
) -> dict[str, Any]:
    normalized = (
        text
        .replace(
            "\u00a0",
            " "
        )
        .lower()
    )

    has_nc = any(
        marker in normalized
        for marker in [
            "cc by-nc",
            "noncommercial",
            "non-commercial",
        ]
    )

    has_sa = any(
        marker in normalized
        for marker in [
            "cc by-sa",
            "sharealike",
            "share-alike",
        ]
    )

    explicit_by4 = any(
        marker in normalized
        for marker in [
            "cc by 4.0",
            (
                "creative commons attribution "
                "4.0 international"
            ),
        ]
    )

    public_domain = any(
        marker in normalized
        for marker in [
            "cc0 1.0",
            "public domain",
        ]
    )

    if has_nc:
        return {
            "classification":
                "BLOCKED_LICENSE",

            "sourceFamily":
                "LIBRETEXTS_NONCOMMERCIAL",

            "commercialReuse":
                False,

            "reason":
                (
                    "Exact page contains a "
                    "NonCommercial license."
                ),
        }

    if has_sa:
        return {
            "classification":
                "BLOCKED_PRODUCT_POLICY",

            "sourceFamily":
                "LIBRETEXTS_SHAREALIKE",

            "commercialReuse":
                True,

            "reason":
                (
                    "ShareAlike can permit commercial "
                    "use, but would impose downstream "
                    "license obligations on adapted "
                    "lesson content. Edulytics will not "
                    "import it automatically."
                ),
        }

    if public_domain:
        return {
            "classification":
                "APPROVED",

            "sourceFamily":
                "LIBRETEXTS_PUBLIC_DOMAIN",

            "license":
                "Public Domain / CC0",

            "commercialReuse":
                True,

            "requiresAttribution":
                False,

            "licenseEvidence":
                "EXACT_ARTIFACT_TEXT",
        }

    if explicit_by4:
        return {
            "classification":
                "APPROVED",

            "sourceFamily":
                "LIBRETEXTS_EXACT_CC_BY_4",

            "license":
                "CC BY 4.0",

            "commercialReuse":
                True,

            "requiresAttribution":
                True,

            "licenseEvidence":
                "EXACT_ARTIFACT_TEXT",
        }

    return {
        "classification":
            "BLOCKED_UNVERIFIED",

        "sourceFamily":
            "LIBRETEXTS_UNVERIFIED",

        "commercialReuse":
            False,

        "reason":
            (
                "No exact commercial-compatible "
                "license was proven on this artifact."
            ),
    }


def classify(
    url: str,
    text: str,
) -> dict[str, Any]:
    host = normal_host(
        url
    )

    if host == "im.kendallhunt.com":
        return classify_im_kendall(
            url
        )

    if host == "mathematicsvisionproject.org":
        return classify_mvp(
            text
        )

    if host in {
        "math.libretexts.org",
        "stats.libretexts.org",
    }:
        return classify_libretexts(
            text
        )

    return {
        "classification":
            "BLOCKED_UNVERIFIED",

        "sourceFamily":
            "UNKNOWN_SOURCE_FAMILY",

        "commercialReuse":
            False,

        "reason":
            (
                f"Host is not approved by "
                f"the fail-closed source policy: {host}"
            ),
    }


def process_one(
    url: str,
    index: int,
    total: int,
) -> dict[str, Any]:

    if checkpoint_valid(
        url
    ):
        state = read_json(
            source_state_path(
                url
            )
        )

        if (
            index == 1
            or
            index % 250 == 0
            or
            index >= total - 4
        ):
            print(
                f"[{index}/{total}] "
                f"{state['classification']} "
                f"→ CACHE/CHECKPOINT HIT"
            )

        return state

    print(
        f"[{index}/{total}] FETCH"
    )

    print(
        f"    {url}"
    )

    fetch(
        url
    )

    artifact = artifact_path(
        url
    )

    artifact_sha = sha256_file(
        artifact
    )

    text = extract_text(
        url
    )

    result = classify(
        url,
        text,
    )

    state = {
        "schemaVersion":
            1,

        "url":
            url,

        "host":
            normal_host(
                url
            ),

        "processedAtUtc":
            now(),

        "artifactPath":
            str(
                artifact.relative_to(
                    ROOT
                )
            ),

        "artifactSha256":
            artifact_sha,

        "artifactBytes":
            artifact.stat().st_size,

        "contentType":
            content_type(
                url
            ),

        **retrieval_metadata(
            url
        ),

        "textExtractionAvailable":
            bool(
                text.strip()
            ),

        **result,
    }

    write_json(
        source_state_path(
            url
        ),
        state,
    )

    print(
        f"    => {state['classification']}"
    )

    if state.get(
        "license"
    ):
        print(
            f"       license={state['license']}"
        )

    return state


def update_checkpoint(
    report_sha: str,
    report: dict[str, Any],
) -> None:
    state = read_json(
        RUN_STATE
    )

    checkpoints = state.setdefault(
        "checkpoints",
        {},
    )

    checkpoints[
        "source-license-acquisition"
    ] = {
        "status":
            "PASS",

        "completedAtUtc":
            now(),

        "reportSha256":
            report_sha,

        "totalSources":
            report[
                "summary"
            ][
                "total"
            ],

        "approved":
            report[
                "summary"
            ][
                "approved"
            ],

        "blockedLicense":
            report[
                "summary"
            ][
                "blockedLicense"
            ],

        "blockedProductPolicy":
            report[
                "summary"
            ][
                "blockedProductPolicy"
            ],

        "blockedUnverified":
            report[
                "summary"
            ][
                "blockedUnverified"
            ],
    }

    state[
        "currentStage"
    ] = "source-license-acquisition"

    state[
        "updatedAtUtc"
    ] = now()

    write_json(
        RUN_STATE,
        state,
    )


def acquire() -> None:
    audit = read_json(
        AUDIT_PATH
    )

    urls = sorted(
        set(
            audit[
                "blueprints"
            ][
                "sourceUrls"
            ]
        )
    )

    if not urls:
        raise RuntimeError(
            "No source URLs found in "
            "architecture audit."
        )

    CACHE_ROOT.mkdir(
        parents=True,
        exist_ok=True,
    )

    SOURCE_STATE_ROOT.mkdir(
        parents=True,
        exist_ok=True,
    )

    results = []

    total = len(
        urls
    )

    print(
        f"Unique source URLs: {total}"
    )

    print()

    for index, url in enumerate(
        urls,
        start=1,
    ):
        result = process_one(
            url,
            index,
            total,
        )

        results.append(
            result
        )

        # Small politeness delay only
        # after actual network operations.
        time.sleep(
            0.10
        )

    counts = {
        "APPROVED":
            0,

        "BLOCKED_LICENSE":
            0,

        "BLOCKED_PRODUCT_POLICY":
            0,

        "BLOCKED_UNVERIFIED":
            0,
    }

    for result in results:
        classification = result[
            "classification"
        ]

        counts[
            classification
        ] += 1

    report = {
        "schemaVersion":
            1,

        "completedAtUtc":
            now(),

        "policy":
            "FAIL_CLOSED_COMMERCIAL_REUSE",

        "summary": {
            "total":
                total,

            "approved":
                counts[
                    "APPROVED"
                ],

            "blockedLicense":
                counts[
                    "BLOCKED_LICENSE"
                ],

            "blockedProductPolicy":
                counts[
                    "BLOCKED_PRODUCT_POLICY"
                ],

            "blockedUnverified":
                counts[
                    "BLOCKED_UNVERIFIED"
                ],
        },

        "sources":
            results,
    }

    write_json(
        REPORT_PATH,
        report,
    )

    report_sha = sha256_file(
        REPORT_PATH
    )

    update_checkpoint(
        report_sha,
        report,
    )

    print()
    print(
        "=============================================================="
    )

    print(
        " SOURCE / LICENSE ACQUISITION: PASS"
    )

    print(
        "=============================================================="
    )

    print(
        f"Total sources              : {total}"
    )

    print(
        "Approved for importer      :",
        counts[
            "APPROVED"
        ],
    )

    print(
        "Blocked — noncommercial    :",
        counts[
            "BLOCKED_LICENSE"
        ],
    )

    print(
        "Blocked — product policy   :",
        counts[
            "BLOCKED_PRODUCT_POLICY"
        ],
    )

    print(
        "Blocked — unverified       :",
        counts[
            "BLOCKED_UNVERIFIED"
        ],
    )

    print()

    if (
        counts[
            "BLOCKED_LICENSE"
        ]
        or
        counts[
            "BLOCKED_PRODUCT_POLICY"
        ]
        or
        counts[
            "BLOCKED_UNVERIFIED"
        ]
    ):
        print(
            "IMPORTANT:"
        )

        print(
            "Step 05 completed successfully, "
            "but blocked sources MUST NOT enter "
            "the content importer."
        )

        print(
            "They require replacement with an "
            "approved lesson-specific source "
            "before content migration."
        )

    else:
        print(
            "All discovered source artifacts "
            "are commercially compatible."
        )

    print(
        "=============================================================="
    )


def status() -> None:
    if REPORT_PATH.exists():
        report = read_json(
            REPORT_PATH
        )

        print(
            json.dumps(
                report[
                    "summary"
                ],
                indent=2,
            )
        )

    else:
        print(
            "source/license acquisition: PENDING"
        )


def main() -> None:
    parser = argparse.ArgumentParser()

    parser.add_argument(
        "--acquire",
        action="store_true",
    )

    parser.add_argument(
        "--status",
        action="store_true",
    )

    args = parser.parse_args()

    if args.acquire:
        acquire()
        return

    if args.status:
        status()
        return

    parser.error(
        "Specify --acquire or --status"
    )


if __name__ == "__main__":
    main()
