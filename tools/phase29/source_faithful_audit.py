#!/usr/bin/env python3

from __future__ import annotations

import argparse
import glob
import hashlib
import json
import os
import re
import sys
from datetime import datetime, timezone
from pathlib import Path
from typing import Any


ROOT = Path.cwd()

STATE_ROOT = ROOT / ".phase29-source-rebuild"

REPORT_ROOT = (
    STATE_ROOT /
    "reports"
)

INVENTORY_PATH = (
    STATE_ROOT /
    "inventory.json"
)

SOURCE_LOCK_PATH = (
    STATE_ROOT /
    "source-lock.json"
)

RUN_STATE_PATH = (
    STATE_ROOT /
    "run.json"
)


def now_utc() -> str:
    return (
        datetime.now(timezone.utc)
        .isoformat()
    )


def sha256_bytes(
    value: bytes,
) -> str:
    return hashlib.sha256(
        value
    ).hexdigest()


def sha256_file(
    path: Path,
) -> str:
    return sha256_bytes(
        path.read_bytes()
    )


def load_json(
    path: Path,
) -> Any:
    return json.loads(
        path.read_text(
            encoding="utf-8"
        )
    )


def write_json(
    path: Path,
    value: Any,
) -> None:
    path.parent.mkdir(
        parents=True,
        exist_ok=True,
    )

    path.write_text(
        json.dumps(
            value,
            ensure_ascii=False,
            indent=2,
        )
        + "\n",
        encoding="utf-8",
    )


def first_existing(
    candidates: list[Path],
) -> Path | None:
    for candidate in candidates:
        if candidate.exists():
            return candidate

    return None


def find_files(
    pattern: str,
) -> list[Path]:
    return sorted(
        Path(x).resolve()
        for x in glob.glob(
            pattern,
            recursive=True,
        )
    )


def recursive_values(
    node: Any,
    keys: set[str],
) -> list[str]:
    found: list[str] = []

    if isinstance(
        node,
        dict,
    ):
        for key, value in node.items():
            if (
                key.lower()
                in keys
                and
                isinstance(
                    value,
                    str,
                )
                and
                value.strip()
            ):
                found.append(
                    value.strip()
                )

            found.extend(
                recursive_values(
                    value,
                    keys,
                )
            )

    elif isinstance(
        node,
        list,
    ):
        for value in node:
            found.extend(
                recursive_values(
                    value,
                    keys,
                )
            )

    return found


def require(
    condition: bool,
    message: str,
) -> None:
    if not condition:
        raise RuntimeError(
            message
        )


def inspect_contract() -> dict[str, Any]:
    candidates = [
        ROOT /
        "src/Edulytics.Core/Curriculum/"
        "LessonContent/"
        "CanonicalLessonContentPack.cs",

        ROOT /
        "src/Edulytics.Core/Curriculum/"
        "LessonContent/"
        "CanonicalLessonContentPackDocument.cs",
    ]

    contract = first_existing(
        candidates
    )

    if contract is None:
        matches = find_files(
            "src/**/*.cs"
        )

        contract = next(
            (
                p
                for p in matches
                if (
                    "CanonicalLessonContentPackDocument"
                    in p.read_text(
                        encoding="utf-8",
                        errors="ignore",
                    )
                )
            ),
            None,
        )

    require(
        contract is not None,
        "CanonicalLessonContentPackDocument "
        "source file not found.",
    )

    text = contract.read_text(
        encoding="utf-8",
        errors="replace",
    )

    outcome_required = bool(
        re.search(
            r"OutcomeCodes\.Count\s*==\s*0",
            text,
        )
    )

    has_source_policy_v2 = (
        "SourcePolicyVersion"
        in text
        and
        "PedagogicalSource"
        in text
    )

    has_title_provenance = (
        "TitleProvenance"
        in text
    )

    return {
        "path": str(
            contract.relative_to(
                ROOT
            )
        ),
        "sha256":
            sha256_file(
                contract
            ),
        "outcomeCodesCurrentlyRequired":
            outcome_required,
        "sourcePolicyV2":
            has_source_policy_v2,
        "titleProvenance":
            has_title_provenance,
    }


def inspect_seeder() -> dict[str, Any]:
    matches = find_files(
        "src/**/*.cs"
    )

    candidates = []

    for path in matches:
        text = path.read_text(
            encoding="utf-8",
            errors="ignore",
        )

        if (
            "CanonicalLessonContent"
            in text
            and
            "SaveChangesAsync"
            in text
            and
            "OutcomeCodes"
            in text
        ):
            candidates.append(
                path
            )

    require(
        candidates,
        "Canonical lesson content seeder "
        "not found.",
    )

    return {
        "candidates": [
            {
                "path": str(
                    p.relative_to(
                        ROOT
                    )
                ),
                "sha256":
                    sha256_file(
                        p
                    ),
            }
            for p in candidates
        ]
    }


def inspect_service() -> dict[str, Any]:
    candidates = find_files(
        "src/Edulytics.Services/"
        "LessonContent/*.cs"
    )

    report = []

    for path in candidates:
        text = path.read_text(
            encoding="utf-8",
            errors="ignore",
        )

        if (
            "ProductionReadyLessons"
            in text
            or
            "IsStandaloneCanonicalTarget"
            in text
            or
            "HasOfficialAlignment"
            in text
        ):
            report.append(
                {
                    "path": str(
                        path.relative_to(
                            ROOT
                        )
                    ),
                    "sha256":
                        sha256_file(
                            path
                        ),
                    "productionReady":
                        "ProductionReadyLessons"
                        in text,
                    "standalonePolicy":
                        "IsStandaloneCanonicalTarget"
                        in text,
                    "hasOfficialAlignment":
                        "HasOfficialAlignment"
                        in text,
                }
            )

    require(
        report,
        "LessonContent service policy "
        "files not found.",
    )

    return {
        "files": report
    }


def inspect_ui() -> dict[str, Any]:
    index = (
        ROOT /
        "src/Edulytics.Web/Views/"
        "LessonContent/Index.cshtml"
    )

    detail = (
        ROOT /
        "src/Edulytics.Web/Views/"
        "LessonContent/Detail.cshtml"
    )

    require(
        index.exists(),
        "LessonContent Index.cshtml missing.",
    )

    require(
        detail.exists(),
        "LessonContent Detail.cshtml missing.",
    )

    index_text = index.read_text(
        encoding="utf-8",
    )

    detail_text = detail.read_text(
        encoding="utf-8",
    )

    return {
        "index": {
            "sha256":
                sha256_file(
                    index
                ),
            "supportingRowsAreLinks":
                bool(
                    re.search(
                        r'isSupporting.*?<a',
                        index_text,
                        flags=re.S,
                    )
                ),
            "standaloneDenominator":
                "standaloneCount"
                in index_text,
            "supportingCount":
                "supportingCount"
                in index_text,
        },
        "detail": {
            "sha256":
                sha256_file(
                    detail
                ),
            "supportingShortCircuitsBody":
                bool(
                    re.search(
                        r'@if\s*\(isSupporting\).*?'
                        r'else\s+if\s*'
                        r'\(Model\.Lesson\.Body\s+is\s+null\)',
                        detail_text,
                        flags=re.S,
                    )
                ),
            "structuredReader":
                "lesson-learning-section"
                in detail_text,
        },
    }


def inspect_content_packs() -> dict[str, Any]:
    files = find_files(
        "src/Edulytics.Core/"
        "Curriculum/LessonContent/Packs/"
        "us-ccss-math-*-phase29-v1."
        "lesson-content-pack.json"
    )

    require(
        files,
        "Common Core canonical "
        "content packs not found.",
    )

    documents = [
        load_json(
            path
        )
        for path in files
    ]

    lessons = []

    for path, document in zip(
        files,
        documents,
    ):
        raw_lessons = (
            document.get(
                "lessons"
            )
            or
            document.get(
                "Lessons"
            )
            or
            []
        )

        for lesson in raw_lessons:
            lessons.append(
                (
                    path,
                    document,
                    lesson,
                )
            )

    lesson_codes = []

    translation_count = 0
    empty_outcome_count = 0

    for _, _, lesson in lessons:
        code = (
            lesson.get(
                "lessonCode"
            )
            or
            lesson.get(
                "LessonCode"
            )
        )

        require(
            bool(code),
            "Content pack lesson "
            "without LessonCode.",
        )

        lesson_codes.append(
            code
        )

        outcomes = (
            lesson.get(
                "outcomeCodes"
            )
            if "outcomeCodes"
               in lesson
            else lesson.get(
                "OutcomeCodes",
                [],
            )
        )

        if not outcomes:
            empty_outcome_count += 1

        translations = (
            lesson.get(
                "translations"
            )
            if "translations"
               in lesson
            else lesson.get(
                "Translations",
                [],
            )
        )

        translation_count += len(
            translations
        )

    return {
        "packCount":
            len(
                files
            ),
        "lessonCount":
            len(
                lessons
            ),
        "uniqueLessonCodes":
            len(
                set(
                    lesson_codes
                )
            ),
        "translationCount":
            translation_count,
        "lessonsWithZeroOutcomeCodes":
            empty_outcome_count,
        "files": [
            {
                "path": str(
                    path.relative_to(
                        ROOT
                    )
                ),
                "sha256":
                    sha256_file(
                        path
                    ),
            }
            for path in files
        ],
    }


def inspect_blueprints() -> dict[str, Any]:
    files = find_files(
        "src/Edulytics.Core/"
        "Curriculum/LessonBlueprints/Packs/"
        "*.json"
    )

    require(
        files,
        "Pedagogical lesson "
        "blueprint packs not found.",
    )

    relevant = []
    all_urls = []

    for path in files:
        try:
            document = load_json(
                path
            )
        except Exception:
            continue

        dumped = json.dumps(
            document,
            ensure_ascii=False,
        )

        if (
            "CCSS"
            not in dumped
            and
            "Common Core"
            not in dumped
            and
            "US-CCSS-MATH"
            not in dumped
        ):
            continue

        urls = recursive_values(
            document,
            {
                "sourceurl",
                "url",
                "evidenceurl",
                "pedagogicalsourceurl",
            },
        )

        relevant.append(
            {
                "path": str(
                    path.relative_to(
                        ROOT
                    )
                ),
                "sha256":
                    sha256_file(
                        path
                    ),
                "sourceUrlCount":
                    len(
                        set(
                            urls
                        )
                    ),
            }
        )

        all_urls.extend(
            urls
        )

    require(
        relevant,
        "No Common Core blueprint "
        "pack detected.",
    )

    return {
        "packCount":
            len(
                relevant
            ),
        "sourceUrlCount":
            len(
                set(
                    all_urls
                )
            ),
        "files":
            relevant,
        "sourceUrls":
            sorted(
                set(
                    all_urls
                )
            ),
    }


def build_source_lock(
    blueprint_report: dict[str, Any],
) -> dict[str, Any]:
    urls = (
        blueprint_report.get(
            "sourceUrls",
            [],
        )
    )

    domains = sorted(
        {
            re.sub(
                r"^www\.",
                "",
                re.sub(
                    r"^https?://",
                    "",
                    url,
                )
                .split(
                    "/",
                    1,
                )[0]
                .lower(),
            )
            for url in urls
            if url.startswith(
                (
                    "http://",
                    "https://",
                )
            )
        }
    )

    # Fail-closed legal policy.
    #
    # Commercial reuse is allowed only for
    # source families explicitly locked here.
    #
    # A domain being present does NOT by itself
    # grant rights; the exact selected edition/
    # page must carry the approved license.
    approved_families = [
        {
            "key":
                "IM_K12_FIRST_EDITION",
            "publisher":
                "Illustrative Mathematics",
            "hosts": [
                "im.kendallhunt.com",
                "curriculum.illustrativemathematics.org",
            ],
            "license":
                "CC BY 4.0",
            "commercialReuse":
                True,
            "requiresAttribution":
                True,
            "forbiddenAssets": [
                "Illustrative Mathematics logos",
                "IM logos/trademarks",
                "third-party assets whose own license "
                "does not permit reuse",
            ],
            "licenseEvidence":
                "https://illustrativemathematics.org/terms-of-use/",
        },
        {
            "key":
                "OPEN_UP_6_8_ED1_ED2",
            "publisher":
                "Open Up Resources",
            "hosts": [
                "access.openupresources.org",
            ],
            "license":
                "CC BY 4.0",
            "commercialReuse":
                True,
            "requiresAttribution":
                True,
            "forbiddenAssets": [
                "Open Up trademarks/logos/covers",
                "assessments excluded from Creative Commons",
                "third-party assets with incompatible rights",
            ],
            "licenseEvidence":
                "https://www.openupresources.org/"
                "help-support/licensing-questions/",
        },
        {
            "key":
                "MVP_EXPLICIT_CC_BY_4",
            "publisher":
                "Mathematics Vision Project",
            "hosts": [
                "mathematicsvisionproject.org",
                "www.mathematicsvisionproject.org",
            ],
            "license":
                "CC BY 4.0 ONLY WHEN THE EXACT "
                "SOURCE PAGE/PDF STATES CC BY 4.0",
            "commercialReuse":
                True,
            "requiresAttribution":
                True,
            "forbiddenAssets": [
                "MVP logo/trademark",
                "purchased answer keys",
                "purchased/sample assessments",
                "any edition/page marked BY-NC or BY-NC-SA",
                "third-party assets with incompatible rights",
            ],
            "licenseEvidence":
                "Exact source page/PDF footer "
                "must be captured per artifact.",
        },
    ]

    blocked = [
        {
            "key":
                "OPEN_UP_HS_CURRENT",
            "reason":
                "Open Up HS Math is CC BY-NC 4.0 "
                "and is not approved for commercial reuse.",
        },
        {
            "key":
                "IM_V360",
            "reason":
                "IM v.360 is CC BY-NC 4.0 "
                "and is not approved for commercial reuse.",
        },
    ]

    return {
        "schemaVersion": 1,
        "checkedAtUtc":
            now_utc(),
        "policy":
            "FAIL_CLOSED_COMMERCIAL_REUSE",
        "discoveredBlueprintDomains":
            domains,
        "approvedFamilies":
            approved_families,
        "explicitlyBlockedFamilies":
            blocked,
        "rule":
            (
                "No lesson body may be imported unless "
                "the exact source artifact is matched to "
                "an approved family and its license is "
                "verified on the artifact or authoritative "
                "license page."
            ),
    }


def build_inventory(
    content_report: dict[str, Any],
    blueprint_report: dict[str, Any],
) -> dict[str, Any]:
    return {
        "schemaVersion": 1,
        "createdAtUtc":
            now_utc(),

        "acceptedProductTarget": {
            "totalPedagogicalLessons":
                1560,
            "standaloneLessons":
                1466,
            "supportingLessons":
                94,
            "englishBodies":
                1560,
            "polishBodies":
                1560,
            "totalTranslations":
                3120,
            "missingContent":
                0,
        },

        "currentCanonicalState": {
            "contentPacks":
                content_report[
                    "packCount"
                ],
            "publishedStandaloneBodies":
                content_report[
                    "lessonCount"
                ],
            "translations":
                content_report[
                    "translationCount"
                ],
            "zeroOutcomeCanonicalBodies":
                content_report[
                    "lessonsWithZeroOutcomeCodes"
                ],
        },

        "blueprintState": {
            "packCount":
                blueprint_report[
                    "packCount"
                ],
            "sourceUrlCount":
                blueprint_report[
                    "sourceUrlCount"
                ],
        },

        "requiredTransformation": {
            "replaceGeneratedStandaloneBodies":
                1466,
            "addSupportingBodies":
                94,
            "finalEnglishBodies":
                1560,
            "finalPolishBodies":
                1560,
            "finalTranslations":
                3120,
        },
    }


def update_checkpoint(
    name: str,
    payload: dict[str, Any],
) -> None:
    state = load_json(
        RUN_STATE_PATH
    )

    checkpoints = state.setdefault(
        "checkpoints",
        {},
    )

    checkpoints[name] = {
        "status":
            "PASS",
        "completedAtUtc":
            now_utc(),
        **payload,
    }

    state[
        "currentStage"
    ] = name

    state[
        "updatedAtUtc"
    ] = now_utc()

    write_json(
        RUN_STATE_PATH,
        state,
    )


def run_audit() -> None:
    contract = inspect_contract()
    seeder = inspect_seeder()
    service = inspect_service()
    ui = inspect_ui()
    content = inspect_content_packs()
    blueprints = inspect_blueprints()

    require(
        content[
            "lessonCount"
        ] == 1466,
        "Expected 1466 existing "
        "standalone canonical bodies; "
        f"found {content['lessonCount']}.",
    )

    require(
        content[
            "translationCount"
        ] == 2932,
        "Expected 2932 existing "
        "EN/PL translations; "
        f"found {content['translationCount']}.",
    )

    report = {
        "schemaVersion": 1,
        "auditedAtUtc":
            now_utc(),
        "contract":
            contract,
        "seeder":
            seeder,
        "service":
            service,
        "ui":
            ui,
        "content":
            content,
        "blueprints":
            blueprints,
        "findings": {
            "supportingCanonicalContractChangeRequired":
                bool(
                    contract[
                        "outcomeCodesCurrentlyRequired"
                    ]
                ),
            "supportingReaderChangeRequired":
                bool(
                    ui[
                        "detail"
                    ][
                        "supportingShortCircuitsBody"
                    ]
                ),
            "supportingListLinkChangeRequired":
                not bool(
                    ui[
                        "index"
                    ][
                        "supportingRowsAreLinks"
                    ]
                ),
            "coverageSemanticsChangeRequired":
                True,
            "legacyGeneratedContentMustBeReplaced":
                True,
        },
    }

    write_json(
        REPORT_ROOT /
        "architecture-audit.json",
        report,
    )

    source_lock = build_source_lock(
        blueprints
    )

    write_json(
        SOURCE_LOCK_PATH,
        source_lock,
    )

    inventory = build_inventory(
        content,
        blueprints,
    )

    write_json(
        INVENTORY_PATH,
        inventory,
    )

    digest = sha256_file(
        REPORT_ROOT /
        "architecture-audit.json"
    )

    update_checkpoint(
        "architecture-audit",
        {
            "reportSha256":
                digest,
            "contentLessonCount":
                content[
                    "lessonCount"
                ],
            "translationCount":
                content[
                    "translationCount"
                ],
        },
    )

    print()
    print(
        "=============================================================="
    )
    print(
        " PHASE 29 SOURCE-FIDELITY ARCHITECTURE AUDIT: PASS"
    )
    print(
        "=============================================================="
    )
    print(
        "Existing standalone bodies :",
        content[
            "lessonCount"
        ],
    )
    print(
        "Existing translations      :",
        content[
            "translationCount"
        ],
    )
    print(
        "Target total lessons       : 1560"
    )
    print(
        "Target standalone          : 1466"
    )
    print(
        "Target supporting          : 94"
    )
    print(
        "Target translations        : 3120"
    )
    print()
    print(
        "Contract currently requires "
        "OutcomeCodes:",
        contract[
            "outcomeCodesCurrentlyRequired"
        ],
    )
    print(
        "Supporting UI bypasses body:",
        ui[
            "detail"
        ][
            "supportingShortCircuitsBody"
        ],
    )
    print()
    print(
        "NO CONTENT MUTATION PERFORMED."
    )
    print(
        "Next resumable stage: "
        "source-artifact acquisition + "
        "exact license verification."
    )
    print(
        "=============================================================="
    )


def status() -> None:
    state = load_json(
        RUN_STATE_PATH
    )

    print(
        json.dumps(
            state,
            ensure_ascii=False,
            indent=2,
        )
    )


def main() -> None:
    parser = argparse.ArgumentParser()

    parser.add_argument(
        "--audit",
        action="store_true",
    )

    parser.add_argument(
        "--status",
        action="store_true",
    )

    args = parser.parse_args()

    if args.audit:
        run_audit()
        return

    if args.status:
        status()
        return

    parser.error(
        "Specify --audit or --status"
    )


if __name__ == "__main__":
    main()
