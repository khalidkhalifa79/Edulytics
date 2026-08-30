#!/usr/bin/env python3

from __future__ import annotations

import hashlib
import html
import json
import re
from collections import Counter
from datetime import datetime, timezone
from html.parser import HTMLParser
from pathlib import Path


ROOT = Path.cwd()
STATE = ROOT / ".phase29-source-rebuild"

BODY_INDEX = (
    STATE /
    "body-source/lesson-index.json"
)

RUN_PATH = (
    STATE /
    "run.json"
)

OUT_ROOT = (
    STATE /
    "candidates"
)

EVIDENCE_DIR = (
    OUT_ROOT /
    "evidence"
)

INDEX_PATH = (
    OUT_ROOT /
    "lesson-index.json"
)

REPORT_PATH = (
    STATE /
    "reports/source-faithful-candidate-extraction.json"
)

VERSION = (
    "phase29-source-faithful-candidate-v1"
)

EXPECTED = 1466
EXPECTED_IM = 1437
EXPECTED_PDF = 23
EXPECTED_EXACT_HTML = 6


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
    return sha_text(
        json.dumps(
            value,
            sort_keys=True,
            ensure_ascii=False,
            separators=(",", ":"),
        )
    )


def clean(value):
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


def key(value):
    return re.sub(
        r"\s+",
        " ",
        clean(value).lower(),
    ).strip()


def useful(value):
    value = clean(
        value
    )

    if len(value) < 20:
        return False

    lowered = value.lower()

    noise = (
        "teachers with a valid work email",
        "click here to register",
        "privacy policy",
        "accessibility information",
        "illustrative mathematics name and logo",
    )

    return not any(
        x in lowered
        for x in noise
    )


class StructuredHtmlParser(
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
        "li",
        "div",
        "section",
        "article",
        "br",
        "tr",
        "td",
        "th",
    }

    def __init__(self):
        super().__init__(
            convert_charrefs=True
        )

        self.skip_depth = 0

        self.heading_level = None
        self.heading_parts = []

        self.current_heading = ""
        self.current_level = 0
        self.body_parts = []

        self.sections = []

    def flush(self):
        body = clean(
            " ".join(
                self.body_parts
            )
        )

        heading = clean(
            self.current_heading
        )

        if (
            heading
            or useful(body)
        ):
            self.sections.append({
                "headingLevel":
                    self.current_level,

                "heading":
                    heading,

                "body":
                    body,
            })

        self.body_parts = []

    def handle_starttag(
        self,
        tag,
        attrs,
    ):
        tag = tag.lower()

        if tag in self.SKIP:
            self.skip_depth += 1
            return

        if self.skip_depth:
            return

        if re.fullmatch(
            r"h[1-6]",
            tag,
        ):
            self.flush()

            self.heading_level = int(
                tag[1]
            )

            self.heading_parts = []

        elif tag in self.BLOCK:
            self.body_parts.append(
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

        if self.skip_depth:
            return

        if re.fullmatch(
            r"h[1-6]",
            tag,
        ):
            self.current_heading = clean(
                " ".join(
                    self.heading_parts
                )
            )

            self.current_level = (
                self.heading_level
                or 0
            )

            self.heading_level = None
            self.heading_parts = []

        elif tag in self.BLOCK:
            self.body_parts.append(
                "\n"
            )

    def handle_data(
        self,
        data,
    ):
        if (
            self.skip_depth
            or not data.strip()
        ):
            return

        if (
            self.heading_level
            is not None
        ):
            self.heading_parts.append(
                data
            )
        else:
            self.body_parts.append(
                data
            )

    def finish(self):
        self.flush()

        return [
            x
            for x in self.sections
            if (
                x["heading"]
                or useful(
                    x["body"]
                )
            )
        ]


def classify_heading(
    heading,
):
    h = key(
        heading
    )

    if not h:
        return "UNLABELED"

    if (
        h == "narrative"
        or h.endswith(
            " narrative"
        )
    ):
        return "NARRATIVE"

    if (
        "student facing"
        in h
    ):
        return "STUDENT_FACING"

    if (
        "anticipated misconception"
        in h
        or "advancing student thinking"
        in h
    ):
        return "MISCONCEPTIONS"

    if (
        "activity synthesis"
        in h
    ):
        return "ACTIVITY_SYNTHESIS"

    if (
        "lesson synthesis"
        in h
        or h == "synthesis"
    ):
        return "LESSON_SYNTHESIS"

    if (
        h == "activity"
        or h.startswith(
            "activity "
        )
        or "optional activity"
        in h
    ):
        return "ACTIVITY"

    if (
        h == "warm-up"
        or h.startswith(
            "warm-up"
        )
    ):
        return "WARM_UP"

    if (
        h == "launch"
        or h.startswith(
            "launch"
        )
    ):
        return "LAUNCH"

    if (
        "cool-down"
        in h
    ):
        return "COOL_DOWN"

    if (
        "student response"
        in h
    ):
        return "STUDENT_RESPONSE_LOCKED"

    if (
        "required material"
        in h
        or "required preparation"
        in h
    ):
        return "MATERIALS"

    if (
        h.startswith(
            "lesson "
        )
    ):
        return "LESSON_HEADER"

    return "OTHER"


def filter_blocks(
    blocks,
):
    result = []

    for block in blocks:
        category = classify_heading(
            block.get(
                "heading",
                "",
            )
        )

        body = clean(
            block.get(
                "body",
                "",
            )
        )

        if category in {
            "STUDENT_RESPONSE_LOCKED",
            "MATERIALS",
        }:
            continue

        if (
            not useful(body)
            and category not in {
                "LESSON_HEADER",
            }
        ):
            continue

        result.append({
            "category":
                category,

            "headingLevel":
                block.get(
                    "headingLevel",
                    0,
                ),

            "heading":
                clean(
                    block.get(
                        "heading",
                        "",
                    )
                ),

            "text":
                body,
        })

    return result


def extract_pdf_window(
    source_text,
    locator,
):
    section = clean(
        locator or ""
    )

    if not section:
        return source_text

    lowered = source_text.lower()
    target = section.lower()

    pos = lowered.find(
        target
    )

    if pos < 0:
        # Known historical spelling.
        if "cavalieri" in target:
            pos = lowered.find(
                target.replace(
                    "cavalieri",
                    "cavelieri",
                )
            )

    if pos < 0:
        return source_text

    # Keep a bounded instructional window.
    start = max(
        0,
        pos - 1000,
    )

    end = min(
        len(source_text),
        pos + 18000,
    )

    return clean(
        source_text[
            start:end
        ]
    )


def evidence_metrics(
    blocks,
):
    counts = Counter(
        x[
            "category"
        ]
        for x in blocks
    )

    chars = sum(
        len(
            x["text"]
        )
        for x in blocks
    )

    pedagogical = sum(
        counts[x]
        for x in (
            "NARRATIVE",
            "ACTIVITY",
            "WARM_UP",
            "ACTIVITY_SYNTHESIS",
            "LESSON_SYNTHESIS",
            "STUDENT_FACING",
            "MISCONCEPTIONS",
        )
    )

    return (
        counts,
        chars,
        pedagogical,
    )


# ============================================================
# PREFLIGHT
# ============================================================

EVIDENCE_DIR.mkdir(
    parents=True,
    exist_ok=True,
)

run = load(
    RUN_PATH
)

cp = (
    run.get(
        "checkpoints",
        {}
    )
    .get(
        "full-lesson-body-acquisition"
    )
)

if (
    not cp
    or cp.get(
        "status"
    ) != "PASS"
    or cp.get(
        "bodyReady"
    ) != 1466
):
    raise SystemExit(
        "FAIL: Step 05B checkpoint is not a clean 1466/1466 PASS"
    )

body_index = load(
    BODY_INDEX
)

if (
    body_index.get(
        "lessonCount"
    )
    != EXPECTED
):
    raise SystemExit(
        "FAIL: body-source index is not 1466"
    )


# ============================================================
# EXTRACT CANDIDATE EVIDENCE
# ============================================================

candidate_index = {}

strategy_counts = Counter()
category_counts = Counter()

written = 0
resumed = 0
failures = []

for number, (
    lesson_code,
    item,
) in enumerate(
    body_index[
        "lessons"
    ].items(),
    1,
):
    packet_path = Path(
        item[
            "packetPath"
        ]
    )

    packet = load(
        packet_path
    )

    strategy = packet[
        "strategy"
    ]

    strategy_counts[
        strategy
    ] += 1

    body = packet[
        "bodySource"
    ]

    body_text_path = Path(
        body[
            "bodyTextPath"
        ]
    )

    if not body_text_path.exists():
        failures.append({
            "lessonCode":
                lesson_code,

            "reason":
                "BODY_TEXT_MISSING",
        })
        continue

    actual_text_sha = sha_file(
        body_text_path
    )

    if (
        actual_text_sha
        != body[
            "bodyTextSha256"
        ]
    ):
        failures.append({
            "lessonCode":
                lesson_code,

            "reason":
                "BODY_TEXT_SHA_MISMATCH",
        })
        continue

    raw_text = (
        body_text_path.read_text(
            encoding="utf-8"
        )
    )

    blocks = []

    if (
        strategy
        == "HTML_PREPARATION_TO_FULL_LESSON"
    ):
        artifact_path = Path(
            body[
                "bodyArtifactPath"
            ]
        )

        if not artifact_path.exists():
            failures.append({
                "lessonCode":
                    lesson_code,

                "reason":
                    "FULL_HTML_ARTIFACT_MISSING",
            })
            continue

        raw_html = artifact_path.read_text(
            encoding="utf-8",
            errors="ignore",
        )

        parser = (
            StructuredHtmlParser()
        )

        parser.feed(
            raw_html
        )

        parser.close()

        blocks = filter_blocks(
            parser.finish()
        )

    elif (
        strategy
        == "PDF_SECTION_LOCATOR"
    ):
        section = (
            packet.get(
                "bodySource",
                {},
            )
            .get(
                "validationValue"
            )
        )

        window = extract_pdf_window(
            raw_text,
            section,
        )

        blocks = [{
            "category":
                "PDF_INSTRUCTIONAL_SECTION",

            "headingLevel":
                0,

            "heading":
                section or "",

            "text":
                window,
        }]

    elif (
        strategy
        == "HTML_CURRENT_SOURCE_PAGE"
    ):
        blocks = [{
            "category":
                "EXACT_HTML_SOURCE",

            "headingLevel":
                0,

            "heading":
                packet.get(
                    "lessonTitle"
                )
                or "",

            "text":
                clean(
                    raw_text
                ),
        }]

    else:
        failures.append({
            "lessonCode":
                lesson_code,

            "reason":
                "UNKNOWN_STRATEGY",

            "strategy":
                strategy,
        })
        continue

    counts, chars, pedagogical = (
        evidence_metrics(
            blocks
        )
    )

    # Fail closed against empty/wrong pages.
    if chars < 200:
        failures.append({
            "lessonCode":
                lesson_code,

            "reason":
                "INSUFFICIENT_SOURCE_EVIDENCE",

            "characters":
                chars,
        })
        continue

    if (
        strategy
        == "HTML_PREPARATION_TO_FULL_LESSON"
        and pedagogical < 1
    ):
        failures.append({
            "lessonCode":
                lesson_code,

            "reason":
                "NO_PEDAGOGICAL_BLOCKS",
        })
        continue

    for category, count in (
        counts.items()
    ):
        category_counts[
            category
        ] += count

    candidate_input_hash = (
        canonical_hash({
            "version":
                VERSION,

            "lessonCode":
                lesson_code,

            "bodyInputHash":
                packet[
                    "bodyInputHash"
                ],

            "bodyTextSha256":
                body[
                    "bodyTextSha256"
                ],

            "strategy":
                strategy,
        })
    )

    candidate = {
        "schemaVersion":
            1,

        "extractorVersion":
            VERSION,

        "lessonCode":
            lesson_code,

        "lessonTitle":
            packet.get(
                "lessonTitle"
            ),

        "lessonType":
            packet.get(
                "lessonType"
            ),

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

        "source": {
            "url":
                body.get(
                    "bodySourceUrl"
                ),

            "artifactPath":
                body.get(
                    "bodyArtifactPath"
                ),

            "artifactSha256":
                body.get(
                    "bodyArtifactSha256"
                ),

            "textPath":
                body.get(
                    "bodyTextPath"
                ),

            "textSha256":
                body.get(
                    "bodyTextSha256"
                ),

            "validationType":
                body.get(
                    "validationType"
                ),

            "validationValue":
                body.get(
                    "validationValue"
                ),

            "validationPassed":
                body.get(
                    "validationPassed"
                ),
        },

        "rights":
            packet.get(
                "rights"
            )
            or {},

        "evidence": {
            "blocks":
                blocks,

            "categoryCounts":
                dict(
                    sorted(
                        counts.items()
                    )
                ),

            "characterCount":
                chars,

            "pedagogicalBlockCount":
                pedagogical,
        },

        # Nothing below is canonical content yet.
        "candidateState":
            "SOURCE_EVIDENCE_READY",

        "authoringRequired":
            True,

        "candidateInputHash":
            candidate_input_hash,
    }

    filename = (
        sha_text(
            lesson_code
        )[:24]
        + ".json"
    )

    out_path = (
        EVIDENCE_DIR /
        filename
    )

    packet_resume = False

    if out_path.exists():
        try:
            old = load(
                out_path
            )

            if (
                old.get(
                    "candidateInputHash"
                )
                == candidate_input_hash
            ):
                packet_resume = True

        except Exception:
            packet_resume = False

    if packet_resume:
        resumed += 1

    else:
        candidate[
            "createdAtUtc"
        ] = now()

        atomic_json(
            out_path,
            candidate,
        )

        written += 1

    candidate_index[
        lesson_code
    ] = {
        "candidatePath":
            str(
                out_path.relative_to(
                    ROOT
                )
            ),

        "candidateInputHash":
            candidate_input_hash,

        "sourceTextSha256":
            body[
                "bodyTextSha256"
            ],
    }

    if (
        number % 100 == 0
        or number == EXPECTED
    ):
        print(
            f"Progress: {number}/{EXPECTED} "
            f"| ready={len(candidate_index)} "
            f"| written={written} "
            f"| resumed={resumed} "
            f"| failures={len(failures)}",
            flush=True,
        )


# ============================================================
# FINAL VALIDATION
# ============================================================

if len(
    candidate_index
) != EXPECTED:
    report = {
        "schemaVersion":
            1,

        "generatedAtUtc":
            now(),

        "status":
            "INCOMPLETE",

        "summary": {
            "expected":
                EXPECTED,

            "ready":
                len(
                    candidate_index
                ),

            "failures":
                len(
                    failures
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
        "STEP 05C NOT CLOSED."
    )

    for item in failures[:50]:
        print(
            "FAIL",
            item[
                "lessonCode"
            ],
            item[
                "reason"
            ],
        )

    raise SystemExit(2)


if failures:
    raise SystemExit(
        "FAIL: candidate extraction contains failures"
    )


if (
    strategy_counts[
        "HTML_PREPARATION_TO_FULL_LESSON"
    ]
    != EXPECTED_IM
):
    raise SystemExit(
        "FAIL: expected 1437 IM candidates"
    )

if (
    strategy_counts[
        "PDF_SECTION_LOCATOR"
    ]
    != EXPECTED_PDF
):
    raise SystemExit(
        "FAIL: expected 23 PDF candidates"
    )

if (
    strategy_counts[
        "HTML_CURRENT_SOURCE_PAGE"
    ]
    != EXPECTED_EXACT_HTML
):
    raise SystemExit(
        "FAIL: expected 6 exact HTML candidates"
    )


index_doc = {
    "schemaVersion":
        1,

    "generatedAtUtc":
        now(),

    "lessonCount":
        EXPECTED,

    "lessons":
        candidate_index,
}

atomic_json(
    INDEX_PATH,
    index_doc,
)

index_sha = sha_file(
    INDEX_PATH
)


report = {
    "schemaVersion":
        1,

    "generatedAtUtc":
        now(),

    "status":
        "PASS",

    "summary": {
        "standaloneLessons":
            EXPECTED,

        "candidateEvidenceReady":
            len(
                candidate_index
            ),

        "failures":
            0,

        "writtenThisRun":
            written,

        "resumed":
            resumed,

        "strategies":
            dict(
                sorted(
                    strategy_counts.items()
                )
            ),

        "evidenceCategories":
            dict(
                sorted(
                    category_counts.items()
                )
            ),
    },

    "candidateIndexSha256":
        index_sha,
}

atomic_json(
    REPORT_PATH,
    report,
)


run = load(
    RUN_PATH
)

checkpoints = run.setdefault(
    "checkpoints",
    {}
)

checkpoints[
    "source-faithful-candidate-evidence"
] = {
    "status":
        "PASS",

    "completedAtUtc":
        now(),

    "standaloneLessons":
        EXPECTED,

    "candidateEvidenceReady":
        EXPECTED,

    "failures":
        0,

    "candidateIndexSha256":
        index_sha,

    "reportSha256":
        sha_file(
            REPORT_PATH
        ),
}

run[
    "currentStage"
] = (
    "source-faithful-candidate-evidence"
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
    "=============================================================="
)
print(
    " PHASE 29 — STEP 05C RESULT"
)
print(
    "=============================================================="
)
print(
    "Standalone lessons           : 1466"
)
print(
    "Candidate evidence ready     :",
    len(
        candidate_index
    ),
)
print(
    "Failures                     : 0"
)
print()
print(
    "Written this run             :",
    written,
)
print(
    "Resumed                      :",
    resumed,
)
print()
print(
    "Strategies                   :",
    dict(
        sorted(
            strategy_counts.items()
        )
    ),
)
print()
print(
    "Evidence categories:"
)

for category, count in sorted(
    category_counts.items()
):
    print(
        f"  {category:28} {count}"
    )

print()
print(
    "PASS: 1466/1466 source-faithful evidence candidates ready"
)
print(
    "PASS: source SHA dependencies locked"
)
print(
    "PASS: Step 05C checkpoint persisted"
)
print()
print(
    "NO SOURCE DOWNLOAD."
)
print(
    "NO CANONICAL LESSON CONTENT CHANGED."
)
