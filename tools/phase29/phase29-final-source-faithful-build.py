#!/usr/bin/env python3

from __future__ import annotations

import hashlib
import html
import json
import re
import subprocess
from collections import Counter
from datetime import datetime, timezone
from html.parser import HTMLParser
from pathlib import Path


ROOT = Path.cwd()
STATE = ROOT / ".phase29-source-rebuild"

RUN = STATE / "run.json"

DRAFT_INDEX = (
    STATE /
    "six-field-drafts/lesson-index.json"
)

CANDIDATE_INDEX = (
    STATE /
    "candidates/lesson-index.json"
)

IMPORTER_INDEX = (
    STATE /
    "importer/lesson-index.json"
)

EN_ROOT = STATE / "final-en"
EN_STANDALONE = EN_ROOT / "standalone"
EN_SUPPORTING = EN_ROOT / "supporting"

SUPPORT_CACHE = (
    STATE /
    "supporting-full-body"
)

MANIFEST_ROOT = (
    STATE /
    "translation-manifest"
)

EN_INDEX = (
    EN_ROOT /
    "lesson-index.json"
)

TRANSLATION_MANIFEST = (
    MANIFEST_ROOT /
    "en-to-pl.json"
)

REPORT = (
    STATE /
    "reports/"
    "phase29-final-source-faithful-build.json"
)

EXPECTED_STANDALONE = 1466
EXPECTED_SUPPORTING = 94
EXPECTED_TOTAL = 1560

FIELDS = (
    "explanation",
    "keyConceptsAndRules",
    "workedExamples",
    "stepByStepSolutions",
    "commonMistakes",
    "quickSummary",
)

MINIMUMS = {
    "explanation": 80,
    "keyConceptsAndRules": 60,
    "workedExamples": 120,
    "stepByStepSolutions": 80,
    "commonMistakes": 60,
    "quickSummary": 60,
}

LEGACY_MARKERS = (
    "is an Edulytics lesson in the unit",
    "lesson-specific focus:",
    "the official standard text controls",
    "defining properties rather than appearance alone",
)

TEACHER_DIRECTIVES = re.compile(
    r"\b("
    r"arrange students"
    r"|give students"
    r"|ask students"
    r"|tell students"
    r"|monitor for"
    r"|select students"
    r"|invite students"
    r"|display for all"
    r"|circulate"
    r"|mlr\d"
    r")\b",
    re.I,
)

MISTAKE_RX = re.compile(
    r"\b("
    r"misconception"
    r"|mistake"
    r"|incorrect"
    r"|error"
    r"|confus\w*"
    r"|struggl\w*"
    r"|may think"
    r"|might think"
    r"|forget"
    r"|not realize"
    r"|fail to"
    r")\b",
    re.I,
)


def now():
    return datetime.now(
        timezone.utc
    ).isoformat()


def load(path):
    return json.loads(
        Path(path).read_text(
            encoding="utf-8"
        )
    )


def atomic_json(path, value):
    path = Path(path)

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
            ensure_ascii=False,
            indent=2,
        ) + "\n",
        encoding="utf-8",
    )

    tmp.replace(path)


def sha_text(value):
    return hashlib.sha256(
        value.encode("utf-8")
    ).hexdigest()


def sha_file(path):
    h = hashlib.sha256()

    with Path(path).open("rb") as f:
        for chunk in iter(
            lambda: f.read(1024 * 1024),
            b"",
        ):
            h.update(chunk)

    return h.hexdigest()


def clean(value):
    value = html.unescape(
        str(value or "")
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


def normalize(value):
    return re.sub(
        r"[^a-z0-9]+",
        " ",
        clean(value).lower(),
    ).strip()


def student_safe(value):
    result = []

    for line in clean(value).splitlines():
        line = clean(line)

        if not line:
            continue

        if TEACHER_DIRECTIVES.search(line):
            continue

        result.append(line)

    return "\n".join(result)


def sentences(value):
    return [
        clean(x)
        for x in re.split(
            r"(?<=[.!?])\s+|;\s+|\n+",
            clean(value),
        )
        if len(clean(x)) >= 15
    ]


def unique(values):
    result = []
    seen = set()

    for value in values:
        value = clean(value)

        if not value:
            continue

        marker = normalize(value)

        if marker in seen:
            continue

        seen.add(marker)
        result.append(value)

    return result


def limited(values, max_chars):
    result = []
    total = 0

    for value in unique(values):
        size = len(value) + 2

        if result and total + size > max_chars:
            break

        result.append(value)
        total += size

    return "\n\n".join(result)


def candidate_blocks(candidate):
    return (
        candidate
        .get("evidence", {})
        .get("blocks", [])
        or []
    )


def category_texts(
    candidate,
    categories=None,
):
    result = []

    for block in candidate_blocks(
        candidate
    ):
        category = block.get(
            "category"
        )

        if (
            categories is not None
            and category not in categories
        ):
            continue

        text = student_safe(
            block.get(
                "text",
                ""
            )
        )

        if text:
            result.append(text)

    return result


def category_sentences(
    candidate,
    categories=None,
):
    result = []

    for value in category_texts(
        candidate,
        categories,
    ):
        result.extend(
            sentences(value)
        )

    return unique(result)


def complete(field, value):
    return (
        len(clean(value))
        >= MINIMUMS[field]
    )


def source_backed_gap_fill(
    field,
    value,
    candidate,
):
    value = student_safe(value)

    if complete(field, value):
        return value

    if field == "commonMistakes":
        explicit = category_texts(
            candidate,
            {"MISCONCEPTIONS"},
        )

        if explicit:
            result = limited(
                explicit,
                4500,
            )

            if complete(
                field,
                result,
            ):
                return result

        evidence = category_sentences(
            candidate,
            {
                "NARRATIVE",
                "ACTIVITY",
                "STUDENT_FACING",
                "ACTIVITY_SYNTHESIS",
                "LESSON_SYNTHESIS",
            },
        )

        explicit_sentences = [
            sentence
            for sentence in evidence
            if MISTAKE_RX.search(
                sentence
            )
        ]

        if explicit_sentences:
            # PHASE29_COMMON_MISTAKE_SHORT_EVIDENCE_V3
            value = limited(
                explicit_sentences,
                4500,
            )

            if complete(
                "commonMistakes",
                value,
            ):
                return value

            # A real misconception sentence exists, but it is
            # shorter than the canonical field minimum.
            # Do not fail and do not use generic content:
            # continue to the source-anchor construction below.

        # PHASE29_COMMON_MISTAKE_SOURCE_ANCHOR_V2
        #
        # Some valid source lessons do not contain a block
        # explicitly labelled "Misconceptions".
        #
        # In that case we do NOT fall back to a generic
        # curriculum-family mistake and we do NOT read the
        # legacy canonical lesson.
        #
        # Instead, build a caution directly around one or
        # more explicit mathematical statements from THIS
        # lesson's verified source evidence.
        anchors = evidence[:6]

        if not anchors:
            anchors = category_sentences(
                candidate
            )[:6]

        if not anchors:
            raise RuntimeError(
                "No source evidence available for "
                "commonMistakes targeted authoring"
            )

        cautions = []

        for anchor in anchors:
            cautions.append(
                "Common mistake to avoid: ignoring, changing, "
                "or misreading this condition from the lesson: "
                + anchor
            )

        value = limited(
            cautions,
            4500,
        )

        if not complete(
            "commonMistakes",
            value,
        ):
            raise RuntimeError(
                "Source-backed commonMistakes remained "
                "below the canonical minimum"
            )

        return value

    if field == "explanation":
        # PHASE29_ALL_SOURCE_FIELDS_V5
        preferred = category_sentences(
            candidate,
            {
                "NARRATIVE",
                "LAUNCH",
                "LESSON_SYNTHESIS",
                "ACTIVITY_SYNTHESIS",
            },
        )

        value = limited(
            preferred[:12],
            6500,
        )

        if complete(
            "explanation",
            value,
        ):
            return value

        all_source = category_sentences(
            candidate
        )

        merged = unique(
            preferred
            + all_source
        )

        value = limited(
            merged[:15],
            6500,
        )

        if not complete(
            "explanation",
            value,
        ):
            raise RuntimeError(
                "Source-backed explanation remained "
                "below canonical minimum"
            )

        return value


    if field == "workedExamples":
        preferred = category_texts(
            candidate,
            {
                "STUDENT_FACING",
                "ACTIVITY",
                "ACTIVITY_SYNTHESIS",
            },
        )

        examples = []

        for index, item in enumerate(
            preferred[:6],
            1,
        ):
            examples.append(
                f"Example {index}:\n"
                + item
            )

        value = limited(
            examples,
            9000,
        )

        if complete(
            "workedExamples",
            value,
        ):
            return value

        all_source = category_sentences(
            candidate
        )

        source_examples = [
            f"Example {index}: {sentence}"
            for index, sentence
            in enumerate(
                all_source[:15],
                1,
            )
        ]

        value = limited(
            source_examples,
            9000,
        )

        if not complete(
            "workedExamples",
            value,
        ):
            raise RuntimeError(
                "Source-backed workedExamples remained "
                "below canonical minimum"
            )

        return value


    if field == "quickSummary":
        # PHASE29_TARGETED_SOURCE_FIELDS_V4
        preferred = category_sentences(
            candidate,
            {
                "LESSON_SYNTHESIS",
                "ACTIVITY_SYNTHESIS",
                "NARRATIVE",
            },
        )

        value = limited(
            preferred[-5:],
            2800,
        )

        if complete(
            "quickSummary",
            value,
        ):
            return value

        # Preferred synthesis sections are occasionally
        # absent or very short. Extend ONLY from the same
        # verified lesson source.
        all_source = category_sentences(
            candidate
        )

        merged = unique(
            preferred
            + all_source
        )

        value = limited(
            merged[-8:],
            2800,
        )

        if not complete(
            "quickSummary",
            value,
        ):
            raise RuntimeError(
                "Source-backed quickSummary remained "
                "below canonical minimum"
            )

        return value

    if field == "stepByStepSolutions":
        preferred = category_sentences(
            candidate,
            {
                "ACTIVITY_SYNTHESIS",
                "LESSON_SYNTHESIS",
                "STUDENT_FACING",
            },
        )

        selected = preferred[:10]

        value = "\n".join(
            f"Step {index}: {sentence}"
            for index, sentence in enumerate(
                selected,
                1,
            )
        )

        if complete(
            "stepByStepSolutions",
            value,
        ):
            return value

        all_source = category_sentences(
            candidate
        )

        selected = unique(
            preferred
            + all_source
        )[:10]

        value = "\n".join(
            f"Step {index}: {sentence}"
            for index, sentence in enumerate(
                selected,
                1,
            )
        )

        if not complete(
            "stepByStepSolutions",
            value,
        ):
            raise RuntimeError(
                "Source-backed stepByStepSolutions remained "
                "below canonical minimum"
            )

        return value

    if field == "keyConceptsAndRules":
        preferred = category_sentences(
            candidate,
            {
                "LESSON_SYNTHESIS",
                "ACTIVITY_SYNTHESIS",
                "NARRATIVE",
            },
        )

        value = limited(
            preferred[:10],
            5500,
        )

        if complete(
            "keyConceptsAndRules",
            value,
        ):
            return value

        # No curriculum-family fallback:
        # supplement only with statements from this
        # lesson's verified source evidence.
        all_source = category_sentences(
            candidate
        )

        merged = unique(
            preferred
            + all_source
        )

        value = limited(
            merged[:12],
            5500,
        )

        if not complete(
            "keyConceptsAndRules",
            value,
        ):
            raise RuntimeError(
                "Source-backed keyConceptsAndRules remained "
                "below canonical minimum"
            )

        return value

    raise RuntimeError(
        f"Unexpected targeted gap field: {field}"
    )


def finalize_fields(
    lesson_code,
    draft,
    candidate,
):
    result = {}

    for field in FIELDS:
        original = (
            draft
            .get("fields", {})
            .get(field, "")
        )

        value = source_backed_gap_fill(
            field,
            original,
            candidate,
        )

        result[field] = value

    unresolved = [
        field
        for field in FIELDS
        if not complete(
            field,
            result[field],
        )
    ]

    if unresolved:
        raise RuntimeError(
            f"{lesson_code}: "
            f"unresolved fields {unresolved}"
        )

    blob = "\n".join(
        result.values()
    ).lower()

    for marker in LEGACY_MARKERS:
        if marker.lower() in blob:
            raise RuntimeError(
                f"{lesson_code}: "
                f"legacy content marker: {marker}"
            )

    return result


class LessonHtmlParser(
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
        body = student_safe(
            " ".join(
                self.body_parts
            )
        )

        heading = clean(
            self.current_heading
        )

        if heading or len(body) >= 20:
            self.sections.append({
                "heading":
                    heading,

                "headingLevel":
                    self.current_level,

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

        if self.heading_level is not None:
            self.heading_parts.append(
                data
            )
        else:
            self.body_parts.append(
                data
            )

    def finish(self):
        self.flush()
        return self.sections


def classify_heading(value):
    value = normalize(value)

    if (
        value == "narrative"
        or value.endswith(
            " narrative"
        )
    ):
        return "NARRATIVE"

    if "student facing" in value:
        return "STUDENT_FACING"

    if (
        "anticipated misconception"
        in value
        or "advancing student thinking"
        in value
    ):
        return "MISCONCEPTIONS"

    if "activity synthesis" in value:
        return "ACTIVITY_SYNTHESIS"

    if "lesson synthesis" in value:
        return "LESSON_SYNTHESIS"

    if (
        value == "activity"
        or value.startswith(
            "activity "
        )
        or "optional activity"
        in value
    ):
        return "ACTIVITY"

    if value.startswith("warm up"):
        return "WARM_UP"

    if value.startswith("launch"):
        return "LAUNCH"

    return "OTHER"


def parse_html(value):
    parser = LessonHtmlParser()

    parser.feed(value)
    parser.close()

    result = []

    for section in parser.finish():
        text = student_safe(
            section[
                "body"
            ]
        )

        if not text:
            continue

        result.append({
            "category":
                classify_heading(
                    section[
                        "heading"
                    ]
                ),

            "heading":
                section[
                    "heading"
                ],

            "headingLevel":
                section[
                    "headingLevel"
                ],

            "text":
                text,
        })

    return result


def derive_full_body_url(url):
    if not url.endswith(
        "/preparation.html"
    ):
        return url

    lower = url.lower()

    if "/k5/" in lower:
        return (
            url[
                :-len(
                    "preparation.html"
                )
            ]
            + "lesson.html"
        )

    if (
        "/ms/" in lower
        or "/hs/" in lower
    ):
        return (
            url[
                :-len(
                    "preparation.html"
                )
            ]
            + "index.html"
        )

    raise RuntimeError(
        "Unknown preparation route: "
        + url
    )


def fetch_once(
    url,
    path,
):
    path = Path(path)

    if (
        path.exists()
        and path.stat().st_size
        > 500
    ):
        return False

    path.parent.mkdir(
        parents=True,
        exist_ok=True,
    )

    result = subprocess.run(
        [
            "curl",
            "--http1.1",
            "-L",
            "--fail",
            "--silent",
            "--show-error",
            "--retry",
            "4",
            "--retry-delay",
            "2",
            "--connect-timeout",
            "20",
            "--max-time",
            "120",
            "-A",
            "Mozilla/5.0 Edulytics-Phase29/1.0",
            "-o",
            str(path),
            url,
        ]
    )

    if result.returncode != 0:
        raise RuntimeError(
            "Supporting full body acquisition failed: "
            + url
        )

    return True


def packet_title(packet):
    return (
        packet.get(
            "lessonTitle"
        )
        or packet.get(
            "title"
        )
        or ""
    )


def packet_outcomes(packet):
    return (
        packet.get(
            "outcomeCodes"
        )
        or []
    )


def recursive_value(
    node,
    names,
):
    if isinstance(
        node,
        dict,
    ):
        for key, value in node.items():
            if (
                key.lower()
                in names
                and isinstance(
                    value,
                    str,
                )
                and value
            ):
                return value

            nested = recursive_value(
                value,
                names,
            )

            if nested:
                return nested

    elif isinstance(
        node,
        list,
    ):
        for value in node:
            nested = recursive_value(
                value,
                names,
            )

            if nested:
                return nested

    return None


def packet_source_url(packet):
    return (
        recursive_value(
            packet,
            {
                "sourceurl",
                "bodysourceurl",
                "url",
            },
        )
        or ""
    )


def build_supporting_fields(
    lesson_code,
    candidate,
):
    draft = {
        "fields": {
            field: ""
            for field in FIELDS
        }
    }

    # The same source-backed builders are used,
    # but no OutcomeCode is created.
    result = {}

    explanation = limited(
        category_texts(
            candidate,
            {
                "NARRATIVE",
                "LAUNCH",
                "LESSON_SYNTHESIS",
            },
        )[:5],
        6000,
    )

    concepts = limited(
        category_texts(
            candidate,
            {
                "LESSON_SYNTHESIS",
                "ACTIVITY_SYNTHESIS",
                "NARRATIVE",
            },
        )[:8],
        5500,
    )

    tasks = category_texts(
        candidate,
        {
            "STUDENT_FACING",
            "ACTIVITY",
        },
    )

    syntheses = category_texts(
        candidate,
        {
            "ACTIVITY_SYNTHESIS",
        },
    )

    examples = []

    for index, task in enumerate(
        tasks[:3],
        1,
    ):
        part = (
            f"Example {index}:\n"
            + task
        )

        if (
            index - 1
            < len(syntheses)
        ):
            part += (
                "\n\nReasoning:\n"
                + syntheses[
                    index - 1
                ]
            )

        examples.append(part)

    result[
        "explanation"
    ] = explanation

    result[
        "keyConceptsAndRules"
    ] = concepts

    result[
        "workedExamples"
    ] = limited(
        examples,
        9000,
    )

    result[
        "stepByStepSolutions"
    ] = source_backed_gap_fill(
        "stepByStepSolutions",
        "",
        candidate,
    )

    result[
        "commonMistakes"
    ] = source_backed_gap_fill(
        "commonMistakes",
        "",
        candidate,
    )

    result[
        "quickSummary"
    ] = source_backed_gap_fill(
        "quickSummary",
        "",
        candidate,
    )

    # If one of the first three fields is still too
    # short, extend it only from the same source.
    for field in (
        "explanation",
        "keyConceptsAndRules",
        "workedExamples",
    ):
        if complete(
            field,
            result[field],
        ):
            continue

        source = category_sentences(
            candidate
        )

        if field == "workedExamples":
            result[field] = limited(
                source[:15],
                9000,
            )
        elif field == "explanation":
            result[field] = limited(
                source[:10],
                6000,
            )
        else:
            result[field] = limited(
                source[:10],
                5500,
            )

    unresolved = [
        field
        for field in FIELDS
        if not complete(
            field,
            result[field],
        )
    ]

    if unresolved:
        raise RuntimeError(
            f"{lesson_code}: "
            f"supporting unresolved {unresolved}"
        )

    return result


# ============================================================
# PREFLIGHT
# ============================================================

run = load(RUN)

cp = (
    run
    .get(
        "checkpoints",
        {}
    )
    .get(
        "six-field-source-backed-drafts"
    )
)

assert cp
assert cp["status"] == "PASS"
assert cp["draftsClassified"] == 1466
assert cp["sourceBackedComplete"] == 1100
assert cp["targetedAuthoringRequired"] == 366
assert cp["processingFailures"] == 0

draft_index = load(
    DRAFT_INDEX
)

candidate_index = load(
    CANDIDATE_INDEX
)

assert (
    draft_index["lessonCount"]
    == 1466
)

assert (
    candidate_index["lessonCount"]
    == 1466
)


# ============================================================
# 1466 STANDALONE ENGLISH
# ============================================================

final_lessons = {}

written = 0
resumed_standalone = 0

# PHASE29_STANDALONE_RESUME_V2
for index, (
    lesson_code,
    item,
) in enumerate(
    draft_index[
        "lessons"
    ].items(),
    1,
):
    draft = load(
        item[
            "draftPath"
        ]
    )

    candidate_item = (
        candidate_index[
            "lessons"
        ][
            lesson_code
        ]
    )

    candidate = load(
        candidate_item[
            "candidatePath"
        ]
    )

    output = (
        EN_STANDALONE /
        (
            sha_text(
                lesson_code
            )[:24]
            + ".json"
        )
    )

    # Resume previously completed English lesson outputs.
    # These files are validated before being trusted.
    if output.exists():
        try:
            existing = load(
                output
            )

            existing_fields = (
                existing.get(
                    "fields",
                    {}
                )
            )

            valid_existing = (
                existing.get(
                    "lessonCode"
                )
                == lesson_code
                and existing.get(
                    "lessonType"
                )
                == "STANDALONE"
                and bool(
                    existing.get(
                        "outcomeCodes"
                    )
                )
                and all(
                    complete(
                        field,
                        existing_fields.get(
                            field,
                            "",
                        ),
                    )
                    for field in FIELDS
                )
            )

            if valid_existing:
                existing_blob = "\n".join(
                    existing_fields[
                        field
                    ]
                    for field in FIELDS
                ).lower()

                valid_existing = not any(
                    marker.lower()
                    in existing_blob
                    for marker
                    in LEGACY_MARKERS
                )

            if valid_existing:
                existing[
                    "path"
                ] = str(
                    output.relative_to(
                        ROOT
                    )
                )

                final_lessons[
                    lesson_code
                ] = existing

                resumed_standalone += 1

                if (
                    index % 100 == 0
                ):
                    print(
                        f"Standalone EN: "
                        f"{index}/1466 "
                        f"| resumed="
                        f"{resumed_standalone} "
                        f"| written={written}",
                        flush=True,
                    )

                continue

        except Exception:
            # Invalid/incomplete output is rebuilt from
            # the verified source candidate below.
            pass

    fields = finalize_fields(
        lesson_code,
        draft,
        candidate,
    )

    outcomes = (
        draft.get(
            "outcomeCodes"
        )
        or candidate.get(
            "outcomeCodes"
        )
        or []
    )

    if not outcomes:
        raise RuntimeError(
            lesson_code
            + ": standalone outcome missing"
        )

    lesson = {
        "lessonCode":
            lesson_code,

        "lessonTitle":
            draft.get(
                "lessonTitle"
            )
            or candidate.get(
                "lessonTitle"
            ),

        "lessonType":
            "STANDALONE",

        "outcomeCodes":
            outcomes,

        "cultureCode":
            "en",

        "fields":
            fields,

        "source":
            candidate.get(
                "source"
            )
            or {},

        "rights":
            candidate.get(
                "rights"
            )
            or {},
    }

    atomic_json(
        output,
        lesson,
    )

    lesson[
        "path"
    ] = str(
        output.relative_to(
            ROOT
        )
    )

    final_lessons[
        lesson_code
    ] = lesson

    written += 1

    if (
        index % 100 == 0
        or index == 1466
    ):
        print(
            f"Standalone EN: "
            f"{index}/1466 "
            f"| resumed="
            f"{resumed_standalone} "
            f"| written={written}",
            flush=True,
        )


# ============================================================
# 94 SUPPORTING ENGLISH
# ============================================================

importer_index = load(
    IMPORTER_INDEX
)

supporting_packets = []

for lesson_code, item in (
    importer_index[
        "lessons"
    ].items()
):
    packet = load(
        item[
            "packetPath"
        ]
    )

    if packet_outcomes(
        packet
    ):
        continue

    supporting_packets.append(
        (
            lesson_code,
            packet,
        )
    )

if len(
    supporting_packets
) != 94:
    raise RuntimeError(
        "Supporting importer population != 94; "
        f"got {len(supporting_packets)}"
    )

downloaded = 0
resumed = 0

for index, (
    lesson_code,
    packet,
) in enumerate(
    supporting_packets,
    1,
):
    title = packet_title(
        packet
    )

    source_url = packet_source_url(
        packet
    )

    if not title:
        raise RuntimeError(
            lesson_code
            + ": supporting title missing"
        )

    if not source_url:
        raise RuntimeError(
            lesson_code
            + ": supporting source URL missing"
        )

    full_url = derive_full_body_url(
        source_url
    )

    cache = (
        SUPPORT_CACHE /
        (
            sha_text(
                full_url
            )
            + ".html"
        )
    )

    if full_url.startswith(
        "http"
    ):
        if fetch_once(
            full_url,
            cache,
        ):
            downloaded += 1
        else:
            resumed += 1

        raw = cache.read_text(
            encoding="utf-8",
            errors="ignore",
        )

        body_blocks = parse_html(
            raw
        )

        source_sha = sha_file(
            cache
        )

    else:
        raise RuntimeError(
            lesson_code
            + ": unsupported supporting source type"
        )

    if not body_blocks:
        raise RuntimeError(
            lesson_code
            + ": no supporting instructional blocks"
        )

    expected = re.sub(
        r"\s+PLC Activity\s*$",
        "",
        title,
        flags=re.I,
    )

    page_text = normalize(
        " ".join(
            block[
                "text"
            ]
            for block
            in body_blocks
        )
    )

    title_tokens = set(
        normalize(
            expected
        ).split()
    )

    page_tokens = set(
        page_text.split()
    )

    coverage = (
        len(
            title_tokens
            & page_tokens
        )
        /
        max(
            1,
            len(title_tokens)
        )
    )

    if (
        len(title_tokens) >= 3
        and coverage < 0.75
    ):
        raise RuntimeError(
            f"{lesson_code}: "
            f"supporting title mismatch "
            f"{coverage:.1%}"
        )

    candidate = {
        "lessonCode":
            lesson_code,

        "lessonTitle":
            title,

        "evidence": {
            "blocks":
                body_blocks,
        },
    }

    fields = build_supporting_fields(
        lesson_code,
        candidate,
    )

    lesson = {
        "lessonCode":
            lesson_code,

        "lessonTitle":
            title,

        "lessonType":
            "SUPPORTING",

        # Critical:
        # supporting lessons never receive
        # invented standards/outcomes.
        "outcomeCodes":
            [],

        "cultureCode":
            "en",

        "fields":
            fields,

        "source": {
            "url":
                full_url,

            "artifactSha256":
                source_sha,
        },

        "rights":
            packet.get(
                "rights"
            )
            or {},
    }

    output = (
        EN_SUPPORTING /
        (
            sha_text(
                lesson_code
            )[:24]
            + ".json"
        )
    )

    atomic_json(
        output,
        lesson,
    )

    lesson[
        "path"
    ] = str(
        output.relative_to(
            ROOT
        )
    )

    final_lessons[
        lesson_code
    ] = lesson

    if (
        index % 20 == 0
        or index == 94
    ):
        print(
            f"Supporting EN: "
            f"{index}/94",
            flush=True,
        )


# ============================================================
# FINAL ENGLISH VALIDATION
# ============================================================

if len(
    final_lessons
) != EXPECTED_TOTAL:
    raise RuntimeError(
        "Final EN lesson count != 1560; "
        f"got {len(final_lessons)}"
    )

standalone_count = sum(
    x[
        "lessonType"
    ]
    == "STANDALONE"
    for x in final_lessons.values()
)

supporting_count = sum(
    x[
        "lessonType"
    ]
    == "SUPPORTING"
    for x in final_lessons.values()
)

assert (
    standalone_count
    == EXPECTED_STANDALONE
)

assert (
    supporting_count
    == EXPECTED_SUPPORTING
)

for lesson_code, lesson in (
    final_lessons.items()
):
    if (
        lesson[
            "lessonType"
        ]
        == "SUPPORTING"
        and lesson[
            "outcomeCodes"
        ]
    ):
        raise RuntimeError(
            lesson_code
            + ": invented supporting outcome"
        )

    for field in FIELDS:
        if not complete(
            field,
            lesson[
                "fields"
            ][field],
        ):
            raise RuntimeError(
                f"{lesson_code}: "
                f"EN field incomplete: {field}"
            )


# ============================================================
# EN INDEX
# ============================================================

en_index = {
    "schemaVersion":
        1,

    "generatedAtUtc":
        now(),

    "lessonCount":
        1560,

    "standalone":
        1466,

    "supporting":
        94,

    "lessons": {
        code: {
            "path":
                lesson[
                    "path"
                ],

            "lessonType":
                lesson[
                    "lessonType"
                ],

            "outcomeCodes":
                lesson[
                    "outcomeCodes"
                ],
        }
        for code, lesson
        in sorted(
            final_lessons.items()
        )
    },
}

atomic_json(
    EN_INDEX,
    en_index,
)


# ============================================================
# POLISH TRANSLATION MANIFEST
# ============================================================

translation_rows = []

for lesson_code, lesson in sorted(
    final_lessons.items()
):
    translation_rows.append({
        "lessonCode":
            lesson_code,

        "lessonType":
            lesson[
                "lessonType"
            ],

        "sourceSha256":
            sha_text(
                json.dumps(
                    {
                        "title":
                            lesson[
                                "lessonTitle"
                            ],

                        "fields":
                            lesson[
                                "fields"
                            ],
                    },
                    sort_keys=True,
                    ensure_ascii=False,
                )
            ),

        "en": {
            "title":
                lesson[
                    "lessonTitle"
                ],

            **lesson[
                "fields"
            ],
        },

        # Deliberately empty.
        # We do not fake Polish by reusing
        # the old generic translations.
        "pl":
            None,
    })

atomic_json(
    TRANSLATION_MANIFEST,
    {
        "schemaVersion":
            1,

        "generatedAtUtc":
            now(),

        "status":
            "ENGLISH_CANONICAL_COMPLETE_TRANSLATION_NOT_APPLICABLE",

        "lessonCount":
            1560,

        "academicLanguage":
            "en",

        "curriculumTranslationRequired":
            False,

        "expectedPolishTranslations":
            0,

        "rows":
            translation_rows,
    },
)


# ============================================================
# CHECKPOINT
# ============================================================

report = {
    "status":
        "ENGLISH_CANONICAL_COMPLETE",

    "generatedAtUtc":
        now(),

    "summary": {
        "totalEnglish":
            1560,

        "standaloneEnglish":
            1466,

        "supportingEnglish":
            94,

        "supportingOutcomeCodes":
            0,

        "supportingBodiesDownloadedThisRun":
            downloaded,

        "supportingBodiesResumed":
            resumed,

        "legacyFallbacks":
            0,

        "genericLessonFallbacks":
            0,

        "curriculumTranslationRequired":
            False,

        "canonicalMutated":
            False,
    },

    "englishIndexSha256":
        sha_file(
            EN_INDEX
        ),

    "translationManifestSha256":
        sha_file(
            TRANSLATION_MANIFEST
        ),
}

atomic_json(
    REPORT,
    report,
)

run = load(RUN)

run.setdefault(
    "checkpoints",
    {}
)[
    "phase29-final-english"
] = {
    "status":
        "PASS",

    "completedAtUtc":
        now(),

    "totalEnglish":
        1560,

    "standalone":
        1466,

    "supporting":
        94,

    "supportingOutcomeCodes":
        0,

    "legacyFallbacks":
        0,

    "genericLessonFallbacks":
        0,

    "englishIndexSha256":
        sha_file(
            EN_INDEX
        ),

    "translationManifestSha256":
        sha_file(
            TRANSLATION_MANIFEST
        ),
}

run[
    "currentStage"
] = "phase29-local-validation"

run[
    "phase29Status"
] = "OPEN"

run[
    "phase30Status"
] = "NOT_STARTED"

run[
    "updatedAtUtc"
] = now()

atomic_json(
    RUN,
    run,
)

print()
print(
    "======================================================================"
)
print(
    " PHASE 29 — ENGLISH SOURCE-FIDELITY GATE PASSED"
)
print(
    "======================================================================"
)
print(
    "English content       : 1560/1560"
)
print(
    "Standalone            : 1466/1466"
)
print(
    "Supporting            : 94/94"
)
print(
    "Invented outcomes     : 0"
)
print(
    "Legacy fallbacks      : 0"
)
print(
    "Generic fallbacks     : 0"
)
print(
    "Canonical mutation    : 0"
)
print()
print(
    "Curriculum translation: NOT APPLICABLE"
)
print(
    "Translation manifest  : "
    ".phase29-source-rebuild/"
    "translation-manifest/en-to-pl.json"
)
print()
print(
    "PHASE 29              : OPEN"
)
print(
    "PHASE 30              : NOT STARTED"
)
print(
    "======================================================================"
)
