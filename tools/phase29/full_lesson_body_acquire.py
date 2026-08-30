#!/usr/bin/env python3

from __future__ import annotations

import hashlib
import html
import json
import re
import subprocess
import threading
from collections import Counter
from concurrent.futures import ThreadPoolExecutor, as_completed
from datetime import datetime, timezone
from html.parser import HTMLParser
from pathlib import Path
from urllib.parse import quote, urlparse


ROOT = Path.cwd()
STATE = ROOT / ".phase29-source-rebuild"

IMPORT_INDEX = (
    STATE /
    "importer/lesson-index.json"
)

RUN_PATH = (
    STATE /
    "run.json"
)

BODY_ROOT = (
    STATE /
    "body-source"
)

ARTIFACT_DIR = (
    BODY_ROOT /
    "artifacts"
)

TEXT_DIR = (
    BODY_ROOT /
    "text"
)

META_DIR = (
    BODY_ROOT /
    "meta"
)

LESSON_DIR = (
    BODY_ROOT /
    "lessons"
)

BODY_INDEX = (
    BODY_ROOT /
    "lesson-index.json"
)

REPORT_PATH = (
    STATE /
    "reports/full-lesson-body-acquisition.json"
)

ACQUIRER_VERSION = (
    "phase29-full-lesson-body-v1"
)

EXPECTED_TOTAL = 1466
EXPECTED_IM_FULL = 1437
EXPECTED_PDF = 23
EXPECTED_CURRENT_HTML = 6

MAX_WORKERS = 6

write_lock = threading.Lock()


def now():
    return datetime.now(
        timezone.utc
    ).isoformat()


def load(path):
    return json.loads(
        path.read_text(
            encoding="utf-8"
        )
    )


def atomic_json(path, value):
    path.parent.mkdir(
        parents=True,
        exist_ok=True,
    )

    tmp = path.with_suffix(
        path.suffix + ".tmp"
    )

    tmp.write_text(
        json.dumps(
            value,
            indent=2,
            ensure_ascii=False,
        ) + "\n",
        encoding="utf-8",
    )

    tmp.replace(path)


def atomic_text(path, value):
    path.parent.mkdir(
        parents=True,
        exist_ok=True,
    )

    tmp = path.with_suffix(
        path.suffix + ".tmp"
    )

    tmp.write_text(
        value,
        encoding="utf-8",
    )

    tmp.replace(path)


def sha_file(path):
    h = hashlib.sha256()

    with path.open("rb") as f:
        for chunk in iter(
            lambda: f.read(
                1024 * 1024
            ),
            b"",
        ):
            h.update(chunk)

    return h.hexdigest()


def sha_text(value):
    return hashlib.sha256(
        value.encode("utf-8")
    ).hexdigest()


def canonical_hash(value):
    payload = json.dumps(
        value,
        sort_keys=True,
        ensure_ascii=False,
        separators=(",", ":"),
    )

    return sha_text(
        payload
    )


def normalize_text(value):
    value = html.unescape(
        value or ""
    )

    value = (
        value
        .replace("\u00a0", " ")
        .replace("\u200b", "")
        .replace("’", "'")
        .replace("‘", "'")
        .replace("–", "-")
        .replace("—", "-")
    )

    value = re.sub(
        r"[ \t]+",
        " ",
        value,
    )

    value = re.sub(
        r"\n[ \t]+",
        "\n",
        value,
    )

    value = re.sub(
        r"\n{3,}",
        "\n\n",
        value,
    )

    return value.strip()


def search_key(value):
    value = normalize_text(
        value
    ).lower()

    value = re.sub(
        r"_+",
        " ",
        value,
    )

    value = re.sub(
        r"[^a-z0-9]+",
        " ",
        value,
    )

    return re.sub(
        r"\s+",
        " ",
        value,
    ).strip()


class VisibleTextParser(
    HTMLParser
):
    SKIP = {
        "script",
        "style",
        "noscript",
        "svg",
        "canvas",
        "template",
    }

    BLOCK = {
        "p",
        "div",
        "section",
        "article",
        "main",
        "header",
        "footer",
        "aside",
        "li",
        "ul",
        "ol",
        "table",
        "tr",
        "td",
        "th",
        "h1",
        "h2",
        "h3",
        "h4",
        "h5",
        "h6",
        "br",
    }

    def __init__(self):
        super().__init__(
            convert_charrefs=True
        )

        self.skip_depth = 0
        self.parts = []

    def handle_starttag(
        self,
        tag,
        attrs,
    ):
        tag = tag.lower()

        if tag in self.SKIP:
            self.skip_depth += 1
            return

        if (
            self.skip_depth == 0
            and tag in self.BLOCK
        ):
            self.parts.append(
                "\n"
            )

    def handle_endtag(
        self,
        tag,
    ):
        tag = tag.lower()

        if tag in self.SKIP:
            self.skip_depth = max(
                0,
                self.skip_depth - 1,
            )
            return

        if (
            self.skip_depth == 0
            and tag in self.BLOCK
        ):
            self.parts.append(
                "\n"
            )

    def handle_data(
        self,
        data,
    ):
        if (
            not self.skip_depth
            and data.strip()
        ):
            self.parts.append(
                data
            )

    def text(self):
        return normalize_text(
            " ".join(
                self.parts
            )
        )


def html_to_text(raw):
    parser = VisibleTextParser()

    parser.feed(
        raw
    )

    parser.close()

    return parser.text()


def pdf_to_text(path):
    result = subprocess.run(
        [
            "pdftotext",
            "-layout",
            str(path),
            "-",
        ],
        stdout=subprocess.PIPE,
        stderr=subprocess.PIPE,
        text=True,
        timeout=300,
    )

    if result.returncode != 0:
        raise RuntimeError(
            "pdftotext failed: "
            + result.stderr.strip()
        )

    return normalize_text(
        result.stdout
    )


def title_variants(title):
    base = search_key(
        title
    )

    values = {
        base
    }

    # PHASE29_K5_PLC_TITLE_NORMALIZATION_V3
    #
    # K-5 blueprint titles retain the pedagogical label
    # "PLC Activity", while the actual IM full lesson page
    # intentionally displays the instructional lesson title
    # without that suffix.
    #
    # This is deliberately narrow:
    # ONLY a terminal "PLC Activity" label is removed.
    plc_title = re.sub(
        r"\s+PLC Activity\s*$",
        "",
        title or "",
        flags=re.IGNORECASE,
    ).strip()

    if (
        plc_title
        and plc_title != (
            title or ""
        ).strip()
    ):
        values.add(
            search_key(
                plc_title
            )
        )

    # Parenthetical labels occasionally vary
    # between preparation/full lesson pages.
    without_parenthetical = re.sub(
        r"\([^)]*\)",
        " ",
        title or "",
    )

    values.add(
        search_key(
            without_parenthetical
        )
    )

    return {
        x
        for x in values
        if x
    }


def title_matches(
    title,
    source_text,
):
    body = search_key(
        source_text
    )

    variants = title_variants(
        title
    )

    for variant in variants:
        if variant in body:
            return (
                True,
                "NORMALIZED_EXACT",
            )

    # Fail-safe secondary check for punctuation /
    # blank-line source-title differences.
    title_tokens = [
        x
        for x in search_key(
            title
        ).split()
        if len(x) > 1
    ]

    if not title_tokens:
        return False, "NO_TITLE_TOKENS"

    body_tokens = set(
        body.split()
    )

    matched = sum(
        token in body_tokens
        for token in title_tokens
    )

    coverage = (
        matched /
        len(title_tokens)
    )

    if (
        len(title_tokens) >= 4
        and coverage >= 0.90
    ):
        return (
            True,
            "TOKEN_COVERAGE_90",
        )

    return (
        False,
        f"TOKEN_COVERAGE_{coverage:.3f}",
    )


def locator_matches(
    section,
    source_text,
):
    section_key = search_key(
        section
    )

    body_key = search_key(
        source_text
    )

    variants = {
        section_key
    }

    if "cavalieri" in section_key:
        variants.add(
            section_key.replace(
                "cavalieri",
                "cavelieri",
            )
        )

    if (
        "amazing inverse trig "
        "function race"
        in section_key
    ):
        variants.update({
            search_key(
                "The Amazing Inverse Trig Function Race"
            ),
            search_key(
                "Amazing Inverse Trig Function Race"
            ),
            search_key(
                "The Amazing Trig Function Race"
            ),
        })

    return any(
        value in body_key
        for value in variants
    )


def html_valid(raw):
    if len(raw) < 1000:
        return False

    lower = raw.lower()

    return (
        "<html" in lower
        or "<!doctype html" in lower
        or "<body" in lower
        or "<main" in lower
        or "<article" in lower
    )


def curl_html(url):
    result = subprocess.run(
        [
            "curl",
            "--http1.1",
            "-L",
            "--fail",
            "--silent",
            "--show-error",
            "--retry", "2",
            "--retry-all-errors",
            "--retry-delay", "1",
            "--connect-timeout", "20",
            "--max-time", "120",
            "-A",
            "Edulytics-Phase29-SourceVerifier/1.0",
            url,
        ],
        stdout=subprocess.PIPE,
        stderr=subprocess.PIPE,
        timeout=140,
    )

    if result.returncode != 0:
        return None

    raw = result.stdout.decode(
        "utf-8",
        errors="ignore",
    )

    if not html_valid(raw):
        return None

    return raw


def wayback_fetch(url):
    endpoint = (
        "https://archive.org/wayback/available?url="
        + quote(
            url,
            safe="",
        )
    )

    result = subprocess.run(
        [
            "curl",
            "--http1.1",
            "-L",
            "--fail",
            "--silent",
            "--show-error",
            "--connect-timeout", "20",
            "--max-time", "60",
            endpoint,
        ],
        stdout=subprocess.PIPE,
        stderr=subprocess.PIPE,
        timeout=70,
    )

    if result.returncode != 0:
        return None

    try:
        data = json.loads(
            result.stdout.decode(
                "utf-8",
                errors="ignore",
            )
        )
    except Exception:
        return None

    closest = (
        data.get(
            "archived_snapshots",
            {}
        )
        .get(
            "closest",
            {}
        )
    )

    if not closest.get(
        "available"
    ):
        return None

    timestamp = closest.get(
        "timestamp"
    )

    if not timestamp:
        return None

    raw_url = (
        "https://web.archive.org/web/"
        f"{timestamp}id_/{url}"
    )

    raw = curl_html(
        raw_url
    )

    if raw is None:
        return None

    return {
        "raw":
            raw,

        "channel":
            "INTERNET_ARCHIVE",

        "retrievalUrl":
            raw_url,

        "archiveTimestamp":
            timestamp,
    }


def body_url_for(packet):
    source = packet.get(
        "source",
        {}
    )

    imported = packet.get(
        "importedSource",
        {}
    )

    source_url = (
        source.get(
            "sourceUrl"
        )
        or ""
    )

    kind = imported.get(
        "artifactKind"
    )

    parsed = urlparse(
        source_url
    )

    path = parsed.path or ""

    if kind == "PDF":
        return {
            "strategy":
                "PDF_SECTION_LOCATOR",

            "bodyUrl":
                source_url,

            "needsDownload":
                False,
        }

    # PHASE29_IM_FULL_LESSON_ROUTE_V2
    #
    # IM First Edition uses different full-lesson filenames:
    #
    #   K-5 : preparation.html -> lesson.html
    #   MS  : preparation.html -> index.html
    #   HS  : preparation.html -> index.html
    #
    # Fail closed for any unrecognised IM route rather than
    # silently generating an incorrect URL.
    if (
        kind == "HTML"
        and path.endswith(
            "/preparation.html"
        )
    ):
        path_lower = path.lower()

        if "/k5/" in path_lower:
            full_lesson_filename = (
                "lesson.html"
            )

        elif (
            "/ms/" in path_lower
            or "/hs/" in path_lower
        ):
            full_lesson_filename = (
                "index.html"
            )

        else:
            raise RuntimeError(
                "unsupported IM preparation route: "
                + source_url
            )

        return {
            "strategy":
                "HTML_PREPARATION_TO_FULL_LESSON",

            "bodyUrl":
                (
                    source_url[
                        :-len(
                            "preparation.html"
                        )
                    ]
                    + full_lesson_filename
                ),

            "needsDownload":
                True,
        }

    if kind == "HTML":
        return {
            "strategy":
                "HTML_CURRENT_SOURCE_PAGE",

            "bodyUrl":
                source_url,

            "needsDownload":
                False,
        }

    raise RuntimeError(
        "unsupported artifact kind: "
        + str(kind)
    )


def acquire_im_body(
    request,
):
    lesson_code = request[
        "lessonCode"
    ]

    lesson_title = request[
        "lessonTitle"
    ]

    url = request[
        "bodyUrl"
    ]

    url_hash = sha_text(
        url
    )

    artifact_path = (
        ARTIFACT_DIR /
        f"{url_hash}.html"
    )

    meta_path = (
        META_DIR /
        f"{url_hash}.json"
    )

    route_input_hash = (
        canonical_hash({
            "acquirerVersion":
                ACQUIRER_VERSION,

            "lessonCode":
                lesson_code,

            "lessonTitle":
                lesson_title,

            "bodyUrl":
                url,
        })
    )

    # --------------------------------------------------------
    # Resume if artifact + metadata + hash + title still valid.
    # --------------------------------------------------------

    if (
        artifact_path.exists()
        and meta_path.exists()
    ):
        try:
            meta = load(
                meta_path
            )

            actual_sha = sha_file(
                artifact_path
            )

            if (
                meta.get(
                    "routeInputHash"
                )
                == route_input_hash
                and meta.get(
                    "artifactSha256"
                )
                == actual_sha
            ):
                raw = artifact_path.read_text(
                    encoding="utf-8",
                    errors="ignore",
                )

                text = html_to_text(
                    raw
                )

                verified, mode = (
                    title_matches(
                        lesson_title,
                        text,
                    )
                )

                if verified:
                    return {
                        "status":
                            "PASS",

                        "lessonCode":
                            lesson_code,

                        "lessonTitle":
                            lesson_title,

                        "bodyUrl":
                            url,

                        "artifactPath":
                            str(
                                artifact_path.relative_to(
                                    ROOT
                                )
                            ),

                        "artifactSha256":
                            actual_sha,

                        "text":
                            text,

                        "titleVerified":
                            True,

                        "titleVerificationMode":
                            mode,

                        "retrievalChannel":
                            meta.get(
                                "retrievalChannel"
                            ),

                        "retrievalUrl":
                            meta.get(
                                "retrievalUrl"
                            ),

                        "retrievalTimestamp":
                            meta.get(
                                "retrievalTimestamp"
                            ),

                        "resumed":
                            True,
                    }

        except Exception:
            pass

    # --------------------------------------------------------
    # Origin.
    # --------------------------------------------------------

    raw = curl_html(
        url
    )

    retrieval = None

    if raw is not None:
        retrieval = {
            "raw":
                raw,

            "channel":
                "ORIGIN",

            "retrievalUrl":
                url,

            "archiveTimestamp":
                None,
        }

    # --------------------------------------------------------
    # Wayback only for origin failures.
    # --------------------------------------------------------

    if retrieval is None:
        retrieval = wayback_fetch(
            url
        )

    if retrieval is None:
        return {
            "status":
                "FAIL_DOWNLOAD",

            "lessonCode":
                lesson_code,

            "lessonTitle":
                lesson_title,

            "bodyUrl":
                url,
        }

    raw = retrieval[
        "raw"
    ]

    text = html_to_text(
        raw
    )

    if len(
        search_key(
            text
        )
    ) < 500:
        return {
            "status":
                "FAIL_SHORT_BODY",

            "lessonCode":
                lesson_code,

            "lessonTitle":
                lesson_title,

            "bodyUrl":
                url,
        }

    verified, mode = (
        title_matches(
            lesson_title,
            text,
        )
    )

    if not verified:
        return {
            "status":
                "FAIL_TITLE",

            "lessonCode":
                lesson_code,

            "lessonTitle":
                lesson_title,

            "bodyUrl":
                url,

            "titleVerificationMode":
                mode,
        }

    with write_lock:
        atomic_text(
            artifact_path,
            raw,
        )

        actual_sha = sha_file(
            artifact_path
        )

        atomic_json(
            meta_path,
            {
                "schemaVersion":
                    1,

                "acquirerVersion":
                    ACQUIRER_VERSION,

                "routeInputHash":
                    route_input_hash,

                "lessonCode":
                    lesson_code,

                "lessonTitle":
                    lesson_title,

                "bodyUrl":
                    url,

                "artifactPath":
                    str(
                        artifact_path.relative_to(
                            ROOT
                        )
                    ),

                "artifactSha256":
                    actual_sha,

                "titleVerified":
                    True,

                "titleVerificationMode":
                    mode,

                "retrievalChannel":
                    retrieval[
                        "channel"
                    ],

                "retrievalUrl":
                    retrieval[
                        "retrievalUrl"
                    ],

                "retrievalTimestamp":
                    retrieval.get(
                        "archiveTimestamp"
                    ),

                "acquiredAtUtc":
                    now(),
            },
        )

    return {
        "status":
            "PASS",

        "lessonCode":
            lesson_code,

        "lessonTitle":
            lesson_title,

        "bodyUrl":
            url,

        "artifactPath":
            str(
                artifact_path.relative_to(
                    ROOT
                )
            ),

        "artifactSha256":
            actual_sha,

        "text":
            text,

        "titleVerified":
            True,

        "titleVerificationMode":
            mode,

        "retrievalChannel":
            retrieval[
                "channel"
            ],

        "retrievalUrl":
            retrieval[
                "retrievalUrl"
            ],

        "retrievalTimestamp":
            retrieval.get(
                "archiveTimestamp"
            ),

        "resumed":
            False,
    }


# ============================================================
# PREFLIGHT
# ============================================================

for directory in (
    ARTIFACT_DIR,
    TEXT_DIR,
    META_DIR,
    LESSON_DIR,
):
    directory.mkdir(
        parents=True,
        exist_ok=True,
    )

run = load(
    RUN_PATH
)

step04 = (
    run.get(
        "checkpoints",
        {}
    )
    .get(
        "source-importer"
    )
)

if (
    not step04
    or step04.get(
        "status"
    ) != "PASS"
):
    raise SystemExit(
        "FAIL: Step 04 checkpoint is not PASS"
    )

import_index = load(
    IMPORT_INDEX
)

if (
    import_index.get(
        "lessonCount"
    )
    != 1560
):
    raise SystemExit(
        "FAIL: Step 04 importer index is not 1560"
    )


# ============================================================
# BUILD STANDALONE ROUTES
# ============================================================

requests = []
packet_by_code = {}

strategy_counts = Counter()

for lesson_code, item in (
    import_index[
        "lessons"
    ].items()
):
    packet_path = Path(
        item[
            "packetPath"
        ]
    )

    packet = load(
        packet_path
    )

    if (
        packet.get(
            "lessonType"
        )
        != "STANDALONE"
    ):
        continue

    packet_by_code[
        lesson_code
    ] = packet

    route = body_url_for(
        packet
    )

    strategy_counts[
        route[
            "strategy"
        ]
    ] += 1

    requests.append({
        "lessonCode":
            lesson_code,

        "lessonTitle":
            packet.get(
                "lessonTitle"
            )
            or "",

        "strategy":
            route[
                "strategy"
            ],

        "bodyUrl":
            route[
                "bodyUrl"
            ],

        "needsDownload":
            route[
                "needsDownload"
            ],
    })


if len(requests) != EXPECTED_TOTAL:
    raise SystemExit(
        "FAIL: expected 1466 standalone body routes"
    )

if (
    strategy_counts[
        "HTML_PREPARATION_TO_FULL_LESSON"
    ]
    != EXPECTED_IM_FULL
):
    raise SystemExit(
        "FAIL: expected 1437 IM full-page routes"
    )

if (
    strategy_counts[
        "PDF_SECTION_LOCATOR"
    ]
    != EXPECTED_PDF
):
    raise SystemExit(
        "FAIL: expected 23 PDF routes"
    )

if (
    strategy_counts[
        "HTML_CURRENT_SOURCE_PAGE"
    ]
    != EXPECTED_CURRENT_HTML
):
    raise SystemExit(
        "FAIL: expected 6 current-page HTML routes"
    )


# ============================================================
# ACQUIRE 1437 IM FULL PAGES
# ============================================================

im_requests = [
    x
    for x in requests
    if x[
        "needsDownload"
    ]
]

print()
print(
    "IM full lesson pages to resolve :",
    len(
        im_requests
    ),
)
print(
    "Workers                         :",
    MAX_WORKERS,
)
print()

im_results = {}
processed = 0
downloaded = 0
resumed = 0
failures = []

with ThreadPoolExecutor(
    max_workers=MAX_WORKERS
) as executor:

    future_map = {
        executor.submit(
            acquire_im_body,
            request,
        ):
        request
        for request in im_requests
    }

    for future in as_completed(
        future_map
    ):
        result = future.result()

        processed += 1

        code = result[
            "lessonCode"
        ]

        im_results[
            code
        ] = result

        if (
            result[
                "status"
            ]
            != "PASS"
        ):
            failures.append(
                result
            )

        elif result.get(
            "resumed"
        ):
            resumed += 1

        else:
            downloaded += 1

        if (
            processed % 50 == 0
            or processed == len(
                im_requests
            )
        ):
            print(
                f"Progress: {processed}/"
                f"{len(im_requests)} "
                f"| downloaded={downloaded} "
                f"| resumed={resumed} "
                f"| failed={len(failures)}",
                flush=True,
            )


# ============================================================
# BUILD 1466 BODY PACKETS
# ============================================================

body_index = {}

body_ready = 0

pdf_locator_verified = 0
current_html_reused = 0
im_title_verified = 0

body_packet_written = 0
body_packet_resumed = 0

for request in requests:
    code = request[
        "lessonCode"
    ]

    packet = packet_by_code[
        code
    ]

    strategy = request[
        "strategy"
    ]

    source = packet.get(
        "source",
        {}
    )

    rights = packet.get(
        "rights",
        {}
    )

    imported = packet.get(
        "importedSource",
        {}
    )

    if (
        strategy
        == "HTML_PREPARATION_TO_FULL_LESSON"
    ):
        result = im_results.get(
            code
        )

        if (
            not result
            or result.get(
                "status"
            )
            != "PASS"
        ):
            continue

        artifact_path = Path(
            result[
                "artifactPath"
            ]
        )

        source_text = result[
            "text"
        ]

        text_sha = sha_text(
            source_text
        )

        text_path = (
            TEXT_DIR /
            f"{text_sha}.txt"
        )

        if not text_path.exists():
            atomic_text(
                text_path,
                source_text + "\n",
            )

        body_source = {
            "bodySourceUrl":
                result[
                    "bodyUrl"
                ],

            "bodyArtifactPath":
                str(
                    artifact_path
                ),

            "bodyArtifactSha256":
                result[
                    "artifactSha256"
                ],

            "bodyTextPath":
                str(
                    text_path.relative_to(
                        ROOT
                    )
                ),

            "bodyTextSha256":
                sha_file(
                    text_path
                ),

            "bodyTextCharacterCount":
                len(
                    source_text
                ),

            "validationType":
                "LESSON_TITLE",

            "validationValue":
                packet.get(
                    "lessonTitle"
                ),

            "validationPassed":
                True,

            "validationMode":
                result[
                    "titleVerificationMode"
                ],

            "retrievalChannel":
                result[
                    "retrievalChannel"
                ],

            "retrievalUrl":
                result[
                    "retrievalUrl"
                ],

            "retrievalTimestamp":
                result[
                    "retrievalTimestamp"
                ],
        }

        im_title_verified += 1

    elif (
        strategy
        == "PDF_SECTION_LOCATOR"
    ):
        artifact_path = Path(
            imported[
                "sourceArtifactPath"
            ]
        )

        if not artifact_path.is_absolute():
            artifact_path = (
                ROOT /
                artifact_path
            )

        if not artifact_path.exists():
            failures.append({
                "status":
                    "FAIL_EXISTING_ARTIFACT",

                "lessonCode":
                    code,

                "artifactPath":
                    str(
                        artifact_path
                    ),
            })
            continue

        source_text_path = Path(
            imported[
                "sourceTextPath"
            ]
        )

        if not source_text_path.is_absolute():
            source_text_path = (
                ROOT /
                source_text_path
            )

        if not source_text_path.exists():
            failures.append({
                "status":
                    "FAIL_EXISTING_TEXT",

                "lessonCode":
                    code,
            })
            continue

        source_text = (
            source_text_path.read_text(
                encoding="utf-8"
            )
        )

        section = (
            source.get(
                "sourceLocator"
            )
            or {}
        ).get(
            "section"
        )

        if (
            not section
            or not locator_matches(
                section,
                source_text,
            )
        ):
            failures.append({
                "status":
                    "FAIL_PDF_LOCATOR",

                "lessonCode":
                    code,

                "section":
                    section,
            })
            continue

        body_source = {
            "bodySourceUrl":
                source.get(
                    "sourceUrl"
                ),

            "bodyArtifactPath":
                str(
                    artifact_path.relative_to(
                        ROOT
                    )
                ),

            "bodyArtifactSha256":
                sha_file(
                    artifact_path
                ),

            "bodyTextPath":
                str(
                    source_text_path.relative_to(
                        ROOT
                    )
                ),

            "bodyTextSha256":
                sha_file(
                    source_text_path
                ),

            "bodyTextCharacterCount":
                len(
                    source_text
                ),

            "validationType":
                "PDF_SECTION_LOCATOR",

            "validationValue":
                section,

            "validationPassed":
                True,

            "validationMode":
                "NORMALIZED_SECTION_MATCH",

            "retrievalChannel":
                source.get(
                    "retrievalChannel"
                ),

            "retrievalUrl":
                source.get(
                    "retrievalUrl"
                ),

            "retrievalTimestamp":
                source.get(
                    "retrievalTimestamp"
                ),
        }

        pdf_locator_verified += 1

    elif (
        strategy
        == "HTML_CURRENT_SOURCE_PAGE"
    ):
        artifact_path = Path(
            imported[
                "sourceArtifactPath"
            ]
        )

        if not artifact_path.is_absolute():
            artifact_path = (
                ROOT /
                artifact_path
            )

        source_text_path = Path(
            imported[
                "sourceTextPath"
            ]
        )

        if not source_text_path.is_absolute():
            source_text_path = (
                ROOT /
                source_text_path
            )

        if (
            not artifact_path.exists()
            or not source_text_path.exists()
        ):
            failures.append({
                "status":
                    "FAIL_EXISTING_HTML",

                "lessonCode":
                    code,
            })
            continue

        source_text = (
            source_text_path.read_text(
                encoding="utf-8"
            )
        )

        if len(
            search_key(
                source_text
            )
        ) < 300:
            failures.append({
                "status":
                    "FAIL_CURRENT_HTML_SHORT",

                "lessonCode":
                    code,
            })
            continue

        body_source = {
            "bodySourceUrl":
                source.get(
                    "sourceUrl"
                ),

            "bodyArtifactPath":
                str(
                    artifact_path.relative_to(
                        ROOT
                    )
                ),

            "bodyArtifactSha256":
                sha_file(
                    artifact_path
                ),

            "bodyTextPath":
                str(
                    source_text_path.relative_to(
                        ROOT
                    )
                ),

            "bodyTextSha256":
                sha_file(
                    source_text_path
                ),

            "bodyTextCharacterCount":
                len(
                    source_text
                ),

            "validationType":
                "EXACT_SOURCE_PAGE",

            "validationValue":
                source.get(
                    "sourceUrl"
                ),

            "validationPassed":
                True,

            "validationMode":
                "STEP03_EXACT_PAGE_PLUS_NONEMPTY_BODY",

            "retrievalChannel":
                source.get(
                    "retrievalChannel"
                ),

            "retrievalUrl":
                source.get(
                    "retrievalUrl"
                ),

            "retrievalTimestamp":
                source.get(
                    "retrievalTimestamp"
                ),
        }

        current_html_reused += 1

    else:
        failures.append({
            "status":
                "FAIL_UNKNOWN_STRATEGY",

            "lessonCode":
                code,

            "strategy":
                strategy,
        })
        continue

    body_input_hash = (
        canonical_hash({
            "acquirerVersion":
                ACQUIRER_VERSION,

            "lessonCode":
                code,

            "lessonTitle":
                packet.get(
                    "lessonTitle"
                ),

            "strategy":
                strategy,

            "bodySourceUrl":
                body_source[
                    "bodySourceUrl"
                ],

            "bodyArtifactSha256":
                body_source[
                    "bodyArtifactSha256"
                ],

            "bodyTextSha256":
                body_source[
                    "bodyTextSha256"
                ],

            "rightsContentUseMode":
                rights.get(
                    "contentUseMode"
                ),
        })
    )

    body_packet = {
        "schemaVersion":
            1,

        "acquirerVersion":
            ACQUIRER_VERSION,

        "lessonCode":
            code,

        "lessonTitle":
            packet.get(
                "lessonTitle"
            ),

        "lessonType":
            "STANDALONE",

        "unitNumber":
            packet.get(
                "unitNumber"
            ),

        "unitTitle":
            packet.get(
                "unitTitle"
            ),

        "lessonNumber":
            packet.get(
                "lessonNumber"
            ),

        "outcomeCodes":
            packet.get(
                "outcomeCodes"
            )
            or [],

        "strategy":
            strategy,

        "preparationSourceUrl":
            source.get(
                "sourceUrl"
            ),

        "bodySource":
            body_source,

        "rights":
            rights,

        "bodyInputHash":
            body_input_hash,
    }

    name = (
        sha_text(
            code
        )[:24]
        + ".json"
    )

    body_packet_path = (
        LESSON_DIR /
        name
    )

    packet_resume = False

    if body_packet_path.exists():
        try:
            old = load(
                body_packet_path
            )

            if (
                old.get(
                    "bodyInputHash"
                )
                == body_input_hash
            ):
                packet_resume = True

        except Exception:
            packet_resume = False

    if packet_resume:
        body_packet_resumed += 1

    else:
        body_packet[
            "createdAtUtc"
        ] = now()

        atomic_json(
            body_packet_path,
            body_packet,
        )

        body_packet_written += 1

    body_index[
        code
    ] = {
        "packetPath":
            str(
                body_packet_path.relative_to(
                    ROOT
                )
            ),

        "bodyInputHash":
            body_input_hash,

        "bodyTextSha256":
            body_source[
                "bodyTextSha256"
            ],
    }

    body_ready += 1


# ============================================================
# FINAL GATE
# ============================================================

# Deduplicate failure rows for reporting.
failure_by_key = {}

for item in failures:
    key = (
        item.get(
            "lessonCode"
        ),
        item.get(
            "status"
        ),
    )

    failure_by_key[
        key
    ] = item

failures = list(
    failure_by_key.values()
)

report = {
    "schemaVersion":
        1,

    "generatedAtUtc":
        now(),

    "status":
        (
            "PASS"
            if (
                body_ready
                == EXPECTED_TOTAL
                and not failures
            )
            else "INCOMPLETE"
        ),

    "summary": {
        "standaloneLessons":
            EXPECTED_TOTAL,

        "bodyReady":
            body_ready,

        "bodyFailures":
            len(
                failures
            ),

        "imFullLessonRoutes":
            EXPECTED_IM_FULL,

        "imFullLessonDownloadedThisRun":
            downloaded,

        "imFullLessonResumed":
            resumed,

        "imTitleVerified":
            im_title_verified,

        "pdfSectionVerified":
            pdf_locator_verified,

        "currentHtmlReused":
            current_html_reused,

        "bodyPacketsWrittenThisRun":
            body_packet_written,

        "bodyPacketsResumed":
            body_packet_resumed,

        "strategies":
            dict(
                sorted(
                    strategy_counts.items()
                )
            ),
    },

    "failures":
        failures,
}

atomic_json(
    REPORT_PATH,
    report,
)


print()
print(
    "=============================================================="
)
print(
    " PHASE 29 — STEP 05B RESULT"
)
print(
    "=============================================================="
)
print(
    "Standalone lessons        :",
    EXPECTED_TOTAL,
)
print(
    "Body ready                :",
    body_ready,
)
print(
    "Failures                  :",
    len(
        failures
    ),
)
print()
print(
    "IM full pages             :",
    EXPECTED_IM_FULL,
)
print(
    "IM downloaded this run    :",
    downloaded,
)
print(
    "IM resumed                :",
    resumed,
)
print(
    "IM titles verified        :",
    im_title_verified,
)
print()
print(
    "PDF locators verified     :",
    pdf_locator_verified,
)
print(
    "Existing HTML reused      :",
    current_html_reused,
)
print()
print(
    "Body packets written      :",
    body_packet_written,
)
print(
    "Body packets resumed      :",
    body_packet_resumed,
)


if (
    body_ready
    != EXPECTED_TOTAL
    or failures
):
    print()
    print(
        "STEP 05B NOT CLOSED."
    )
    print(
        "ONLY FAILED BODY ROUTES REQUIRE RECOVERY."
    )

    for item in failures[:50]:
        print(
            "FAIL",
            item.get(
                "lessonCode"
            ),
            item.get(
                "status"
            ),
            item.get(
                "bodyUrl",
                "",
            ),
        )

    raise SystemExit(2)


index_document = {
    "schemaVersion":
        1,

    "generatedAtUtc":
        now(),

    "lessonCount":
        len(
            body_index
        ),

    "lessons":
        body_index,
}

atomic_json(
    BODY_INDEX,
    index_document,
)

body_index_sha = sha_file(
    BODY_INDEX
)


# ============================================================
# CHECKPOINT
# ============================================================

run = load(
    RUN_PATH
)

checkpoints = run.setdefault(
    "checkpoints",
    {}
)

checkpoints[
    "full-lesson-body-acquisition"
] = {
    "status":
        "PASS",

    "completedAtUtc":
        now(),

    "standaloneLessons":
        1466,

    "bodyReady":
        1466,

    "imFullLessonPages":
        1437,

    "pdfSectionLessons":
        23,

    "currentHtmlLessons":
        6,

    "bodyIndexSha256":
        body_index_sha,

    "reportSha256":
        sha_file(
            REPORT_PATH
        ),
}

run[
    "currentStage"
] = (
    "full-lesson-body-acquisition"
)

run[
    "updatedAtUtc"
] = now()

atomic_json(
    RUN_PATH,
    run,
)


print()
print(
    "PASS: 1466/1466 full body sources ready"
)
print(
    "PASS: 1437 IM full lesson titles verified"
)
print(
    "PASS: 23 PDF exact locators verified"
)
print(
    "PASS: 6 exact HTML source pages reused"
)
print(
    "PASS: Step 05B checkpoint persisted"
)
