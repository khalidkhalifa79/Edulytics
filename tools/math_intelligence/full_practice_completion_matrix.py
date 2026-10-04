#!/usr/bin/env python3
from __future__ import annotations

import argparse
import hashlib
import json
import re
from collections import Counter, defaultdict
from pathlib import Path
from typing import Any

import lesson_generation_readiness_audit
import lesson_semantic_content_audit
import lesson_skill_resolution_audit
import practice_eligibility

ROOT = Path(__file__).resolve().parents[2]
CONTENT_DIR = ROOT / "src/Edulytics.Core/Curriculum/LessonContent/Packs"
SKILL_REGISTRY = ROOT / "src/Edulytics.Core/Mathematics/Skills/skill-registry.v1.json"
FAMILY_REGISTRY = ROOT / "src/Edulytics.Core/Mathematics/Generation/question-family-registry.v1.json"
FULL_REPORT = ROOT / "artifacts/math-intelligence/full-practice-completion-matrix.json"
SUPPORTING_REPORT = ROOT / "artifacts/math-intelligence/supporting-practice-completion.json"

VISUAL_REPRESENTATIONS = {
    "diagram_metadata",
    "graph",
    "vector",
    "right_triangle",
    "angle",
    "coordinate_pair",
    "solid_dimensions",
}


def read_json(path: Path) -> Any:
    return json.loads(path.read_text(encoding="utf-8-sig"))


def clean_list(value: Any) -> list[str]:
    if not isinstance(value, list):
        return []
    return [str(item).strip() for item in value if str(item).strip()]


def normalize_target(title: str) -> str:
    value = re.sub(
        r"\s*[:—-]\s*(Build the Idea|Reason and Apply|advanced reasoning)\s*$",
        "",
        title or "",
        flags=re.IGNORECASE,
    )
    value = re.sub(r"^\s*Consolidating\s+", "", value, flags=re.IGNORECASE)
    return re.sub(r"\s+", " ", value).strip()


def cluster_id(title: str) -> str:
    normalized = normalize_target(title).lower()
    digest = hashlib.sha256(normalized.encode("utf-8")).hexdigest()[:16]
    return f"practice.target.{digest}"


def blocker_codes(
    readiness: str,
    semantic: str,
    skill_status: str,
    approved_mapping: bool,
    reviewed_official_mapping: bool,
    has_family: bool,
    solver_ready: bool,
) -> list[str]:
    result: list[str] = []
    if semantic == "CONTENT_WEAK":
        result.append("CONTENT_WEAK")
    elif semantic == "REVIEW_REQUIRED":
        result.append("CONTENT_REVIEW_REQUIRED")
    elif semantic == "UNCLASSIFIED" and not reviewed_official_mapping:
        result.append("CONTENT_TARGET_UNCLASSIFIED")

    if skill_status == "AMBIGUOUS":
        result.append("MAPPING_CONFLICT")
    elif skill_status in {"REVIEW_REQUIRED", "HIGH_CONFIDENCE_CANDIDATE"}:
        result.append("ACADEMIC_REVIEW_REQUIRED")
    elif skill_status == "ONTOLOGY_GAP":
        result.append("ACADEMIC_SKILL_PROMOTION_REQUIRED")

    if not approved_mapping:
        result.append("APPROVED_MAPPING_MISSING")
    if approved_mapping and not has_family:
        result.append("QUESTION_FAMILY_MISSING")
    if approved_mapping and has_family and not solver_ready:
        result.append("SOLVER_CAPABILITY_MISSING")

    if readiness == "REPRESENTATION_MISSING":
        result.append("REPRESENTATION_MISSING")
    if readiness == "SKILL_AMBIGUOUS":
        result.append("MAPPING_CONFLICT")
    if readiness == "REQUIRES_ACADEMIC_REVIEW":
        result.append("ACADEMIC_REVIEW_REQUIRED")
    if readiness == "CONTENT_WEAK":
        result.append("CONTENT_WEAK")
    if readiness == "QUESTION_FAMILY_MISSING":
        result.append("QUESTION_FAMILY_MISSING")
    if readiness == "SOLVER_CAPABILITY_MISSING":
        result.append("SOLVER_CAPABILITY_MISSING")

    return sorted(set(result))


def load_practice_eligibility() -> dict[str, practice_eligibility.PracticeEligibility]:
    result: dict[str, practice_eligibility.PracticeEligibility] = {}
    for path in sorted(CONTENT_DIR.glob("*.lesson-content-pack.json")):
        pack = read_json(path)
        lessons = pack.get("Lessons") or pack.get("lessons") or []
        if not isinstance(lessons, list):
            continue
        for lesson in lessons:
            if not isinstance(lesson, dict):
                continue
            code = str(lesson.get("LessonCode") or lesson.get("lessonCode") or "").strip()
            if not code or code in result:
                continue
            result[code] = practice_eligibility.classify_lesson(pack, lesson)
    return result


def audit() -> dict[str, Any]:
    readiness = lesson_generation_readiness_audit.audit()
    semantic = lesson_semantic_content_audit.audit()
    skill = lesson_skill_resolution_audit.audit()
    eligibility_by_code = load_practice_eligibility()

    skills_doc = read_json(SKILL_REGISTRY)
    families_doc = read_json(FAMILY_REGISTRY)
    skills = {
        str(row.get("id") or ""): row
        for row in skills_doc.get("skills", [])
        if isinstance(row, dict) and str(row.get("id") or "")
    }
    families = {
        str(row.get("id") or ""): row
        for row in families_doc.get("families", [])
        if isinstance(row, dict) and str(row.get("id") or "")
    }

    semantic_by_code = {row["lessonCode"]: row for row in semantic["lessons"]}
    skill_by_code = {row["lessonCode"]: row for row in skill["lessons"]}

    rows: list[dict[str, Any]] = []
    summary = Counter()
    by_pack: dict[str, Counter] = defaultdict(Counter)
    by_domain: dict[str, Counter] = defaultdict(Counter)
    internal_blockers: list[str] = []

    for row in readiness["lessons"]:
        code = str(row["lessonCode"])
        semantic_row = semantic_by_code.get(code, {})
        skill_row = skill_by_code.get(code, {})
        title = str(row.get("title") or semantic_row.get("title") or skill_row.get("title") or "")
        primary_skills = clean_list(row.get("approvedPrimarySkills"))

        question_families = clean_list(row.get("approvedQuestionFamilies"))
        representations: set[str] = set()
        domains: set[str] = set()
        for skill_id in primary_skills:
            skill_doc = skills.get(skill_id)
            if skill_doc is None:
                internal_blockers.append(f"{code}: approved SkillId {skill_id} missing from registry")
                continue
            domains.add(str(skill_doc.get("domain") or "unknown"))

        for family_id in question_families:
            family = families.get(family_id)
            if family is None:
                internal_blockers.append(f"{code}: approved family {family_id} missing from registry")
                continue
            if str(family.get("skillId") or "") not in primary_skills:
                internal_blockers.append(
                    f"{code}: approved family {family_id} is not bound to an approved primary SkillId"
                )
            representations.update(clean_list(family.get("representations")))

        readiness_state = str(row.get("generationReadiness") or "UNRESOLVED")
        semantic_status = str(row.get("semanticContentStatus") or "UNCLASSIFIED")
        skill_status = str(row.get("skillResolutionStatus") or "UNRESOLVED")
        approved_mapping = bool(row.get("approvedMapping"))
        reviewed_official_mapping = bool(row.get("reviewedOfficialMapping"))
        has_family = bool(row.get("hasQuestionFamily"))
        solver_ready = bool(row.get("hasVerifiedSolverCapability"))
        visual_required = bool(representations & VISUAL_REPRESENTATIONS)
        visual_status = "READY_METADATA" if visual_required and representations else (
            "NOT_REQUIRED" if not visual_required else "MISSING"
        )
        diagnostic_blockers = blocker_codes(
            readiness_state,
            semantic_status,
            skill_status,
            approved_mapping,
            reviewed_official_mapping,
            has_family,
            solver_ready,
        )
        runtime_contract_ready = (
            skill_status == "EXISTING_VERIFIED_MAPPING"
            and approved_mapping
            and has_family
            and solver_ready
        )
        eligibility = eligibility_by_code.get(code)
        if eligibility is None:
            internal_blockers.append(f"{code}: missing Practice eligibility classification")
            eligibility_status = "UNCLASSIFIED"
            eligibility_reason_code = "MISSING"
            eligibility_evidence: list[str] = []
        else:
            eligibility_status = eligibility.status
            eligibility_reason_code = eligibility.reason_code
            eligibility_evidence = list(eligibility.evidence)

        if eligibility_status == "NON_STANDALONE_WITH_EVIDENCE":
            if not eligibility_evidence:
                internal_blockers.append(
                    f"{code}: NON_STANDALONE_WITH_EVIDENCE has no evidence"
                )
            blockers: list[str] = []
            terminal = "NON_STANDALONE_WITH_EVIDENCE"
        else:
            blockers = [] if runtime_contract_ready else diagnostic_blockers
            terminal = (
                "READY_VERIFIED"
                if runtime_contract_ready
                else "BLOCKED_TEMPORARY"
            )
        source_type = str(row.get("sourceType") or "Unknown")
        pack_code = str(row.get("packCode") or "")
        domain = ",".join(sorted(domains)) if domains else "unresolved"

        summary["lessonCount"] += 1
        summary[terminal] += 1
        if eligibility_status == "PRACTICE_ELIGIBLE":
            summary["practiceEligibleLessonCount"] += 1
            if terminal == "BLOCKED_TEMPORARY":
                summary["practiceEligibleBlockedCount"] += 1
        elif eligibility_status == "NON_STANDALONE_WITH_EVIDENCE":
            summary["nonStandaloneLessonCount"] += 1
        else:
            summary["eligibilityUnclassifiedCount"] += 1
        if source_type == "PedagogicalUnmapped":
            summary["supportingLessonCount"] += 1
            summary[f"supporting:{terminal}"] += 1
        if visual_required:
            summary["visualRequiredCount"] += 1
        for blocker in blockers:
            summary[f"blocker:{blocker}"] += 1

        by_pack[pack_code]["lessonCount"] += 1
        by_pack[pack_code][terminal] += 1
        by_domain[domain]["lessonCount"] += 1
        by_domain[domain][terminal] += 1

        rows.append({
            "lessonCode": code,
            "packCode": pack_code,
            "sourceType": source_type,
            "title": title,
            "practiceEligibility": eligibility_status,
            "practiceEligibilityReasonCode": eligibility_reason_code,
            "practiceEligibilityEvidence": eligibility_evidence,
            "canonicalTarget": normalize_target(title),
            "remediationCluster": cluster_id(title),
            "skillResolutionStatus": skill_status,
            "semanticContentStatus": semantic_status,
            "approvedMapping": approved_mapping,
            "primarySkills": primary_skills,
            "domains": sorted(domains),
            "questionFamilies": question_families,
            "representations": sorted(representations),
            "visualRequired": visual_required,
            "visualStatus": visual_status,
            "solverReady": solver_ready,
            "verifierReady": solver_ready,
            "generationReadiness": readiness_state,
            "terminalPracticeStatus": terminal,
            "blockerCodes": blockers,
            "diagnosticBlockerCodes": diagnostic_blockers,
            "reasons": row.get("reasons") or [],
        })

    supporting_rows = [row for row in rows if row["sourceType"] == "PedagogicalUnmapped"]
    supporting_summary = Counter()
    for row in supporting_rows:
        supporting_summary["supportingLessonCount"] += 1
        supporting_summary[row["terminalPracticeStatus"]] += 1
        for blocker in row["blockerCodes"]:
            supporting_summary[f"blocker:{blocker}"] += 1

    summary["skillRegistryCount"] = len(skills)
    summary["questionFamilyRegistryCount"] = len(families)
    summary["internalBlockerCount"] = len(internal_blockers)
    summary["completionPercent"] = round(
        100 * summary["READY_VERIFIED"] / max(1, summary["practiceEligibleLessonCount"]),
        2,
    )

    return {
        "schemaVersion": 1,
        "audit": "Edulytics Mathematics Practice completion matrix",
        "authority": (
            "BLOCKED_TEMPORARY is protective and is never counted as programme completion. "
            "Only READY_VERIFIED is a successful terminal Practice state."
        ),
        "summary": dict(summary),
        "byPack": {key: dict(value) for key, value in sorted(by_pack.items())},
        "byDomain": {key: dict(value) for key, value in sorted(by_domain.items())},
        "internalBlockers": internal_blockers,
        "lessons": rows,
        "supporting": {
            "summary": dict(supporting_summary),
            "lessons": supporting_rows,
        },
    }


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--write-report", action="store_true")
    parser.add_argument("--strict", action="store_true")
    parser.add_argument("--require-complete", action="store_true")
    args = parser.parse_args()

    report = audit()
    if args.write_report:
        FULL_REPORT.parent.mkdir(parents=True, exist_ok=True)
        FULL_REPORT.write_text(
            json.dumps(report, ensure_ascii=False, indent=2) + "\n",
            encoding="utf-8",
        )
        SUPPORTING_REPORT.write_text(
            json.dumps(report["supporting"], ensure_ascii=False, indent=2) + "\n",
            encoding="utf-8",
        )

    print(json.dumps(report["summary"], ensure_ascii=False, indent=2))

    if args.strict and (
        report["summary"]["internalBlockerCount"]
        or report["summary"].get("eligibilityUnclassifiedCount", 0)
    ):
        return 2
    if args.require_complete and report["summary"].get("practiceEligibleBlockedCount", 0):
        return 3
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
