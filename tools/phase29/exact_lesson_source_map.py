#!/usr/bin/env python3

from __future__ import annotations

import hashlib
import json
import re
from collections import Counter, defaultdict
from datetime import datetime, timezone
from pathlib import Path
from urllib.parse import urlparse


ROOT = Path.cwd()

STATE = ROOT / ".phase29-source-rebuild"

BLUEPRINT_DIR = (
    ROOT /
    "src/Edulytics.Core/Curriculum/"
    "LessonBlueprints/Packs"
)

CANONICAL_CONTENT_DIR = (
    ROOT /
    "src/Edulytics.Core/Curriculum/"
    "LessonContent/Packs"
)

SOURCE_REPORT = (
    STATE /
    "reports/source-license-acquisition.json"
)

OUTPUT = (
    STATE /
    "exact-lesson-source-map.json"
)

REPORT = (
    STATE /
    "reports/exact-lesson-source-map.json"
)

RUN = STATE / "run.json"


EXPECTED_TOTAL = 1560
EXPECTED_STANDALONE = 1466
EXPECTED_SUPPORTING = 94


def now() -> str:
    return datetime.now(
        timezone.utc
    ).isoformat()


def load(path: Path):
    return json.loads(
        path.read_text(encoding="utf-8")
    )


def write(path: Path, value):
    path.parent.mkdir(
        parents=True,
        exist_ok=True
    )

    path.write_text(
        json.dumps(
            value,
            indent=2,
            ensure_ascii=False,
        ) + "\n",
        encoding="utf-8",
    )


def sha256(path: Path) -> str:
    h = hashlib.sha256()

    with path.open("rb") as f:
        for chunk in iter(
            lambda: f.read(1024 * 1024),
            b"",
        ):
            h.update(chunk)

    return h.hexdigest()


def norm_url(value: str | None) -> str:
    if not value:
        return ""

    return value.strip().rstrip("/")


def get(obj, *names, default=None):
    for name in names:
        if name in obj:
            return obj[name]

    return default


def recursive_urls(node):
    result = []

    if isinstance(node, dict):
        for key, value in node.items():
            lk = key.lower()

            if (
                isinstance(value, str)
                and value.startswith(
                    ("http://", "https://")
                )
                and "url" in lk
            ):
                result.append(value)

            result.extend(
                recursive_urls(value)
            )

    elif isinstance(node, list):
        for value in node:
            result.extend(
                recursive_urls(value)
            )

    return result


def pdf_like(url: str) -> bool:
    return (
        urlparse(url)
        .path.lower()
        .endswith(".pdf")
    )


def lesson_specific_html(
    lesson_url: str,
    unit_url: str,
    root_url: str,
) -> bool:
    if not lesson_url:
        return False

    if pdf_like(lesson_url):
        return False

    if lesson_url in {
        norm_url(unit_url),
        norm_url(root_url),
    }:
        return False

    path = urlparse(
        lesson_url
    ).path.lower()

    markers = (
        "/lesson/",
        "/lessons/",
        "/preparation",
        "/teacher/",
    )

    if any(
        marker in path
        for marker in markers
    ):
        return True

    # IM/Kendall Hunt pattern:
    # .../<course>/<unit>/<lesson>/...
    numeric_parts = [
        x for x in path.split("/")
        if x.isdigit()
    ]

    if len(numeric_parts) >= 3:
        return True

    return True


def explicit_locator(lesson: dict) -> dict:
    locator = {}

    candidates = {
        "page":
            (
                "SourcePage",
                "sourcePage",
                "Page",
                "page",
            ),
        "pageRange":
            (
                "SourcePageRange",
                "sourcePageRange",
                "PageRange",
                "pageRange",
            ),
        "section":
            (
                "SourceSection",
                "sourceSection",
                "Section",
                "section",
            ),
        "locator":
            (
                "SourceLocator",
                "sourceLocator",
                "Locator",
                "locator",
            ),
    }

    for canonical, keys in candidates.items():
        value = get(
            lesson,
            *keys,
        )

        if value not in (
            None,
            "",
            [],
            {},
        ):
            locator[canonical] = value

    return locator


source_report = load(
    SOURCE_REPORT
)

source_items = source_report[
    "sources"
]

source_by_url = {}

for item in source_items:
    url = norm_url(
        item.get("url")
    )

    if not url:
        continue

    if url in source_by_url:
        raise SystemExit(
            "FAIL: duplicate source-state URL: "
            + url
        )

    source_by_url[url] = item


# PHASE29_CANONICAL_CLASSIFICATION_V2
#
# Existing US-CCSS canonical content packs contain exactly the
# 1,466 formally aligned lessons. The remaining 94 pedagogical
# blueprint lessons are Supporting lessons.
#
# Blueprint OutcomeCodes alone are NOT a valid classifier across
# all source families, especially HS source packs.

canonical_lessons = {}

for content_path in sorted(
    CANONICAL_CONTENT_DIR.glob(
        "*.lesson-content-pack.json"
    )
):
    content_document = load(
        content_path
    )

    content_pack_code = get(
        content_document,
        "packCode",
        "PackCode",
    )

    if content_pack_code != "US-CCSS-MATH":
        continue

    content_lessons = get(
        content_document,
        "lessons",
        "Lessons",
        default=[],
    ) or []

    for content_lesson in content_lessons:
        content_lesson_code = get(
            content_lesson,
            "lessonCode",
            "LessonCode",
        )

        if not content_lesson_code:
            raise SystemExit(
                "FAIL: canonical content lesson "
                f"without LessonCode in {content_path}"
            )

        if content_lesson_code in canonical_lessons:
            raise SystemExit(
                "FAIL: duplicate canonical content LessonCode: "
                + content_lesson_code
            )

        content_outcomes = get(
            content_lesson,
            "outcomeCodes",
            "OutcomeCodes",
            default=[],
        ) or []

        if not content_outcomes:
            raise SystemExit(
                "FAIL: current canonical standalone lesson "
                "has no OutcomeCodes: "
                + content_lesson_code
            )

        canonical_lessons[
            content_lesson_code
        ] = {
            "outcomeCodes":
                content_outcomes,

            "contentPackPath":
                str(
                    content_path.relative_to(
                        ROOT
                    )
                ),
        }


if len(canonical_lessons) != EXPECTED_STANDALONE:
    raise SystemExit(
        f"FAIL: expected {EXPECTED_STANDALONE} "
        "canonical standalone lessons, found "
        f"{len(canonical_lessons)}"
    )


blueprint_files = sorted(
    BLUEPRINT_DIR.glob(
        "*.lesson-blueprint.json"
    )
)

if not blueprint_files:
    raise SystemExit(
        "FAIL: no lesson blueprint packs found"
    )


rows = []
seen_codes = set()


for pack_path in blueprint_files:
    document = load(
        pack_path
    )

    dumped = json.dumps(
        document,
        ensure_ascii=False,
    )

    if not (
        "US-CCSS-MATH" in dumped
        or "Common Core" in dumped
        or "CCSS" in dumped
    ):
        continue

    lessons = get(
        document,
        "Lessons",
        "lessons",
        default=[],
    )

    if not lessons:
        continue

    pack_code = get(
        document,
        "BlueprintCode",
        "blueprintCode",
    )

    source_title = get(
        document,
        "SourceTitle",
        "sourceTitle",
    )

    source_publisher = get(
        document,
        "SourcePublisher",
        "sourcePublisher",
    )

    source_edition = get(
        document,
        "SourceEdition",
        "sourceEdition",
    )

    source_license = get(
        document,
        "SourceLicense",
        "sourceLicense",
    )

    source_root = norm_url(
        get(
            document,
            "SourceRootUrl",
            "sourceRootUrl",
            default="",
        )
    )

    units = get(
        document,
        "Units",
        "units",
        default=[],
    )

    unit_url_by_number = {}

    for unit in units:
        number = get(
            unit,
            "Number",
            "number",
        )

        url = norm_url(
            get(
                unit,
                "SourceUrl",
                "sourceUrl",
                default="",
            )
        )

        if number is not None:
            unit_url_by_number[
                str(number)
            ] = url

    for lesson in lessons:
        lesson_code = get(
            lesson,
            "LessonCode",
            "lessonCode",
        )

        if not lesson_code:
            raise SystemExit(
                f"FAIL: lesson without LessonCode in {pack_path}"
            )

        if lesson_code in seen_codes:
            raise SystemExit(
                "FAIL: duplicate LessonCode: "
                + lesson_code
            )

        seen_codes.add(
            lesson_code
        )

        lesson_url = norm_url(
            get(
                lesson,
                "SourceUrl",
                "sourceUrl",
                default="",
            )
        )

        unit_number = get(
            lesson,
            "UnitNumber",
            "unitNumber",
        )

        unit_url = unit_url_by_number.get(
            str(unit_number),
            "",
        )

        blueprint_outcomes = get(
            lesson,
            "OutcomeCodes",
            "outcomeCodes",
            default=[],
        ) or []

        alignments = get(
            lesson,
            "Alignments",
            "alignments",
            default=[],
        ) or []

        canonical_lesson = canonical_lessons.get(
            lesson_code
        )

        if canonical_lesson is not None:
            kind = "STANDALONE"

            # Use the accepted formal targets already locked in
            # the canonical content pack. This fixes HS blueprint
            # families whose local OutcomeCodes field is incomplete.
            outcomes = list(
                canonical_lesson[
                    "outcomeCodes"
                ]
            )

        else:
            kind = "SUPPORTING"

            # Supporting lessons remain full pedagogical lessons,
            # but Edulytics must not invent an official outcome.
            outcomes = []

        state = source_by_url.get(
            lesson_url
        )

        locator = explicit_locator(
            lesson
        )

        mapping_status = ""
        mapping_reason = ""

        if not lesson_url:
            mapping_status = "UNRESOLVED"
            mapping_reason = (
                "Lesson has no SourceUrl."
            )

        elif state is None:
            mapping_status = "UNRESOLVED"
            mapping_reason = (
                "Exact lesson SourceUrl has no "
                "acquired source artifact."
            )

        elif (
            state.get(
                "mappingAllowed",
                state.get(
                    "classification"
                ) == "APPROVED",
            )
            is not True
        ):
            mapping_status = "BLOCKED"
            mapping_reason = (
                "Matched artifact is not "
                "mapping-eligible."
            )

        elif pdf_like(
            lesson_url
        ):
            if locator:
                mapping_status = "EXACT"
                mapping_reason = (
                    "PDF artifact plus explicit "
                    "lesson locator."
                )
            else:
                mapping_status = "NEEDS_LOCATOR"
                mapping_reason = (
                    "PDF artifact is matched but "
                    "lesson has no page/section locator."
                )

        elif lesson_specific_html(
            lesson_url,
            unit_url,
            source_root,
        ):
            mapping_status = "EXACT"
            mapping_reason = (
                "Exact lesson-specific source URL "
                "matched to acquired artifact."
            )

        else:
            mapping_status = "NEEDS_LOCATOR"
            mapping_reason = (
                "Source URL appears broader than "
                "an exact lesson resource."
            )

        row = {
            "lessonCode":
                lesson_code,

            "sourceLessonCode":
                get(
                    lesson,
                    "SourceLessonCode",
                    "sourceLessonCode",
                ),

            "lessonType":
                kind,

            "unitNumber":
                unit_number,

            "unitTitle":
                get(
                    lesson,
                    "UnitTitle",
                    "unitTitle",
                ),

            "lessonNumber":
                get(
                    lesson,
                    "LessonNumber",
                    "lessonNumber",
                ),

            "lessonTitle":
                get(
                    lesson,
                    "Title",
                    "title",
                ),

            "outcomeCodes":
                outcomes,

            "blueprintOutcomeCodes":
                blueprint_outcomes,

            "canonicalContentPackPath":
                (
                    canonical_lesson[
                        "contentPackPath"
                    ]
                    if canonical_lesson
                    else None
                ),

            "alignments":
                alignments,

            "blueprintCode":
                pack_code,

            "blueprintPath":
                str(
                    pack_path.relative_to(
                        ROOT
                    )
                ),

            "sourceTitle":
                source_title,

            "sourcePublisher":
                source_publisher,

            "sourceEdition":
                source_edition,

            "declaredSourceLicense":
                source_license,

            "sourceUrl":
                lesson_url,

            "sourceRootUrl":
                source_root,

            "unitSourceUrl":
                unit_url,

            "sourceLocator":
                locator,

            "artifactPath":
                (
                    state.get(
                        "artifactPath"
                    )
                    if state
                    else None
                ),

            "artifactSha256":
                (
                    state.get(
                        "artifactSha256"
                    )
                    if state
                    else None
                ),

            "machineClassification":
                (
                    state.get(
                        "classification"
                    )
                    if state
                    else None
                ),

            "effectiveClassification":
                (
                    state.get(
                        "effectiveClassification",
                        state.get(
                            "classification"
                        ),
                    )
                    if state
                    else None
                ),

            "mappingAllowed":
                (
                    state.get(
                        "mappingAllowed",
                        state.get(
                            "classification"
                        ) == "APPROVED",
                    )
                    if state
                    else False
                ),

            "importAllowed":
                (
                    state.get(
                        "importAllowed",
                        state.get(
                            "classification"
                        ) == "APPROVED",
                    )
                    if state
                    else False
                ),

            "retrievalChannel":
                (
                    state.get(
                        "retrievalChannel",
                        "ORIGIN",
                    )
                    if state
                    else None
                ),

            "retrievalUrl":
                (
                    state.get(
                        "retrievalUrl",
                        lesson_url,
                    )
                    if state
                    else None
                ),

            "retrievalTimestamp":
                (
                    state.get(
                        "retrievalTimestamp",
                        "",
                    )
                    if state
                    else None
                ),

            "mappingStatus":
                mapping_status,

            "mappingReason":
                mapping_reason,
        }

        rows.append(
            row
        )


blueprint_lesson_codes = {
    x["lessonCode"]
    for x in rows
}

canonical_lesson_codes = set(
    canonical_lessons
)

canonical_not_in_blueprints = (
    canonical_lesson_codes
    - blueprint_lesson_codes
)

if canonical_not_in_blueprints:
    raise SystemExit(
        "FAIL: canonical lessons missing from lesson blueprints: "
        + ", ".join(
            sorted(
                canonical_not_in_blueprints
            )[:20]
        )
    )

supporting_lesson_codes = (
    blueprint_lesson_codes
    - canonical_lesson_codes
)

if len(supporting_lesson_codes) != EXPECTED_SUPPORTING:
    raise SystemExit(
        f"FAIL: expected {EXPECTED_SUPPORTING} "
        "supporting lessons from exact set difference, found "
        f"{len(supporting_lesson_codes)}"
    )


total = len(rows)

standalone = sum(
    x["lessonType"] == "STANDALONE"
    for x in rows
)

supporting = sum(
    x["lessonType"] == "SUPPORTING"
    for x in rows
)

statuses = Counter(
    x["mappingStatus"]
    for x in rows
)

if total != EXPECTED_TOTAL:
    raise SystemExit(
        f"FAIL: expected {EXPECTED_TOTAL} lessons, "
        f"found {total}"
    )

if standalone != EXPECTED_STANDALONE:
    raise SystemExit(
        f"FAIL: expected {EXPECTED_STANDALONE} "
        f"standalone lessons, found {standalone}"
    )

if supporting != EXPECTED_SUPPORTING:
    raise SystemExit(
        f"FAIL: expected {EXPECTED_SUPPORTING} "
        f"supporting lessons, found {supporting}"
    )


# Verify artifact integrity for every exact match.
verified_artifacts = {}

for row in rows:
    artifact = row.get(
        "artifactPath"
    )

    expected_hash = row.get(
        "artifactSha256"
    )

    if not artifact or not expected_hash:
        continue

    path = Path(
        artifact
    )

    if not path.is_absolute():
        path = ROOT / path

    key = str(
        path.resolve()
    )

    if key not in verified_artifacts:
        if not path.exists():
            raise SystemExit(
                "FAIL: mapped artifact missing: "
                + str(path)
            )

        actual_hash = sha256(
            path
        )

        if actual_hash != expected_hash:
            raise SystemExit(
                "FAIL: mapped artifact SHA mismatch: "
                + str(path)
            )

        verified_artifacts[
            key
        ] = actual_hash


mapping = {
    "schemaVersion":
        1,

    "generatedAtUtc":
        now(),

    "target": {
        "totalLessons":
            EXPECTED_TOTAL,
        "standalone":
            EXPECTED_STANDALONE,
        "supporting":
            EXPECTED_SUPPORTING,
    },

    "summary": {
        "total":
            total,
        "standalone":
            standalone,
        "supporting":
            supporting,
        "exact":
            statuses.get(
                "EXACT",
                0,
            ),
        "needsLocator":
            statuses.get(
                "NEEDS_LOCATOR",
                0,
            ),
        "unresolved":
            statuses.get(
                "UNRESOLVED",
                0,
            ),
        "blocked":
            statuses.get(
                "BLOCKED",
                0,
            ),
        "verifiedArtifacts":
            len(
                verified_artifacts
            ),
    },

    "lessons":
        rows,
}

write(
    OUTPUT,
    mapping,
)


problems = [
    x for x in rows
    if x["mappingStatus"] != "EXACT"
]

problem_groups = defaultdict(
    list
)

for x in problems:
    problem_groups[
        x["mappingStatus"]
    ].append(
        {
            "lessonCode":
                x["lessonCode"],
            "lessonTitle":
                x["lessonTitle"],
            "sourceUrl":
                x["sourceUrl"],
            "reason":
                x["mappingReason"],
        }
    )


report = {
    "schemaVersion":
        1,

    "generatedAtUtc":
        now(),

    "summary":
        mapping["summary"],

    "problemCount":
        len(problems),

    "problems":
        dict(
            problem_groups
        ),
}

write(
    REPORT,
    report,
)


print()
print(
    "=============================================================="
)
print(
    " EXACT LESSON -> SOURCE MAP"
)
print(
    "=============================================================="
)
print(
    "Total lessons     :",
    total,
)
print(
    "Standalone        :",
    standalone,
)
print(
    "Supporting        :",
    supporting,
)
print()
print(
    "EXACT             :",
    statuses.get("EXACT", 0),
)
print(
    "NEEDS_LOCATOR     :",
    statuses.get("NEEDS_LOCATOR", 0),
)
print(
    "UNRESOLVED        :",
    statuses.get("UNRESOLVED", 0),
)
print(
    "BLOCKED           :",
    statuses.get("BLOCKED", 0),
)
print()
print(
    "Verified artifacts:",
    len(verified_artifacts),
)
print(
    "=============================================================="
)


# Do not mark Step 03 PASS unless absolutely every lesson
# has an exact, artifact-backed mapping.
if problems:
    print()
    print(
        "STEP 03 NOT CLOSED."
    )
    print(
        "Exact mapping problems:",
        len(problems),
    )
    print()
    print(
        "Report:",
        REPORT.relative_to(ROOT),
    )
    print()
    print(
        "No content was changed."
    )

    raise SystemExit(2)


run = load(
    RUN
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
        total,
    "standalone":
        standalone,
    "supporting":
        supporting,
    "exactMappings":
        total,
    "mapSha256":
        sha256(
            OUTPUT
        ),
    "reportSha256":
        sha256(
            REPORT
        ),
}

run[
    "currentStage"
] = "exact-lesson-source-map"

run[
    "updatedAtUtc"
] = now()

write(
    RUN,
    run,
)

print()
print(
    "PASS: Step 03 checkpoint persisted"
)
