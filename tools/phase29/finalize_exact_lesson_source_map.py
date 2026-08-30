#!/usr/bin/env python3

from __future__ import annotations

import hashlib
import json
import re
import subprocess
from datetime import datetime, timezone
from pathlib import Path


ROOT = Path.cwd()

STATE = ROOT / ".phase29-source-rebuild"

MAP_PATH = (
    STATE /
    "exact-lesson-source-map.json"
)

RECOVERY_PATH = (
    STATE /
    "reports/step03b-exact-source-recovery.json"
)

FINAL_REPORT_PATH = (
    STATE /
    "reports/exact-lesson-source-map-final.json"
)

RUN_PATH = STATE / "run.json"


EXPECTED_TOTAL = 1560
EXPECTED_STANDALONE = 1466
EXPECTED_SUPPORTING = 94
EXPECTED_BASE_EXACT = 1537
EXPECTED_RECOVERY = 23


# Exact authoritative source metadata.
#
# sourceUrl = authoritative/original pedagogical artifact
# retrieval details are separate and never replace provenance.

CATALOG = {
    "mvp_m2_mod7.pdf": {
        "sourceUrl":
            "https://www.mathematicsvisionproject.org/uploads/"
            "1/1/6/3/11636986/m2_mod7_tn_52017f.pdf",
        "license": "CC BY 4.0",
        "usageMode": "OPEN_LICENSE_IMPORT",
        "commercialUseAllowed": True,
        "translationAllowed": True,
        "shareAlikeRequired": False,
        "retrievalChannel": "INTERNET_ARCHIVE",
        "archiveTimestamp": "20170624020339",
    },

    "mvp_g1_mod6h.pdf": {
        "sourceUrl":
            "https://www.mathematicsvisionproject.org/uploads/"
            "1/1/6/3/11636986/g1_mod6h_tn_82017f.pdf",
        "license": "CC BY 4.0",
        "usageMode": "OPEN_LICENSE_IMPORT",
        "commercialUseAllowed": True,
        "translationAllowed": True,
        "shareAlikeRequired": False,
        "retrievalChannel": "INTERNET_ARCHIVE",
        "archiveTimestamp": "20240903135047",
    },

    "mvp_g1_mod7.pdf": {
        "sourceUrl":
            "https://www.mathematicsvisionproject.org/uploads/"
            "1/1/6/3/11636986/g1_mod7_tn_82017f.pdf",
        "license": "CC BY 4.0",
        "usageMode": "OPEN_LICENSE_IMPORT",
        "commercialUseAllowed": True,
        "translationAllowed": True,
        "shareAlikeRequired": False,
        "retrievalChannel": "INTERNET_ARCHIVE",
        "archiveTimestamp": "20251029081342",
    },

    "mvp_m3_mod5.pdf": {
        "sourceUrl":
            "https://www.mathematicsvisionproject.org/uploads/"
            "1/1/6/3/11636986/sec3mod5tn718.pdf",
        "license": "CC BY 4.0",
        "usageMode": "OPEN_LICENSE_IMPORT",
        "commercialUseAllowed": True,
        "translationAllowed": True,
        "shareAlikeRequired": False,
        "retrievalChannel": "VERIFIED_MIRROR",
        "retrievalUrl":
            "https://dl.icdst.org/pdfs/files3/"
            "45a21efc5744474e57e0a99bbe4cd985.pdf",
    },

    "mvp_m2_mod9.pdf": {
        "sourceUrl":
            "https://www.mathematicsvisionproject.org/uploads/"
            "1/1/6/3/11636986/m2_mod9_tn_52017f.pdf",
        "license": "CC BY 4.0",
        "usageMode": "OPEN_LICENSE_IMPORT",
        "commercialUseAllowed": True,
        "translationAllowed": True,
        "shareAlikeRequired": False,
        "retrievalChannel": "INTERNET_ARCHIVE",
        "archiveTimestamp": "20251029071022",
    },

    "mvp_a2_mod10h.pdf": {
        "sourceUrl":
            "https://www.mathematicsvisionproject.org/uploads/"
            "1/1/6/3/11636986/a2mod10tnh718.pdf",
        "license": "CC BY 4.0",
        "usageMode": "OPEN_LICENSE_IMPORT",
        "commercialUseAllowed": True,
        "translationAllowed": True,
        "shareAlikeRequired": False,
        "retrievalChannel": "INTERNET_ARCHIVE",
        "archiveTimestamp": "20240711231146",
    },

    "mvp_a2_mod3h.pdf": {
        "sourceUrl":
            "https://www.mathematicsvisionproject.org/uploads/"
            "1/1/6/3/11636986/a2mod3tnh718.pdf",
        "license": "CC BY 4.0",
        "usageMode": "OPEN_LICENSE_IMPORT",
        "commercialUseAllowed": True,
        "translationAllowed": True,
        "shareAlikeRequired": False,
        "retrievalChannel": "INTERNET_ARCHIVE",
        "archiveTimestamp": "20240713220305",
    },

    "mvp_m3_mod4.pdf": {
        "sourceUrl":
            "https://www.mathematicsvisionproject.org/uploads/"
            "1/1/6/3/11636986/sec3mod4tn718.pdf",
        "license": "CC BY 4.0",
        "usageMode": "OPEN_LICENSE_IMPORT",
        "commercialUseAllowed": True,
        "translationAllowed": True,
        "shareAlikeRequired": False,
        "retrievalChannel": "INTERNET_ARCHIVE",
        "archiveTimestamp": "20240617080052",
    },

    "mvp_m1_mod8h.pdf": {
        "sourceUrl":
            "https://www.mathematicsvisionproject.org/uploads/"
            "1/1/6/3/11636986/m1_mod8_teh_52016f.pdf",
        "license": "CC BY 4.0",
        "usageMode": "OPEN_LICENSE_IMPORT",
        "commercialUseAllowed": True,
        "translationAllowed": True,
        "shareAlikeRequired": False,
        "retrievalChannel": "INTERNET_ARCHIVE",
        "archiveTimestamp": "20170627185700",
    },

    "mvp_m1_mod4h.pdf": {
        "sourceUrl":
            "https://www.mathematicsvisionproject.org/uploads/"
            "1/1/6/3/11636986/m1_mod4_seh_52016f.pdf",
        "license": "CC BY 4.0",
        "usageMode": "OPEN_LICENSE_IMPORT",
        "commercialUseAllowed": True,
        "translationAllowed": True,
        "shareAlikeRequired": False,
        "retrievalChannel": "INTERNET_ARCHIVE",
        "archiveTimestamp": "20170627202514",
    },

    "mvp_m3_mod7.pdf": {
        "sourceUrl":
            "https://www.mathematicsvisionproject.org/uploads/"
            "1/1/6/3/11636986/sec3mod7tn718.pdf",
        "license": "CC BY 4.0",
        "usageMode": "OPEN_LICENSE_IMPORT",
        "commercialUseAllowed": True,
        "translationAllowed": True,
        "shareAlikeRequired": False,
        "retrievalChannel": "INTERNET_ARCHIVE",
        "archiveTimestamp": "20240618202858",
    },

    "mvp_m3_mod2.pdf": {
        "sourceUrl":
            "https://www.mathematicsvisionproject.org/uploads/"
            "1/1/6/3/11636986/sec3mod2tn718.pdf",
        "license": "CC BY 4.0",
        "usageMode": "OPEN_LICENSE_IMPORT",
        "commercialUseAllowed": True,
        "translationAllowed": True,
        "shareAlikeRequired": False,
        "retrievalChannel": "VERIFIED_MIRROR",
        "retrievalUrl":
            "https://freekidsbooks.org/wp-content/uploads/"
            "2019/11/FKB-UEN-SecondaryMathIII-HONORSStudent-"
            "Mod2-LogarithmicFunctions-oer.pdf",
    },

    "mvp_m3_mod7h.pdf": {
        "sourceUrl":
            "https://www.mathematicsvisionproject.org/uploads/"
            "1/1/6/3/11636986/sec3mod7seh718.pdf",
        "license": "CC BY 4.0",
        "usageMode": "OPEN_LICENSE_IMPORT",
        "commercialUseAllowed": True,
        "translationAllowed": True,
        "shareAlikeRequired": False,
        "retrievalChannel": "INTERNET_ARCHIVE",
        "archiveTimestamp": "20240806000944",
    },

    "fair-decisions-eureka-m5.pdf": {
        "sourceUrl":
            "https://unbounded-uploads.s3.amazonaws.com/"
            "attachments/14824/precalculus-m5-student-materials.pdf",
        "license": "CC BY-NC-SA 3.0",
        "usageMode": "NONCOMMERCIAL_SHAREALIKE_IMPORT",
        "commercialUseAllowed": False,
        "translationAllowed": True,
        "shareAlikeRequired": True,
        "retrievalChannel": "ORIGIN",
    },

    "openstax-introductory-statistics-1e.pdf": {
        "sourceUrl":
            "https://assets.openstax.org/oscms-prodcms/media/"
            "documents/IntroductoryStatistics-OP_i6tAI7e.pdf",
        "license": "CC BY 4.0",
        "usageMode": "OPEN_LICENSE_IMPORT",
        "commercialUseAllowed": True,
        "translationAllowed": True,
        "shareAlikeRequired": False,
        "retrievalChannel": "ORIGIN",
    },
}


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


def atomic_write(path, value):
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


def sha256(path):
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


def normalize(value):
    value = (
        value
        .replace("’", "'")
        .replace("‘", "'")
        .replace("–", "-")
        .replace("—", "-")
        .replace("\u00a0", " ")
        .lower()
    )

    return re.sub(
        r"\s+",
        " ",
        value,
    ).strip()


def pdf_text(path):
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
        timeout=240,
    )

    if result.returncode != 0:
        raise SystemExit(
            "FAIL: pdftotext failed: "
            + str(path)
        )

    return result.stdout


def pages_containing(
    raw_text,
    marker,
):
    marker = normalize(marker)

    pages = raw_text.split("\f")

    matches = []

    variants = [marker]

    if "cavalieri" in marker:
        variants.append(
            marker.replace(
                "cavalieri",
                "cavelieri",
            )
        )

    if (
        "amazing inverse trig "
        "function race"
        in marker
    ):
        variants.extend([
            normalize(
                "The Amazing Inverse Trig Function Race"
            ),
            normalize(
                "Amazing Inverse Trig Function Race"
            ),
            normalize(
                "The Amazing Trig Function Race"
            ),
        ])

    for index, page in enumerate(
        pages,
        1,
    ):
        page_norm = normalize(page)

        if any(
            value in page_norm
            for value in variants
        ):
            matches.append(index)

    return matches


mapping = load(
    MAP_PATH
)

recovery = load(
    RECOVERY_PATH
)


# ------------------------------------------------------------
# Validate baseline state.
# ------------------------------------------------------------

summary = mapping[
    "summary"
]

if (
    summary.get("total")
    != EXPECTED_TOTAL
    or summary.get("standalone")
    != EXPECTED_STANDALONE
    or summary.get("supporting")
    != EXPECTED_SUPPORTING
    or summary.get("exact")
    != EXPECTED_BASE_EXACT
    or summary.get("needsLocator")
    != EXPECTED_RECOVERY
    or summary.get("unresolved")
    != 0
    or summary.get("blocked")
    != 0
):
    raise SystemExit(
        "FAIL: exact-map baseline changed unexpectedly"
    )


recovery_summary = recovery[
    "summary"
]

if (
    recovery_summary.get(
        "recoveryLessons"
    ) != 23
    or recovery_summary.get(
        "validatedExactSources"
    ) != 23
    or recovery_summary.get(
        "validationFailures"
    ) != 0
    or recovery_summary.get(
        "projectedExactMappings"
    ) != 1560
    or recovery.get(
        "failures"
    )
):
    raise SystemExit(
        "FAIL: Step 03B recovery manifest "
        "is not a clean 23/23 PASS"
    )


problem_rows = {
    row["lessonCode"]: row
    for row in mapping["lessons"]
    if row.get(
        "mappingStatus"
    ) != "EXACT"
}

recovery_rows = {
    row["lessonCode"]: row
    for row in recovery[
        "lessons"
    ]
}

if len(problem_rows) != 23:
    raise SystemExit(
        "FAIL: expected exactly 23 pending map rows"
    )

if (
    set(problem_rows)
    != set(recovery_rows)
):
    raise SystemExit(
        "FAIL: recovery LessonCodes do not exactly "
        "match the 23 pending map rows"
    )


# ------------------------------------------------------------
# Validate every recovery artifact and exact locator.
# ------------------------------------------------------------

text_cache = {}
sha_cache = {}

for lesson_code, recovered in recovery_rows.items():
    artifact_value = recovered.get(
        "artifactPath"
    )

    if not artifact_value:
        raise SystemExit(
            "FAIL: recovery artifactPath missing: "
            + lesson_code
        )

    artifact = Path(
        artifact_value
    )

    if not artifact.is_absolute():
        artifact = ROOT / artifact

    if not artifact.exists():
        raise SystemExit(
            "FAIL: recovery artifact missing: "
            + str(artifact)
        )

    if artifact.read_bytes()[:5] != b"%PDF-":
        raise SystemExit(
            "FAIL: recovery artifact is not PDF: "
            + str(artifact)
        )

    filename = artifact.name

    if filename not in CATALOG:
        raise SystemExit(
            "FAIL: artifact missing from exact "
            "source catalog: "
            + filename
        )

    if filename not in sha_cache:
        sha_cache[
            filename
        ] = sha256(
            artifact
        )

    actual_sha = sha_cache[
        filename
    ]

    recovery_sha = recovered.get(
        "artifactSha256"
    )

    if (
        recovery_sha
        and recovery_sha
        != actual_sha
    ):
        raise SystemExit(
            "FAIL: recovery SHA mismatch: "
            + lesson_code
        )

    if filename not in text_cache:
        text_cache[
            filename
        ] = pdf_text(
            artifact
        )

    raw_text = text_cache[
        filename
    ]

    locator = recovered.get(
        "sourceLocator"
    ) or {}

    section = locator.get(
        "section"
    )

    if not section:
        raise SystemExit(
            "FAIL: exact source section missing: "
            + lesson_code
        )

    pages = pages_containing(
        raw_text,
        section,
    )

    if not pages:
        raise SystemExit(
            "FAIL: section title not found in "
            "recovery artifact: "
            + lesson_code
        )

    source = CATALOG[
        filename
    ]

    old = problem_rows[
        lesson_code
    ]

    old_source_url = old.get(
        "sourceUrl"
    )

    old[
        "supersededSourceUrl"
    ] = old_source_url

    old[
        "sourceUrl"
    ] = source[
        "sourceUrl"
    ]

    old[
        "sourceLocator"
    ] = {
        "section":
            section,

        "pdfPagesContainingTitle":
            pages,
    }

    old[
        "artifactPath"
    ] = str(
        artifact.relative_to(
            ROOT
        )
    )

    old[
        "artifactSha256"
    ] = actual_sha

    old[
        "licenseStatus"
    ] = source[
        "license"
    ]

    old[
        "usageMode"
    ] = source[
        "usageMode"
    ]

    old[
        "commercialUseAllowed"
    ] = source[
        "commercialUseAllowed"
    ]

    old[
        "translationAllowed"
    ] = source[
        "translationAllowed"
    ]

    old[
        "shareAlikeRequired"
    ] = source[
        "shareAlikeRequired"
    ]

    old[
        "mappingAllowed"
    ] = True

    old[
        "importAllowed"
    ] = True

    old[
        "blocksPhase29"
    ] = False

    old[
        "titleVerifiedInArtifact"
    ] = True

    old[
        "retrievalChannel"
    ] = source[
        "retrievalChannel"
    ]

    old[
        "retrievalTimestamp"
    ] = source.get(
        "archiveTimestamp",
        "",
    )

    old[
        "retrievalUrl"
    ] = source.get(
        "retrievalUrl",
        source[
            "sourceUrl"
        ],
    )

    old[
        "mappingStatus"
    ] = "EXACT"

    old[
        "mappingReason"
    ] = (
        "Exact instructional artifact and "
        "lesson section verified during "
        "Phase 29 Step 03B recovery."
    )


# ------------------------------------------------------------
# Recompute final map from actual rows.
# ------------------------------------------------------------

statuses = {}

for row in mapping[
    "lessons"
]:
    status = row.get(
        "mappingStatus"
    )

    statuses[
        status
    ] = (
        statuses.get(
            status,
            0,
        )
        + 1
    )


exact = statuses.get(
    "EXACT",
    0,
)

needs_locator = statuses.get(
    "NEEDS_LOCATOR",
    0,
)

unresolved = statuses.get(
    "UNRESOLVED",
    0,
)

blocked = statuses.get(
    "BLOCKED",
    0,
)


if (
    exact != 1560
    or needs_locator != 0
    or unresolved != 0
    or blocked != 0
):
    raise SystemExit(
        "FAIL: final source-map counts are not "
        "1560 EXACT / 0 pending"
    )


mapping[
    "generatedAtUtc"
] = now()

mapping[
    "finalizedAtUtc"
] = now()

mapping[
    "finalizedWithRecoveryManifest"
] = True

mapping[
    "recoveryManifest"
] = str(
    RECOVERY_PATH.relative_to(
        ROOT
    )
)

mapping[
    "summary"
][
    "exact"
] = exact

mapping[
    "summary"
][
    "needsLocator"
] = needs_locator

mapping[
    "summary"
][
    "unresolved"
] = unresolved

mapping[
    "summary"
][
    "blocked"
] = blocked

mapping[
    "summary"
][
    "recoveredExactMappings"
] = 23

mapping[
    "summary"
][
    "finalExactMappings"
] = 1560


# Write final map atomically.
atomic_write(
    MAP_PATH,
    mapping,
)


# ------------------------------------------------------------
# Final report.
# ------------------------------------------------------------

final_report = {
    "schemaVersion":
        1,

    "generatedAtUtc":
        now(),

    "status":
        "PASS",

    "summary": {
        "totalLessons":
            1560,

        "standalone":
            1466,

        "supporting":
            94,

        "originalExact":
            1537,

        "recoveredExact":
            23,

        "finalExact":
            1560,

        "needsLocator":
            0,

        "unresolved":
            0,

        "blocked":
            0,

        "recoveryArtifactsVerified":
            len(
                text_cache
            ),
    },

    "exactMapPath":
        str(
            MAP_PATH.relative_to(
                ROOT
            )
        ),

    "recoveryManifestPath":
        str(
            RECOVERY_PATH.relative_to(
                ROOT
            )
        ),

    "exactMapSha256":
        sha256(
            MAP_PATH
        ),

    "recoveryManifestSha256":
        sha256(
            RECOVERY_PATH
        ),
}

atomic_write(
    FINAL_REPORT_PATH,
    final_report,
)


# ------------------------------------------------------------
# Persist Step 03 checkpoint.
# ------------------------------------------------------------

run = load(
    RUN_PATH
)

checkpoints = run.setdefault(
    "checkpoints",
    {}
)

checkpoints[
    "exact-lesson-source-map"
] = {
    "status":
        "PASS",

    "completedAtUtc":
        now(),

    "totalLessons":
        1560,

    "standalone":
        1466,

    "supporting":
        94,

    "exactMappings":
        1560,

    "needsLocator":
        0,

    "unresolved":
        0,

    "blocked":
        0,

    "recoveredMappings":
        23,

    "mapSha256":
        sha256(
            MAP_PATH
        ),

    "recoveryManifestSha256":
        sha256(
            RECOVERY_PATH
        ),

    "finalReportSha256":
        sha256(
            FINAL_REPORT_PATH
        ),
}

run[
    "currentStage"
] = "exact-lesson-source-map"

run[
    "updatedAtUtc"
] = now()

atomic_write(
    RUN_PATH,
    run,
)


print(
    "=============================================================="
)
print(
    " PHASE 29 — STEP 03C FINAL RESULT"
)
print(
    "=============================================================="
)
print(
    "Total lessons        : 1560"
)
print(
    "Standalone           : 1466"
)
print(
    "Supporting           : 94"
)
print()
print(
    "Existing exact       : 1537"
)
print(
    "Recovered exact      : 23"
)
print(
    "Final EXACT          : 1560 / 1560"
)
print()
print(
    "NEEDS_LOCATOR        : 0"
)
print(
    "UNRESOLVED           : 0"
)
print(
    "BLOCKED              : 0"
)
print()
print(
    "PASS: exact source map finalized"
)
print(
    "PASS: Step 03 checkpoint persisted"
)
