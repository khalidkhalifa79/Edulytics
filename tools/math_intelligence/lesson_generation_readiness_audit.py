#!/usr/bin/env python3
from __future__ import annotations

import argparse
import json
from collections import Counter, defaultdict
from pathlib import Path
from typing import Any

from lesson_semantic_content_audit import audit as semantic_audit
from lesson_skill_resolution_audit import audit as skill_resolution_audit
from supporting_practice_rules import load_rule_mappings as load_supporting_rule_mappings
from official_practice_rules import (
    load_reviewed_official_rule_mappings,
)

ROOT = Path(__file__).resolve().parents[2]
SKILL_REGISTRY = ROOT / "src/Edulytics.Core/Mathematics/Skills/skill-registry.v1.json"
CAPABILITY_REGISTRY = ROOT / "src/Edulytics.Core/Mathematics/Skills/capability-registry.v1.json"
QUESTION_FAMILY_REGISTRY = ROOT / "src/Edulytics.Core/Mathematics/Generation/question-family-registry.v1.json"
LESSON_MAPPINGS = ROOT / "src/Edulytics.Core/Mathematics/Curriculum/lesson-skill-mappings.v1.json"
REPORT = ROOT / "artifacts/math-intelligence/lesson-generation-readiness-audit.json"

READY = {"READY_VERIFIED", "READY_CONTEXTUAL"}


def read_json(path: Path) -> Any:
    return json.loads(path.read_text(encoding="utf-8-sig"))


def clean_list(value: Any) -> list[str]:
    if not isinstance(value, list):
        return []
    return [str(item).strip() for item in value if str(item).strip()]


def load_index(path: Path, collection: str, key: str = "id") -> dict[str, dict[str, Any]]:
    doc = read_json(path)
    result: dict[str, dict[str, Any]] = {}
    for row in doc.get(collection) or []:
        if not isinstance(row, dict):
            continue
        value = str(row.get(key) or "").strip()
        if value:
            result[value] = row
    return result


def load_approved_mappings() -> tuple[dict[str, dict[str, Any]], list[str]]:
    doc = read_json(LESSON_MAPPINGS)
    result: dict[str, dict[str, Any]] = {}
    for row in doc.get("mappings") or []:
        if not isinstance(row, dict):
            continue
        code = str(row.get("lessonCode") or "").strip()
        if code:
            result[code] = row

    content_dir = ROOT / "src/Edulytics.Core/Curriculum/LessonContent/Packs"
    official, errors = load_reviewed_official_rule_mappings(content_dir)
    for code, row in official.items():
        result.setdefault(code, row)

    supporting, unmatched, supporting_errors = load_supporting_rule_mappings(content_dir)
    errors.extend(supporting_errors)
    for code, row in supporting.items():
        result.setdefault(code, row)

    explicit_codes = {
        str(row.get("lessonCode") or "").strip()
        for row in doc.get("mappings") or []
        if isinstance(row, dict)
    }
    covered_by_non_supporting_mapping = explicit_codes | set(official)
    errors.extend(
        f"Supporting Practice target rule missing for {row['lessonCode']}: {row['title']}"
        for row in unmatched
        if row["lessonCode"] not in covered_by_non_supporting_mapping
    )
    return result, errors


def has_shadow_verified_v2_family(
    skill_id: str,
    skill: dict[str, Any],
    capabilities: dict[str, dict[str, Any]],
    families: dict[str, dict[str, Any]],
) -> tuple[bool, list[str]]:
    if str(skill.get("v2Status") or "") != "ShadowVerified":
        return False, []

    declared_families = clean_list(skill.get("v2QuestionFamilies"))
    if not declared_families:
        return False, [f"Skill {skill_id} is ShadowVerified but declares no V2 question family."]

    diagnostics: list[str] = []
    for family_id in declared_families:
        family = families.get(family_id)
        if family is None:
            diagnostics.append(f"V2 family {family_id} is missing from the registry.")
            continue
        if str(family.get("skillId") or "").strip() != skill_id:
            diagnostics.append(f"V2 family {family_id} is bound to a different SkillId.")
            continue
        if str(family.get("status") or "") != "ShadowVerified":
            diagnostics.append(f"V2 family {family_id} is not ShadowVerified.")
            continue
        if not str(family.get("verificationPolicy") or "").strip():
            diagnostics.append(f"V2 family {family_id} has no verification policy.")
            continue

        required = clean_list(family.get("requiredCapabilities"))
        missing = [capability_id for capability_id in required if capability_id not in capabilities]
        if missing:
            diagnostics.append(
                f"V2 family {family_id} references missing capabilities: {', '.join(sorted(missing))}."
            )
            continue

        return True, [
            f"Skill {skill_id} has ShadowVerified V2 family {family_id} with registered solver/verifier capabilities."
        ]

    return False, diagnostics


def capability_for_mapping(
    mapping: dict[str, Any] | None,
    skills: dict[str, dict[str, Any]],
    capabilities: dict[str, dict[str, Any]],
    families: dict[str, dict[str, Any]],
) -> tuple[bool, bool, bool, bool, list[str]]:
    if not mapping:
        return False, False, False, False, ["No approved lesson-skill mapping exists."]

    primary_skills = clean_list(mapping.get("primarySkills"))
    if not primary_skills:
        return False, False, False, False, ["Approved mapping has no primary skills."]

    missing = [skill_id for skill_id in primary_skills if skill_id not in skills]
    if missing:
        return False, False, False, False, [
            "Mapped SkillIds are absent from the registry: " + ", ".join(sorted(missing))
        ]

    mapped_families = clean_list(mapping.get("allowedQuestionFamilies"))
    if mapped_families:
        family_errors: list[str] = []
        for family_id in mapped_families:
            family = families.get(family_id)
            if family is None:
                family_errors.append(f"Mapped family {family_id} is missing from the registry.")
                continue
            family_skill = str(family.get("skillId") or "").strip()
            if family_skill not in primary_skills:
                family_errors.append(
                    f"Mapped family {family_id} belongs to {family_skill!r}, not an approved primary SkillId."
                )
            if family.get("lessonPracticeRouting") is not True:
                family_errors.append(
                    f"Mapped family {family_id} is not enabled for lesson Practice routing."
                )
            if not str(family.get("verificationPolicy") or "").strip():
                family_errors.append(
                    f"Mapped family {family_id} has no verification policy."
                )
        if not family_errors:
            return True, True, True, False, [
                "Approved mapping declares exact lesson Practice families with valid routing and verification policies."
            ]

    question_family_flags: list[bool] = []
    verified_flags: list[bool] = []
    contextual_flags: list[bool] = []
    v2_flags: list[bool] = []
    reasons: list[str] = []

    for skill_id in primary_skills:
        row = skills[skill_id]
        legacy_verified = bool(row.get("legacyGameMechanic"))
        legacy_contextual = legacy_verified or bool(row.get("legacyCanonicalSkill"))
        v2_verified, v2_reasons = has_shadow_verified_v2_family(
            skill_id,
            row,
            capabilities,
            families,
        )

        question_family_flags.append(legacy_contextual or v2_verified)
        verified_flags.append(legacy_verified or v2_verified)
        contextual_flags.append(legacy_contextual or v2_verified)
        v2_flags.append(v2_verified)
        reasons.extend(v2_reasons)

        if legacy_verified:
            reasons.append(
                f"Skill {skill_id} has an exact legacy game mechanic verified against its lesson target."
            )
        elif legacy_contextual and not v2_verified:
            reasons.append(
                f"Skill {skill_id} is covered by an existing native/contextual generation capability."
            )
        elif not v2_verified:
            reasons.append(
                f"Skill {skill_id} has no verified V2 family or sufficient legacy generation binding."
            )

    has_question_family = all(question_family_flags)
    has_verified = all(verified_flags)
    has_contextual = all(contextual_flags)
    is_v2_shadow_verified = all(v2_flags)

    return has_question_family, has_verified, has_contextual, is_v2_shadow_verified, reasons


def decide(
    skill_status: str,
    semantic_status: str,
    has_approved_mapping: bool,
    has_reviewed_official_mapping: bool,
    has_question_family: bool,
    has_verified: bool,
    has_contextual: bool,
) -> tuple[str, list[str]]:
    if semantic_status == "BLOCKED":
        return "BLOCKED", ["Semantic content audit blocked the lesson."]
    if skill_status == "CONFLICT" or semantic_status == "MAPPING_CONFLICT":
        return "MAPPING_CONFLICT", ["Mapping evidence contains a conflict."]
    if (
        has_reviewed_official_mapping
        and has_approved_mapping
        and has_question_family
        and has_verified
    ):
        return "READY_VERIFIED", [
            "Reviewed official Practice mapping supplies an exact approved skill, question family, and verified runtime capability."
        ]
    if semantic_status == "CONTENT_WEAK":
        return "CONTENT_WEAK", ["Worked examples do not demonstrate the recognized mathematical target strongly enough."]
    if semantic_status == "REVIEW_REQUIRED":
        return "REQUIRES_ACADEMIC_REVIEW", ["Semantic content evidence requires explicit academic review."]
    if semantic_status == "UNCLASSIFIED" and not has_reviewed_official_mapping:
        return "REQUIRES_ACADEMIC_REVIEW", ["Semantic content evidence is not strong enough for generation readiness."]
    if skill_status == "AMBIGUOUS":
        return "SKILL_AMBIGUOUS", ["Multiple SkillIds remain plausible."]
    if skill_status == "REVIEW_REQUIRED":
        return "REQUIRES_ACADEMIC_REVIEW", ["Skill evidence requires review before promotion."]
    if skill_status == "HIGH_CONFIDENCE_CANDIDATE":
        return "REQUIRES_ACADEMIC_REVIEW", ["High-confidence candidate is not an approved mapping."]
    if skill_status in {"UNRESOLVED", "ONTOLOGY_GAP"}:
        return "SOLVER_CAPABILITY_MISSING", ["Current Skill/Capability ontology cannot represent the lesson precisely enough."]
    if not has_approved_mapping:
        return "REQUIRES_ACADEMIC_REVIEW", ["No approved lesson-skill profile exists."]
    if not has_question_family:
        return "QUESTION_FAMILY_MISSING", ["Approved skill has no eligible question family."]
    if has_verified:
        return "READY_VERIFIED", ["Approved mapping, semantic content, question family and verified capability are available."]
    if has_contextual:
        return "READY_CONTEXTUAL", ["Approved mapping and contextual generation capability are available."]
    return "SOLVER_CAPABILITY_MISSING", ["Approved skill lacks sufficient generation/solver capability."]


def audit() -> dict[str, Any]:
    skill_report = skill_resolution_audit()
    semantic_report = semantic_audit()
    skills = load_index(SKILL_REGISTRY, "skills")
    capabilities = load_index(CAPABILITY_REGISTRY, "capabilities")
    families = load_index(QUESTION_FAMILY_REGISTRY, "families")
    mappings, mapping_blockers = load_approved_mappings()

    skill_by_code = {row["lessonCode"]: row for row in skill_report["lessons"]}
    semantic_by_code = {row["lessonCode"]: row for row in semantic_report["lessons"]}

    all_codes = sorted(set(skill_by_code) | set(semantic_by_code))
    blockers: list[str] = list(mapping_blockers)
    if set(skill_by_code) != set(semantic_by_code):
        missing_skill = sorted(set(semantic_by_code) - set(skill_by_code))
        missing_semantic = sorted(set(skill_by_code) - set(semantic_by_code))
        if missing_skill:
            blockers.append(f"{len(missing_skill)} lessons are missing from skill resolution audit.")
        if missing_semantic:
            blockers.append(f"{len(missing_semantic)} lessons are missing from semantic content audit.")

    rows: list[dict[str, Any]] = []
    summary = Counter()
    by_pack: dict[str, Counter] = defaultdict(Counter)
    by_source: dict[str, Counter] = defaultdict(Counter)

    for code in all_codes:
        skill = skill_by_code.get(code)
        semantic = semantic_by_code.get(code)
        if not skill or not semantic:
            continue

        skill_status = str(skill.get("status") or "UNRESOLVED")
        semantic_status = str(semantic.get("status") or "UNCLASSIFIED")
        mapping = mappings.get(code)
        has_family, has_verified, has_contextual, is_v2_shadow_verified, capability_reasons = capability_for_mapping(
            mapping,
            skills,
            capabilities,
            families,
        )
        reviewed_official_mapping = bool(
            mapping
            and str(mapping.get("sourceType") or "") in {
                "OfficialReviewedExactCodeRule",
                "OfficialReviewedExactTitleRule",
                "OfficialReviewedUniqueTitleRule",
                "OfficialReviewedCanonicalEvidence",
                "OfficialOutcomeRule",
                "PolishOfficialOutcomeMap",
            }
        )
        readiness, reasons = decide(
            skill_status,
            semantic_status,
            mapping is not None,
            reviewed_official_mapping,
            has_family,
            has_verified,
            has_contextual,
        )

        pack_code = str(skill.get("packCode") or semantic.get("packCode") or "")
        source_type = str(skill.get("sourceType") or semantic.get("sourceType") or "Unknown")
        summary["lessonCount"] += 1
        summary[readiness] += 1
        if readiness in READY:
            summary["generationReadyCount"] += 1
            if is_v2_shadow_verified:
                summary["v2ShadowVerifiedReadyCount"] += 1
        else:
            summary["generationBlockedCount"] += 1
        by_pack[pack_code]["lessonCount"] += 1
        by_pack[pack_code][readiness] += 1
        by_source[source_type]["lessonCount"] += 1
        by_source[source_type][readiness] += 1

        rows.append({
            "lessonCode": code,
            "packCode": pack_code,
            "sourceType": source_type,
            "title": semantic.get("title"),
            "skillResolutionStatus": skill_status,
            "semanticContentStatus": semantic_status,
            "approvedMapping": mapping is not None,
            "reviewedOfficialMapping": reviewed_official_mapping,
            "approvedPrimarySkills": [] if mapping is None else clean_list(mapping.get("primarySkills")),
            "approvedQuestionFamilies": [] if mapping is None else clean_list(mapping.get("allowedQuestionFamilies")),
            "hasQuestionFamily": has_family,
            "hasVerifiedSolverCapability": has_verified,
            "hasContextualGenerationCapability": has_contextual,
            "isV2ShadowVerified": is_v2_shadow_verified,
            "generationReadiness": readiness,
            "reasons": reasons + capability_reasons,
        })

    summary["skillRegistryCount"] = len(skills)
    summary["capabilityRegistryCount"] = len(capabilities)
    summary["questionFamilyRegistryCount"] = len(families)
    summary["approvedLessonSkillMappingCount"] = len(mappings)
    summary["blockerCount"] = len(blockers)

    return {
        "schemaVersion": 2,
        "audit": "Edulytics Mathematics V2 generation readiness gate",
        "authority": (
            "Fail-closed shadow readiness only. READY_VERIFIED may include a ShadowVerified V2 family with productionRouting disabled. "
            "This report never enables learner-facing routing by itself."
        ),
        "summary": dict(summary),
        "blockers": blockers,
        "bySourceType": {key: dict(value) for key, value in sorted(by_source.items())},
        "byPack": {key: dict(value) for key, value in sorted(by_pack.items())},
        "lessons": rows,
    }


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--write-report", action="store_true")
    parser.add_argument("--strict", action="store_true")
    args = parser.parse_args()

    report = audit()
    if args.write_report:
        REPORT.parent.mkdir(parents=True, exist_ok=True)
        REPORT.write_text(
            json.dumps(report, ensure_ascii=False, indent=2) + "\n",
            encoding="utf-8",
        )

    print(json.dumps(report["summary"], ensure_ascii=False, indent=2))
    if args.strict and report["summary"]["blockerCount"]:
        return 2
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
