#!/usr/bin/env python3
from __future__ import annotations

import re
from pathlib import Path
from typing import Any

from official_outcome_practice_rules import load_resolutions as load_outcome_resolutions
from polish_practice_map import (
    lesson_mapping as polish_lesson_mapping,
    load_polish_outcome_mappings,
)
from supporting_practice_rules import (
    SupportingRule,
    choose_translation,
    clean_list,
    get_case,
    load_rules,
    normalize_space,
    normalize_title,
    read_json,
    validate_rules,
)


def _is_reviewed_exact_pattern(pattern: re.Pattern[str]) -> bool:
    raw = pattern.pattern.strip()
    return raw.startswith("^") and raw.endswith("$")


def _matches_reviewed_exact_code(
    lesson_code: str,
    rule: SupportingRule,
) -> bool:
    if rule.title_patterns:
        return False
    exact_patterns = [
        pattern for pattern in rule.code_patterns
        if _is_reviewed_exact_pattern(pattern)
    ]
    return bool(exact_patterns) and any(
        pattern.fullmatch(lesson_code)
        for pattern in exact_patterns
    )


def _matches_reviewed_exact_title(
    lesson_code: str,
    title: str,
    rule: SupportingRule,
) -> bool:
    exact_patterns = [
        pattern for pattern in rule.title_patterns
        if _is_reviewed_exact_pattern(pattern)
    ]
    if not exact_patterns:
        return False

    normalized = normalize_title(title)
    if not any(pattern.fullmatch(normalized) for pattern in exact_patterns):
        return False

    if rule.code_patterns and not any(
        pattern.search(lesson_code)
        for pattern in rule.code_patterns
    ):
        return False

    return True



def _matches_reviewed_title(
    lesson_code: str,
    title: str,
    rule: SupportingRule,
) -> bool:
    normalized = normalize_title(title)

    if rule.title_patterns and not any(
        pattern.search(normalized)
        for pattern in rule.title_patterns
    ):
        return False

    if rule.code_patterns and not any(
        pattern.search(lesson_code)
        for pattern in rule.code_patterns
    ):
        return False

    return True


def _matches_reviewed_canonical_evidence(
    lesson_code: str,
    translation: dict[str, Any],
    rule: SupportingRule,
) -> bool:
    title = normalize_title(
        normalize_space(
            get_case(translation, "Title", "title", default="")
        )
    )
    evidence = normalize_space(" ".join([
        title,
        str(get_case(translation, "Explanation", "explanation", default="") or ""),
        str(get_case(translation, "KeyConceptsAndRules", "keyConceptsAndRules", default="") or ""),
        str(get_case(translation, "WorkedExamples", "workedExamples", default="") or ""),
    ]))

    if rule.title_patterns and not any(
        pattern.search(evidence)
        for pattern in rule.title_patterns
    ):
        return False

    if rule.code_patterns and not any(
        pattern.search(lesson_code)
        for pattern in rule.code_patterns
    ):
        return False

    return True

def mapping_from_rule(
    lesson_code: str,
    outcome_codes: list[str],
    rule: SupportingRule,
    match_mode: str,
) -> dict[str, Any]:
    source_type = (
        "OfficialReviewedExactCodeRule"
        if match_mode == "EXACT_CODE"
        else (
            "OfficialReviewedExactTitleRule"
            if match_mode == "EXACT_TITLE"
            else (
                "OfficialReviewedUniqueTitleRule"
                if match_mode == "UNIQUE_REVIEWED_TITLE"
                else "OfficialReviewedCanonicalEvidence"
            )
        )
    )
    confidence = (
        "ReviewedExactCode"
        if match_mode == "EXACT_CODE"
        else (
            "ReviewedExactTitle"
            if match_mode == "EXACT_TITLE"
            else (
                "ReviewedUniqueTitle"
                if match_mode == "UNIQUE_REVIEWED_TITLE"
                else "ReviewedCanonicalEvidence"
            )
        )
    )
    return {
        "lessonCode": lesson_code,
        "primarySkills": [rule.skill_id],
        "sourceType": source_type,
        "officialOutcomeMapped": True,
        "officialOutcomeCodes": outcome_codes,
        "mappingConfidence": confidence,
        "officialPracticeMatchMode": match_mode,
        "practiceReadiness": "READY_VERIFIED",
        "practiceMechanic": rule.mechanic,
        "allowedQuestionFamilies": list(rule.families),
        "officialPracticeRuleId": rule.rule_id,
        "evidence": [
            "The canonical lesson has official OutcomeCode provenance.",
            (
                "The lesson title full-matches a reviewed anchored Practice target rule."
                if match_mode == "EXACT_TITLE"
                else "Exactly one reviewed Practice target rule matches the canonical lesson title."
            ),
            "Semantic content, family, solver and verifier gates remain independently enforced.",
        ],
    }


def mapping_from_outcomes(
    lesson_code: str,
    outcome_codes: list[str],
    resolutions: list[Any],
) -> dict[str, Any]:
    target_rules = {
        resolution.target_rule.rule_id: resolution.target_rule
        for resolution in resolutions
    }
    ordered = [target_rules[key] for key in sorted(target_rules)]
    skills = sorted({rule.skill_id for rule in ordered})
    families = sorted({
        family
        for rule in ordered
        for family in rule.families
    })
    mechanics = sorted({rule.mechanic for rule in ordered})
    return {
        "lessonCode": lesson_code,
        "primarySkills": skills,
        "sourceType": "OfficialOutcomeRule",
        "officialOutcomeMapped": True,
        "officialOutcomeCodes": outcome_codes,
        "mappingConfidence": "ReviewedOfficialOutcome",
        "officialPracticeMatchMode": "OFFICIAL_OUTCOME_RULE",
        "practiceReadiness": "READY_VERIFIED",
        "practiceMechanic": (
            mechanics[0] if len(mechanics) == 1 else "OFFICIAL_MULTI_OUTCOME"
        ),
        "allowedQuestionFamilies": families,
        "officialPracticeRuleIds": sorted({
            resolution.rule_id for resolution in resolutions
        }),
        "evidence": [
            "Every canonical lesson OutcomeCode resolves through the reviewed official outcome Practice registry.",
            "Each official outcome rule delegates to an existing reviewed Skill/Question-Family target rule.",
            "Multi-outcome lessons retain all resolved exact skills and solver/verifier-backed families.",
        ],
    }


def load_reviewed_official_rule_mappings(
    content_dir: Path,
) -> tuple[dict[str, dict[str, Any]], list[str]]:
    rules, errors = load_rules()
    errors.extend(validate_rules(rules))
    outcome_resolutions, outcome_errors = load_outcome_resolutions()
    errors.extend(outcome_errors)
    polish_mappings, polish_errors = load_polish_outcome_mappings()
    errors.extend(polish_errors)

    mappings: dict[str, dict[str, Any]] = {}
    seen: set[str] = set()

    for path in sorted(content_dir.glob("*.lesson-content-pack.json")):
        try:
            doc = read_json(path)
        except (OSError, ValueError) as ex:
            errors.append(
                f"Unable to read official Practice content pack {path.name}: {ex}"
            )
            continue

        pack_code = str(
            get_case(doc, "PackCode", "packCode", default="") or ""
        ).strip()

        academic_language = str(
            get_case(doc, "AcademicLanguage", "academicLanguage", default="") or ""
        ).strip()
        lessons = get_case(doc, "Lessons", "lessons", default=[])
        if not isinstance(lessons, list):
            continue

        for lesson in lessons:
            if not isinstance(lesson, dict):
                continue

            lesson_code = str(
                get_case(lesson, "LessonCode", "lessonCode", default="") or ""
            ).strip()
            if not lesson_code or lesson_code in seen:
                continue
            seen.add(lesson_code)

            outcomes = clean_list(
                get_case(lesson, "OutcomeCodes", "outcomeCodes", default=[])
            )
            official_reference = str(
                get_case(
                    lesson,
                    "OfficialReferenceCode",
                    "officialReferenceCode",
                    default="",
                )
                or ""
            ).strip()
            is_verified_uae_reference = (
                pack_code == "UAE-MOE-MATH"
                and bool(official_reference)
            )
            if not outcomes and not is_verified_uae_reference:
                continue

            if pack_code == "PL-NATIONAL-MATH":
                polish = polish_lesson_mapping(
                    lesson_code,
                    outcomes,
                    polish_mappings,
                )
                if polish is not None:
                    mappings[lesson_code] = polish
                continue

            resolved_outcomes = [
                outcome_resolutions.get(code)
                for code in outcomes
            ]

            translation = choose_translation(lesson, academic_language)
            title = normalize_space(
                get_case(translation, "Title", "title", default="")
                or get_case(lesson, "Title", "title", default="")
            )

            exact_code_candidates = [
                rule
                for rule in rules
                if _matches_reviewed_exact_code(lesson_code, rule)
            ]
            if len(exact_code_candidates) > 1:
                errors.append(
                    "Official exact-code Practice rule collision for "
                    f"{lesson_code}: "
                    + ", ".join(rule.rule_id for rule in exact_code_candidates)
                )
                continue

            if len(exact_code_candidates) == 1:
                mappings[lesson_code] = mapping_from_rule(
                    lesson_code,
                    outcomes,
                    exact_code_candidates[0],
                    "EXACT_CODE",
                )
                continue

            exact_candidates = [
                rule
                for rule in rules
                if _matches_reviewed_exact_title(lesson_code, title, rule)
            ]
            if len(exact_candidates) > 1:
                errors.append(
                    "Official exact-title Practice rule collision for "
                    f"{lesson_code}: {title!r} -> "
                    + ", ".join(rule.rule_id for rule in exact_candidates)
                )
                continue

            if len(exact_candidates) == 1:
                mappings[lesson_code] = mapping_from_rule(
                    lesson_code,
                    outcomes,
                    exact_candidates[0],
                    "EXACT_TITLE",
                )
                continue

            reviewed_title_candidates = [
                rule
                for rule in rules
                if _matches_reviewed_title(lesson_code, title, rule)
            ]
            if len(reviewed_title_candidates) == 1:
                mappings[lesson_code] = mapping_from_rule(
                    lesson_code,
                    outcomes,
                    reviewed_title_candidates[0],
                    "UNIQUE_REVIEWED_TITLE",
                )
                continue

            should_review_canonical_evidence = (
                is_verified_uae_reference
                or (
                    pack_code == "CAMBRIDGE-INTL-MATH"
                    and any(code.startswith("CAM:REF:9709:") for code in outcomes)
                )
            )
            if should_review_canonical_evidence:
                canonical_candidates = [
                    rule
                    for rule in rules
                    if _matches_reviewed_canonical_evidence(
                        lesson_code,
                        translation,
                        rule,
                    )
                ]
                if len(canonical_candidates) == 1:
                    mappings[lesson_code] = mapping_from_rule(
                        lesson_code,
                        outcomes,
                        canonical_candidates[0],
                        "CANONICAL_EVIDENCE",
                    )
                    continue

            if outcomes and all(
                resolution is not None
                for resolution in resolved_outcomes
            ):
                mappings[lesson_code] = mapping_from_outcomes(
                    lesson_code,
                    outcomes,
                    [
                        resolution
                        for resolution in resolved_outcomes
                        if resolution is not None
                    ],
                )

    return mappings, errors
