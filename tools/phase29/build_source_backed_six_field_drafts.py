#!/usr/bin/env python3

from __future__ import annotations

import hashlib
import json
import re
from collections import Counter
from datetime import datetime, timezone
from pathlib import Path


ROOT = Path.cwd()
STATE = ROOT / ".phase29-source-rebuild"

SOURCE_INDEX = (
    STATE /
    "candidates/lesson-index.json"
)

RUN_PATH = (
    STATE /
    "run.json"
)

OUT_ROOT = (
    STATE /
    "six-field-drafts"
)

LESSON_DIR = (
    OUT_ROOT /
    "lessons"
)

INDEX_PATH = (
    OUT_ROOT /
    "lesson-index.json"
)

REPORT_PATH = (
    STATE /
    "reports/six-field-source-backed-drafts.json"
)

VERSION = (
    "phase29-six-field-source-backed-v1"
)

EXPECTED = 1466


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

    with path.open("rb") as f:
        for chunk in iter(
            lambda: f.read(
                1024 * 1024
            ),
            b"",
        ):
            h.update(chunk)

    return h.hexdigest()


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
    value = (
        value
        or ""
    ).strip()

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


def unique_strings(values):
    result = []
    seen = set()

    for value in values:
        value = clean(
            value
        )

        if not value:
            continue

        marker = re.sub(
            r"\s+",
            " ",
            value.lower(),
        )

        if marker in seen:
            continue

        seen.add(marker)
        result.append(
            value
        )

    return result


def sentence_split(text):
    text = clean(
        text
    )

    if not text:
        return []

    parts = re.split(
        r"(?<=[.!?])\s+|;\s+",
        text,
    )

    return [
        clean(x)
        for x in parts
        if len(
            clean(x)
        ) >= 15
    ]


def soft_limit(
    parts,
    max_chars,
):
    parts = unique_strings(
        parts
    )

    result = []
    total = 0

    for part in parts:
        extra = (
            len(part)
            + (
                2
                if result
                else 0
            )
        )

        if (
            result
            and total + extra > max_chars
        ):
            break

        result.append(
            part
        )

        total += extra

    return "\n\n".join(
        result
    ).strip()


def block_label(block):
    heading = clean(
        block.get(
            "heading",
            "",
        )
    )

    return (
        heading
        or block.get(
            "category",
            "SOURCE"
        )
    )


def blocks_by_category(
    blocks,
    categories,
):
    return [
        (
            index,
            block,
        )
        for index, block in enumerate(
            blocks
        )
        if block.get(
            "category"
        )
        in categories
    ]


def source_parts(
    selected,
    *,
    include_headings=False,
):
    values = []
    refs = []

    for index, block in selected:
        text = clean(
            block.get(
                "text",
                "",
            )
        )

        if not text:
            continue

        if include_headings:
            heading = block_label(
                block
            )

            values.append(
                f"{heading}\n{text}"
            )
        else:
            values.append(
                text
            )

        refs.append({
            "blockIndex":
                index,

            "category":
                block.get(
                    "category"
                ),

            "heading":
                clean(
                    block.get(
                        "heading",
                        "",
                    )
                ),
        })

    return (
        values,
        refs,
    )


def build_explanation(
    blocks,
):
    selected = (
        blocks_by_category(
            blocks,
            {
                "NARRATIVE",
            },
        )
    )

    if not selected:
        selected = (
            blocks_by_category(
                blocks,
                {
                    "LAUNCH",
                    "LESSON_SYNTHESIS",
                },
            )
        )

    if not selected:
        selected = [
            (0, blocks[0])
        ] if blocks else []

    parts, refs = source_parts(
        selected[:5]
    )

    return (
        soft_limit(
            parts,
            6000,
        ),
        refs[:5],
    )


def build_key_concepts(
    blocks,
):
    selected = (
        blocks_by_category(
            blocks,
            {
                "LESSON_SYNTHESIS",
                "ACTIVITY_SYNTHESIS",
                "NARRATIVE",
            },
        )
    )

    if not selected:
        return "", []

    parts, refs = source_parts(
        selected[:8]
    )

    return (
        soft_limit(
            parts,
            5500,
        ),
        refs[:8],
    )


def build_worked_examples(
    blocks,
):
    tasks = blocks_by_category(
        blocks,
        {
            "STUDENT_FACING",
            "ACTIVITY",
        },
    )

    syntheses = (
        blocks_by_category(
            blocks,
            {
                "ACTIVITY_SYNTHESIS",
            },
        )
    )

    if not tasks:
        return "", []

    result = []
    refs = []

    for number, (
        task_index,
        task,
    ) in enumerate(
        tasks[:3],
        1,
    ):
        task_text = clean(
            task.get(
                "text",
                "",
            )
        )

        if not task_text:
            continue

        heading = block_label(
            task
        )

        chunk = [
            f"Example {number}: {heading}",
            task_text,
        ]

        refs.append({
            "blockIndex":
                task_index,

            "category":
                task.get(
                    "category"
                ),

            "heading":
                clean(
                    task.get(
                        "heading",
                        "",
                    )
                ),
        })

        if (
            number - 1
            < len(
                syntheses
            )
        ):
            syn_index, syn = (
                syntheses[
                    number - 1
                ]
            )

            syn_text = clean(
                syn.get(
                    "text",
                    "",
                )
            )

            if syn_text:
                chunk.extend([
                    "Source reasoning / synthesis:",
                    syn_text,
                ])

                refs.append({
                    "blockIndex":
                        syn_index,

                    "category":
                        syn.get(
                            "category"
                        ),

                    "heading":
                        clean(
                            syn.get(
                                "heading",
                                "",
                            )
                        ),
                })

        result.append(
            "\n".join(
                chunk
            )
        )

    return (
        soft_limit(
            result,
            9000,
        ),
        refs,
    )


def build_steps(
    blocks,
):
    selected = (
        blocks_by_category(
            blocks,
            {
                "ACTIVITY_SYNTHESIS",
                "LESSON_SYNTHESIS",
            },
        )
    )

    sentences = []
    refs = []

    for index, block in selected:
        source_sentences = sentence_split(
            block.get(
                "text",
                "",
            )
        )

        if not source_sentences:
            continue

        for sentence in source_sentences:
            if sentence not in sentences:
                sentences.append(
                    sentence
                )

        refs.append({
            "blockIndex":
                index,

            "category":
                block.get(
                    "category"
                ),

            "heading":
                clean(
                    block.get(
                        "heading",
                        "",
                    )
                ),
        })

        if len(sentences) >= 12:
            break

    sentences = (
        sentences[:12]
    )

    if len(sentences) < 2:
        return "", refs

    rendered = "\n".join(
        f"Step {i}: {sentence}"
        for i, sentence in enumerate(
            sentences,
            1,
        )
    )

    return (
        rendered,
        refs,
    )


MISTAKE_PATTERN = re.compile(
    r"\b("
    r"misconception"
    r"|mistake"
    r"|error"
    r"|incorrect"
    r"|confus\w*"
    r"|struggl\w*"
    r"|difficult\w*"
    r"|may think"
    r"|might think"
    r"|not realize"
    r"|fail to"
    r"|forget"
    r")\b",
    flags=re.IGNORECASE,
)


def build_mistakes(
    blocks,
):
    direct = (
        blocks_by_category(
            blocks,
            {
                "MISCONCEPTIONS",
            },
        )
    )

    if direct:
        parts, refs = source_parts(
            direct[:6]
        )

        return (
            soft_limit(
                parts,
                5000,
            ),
            refs[:6],
        )

    # Source-backed fallback ONLY:
    # retain sentences explicitly describing
    # errors/difficulties/misconceptions.
    source_categories = {
        "NARRATIVE",
        "ACTIVITY",
        "ACTIVITY_SYNTHESIS",
        "LESSON_SYNTHESIS",
        "LAUNCH",
    }

    candidate_blocks = (
        blocks_by_category(
            blocks,
            source_categories,
        )
    )

    found = []
    refs = []

    for index, block in candidate_blocks:
        matched_here = False

        for sentence in sentence_split(
            block.get(
                "text",
                "",
            )
        ):
            if MISTAKE_PATTERN.search(
                sentence
            ):
                found.append(
                    sentence
                )

                matched_here = True

        if matched_here:
            refs.append({
                "blockIndex":
                    index,

                "category":
                    block.get(
                        "category"
                    ),

                "heading":
                    clean(
                        block.get(
                            "heading",
                            "",
                        )
                    ),
            })

        if len(found) >= 8:
            break

    return (
        soft_limit(
            found,
            4500,
        ),
        refs,
    )


def build_summary(
    blocks,
):
    selected = (
        blocks_by_category(
            blocks,
            {
                "LESSON_SYNTHESIS",
            },
        )
    )

    if not selected:
        selected = (
            blocks_by_category(
                blocks,
                {
                    "NARRATIVE",
                },
            )
        )

    if not selected:
        return "", []

    # Prefer the final synthesis/narrative.
    selected = selected[-2:]

    parts, refs = source_parts(
        selected
    )

    return (
        soft_limit(
            parts,
            2800,
        ),
        refs,
    )


def source_blob_fallback(
    blocks,
):
    if not blocks:
        return ""

    return clean(
        "\n\n".join(
            block.get(
                "text",
                "",
            )
            for block in blocks
            if clean(
                block.get(
                    "text",
                    "",
                )
            )
        )
    )


def specialized_source_draft(
    blocks,
):
    """
    PDF/LibreTexts/OpenStax exact-source materials do not
    share IM's heading taxonomy.

    Build only directly supportable source extracts.
    Do NOT invent missing six-field semantics.
    """

    source = source_blob_fallback(
        blocks
    )

    if not source:
        return {
            "explanation":
                "",

            "keyConceptsAndRules":
                "",

            "workedExamples":
                "",

            "stepByStepSolutions":
                "",

            "commonMistakes":
                "",

            "quickSummary":
                "",
        }

    paragraphs = [
        clean(x)
        for x in re.split(
            r"\n{2,}",
            source,
        )
        if len(
            clean(x)
        ) >= 30
    ]

    if len(paragraphs) <= 1:
        # Source may already have normalized whitespace.
        sentences = sentence_split(
            source
        )

        paragraphs = [
            " ".join(
                sentences[i:i + 4]
            )
            for i in range(
                0,
                len(sentences),
                4,
            )
        ]

    explanation = soft_limit(
        paragraphs[:5],
        5500,
    )

    concept_candidates = [
        p
        for p in paragraphs
        if re.search(
            r"("
            r"\bdefine\w*"
            r"|\bmeans\b"
            r"|\bbecause\b"
            r"|\btherefore\b"
            r"|\bproperty\b"
            r"|\bformula\b"
            r"|\bequation\b"
            r"|[=<>]"
            r")",
            p,
            flags=re.IGNORECASE,
        )
    ]

    key_concepts = soft_limit(
        concept_candidates[:6],
        5000,
    )

    example_candidates = [
        p
        for p in paragraphs
        if re.search(
            r"("
            r"\bexample\b"
            r"|\bproblem\b"
            r"|\btask\b"
            r"|\bcalculate\b"
            r"|\bfind\b"
            r"|\bdetermine\b"
            r"|[=?]"
            r")",
            p,
            flags=re.IGNORECASE,
        )
    ]

    worked = soft_limit(
        example_candidates[:5],
        7500,
    )

    process_candidates = [
        p
        for p in paragraphs
        if re.search(
            r"("
            r"\bfirst\b"
            r"|\bthen\b"
            r"|\bnext\b"
            r"|\bfinally\b"
            r"|\bsubstitute\b"
            r"|\bsolve\b"
            r"|\bcompute\b"
            r"|\bcalculate\b"
            r")",
            p,
            flags=re.IGNORECASE,
        )
    ]

    process_sentences = []

    for p in process_candidates:
        process_sentences.extend(
            sentence_split(
                p
            )
        )

    process_sentences = unique_strings(
        process_sentences
    )[:12]

    steps = ""

    if len(process_sentences) >= 2:
        steps = "\n".join(
            f"Step {i}: {sentence}"
            for i, sentence in enumerate(
                process_sentences,
                1,
            )
        )

    mistake_sentences = []

    for p in paragraphs:
        for sentence in sentence_split(
            p
        ):
            if MISTAKE_PATTERN.search(
                sentence
            ):
                mistake_sentences.append(
                    sentence
                )

    mistakes = soft_limit(
        mistake_sentences[:8],
        4500,
    )

    summary = soft_limit(
        paragraphs[-2:],
        2500,
    )

    return {
        "explanation":
            explanation,

        "keyConceptsAndRules":
            key_concepts,

        "workedExamples":
            worked,

        "stepByStepSolutions":
            steps,

        "commonMistakes":
            mistakes,

        "quickSummary":
            summary,
    }


FIELD_MINIMUMS = {
    "explanation":
        80,

    "keyConceptsAndRules":
        60,

    "workedExamples":
        120,

    "stepByStepSolutions":
        80,

    "commonMistakes":
        60,

    "quickSummary":
        60,
}


def missing_fields(fields):
    missing = []

    for field, minimum in (
        FIELD_MINIMUMS.items()
    ):
        if len(
            clean(
                fields.get(
                    field,
                    "",
                )
            )
        ) < minimum:
            missing.append(
                field
            )

    return missing


# ============================================================
# PREFLIGHT
# ============================================================

LESSON_DIR.mkdir(
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
        "source-faithful-candidate-evidence"
    )
)

if (
    not cp
    or cp.get(
        "status"
    )
    != "PASS"
    or cp.get(
        "candidateEvidenceReady"
    )
    != EXPECTED
):
    raise SystemExit(
        "FAIL: Step 05C is not a clean 1466/1466 PASS"
    )

source_index = load(
    SOURCE_INDEX
)

if (
    source_index.get(
        "lessonCount"
    )
    != EXPECTED
):
    raise SystemExit(
        "FAIL: Step 05C source index is not 1466"
    )


# ============================================================
# BUILD SIX-FIELD SOURCE-BACKED DRAFTS
# ============================================================

output_index = {}

status_counts = Counter()
missing_counts = Counter()
strategy_counts = Counter()
rights_counts = Counter()

written = 0
resumed = 0
processing_failures = []


for number, (
    lesson_code,
    index_item,
) in enumerate(
    source_index[
        "lessons"
    ].items(),
    1,
):
    candidate_path = Path(
        index_item[
            "candidatePath"
        ]
    )

    candidate = load(
        candidate_path
    )

    blocks = (
        candidate
        .get(
            "evidence",
            {}
        )
        .get(
            "blocks",
            []
        )
    )

    strategy = candidate.get(
        "strategy"
    )

    strategy_counts[
        strategy
    ] += 1

    rights = candidate.get(
        "rights",
        {}
    )

    rights_counts[
        rights.get(
            "contentUseMode",
            "UNKNOWN",
        )
    ] += 1

    if not blocks:
        processing_failures.append({
            "lessonCode":
                lesson_code,

            "reason":
                "NO_EVIDENCE_BLOCKS",
        })

        continue

    field_evidence = {}

    if (
        strategy
        == "HTML_PREPARATION_TO_FULL_LESSON"
    ):
        explanation, refs = (
            build_explanation(
                blocks
            )
        )

        field_evidence[
            "explanation"
        ] = refs

        concepts, refs = (
            build_key_concepts(
                blocks
            )
        )

        field_evidence[
            "keyConceptsAndRules"
        ] = refs

        worked, refs = (
            build_worked_examples(
                blocks
            )
        )

        field_evidence[
            "workedExamples"
        ] = refs

        steps, refs = (
            build_steps(
                blocks
            )
        )

        field_evidence[
            "stepByStepSolutions"
        ] = refs

        mistakes, refs = (
            build_mistakes(
                blocks
            )
        )

        field_evidence[
            "commonMistakes"
        ] = refs

        summary, refs = (
            build_summary(
                blocks
            )
        )

        field_evidence[
            "quickSummary"
        ] = refs

        fields = {
            "explanation":
                explanation,

            "keyConceptsAndRules":
                concepts,

            "workedExamples":
                worked,

            "stepByStepSolutions":
                steps,

            "commonMistakes":
                mistakes,

            "quickSummary":
                summary,
        }

    else:
        fields = (
            specialized_source_draft(
                blocks
            )
        )

        # Single exact source block backs all
        # specialized-source field candidates.
        field_evidence = {
            field: [{
                "blockIndex":
                    0,

                "category":
                    blocks[0].get(
                        "category"
                    ),

                "heading":
                    clean(
                        blocks[0].get(
                            "heading",
                            "",
                        )
                    ),
            }]
            if clean(
                value
            )
            else []
            for field, value in (
                fields.items()
            )
        }

    gaps = missing_fields(
        fields
    )

    for field in gaps:
        missing_counts[
            field
        ] += 1

    if gaps:
        state = (
            "TARGETED_AUTHORING_REQUIRED"
        )
    else:
        state = (
            "SOURCE_BACKED_COMPLETE"
        )

    status_counts[
        state
    ] += 1

    input_hash = canonical_hash({
        "version":
            VERSION,

        "lessonCode":
            lesson_code,

        "candidateInputHash":
            candidate.get(
                "candidateInputHash"
            ),

        "sourceTextSha256":
            candidate.get(
                "source",
                {},
            ).get(
                "textSha256"
            ),

        "strategy":
            strategy,

        "rightsMode":
            rights.get(
                "contentUseMode"
            ),
    })

    draft = {
        "schemaVersion":
            1,

        "builderVersion":
            VERSION,

        "lessonCode":
            lesson_code,

        "lessonTitle":
            candidate.get(
                "lessonTitle"
            ),

        "lessonType":
            candidate.get(
                "lessonType"
            ),

        "unitNumber":
            candidate.get(
                "unitNumber"
            ),

        "unitTitle":
            candidate.get(
                "unitTitle"
            ),

        "lessonNumber":
            candidate.get(
                "lessonNumber"
            ),

        "outcomeCodes":
            candidate.get(
                "outcomeCodes"
            )
            or [],

        "strategy":
            strategy,

        "source":
            candidate.get(
                "source"
            )
            or {},

        "rights":
            rights,

        "cultureCode":
            "en",

        "fields":
            fields,

        "fieldEvidence":
            field_evidence,

        "missingFields":
            gaps,

        "draftState":
            state,

        "sourceBacked":
            True,

        "usesLegacyCanonicalBody":
            False,

        "usesGenericFallback":
            False,

        "requiresTargetedAuthoring":
            bool(
                gaps
            ),

        "draftInputHash":
            input_hash,
    }

    filename = (
        sha_text(
            lesson_code
        )[:24]
        + ".json"
    )

    out_path = (
        LESSON_DIR /
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
                    "draftInputHash"
                )
                == input_hash
            ):
                packet_resume = True

        except Exception:
            packet_resume = False

    if packet_resume:
        resumed += 1

    else:
        draft[
            "createdAtUtc"
        ] = now()

        atomic_json(
            out_path,
            draft,
        )

        written += 1

    output_index[
        lesson_code
    ] = {
        "draftPath":
            str(
                out_path.relative_to(
                    ROOT
                )
            ),

        "draftInputHash":
            input_hash,

        "draftState":
            state,

        "missingFields":
            gaps,
    }

    if (
        number % 100 == 0
        or number == EXPECTED
    ):
        print(
            f"Progress: {number}/{EXPECTED} "
            f"| complete="
            f"{status_counts['SOURCE_BACKED_COMPLETE']} "
            f"| targeted="
            f"{status_counts['TARGETED_AUTHORING_REQUIRED']} "
            f"| failures="
            f"{len(processing_failures)}",
            flush=True,
        )


# ============================================================
# PROCESSING SAFETY GATE
# ============================================================

if processing_failures:
    report = {
        "schemaVersion":
            1,

        "generatedAtUtc":
            now(),

        "status":
            "INCOMPLETE",

        "processingFailures":
            processing_failures,
    }

    atomic_json(
        REPORT_PATH,
        report,
    )

    for item in (
        processing_failures[:50]
    ):
        print(
            "FAIL",
            item[
                "lessonCode"
            ],
            item[
                "reason"
            ],
        )

    raise SystemExit(
        "FAIL: six-field draft processing failures"
    )


if len(
    output_index
) != EXPECTED:
    raise SystemExit(
        "FAIL: expected 1466 classified drafts"
    )


# ============================================================
# PERSIST INDEX + REPORT
# ============================================================

index_document = {
    "schemaVersion":
        1,

    "generatedAtUtc":
        now(),

    "lessonCount":
        EXPECTED,

    "lessons":
        output_index,
}

atomic_json(
    INDEX_PATH,
    index_document,
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
        "draftsClassified":
            EXPECTED,

        "sourceBackedComplete":
            status_counts[
                "SOURCE_BACKED_COMPLETE"
            ],

        "targetedAuthoringRequired":
            status_counts[
                "TARGETED_AUTHORING_REQUIRED"
            ],

        "processingFailures":
            0,

        "writtenThisRun":
            written,

        "resumed":
            resumed,

        "missingFieldCounts":
            dict(
                sorted(
                    missing_counts.items()
                )
            ),

        "strategies":
            dict(
                sorted(
                    strategy_counts.items()
                )
            ),

        "rightsModes":
            dict(
                sorted(
                    rights_counts.items()
                )
            ),
    },

    "draftIndexSha256":
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
    "six-field-source-backed-drafts"
] = {
    "status":
        "PASS",

    "completedAtUtc":
        now(),

    "draftsClassified":
        EXPECTED,

    "sourceBackedComplete":
        status_counts[
            "SOURCE_BACKED_COMPLETE"
        ],

    "targetedAuthoringRequired":
        status_counts[
            "TARGETED_AUTHORING_REQUIRED"
        ],

    "processingFailures":
        0,

    "draftIndexSha256":
        index_sha,

    "reportSha256":
        sha_file(
            REPORT_PATH
        ),
}

run[
    "currentStage"
] = (
    "six-field-source-backed-drafts"
)

run[
    "updatedAtUtc"
] = now()

atomic_json(
    RUN_PATH,
    run,
)


# ============================================================
# OUTPUT
# ============================================================

print()
print(
    "=============================================================="
)
print(
    " PHASE 29 — STEP 05D RESULT"
)
print(
    "=============================================================="
)
print(
    "Drafts classified               :",
    EXPECTED,
)
print(
    "Source-backed complete          :",
    status_counts[
        "SOURCE_BACKED_COMPLETE"
    ],
)
print(
    "Targeted authoring required     :",
    status_counts[
        "TARGETED_AUTHORING_REQUIRED"
    ],
)
print(
    "Processing failures             : 0"
)
print()
print(
    "Missing field counts:"
)

for field in FIELD_MINIMUMS:
    print(
        f"  {field:25}",
        missing_counts.get(
            field,
            0,
        ),
    )

print()
print(
    "Rights modes:",
    dict(
        sorted(
            rights_counts.items()
        )
    ),
)
print()
print(
    "PASS: all 1466 lessons classified"
)
print(
    "PASS: no legacy body used as fallback"
)
print(
    "PASS: no generic content fabricated"
)
print(
    "PASS: source evidence refs retained per field"
)
print(
    "PASS: Step 05D checkpoint persisted"
)
