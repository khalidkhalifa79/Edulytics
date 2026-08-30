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

MAP_PATH = (
    STATE /
    "exact-lesson-source-map.json"
)

RUN_PATH = (
    STATE /
    "run.json"
)

IMPORT_ROOT = (
    STATE /
    "importer"
)

TEXT_DIR = (
    IMPORT_ROOT /
    "source-text"
)

META_DIR = (
    IMPORT_ROOT /
    "source-meta"
)

LESSON_DIR = (
    IMPORT_ROOT /
    "lessons"
)

INDEX_PATH = (
    IMPORT_ROOT /
    "lesson-index.json"
)

REPORT_PATH = (
    STATE /
    "reports/source-importer.json"
)

EXTRACTOR_VERSION = "phase29-source-importer-v1"

# PHASE29_SOURCE_RIGHTS_FALLBACK_V2
SOURCE_LICENSE_REPORT_PATH = (
    STATE /
    "reports/source-license-acquisition.json"
)

RIGHTS_METADATA_VERSION = (
    "phase29-source-rights-fallback-v2"
)

EXPECTED_TOTAL = 1560
EXPECTED_STANDALONE = 1466
EXPECTED_SUPPORTING = 94


def now():
    return datetime.now(
        timezone.utc
    ).isoformat()


def load(path: Path):
    return json.loads(
        path.read_text(
            encoding="utf-8"
        )
    )


def atomic_write_json(
    path: Path,
    value,
):
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


def atomic_write_text(
    path: Path,
    value: str,
):
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


def sha256_file(
    path: Path,
):
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


def sha256_text(
    value: str,
):
    return hashlib.sha256(
        value.encode("utf-8")
    ).hexdigest()


def canonical_hash(
    value,
):
    payload = json.dumps(
        value,
        sort_keys=True,
        ensure_ascii=False,
        separators=(",", ":"),
    )

    return sha256_text(
        payload
    )


def normalize(
    value: str,
):
    value = html.unescape(
        value or ""
    )

    value = (
        value
        .replace("’", "'")
        .replace("‘", "'")
        .replace("–", "-")
        .replace("—", "-")
        .replace("\u00a0", " ")
        .replace("\u200b", "")
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


def normalized_search(
    value: str,
):
    return re.sub(
        r"\s+",
        " ",
        normalize(value).lower(),
    ).strip()


class VisibleHtmlTextParser(
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
        if self.skip_depth:
            return

        if data.strip():
            self.parts.append(
                data
            )

    def text(self):
        return normalize(
            " ".join(
                self.parts
            )
        )


def detect_kind(
    path: Path,
):
    head = path.read_bytes()[
        :4096
    ]

    if head.startswith(
        b"%PDF-"
    ):
        return "PDF"

    lowered = head.lower()

    if (
        b"<html" in lowered
        or b"<!doctype html" in lowered
        or b"<body" in lowered
        or b"<article" in lowered
    ):
        return "HTML"

    return "TEXT"


def extract_pdf(
    path: Path,
):
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

    return normalize(
        result.stdout
    )


def extract_html(
    path: Path,
):
    raw = path.read_text(
        encoding="utf-8",
        errors="ignore",
    )

    parser = (
        VisibleHtmlTextParser()
    )

    parser.feed(
        raw
    )

    parser.close()

    return parser.text()


def extract_plain(
    path: Path,
):
    return normalize(
        path.read_text(
            encoding="utf-8",
            errors="ignore",
        )
    )


def extract_source(
    path: Path,
    kind: str,
):
    if kind == "PDF":
        return extract_pdf(
            path
        )

    if kind == "HTML":
        return extract_html(
            path
        )

    return extract_plain(
        path
    )


def locator_variants(
    section: str,
):
    base = normalized_search(
        section
    )

    variants = {
        base
    }

    if "cavalieri" in base:
        variants.add(
            base.replace(
                "cavalieri",
                "cavelieri",
            )
        )

    if (
        "amazing inverse trig "
        "function race"
        in base
    ):
        variants.update({
            normalized_search(
                "The Amazing Inverse Trig Function Race"
            ),
            normalized_search(
                "Amazing Inverse Trig Function Race"
            ),
            normalized_search(
                "The Amazing Trig Function Race"
            ),
        })

    return variants


def normalize_source_url(
    value: str | None,
):
    return (
        value or ""
    ).strip().rstrip("/")


def first_nonempty(
    *values,
):
    for value in values:
        if (
            value is not None
            and str(value).strip()
        ):
            return value

    return None


def first_defined(
    *values,
):
    for value in values:
        if value is not None:
            return value

    return None


def license_traits(
    license_value,
):
    value = (
        str(
            license_value or ""
        )
        .upper()
        .replace("–", "-")
        .replace("—", "-")
    )

    if (
        "CC BY-NC-SA"
        in value
    ):
        return {
            "commercialUseAllowed":
                False,

            "translationAllowed":
                True,

            "shareAlikeRequired":
                True,

            "requiresAttribution":
                True,
        }

    if (
        "CC BY-NC"
        in value
    ):
        return {
            "commercialUseAllowed":
                False,

            "translationAllowed":
                True,

            "shareAlikeRequired":
                False,

            "requiresAttribution":
                True,
        }

    if (
        "CC BY-SA"
        in value
    ):
        return {
            "commercialUseAllowed":
                True,

            "translationAllowed":
                True,

            "shareAlikeRequired":
                True,

            "requiresAttribution":
                True,
        }

    if (
        "CC BY"
        in value
    ):
        return {
            "commercialUseAllowed":
                True,

            "translationAllowed":
                True,

            "shareAlikeRequired":
                False,

            "requiresAttribution":
                True,
        }

    if (
        "CC0"
        in value
        or "PUBLIC DOMAIN"
        in value
    ):
        return {
            "commercialUseAllowed":
                True,

            "translationAllowed":
                True,

            "shareAlikeRequired":
                False,

            "requiresAttribution":
                False,
        }

    return {
        "commercialUseAllowed":
            None,

        "translationAllowed":
            None,

        "shareAlikeRequired":
            None,

        "requiresAttribution":
            None,
    }


def resolved_license(
    row,
    source_state,
):
    state = (
        source_state
        or {}
    )

    return first_nonempty(
        row.get(
            "licenseStatus"
        ),
        row.get(
            "declaredSourceLicense"
        ),
        state.get(
            "license"
        ),
    )


def content_use_mode(
    row,
    source_state=None,
):
    explicit = row.get(
        "usageMode"
    )

    if explicit:
        return explicit

    license_text = str(
        resolved_license(
            row,
            source_state,
        )
        or ""
    ).upper()

    if "CC BY-NC-SA" in license_text:
        return (
            "NONCOMMERCIAL_"
            "SHAREALIKE_IMPORT"
        )

    if "CC BY-NC" in license_text:
        return (
            "NONCOMMERCIAL_IMPORT"
        )

    if (
        "CC BY" in license_text
        or "CC0" in license_text
        or "PUBLIC DOMAIN"
        in license_text
    ):
        return (
            "OPEN_LICENSE_IMPORT"
        )

    effective = first_nonempty(
        row.get(
            "effectiveClassification"
        ),
        (
            source_state
            or {}
        ).get(
            "effectiveClassification"
        ),
        (
            source_state
            or {}
        ).get(
            "classification"
        ),
    )

    if (
        effective
        == "USER_APPROVED_OVERRIDE"
    ):
        return (
            "OWNER_OVERRIDE_REVIEW"
        )

    return "REFERENCE_ONLY"


def verbatim_allowed(
    mode: str,
):
    return mode in {
        "OPEN_LICENSE_IMPORT",
        "NONCOMMERCIAL_IMPORT",
        "NONCOMMERCIAL_SHAREALIKE_IMPORT",
    }


# ------------------------------------------------------------
# Preflight
# ------------------------------------------------------------

TEXT_DIR.mkdir(
    parents=True,
    exist_ok=True,
)

META_DIR.mkdir(
    parents=True,
    exist_ok=True,
)

LESSON_DIR.mkdir(
    parents=True,
    exist_ok=True,
)

mapping = load(
    MAP_PATH
)

run = load(
    RUN_PATH
)

source_license_report = load(
    SOURCE_LICENSE_REPORT_PATH
)

source_rights_by_url = {
    normalize_source_url(
        item.get("url")
    ): item
    for item in source_license_report.get(
        "sources",
        []
    )
    if item.get("url")
}

step03 = (
    run.get(
        "checkpoints",
        {}
    )
    .get(
        "exact-lesson-source-map"
    )
)

if not step03:
    raise SystemExit(
        "FAIL: Step 03 checkpoint missing"
    )

if (
    step03.get("status")
    != "PASS"
):
    raise SystemExit(
        "FAIL: Step 03 is not PASS"
    )

if (
    step03.get(
        "exactMappings"
    )
    != EXPECTED_TOTAL
):
    raise SystemExit(
        "FAIL: Step 03 does not contain "
        "1560 exact mappings"
    )

current_map_sha = (
    sha256_file(
        MAP_PATH
    )
)

if (
    step03.get(
        "mapSha256"
    )
    != current_map_sha
):
    raise SystemExit(
        "FAIL: exact map SHA differs "
        "from Step 03 checkpoint"
    )

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
    != EXPECTED_TOTAL
    or summary.get("needsLocator")
    != 0
    or summary.get("unresolved")
    != 0
    or summary.get("blocked")
    != 0
):
    raise SystemExit(
        "FAIL: exact map baseline "
        "is not Step 03 final PASS"
    )

lessons = mapping[
    "lessons"
]

if len(lessons) != 1560:
    raise SystemExit(
        "FAIL: exact map does not "
        "contain 1560 lesson rows"
    )

if any(
    row.get(
        "mappingStatus"
    )
    != "EXACT"
    for row in lessons
):
    raise SystemExit(
        "FAIL: non-EXACT lesson "
        "found in final map"
    )

if any(
    row.get(
        "importAllowed"
    )
    is not True
    for row in lessons
):
    raise SystemExit(
        "FAIL: importer received "
        "non-importable lesson"
    )


# ------------------------------------------------------------
# Artifact importer
# ------------------------------------------------------------

artifact_cache = {}

artifact_imported = 0
artifact_resumed = 0

for number, row in enumerate(
    lessons,
    1,
):
    artifact_value = row.get(
        "artifactPath"
    )

    expected_sha = row.get(
        "artifactSha256"
    )

    if not artifact_value:
        raise SystemExit(
            "FAIL: artifactPath missing: "
            + row["lessonCode"]
        )

    if not expected_sha:
        raise SystemExit(
            "FAIL: artifactSha256 missing: "
            + row["lessonCode"]
        )

    artifact = Path(
        artifact_value
    )

    if not artifact.is_absolute():
        artifact = (
            ROOT /
            artifact
        )

    if not artifact.exists():
        raise SystemExit(
            "FAIL: artifact missing: "
            + str(artifact)
        )

    artifact_key = str(
        artifact.resolve()
    )

    if artifact_key in artifact_cache:
        continue

    actual_sha = sha256_file(
        artifact
    )

    if actual_sha != expected_sha:
        raise SystemExit(
            "FAIL: source artifact SHA mismatch: "
            + str(artifact)
        )

    kind = detect_kind(
        artifact
    )

    text_path = (
        TEXT_DIR /
        f"{actual_sha}.txt"
    )

    meta_path = (
        META_DIR /
        f"{actual_sha}.json"
    )

    input_hash = canonical_hash({
        "extractorVersion":
            EXTRACTOR_VERSION,

        "artifactSha256":
            actual_sha,

        "artifactKind":
            kind,
    })

    resume = False

    if (
        text_path.exists()
        and meta_path.exists()
    ):
        try:
            old_meta = load(
                meta_path
            )

            if (
                old_meta.get(
                    "inputHash"
                )
                == input_hash
                and old_meta.get(
                    "sourceTextSha256"
                )
                == sha256_file(
                    text_path
                )
            ):
                resume = True

        except Exception:
            resume = False

    if resume:
        source_text = (
            text_path.read_text(
                encoding="utf-8"
            )
        )

        artifact_resumed += 1

    else:
        source_text = extract_source(
            artifact,
            kind,
        )

        if (
            len(
                normalized_search(
                    source_text
                )
            )
            < 100
        ):
            raise SystemExit(
                "FAIL: extracted source "
                "text is unexpectedly short: "
                + str(artifact)
            )

        atomic_write_text(
            text_path,
            source_text + "\n",
        )

        text_sha = sha256_file(
            text_path
        )

        meta = {
            "schemaVersion":
                1,

            "extractorVersion":
                EXTRACTOR_VERSION,

            "inputHash":
                input_hash,

            "sourceArtifactPath":
                str(
                    artifact.relative_to(
                        ROOT
                    )
                ),

            "sourceArtifactSha256":
                actual_sha,

            "artifactKind":
                kind,

            "sourceTextPath":
                str(
                    text_path.relative_to(
                        ROOT
                    )
                ),

            "sourceTextSha256":
                text_sha,

            "characterCount":
                len(source_text),

            "importedAtUtc":
                now(),
        }

        atomic_write_json(
            meta_path,
            meta,
        )

        artifact_imported += 1

    artifact_cache[
        artifact_key
    ] = {
        "artifact":
            artifact,

        "artifactSha256":
            actual_sha,

        "kind":
            kind,

        "text":
            source_text,

        "textPath":
            text_path,

        "textSha256":
            sha256_file(
                text_path
            ),
    }


# ------------------------------------------------------------
# Per-lesson importer packets
# ------------------------------------------------------------

lesson_index = {}
lesson_imported = 0
lesson_resumed = 0

type_counts = Counter()
use_mode_counts = Counter()
kind_counts = Counter()

locator_verified = 0
locator_not_required = 0

rights_fallback_count = 0
source_rights_match_count = 0

for number, row in enumerate(
    lessons,
    1,
):
    code = row[
        "lessonCode"
    ]

    artifact = Path(
        row[
            "artifactPath"
        ]
    )

    if not artifact.is_absolute():
        artifact = (
            ROOT /
            artifact
        )

    cached = artifact_cache[
        str(
            artifact.resolve()
        )
    ]

    source_text = cached[
        "text"
    ]

    searchable = (
        normalized_search(
            source_text
        )
    )

    locator = (
        row.get(
            "sourceLocator"
        )
        or {}
    )

    section = locator.get(
        "section"
    )

    section_verified = False

    if section:
        variants = (
            locator_variants(
                section
            )
        )

        section_verified = any(
            variant in searchable
            for variant in variants
        )

        if not section_verified:
            raise SystemExit(
                "FAIL: source locator no "
                "longer matches extracted text: "
                + code
            )

        locator_verified += 1

    else:
        locator_not_required += 1

    source_url_key = normalize_source_url(
        row.get(
            "sourceUrl"
        )
    )

    source_state = (
        source_rights_by_url.get(
            source_url_key
        )
    )

    if source_state is not None:
        source_rights_match_count += 1

    license_value = resolved_license(
        row,
        source_state,
    )

    traits = license_traits(
        license_value
    )

    rights_fallback_used = bool(
        source_state
        and not first_nonempty(
            row.get(
                "licenseStatus"
            ),
            row.get(
                "declaredSourceLicense"
            ),
        )
        and first_nonempty(
            source_state.get(
                "license"
            )
        )
    )

    if rights_fallback_used:
        rights_fallback_count += 1

    machine_classification = first_nonempty(
        row.get(
            "machineClassification"
        ),
        (
            source_state
            or {}
        ).get(
            "classification"
        ),
    )

    effective_classification = first_nonempty(
        row.get(
            "effectiveClassification"
        ),
        (
            source_state
            or {}
        ).get(
            "effectiveClassification"
        ),
        (
            source_state
            or {}
        ).get(
            "classification"
        ),
    )

    commercial_use_allowed = first_defined(
        row.get(
            "commercialUseAllowed"
        ),
        (
            source_state
            or {}
        ).get(
            "commercialReuse"
        ),
        traits[
            "commercialUseAllowed"
        ],
    )

    translation_allowed = first_defined(
        row.get(
            "translationAllowed"
        ),
        traits[
            "translationAllowed"
        ],
    )

    share_alike_required = first_defined(
        row.get(
            "shareAlikeRequired"
        ),
        traits[
            "shareAlikeRequired"
        ],
    )

    requires_attribution = first_defined(
        row.get(
            "requiresAttribution"
        ),
        (
            source_state
            or {}
        ).get(
            "requiresAttribution"
        ),
        traits[
            "requiresAttribution"
        ],
    )

    license_evidence = first_nonempty(
        row.get(
            "licenseEvidence"
        ),
        (
            source_state
            or {}
        ).get(
            "licenseEvidence"
        ),
    )

    license_evidence_rule = first_nonempty(
        row.get(
            "licenseEvidenceRule"
        ),
        (
            source_state
            or {}
        ).get(
            "licenseEvidenceRule"
        ),
    )

    use_mode = content_use_mode(
        row,
        source_state,
    )

    lesson_type = row.get(
        "lessonType"
    )

    type_counts[
        lesson_type
    ] += 1

    use_mode_counts[
        use_mode
    ] += 1

    kind_counts[
        cached["kind"]
    ] += 1

    packet = {
        "schemaVersion":
            1,

        "lessonCode":
            code,

        "sourceLessonCode":
            row.get(
                "sourceLessonCode"
            ),

        "lessonType":
            lesson_type,

        "unitNumber":
            row.get(
                "unitNumber"
            ),

        "unitTitle":
            row.get(
                "unitTitle"
            ),

        "lessonNumber":
            row.get(
                "lessonNumber"
            ),

        "lessonTitle":
            row.get(
                "lessonTitle"
            ),

        "outcomeCodes":
            row.get(
                "outcomeCodes"
            )
            or [],

        "blueprintOutcomeCodes":
            row.get(
                "blueprintOutcomeCodes"
            )
            or [],

        "source": {
            "sourceUrl":
                row.get(
                    "sourceUrl"
                ),

            "supersededSourceUrl":
                row.get(
                    "supersededSourceUrl"
                ),

            "sourceTitle":
                row.get(
                    "sourceTitle"
                ),

            "sourcePublisher":
                row.get(
                    "sourcePublisher"
                ),

            "sourceEdition":
                row.get(
                    "sourceEdition"
                ),

            "sourceLocator":
                locator,

            "locatorVerified":
                (
                    section_verified
                    if section
                    else None
                ),

            "retrievalChannel":
                row.get(
                    "retrievalChannel"
                ),

            "retrievalUrl":
                row.get(
                    "retrievalUrl"
                ),

            "retrievalTimestamp":
                row.get(
                    "retrievalTimestamp"
                ),
        },

        "rights": {
            "declaredSourceLicense":
                license_value,

            "licenseStatus":
                license_value,

            "machineClassification":
                machine_classification,

            "effectiveClassification":
                effective_classification,

            "contentUseMode":
                use_mode,

            "verbatimImportAllowed":
                verbatim_allowed(
                    use_mode
                ),

            "commercialUseAllowed":
                commercial_use_allowed,

            "translationAllowed":
                translation_allowed,

            "shareAlikeRequired":
                share_alike_required,

            "requiresAttribution":
                requires_attribution,

            "licenseEvidence":
                license_evidence,

            "licenseEvidenceRule":
                license_evidence_rule,

            "rightsMetadataFallback":
                rights_fallback_used,

            "rightsMetadataVersion":
                (
                    RIGHTS_METADATA_VERSION
                    if rights_fallback_used
                    else None
                ),

            "mappingAllowed":
                row.get(
                    "mappingAllowed"
                ),

            "importAllowed":
                row.get(
                    "importAllowed"
                ),
        },

        "importedSource": {
            "sourceArtifactPath":
                str(
                    artifact.relative_to(
                        ROOT
                    )
                ),

            "sourceArtifactSha256":
                cached[
                    "artifactSha256"
                ],

            "artifactKind":
                cached[
                    "kind"
                ],

            "sourceTextPath":
                str(
                    cached[
                        "textPath"
                    ].relative_to(
                        ROOT
                    )
                ),

            "sourceTextSha256":
                cached[
                    "textSha256"
                ],

            "sourceTextCharacterCount":
                len(
                    source_text
                ),
        },
    }

    input_material = {
        "step03MapSha256":
            current_map_sha,

        "lessonCode":
            code,

        "sourceArtifactSha256":
            cached[
                "artifactSha256"
            ],

        "sourceTextSha256":
            cached[
                "textSha256"
            ],

        "sourceLocator":
            locator,

        "contentUseMode":
            use_mode,

        "outcomeCodes":
            packet[
                "outcomeCodes"
            ],
    }

    # Preserve already-successful packets.
    # Only rows actually using Step-02 metadata fallback
    # receive a dependency-hash extension.
    if rights_fallback_used:
        input_material[
            "sourceRightsFallback"
        ] = {
            "metadataVersion":
                RIGHTS_METADATA_VERSION,

            "license":
                license_value,

            "machineClassification":
                machine_classification,

            "effectiveClassification":
                effective_classification,

            "commercialUseAllowed":
                commercial_use_allowed,

            "translationAllowed":
                translation_allowed,

            "shareAlikeRequired":
                share_alike_required,

            "requiresAttribution":
                requires_attribution,

            "licenseEvidence":
                license_evidence,

            "licenseEvidenceRule":
                license_evidence_rule,
        }

    input_hash = canonical_hash(
        input_material
    )

    packet[
        "importInputHash"
    ] = input_hash

    packet_name = (
        sha256_text(
            code
        )[:24]
        + ".json"
    )

    packet_path = (
        LESSON_DIR /
        packet_name
    )

    resume = False

    if packet_path.exists():
        try:
            old = load(
                packet_path
            )

            if (
                old.get(
                    "importInputHash"
                )
                == input_hash
            ):
                resume = True

        except Exception:
            resume = False

    if resume:
        lesson_resumed += 1
    else:
        packet[
            "importedAtUtc"
        ] = now()

        atomic_write_json(
            packet_path,
            packet,
        )

        lesson_imported += 1

    lesson_index[
        code
    ] = {
        "packetPath":
            str(
                packet_path.relative_to(
                    ROOT
                )
            ),

        "importInputHash":
            input_hash,

        "sourceTextSha256":
            cached[
                "textSha256"
            ],
    }


# ------------------------------------------------------------
# Exact final validation
# ------------------------------------------------------------

if len(
    lesson_index
) != EXPECTED_TOTAL:
    raise SystemExit(
        "FAIL: importer did not create "
        "1560 lesson packets"
    )

if (
    type_counts[
        "STANDALONE"
    ]
    != EXPECTED_STANDALONE
):
    raise SystemExit(
        "FAIL: importer standalone "
        "count is not 1466"
    )

if (
    type_counts[
        "SUPPORTING"
    ]
    != EXPECTED_SUPPORTING
):
    raise SystemExit(
        "FAIL: importer supporting "
        "count is not 94"
    )


# Rights propagation acceptance gate.
if rights_fallback_count != 382:
    raise SystemExit(
        "FAIL: expected exactly 382 lessons to "
        "inherit Step-02 rights metadata, got "
        + str(rights_fallback_count)
    )

if use_mode_counts.get(
    "REFERENCE_ONLY",
    0,
) != 0:
    raise SystemExit(
        "FAIL: REFERENCE_ONLY remains after "
        "verified Step-02 rights propagation"
    )

if use_mode_counts.get(
    "OPEN_LICENSE_IMPORT",
    0,
) != 1559:
    raise SystemExit(
        "FAIL: expected OPEN_LICENSE_IMPORT = 1559, got "
        + str(
            use_mode_counts.get(
                "OPEN_LICENSE_IMPORT",
                0,
            )
        )
    )

if use_mode_counts.get(
    "NONCOMMERCIAL_SHAREALIKE_IMPORT",
    0,
) != 1:
    raise SystemExit(
        "FAIL: expected NONCOMMERCIAL_SHAREALIKE_IMPORT = 1"
    )


# Supporting lessons must not acquire
# invented official OutcomeCodes.
for row in lessons:
    if (
        row.get(
            "lessonType"
        )
        == "SUPPORTING"
        and (
            row.get(
                "outcomeCodes"
            )
            or []
        )
    ):
        raise SystemExit(
            "FAIL: Supporting lesson "
            "contains invented/accepted "
            "OutcomeCodes: "
            + row[
                "lessonCode"
            ]
        )


index_document = {
    "schemaVersion":
        1,

    "generatedAtUtc":
        now(),

    "sourceMapSha256":
        current_map_sha,

    "lessonCount":
        len(
            lesson_index
        ),

    "lessons":
        lesson_index,
}

atomic_write_json(
    INDEX_PATH,
    index_document,
)

index_sha = sha256_file(
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
        "totalLessons":
            1560,

        "standalone":
            1466,

        "supporting":
            94,

        "uniqueSourceArtifacts":
            len(
                artifact_cache
            ),

        "artifactsExtractedThisRun":
            artifact_imported,

        "artifactsResumed":
            artifact_resumed,

        "lessonPacketsWrittenThisRun":
            lesson_imported,

        "lessonPacketsResumed":
            lesson_resumed,

        "locatorVerified":
            locator_verified,

        "locatorNotRequired":
            locator_not_required,

        "artifactKinds":
            dict(
                sorted(
                    kind_counts.items()
                )
            ),

        "contentUseModes":
            dict(
                sorted(
                    use_mode_counts.items()
                )
            ),

        "sourceRightsMatched":
            source_rights_match_count,

        "rightsMetadataFallbackLessons":
            rights_fallback_count,

        "rightsMetadataVersion":
            RIGHTS_METADATA_VERSION,

        "referenceOnlyRemaining":
            use_mode_counts.get(
                "REFERENCE_ONLY",
                0,
            ),
    },

    "sourceMapSha256":
        current_map_sha,

    "lessonIndexPath":
        str(
            INDEX_PATH.relative_to(
                ROOT
            )
        ),

    "lessonIndexSha256":
        index_sha,
}

atomic_write_json(
    REPORT_PATH,
    report,
)


# ------------------------------------------------------------
# Persist checkpoint
# ------------------------------------------------------------

run = load(
    RUN_PATH
)

checkpoints = run.setdefault(
    "checkpoints",
    {}
)

checkpoints[
    "source-importer"
] = {
    "status":
        "PASS",

    "completedAtUtc":
        now(),

    "sourceMapSha256":
        current_map_sha,

    "totalLessons":
        1560,

    "standalone":
        1466,

    "supporting":
        94,

    "uniqueSourceArtifacts":
        len(
            artifact_cache
        ),

    "lessonIndexSha256":
        index_sha,

    "reportSha256":
        sha256_file(
            REPORT_PATH
        ),
}

run[
    "currentStage"
] = "source-importer"

run[
    "updatedAtUtc"
] = now()

atomic_write_json(
    RUN_PATH,
    run,
)


print()
print(
    "=============================================================="
)
print(
    " PHASE 29 — STEP 04 SOURCE IMPORTER RESULT"
)
print(
    "=============================================================="
)
print(
    "Total lessons              : 1560"
)
print(
    "Standalone                 : 1466"
)
print(
    "Supporting                 : 94"
)
print()
print(
    "Unique source artifacts    :",
    len(
        artifact_cache
    ),
)
print(
    "Artifacts extracted        :",
    artifact_imported,
)
print(
    "Artifacts resumed          :",
    artifact_resumed,
)
print()
print(
    "Lesson packets written     :",
    lesson_imported,
)
print(
    "Lesson packets resumed     :",
    lesson_resumed,
)
print()
print(
    "Locators verified          :",
    locator_verified,
)
print(
    "Locators not required      :",
    locator_not_required,
)
print()
print(
    "Artifact kinds             :",
    dict(
        sorted(
            kind_counts.items()
        )
    ),
)
print(
    "Content use modes          :",
    dict(
        sorted(
            use_mode_counts.items()
        )
    ),
)
print()
print(
    "Rights fallback lessons    :",
    rights_fallback_count,
)
print(
    "REFERENCE_ONLY remaining   :",
    use_mode_counts.get(
        "REFERENCE_ONLY",
        0,
    ),
)
print()
print(
    "PASS: 1560/1560 source packets staged"
)
print(
    "PASS: Supporting OutcomeCodes remain empty"
)
print(
    "PASS: source importer checkpoint persisted"
)
