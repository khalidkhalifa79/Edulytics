#!/usr/bin/env python3
from __future__ import annotations

import argparse
import json
import os
from pathlib import Path
from typing import Any

import full_practice_completion_matrix
from polish_practice_map import load_polish_outcome_mappings

ROOT = Path(__file__).resolve().parents[2]
CURRICULUM_DIR = ROOT / "src/Edulytics.Core/Curriculum/LessonContent/Packs"
SOURCE_EVIDENCE = (
    ROOT
    / "src/Edulytics.Core/Mathematics/Curriculum"
    / "polish-outcome-source-evidence.v1.json"
)
REPORT = ROOT / "artifacts/math-intelligence/pl-1569-final-closure-audit.json"


def read_json(path: Path) -> Any:
    return json.loads(path.read_text(encoding="utf-8-sig"))


def commit_sha() -> str:
    return os.environ.get("GITHUB_SHA", "").strip() or "local-uncommitted"


def polish_lessons() -> list[dict[str, Any]]:
    result: list[dict[str, Any]] = []
    for path in sorted(CURRICULUM_DIR.glob("*.lesson-content-pack.json")):
        doc = read_json(path)
        if str(doc.get("PackCode") or "") != "PL-NATIONAL-MATH":
            continue
        lessons = doc.get("Lessons") or []
        if not isinstance(lessons, list):
            continue
        for row in lessons:
            if isinstance(row, dict):
                result.append(row)
    return result


def audit() -> dict[str, Any]:
    blockers: list[str] = []

    mappings, mapping_errors = load_polish_outcome_mappings()
    blockers.extend(f"mapping: {message}" for message in mapping_errors)

    evidence_doc = read_json(SOURCE_EVIDENCE)
    evidence_rows = evidence_doc.get("entries") or []
    evidence_by_code = {
        str(row.get("outcomeCode") or ""): row
        for row in evidence_rows
        if isinstance(row, dict) and str(row.get("outcomeCode") or "").strip()
    }

    if int(evidence_doc.get("outcomeCount") or 0) != 306:
        blockers.append(
            "source evidence declared outcomeCount must equal 306"
        )
    if len(evidence_by_code) != 306:
        blockers.append(
            f"source evidence must contain 306 unique outcomes; got {len(evidence_by_code)}"
        )
    if set(mappings) != set(evidence_by_code):
        missing_map = sorted(set(evidence_by_code) - set(mappings))
        missing_evidence = sorted(set(mappings) - set(evidence_by_code))
        if missing_map:
            blockers.append(
                "official outcomes missing Practice mapping: " + ", ".join(missing_map)
            )
        if missing_evidence:
            blockers.append(
                "Practice mappings missing official evidence: " + ", ".join(missing_evidence)
            )

    lessons = polish_lessons()
    if len(lessons) != 1569:
        blockers.append(
            f"Polish learner lesson count must equal 1569; got {len(lessons)}"
        )

    lesson_codes: set[str] = set()
    for lesson in lessons:
        code = str(lesson.get("LessonCode") or "").strip()
        if not code:
            blockers.append("Polish lesson has blank LessonCode")
            continue
        if code in lesson_codes:
            blockers.append(f"duplicate Polish LessonCode: {code}")
        lesson_codes.add(code)

        if bool(lesson.get("IsSupporting")):
            blockers.append(f"{code}: Polish remediation target must not be Supporting")

        outcomes = [
            str(value).strip()
            for value in (lesson.get("OutcomeCodes") or [])
            if str(value).strip()
        ]
        if len(outcomes) != 1:
            blockers.append(
                f"{code}: expected exactly one official OutcomeCode, got {len(outcomes)}"
            )
            continue

        outcome = outcomes[0]
        if outcome not in mappings:
            blockers.append(f"{code}: exact Practice mapping missing for {outcome}")
        if outcome not in evidence_by_code:
            blockers.append(f"{code}: official source evidence missing for {outcome}")

    matrix = full_practice_completion_matrix.audit()
    rows = list(matrix.get("lessons") or [])
    polish_rows = [
        row for row in rows
        if str(row.get("packCode") or "") == "PL-NATIONAL-MATH"
    ]
    if len(polish_rows) != 1569:
        blockers.append(
            f"completion matrix must contain 1569 Polish rows; got {len(polish_rows)}"
        )

    for row in polish_rows:
        code = str(row.get("lessonCode") or "")
        if row.get("practiceEligibility") != "PRACTICE_ELIGIBLE":
            blockers.append(
                f"{code}: practiceEligibility={row.get('practiceEligibility')}"
            )
        if row.get("practiceEligibilityReasonCode") != "POLISH_EXACT_OUTCOME_MAP":
            blockers.append(
                f"{code}: eligibility reason is not POLISH_EXACT_OUTCOME_MAP"
            )
        if row.get("terminalPracticeStatus") != "READY_VERIFIED":
            blockers.append(
                f"{code}: terminalPracticeStatus={row.get('terminalPracticeStatus')}"
            )
        if not bool(row.get("approvedMapping")):
            blockers.append(f"{code}: approved mapping missing")
        if not list(row.get("primarySkills") or []):
            blockers.append(f"{code}: primary skills missing")
        if not list(row.get("questionFamilies") or []):
            blockers.append(f"{code}: question families missing")
        if not bool(row.get("solverReady")):
            blockers.append(f"{code}: solver not ready")
        if not bool(row.get("verifierReady")):
            blockers.append(f"{code}: verifier not ready")
        if list(row.get("blockerCodes") or []):
            blockers.append(
                f"{code}: blockers={','.join(str(x) for x in row.get('blockerCodes') or [])}"
            )

    summary = matrix.get("summary") or {}
    required_global = {
        "lessonCount": 5110,
        "practiceEligibleLessonCount": 5110,
        "READY_VERIFIED": 5110,
        "internalBlockerCount": 0,
    }
    for key, expected in required_global.items():
        actual = int(summary.get(key, 0))
        if actual != expected:
            blockers.append(
                f"global matrix {key} expected {expected}, got {actual}"
            )

    if int(summary.get("nonStandaloneLessonCount", 0)) != 0:
        blockers.append(
            "learner-facing full catalogue must have zero non-standalone lesson exclusions"
        )
    if int(summary.get("practiceEligibleBlockedCount", 0)) != 0:
        blockers.append(
            "full catalogue must have zero Practice-eligible blocked lessons"
        )

    return {
        "schemaVersion": 1,
        "audit": "Polish National Mathematics 1,569 lesson final Practice closure",
        "commitSha": commit_sha(),
        "officialOutcomeCount": len(evidence_by_code),
        "mappedOutcomeCount": len(mappings),
        "polishLearnerLessonCount": len(lessons),
        "polishMatrixLessonCount": len(polish_rows),
        "polishReadyVerifiedCount": sum(
            row.get("terminalPracticeStatus") == "READY_VERIFIED"
            for row in polish_rows
        ),
        "catalogueLessonCount": int(summary.get("lessonCount", 0)),
        "cataloguePracticeEligibleCount": int(
            summary.get("practiceEligibleLessonCount", 0)
        ),
        "catalogueReadyVerifiedCount": int(summary.get("READY_VERIFIED", 0)),
        "nonStandaloneLessonCount": int(
            summary.get("nonStandaloneLessonCount", 0)
        ),
        "practiceEligibleBlockedCount": int(
            summary.get("practiceEligibleBlockedCount", 0)
        ),
        "blockerCount": len(blockers),
        "blockers": sorted(set(blockers)),
    }


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--strict", action="store_true")
    parser.add_argument("--write-report", action="store_true")
    args = parser.parse_args()

    report = audit()
    if args.write_report:
        REPORT.parent.mkdir(parents=True, exist_ok=True)
        REPORT.write_text(
            json.dumps(report, ensure_ascii=False, indent=2) + "\n",
            encoding="utf-8",
        )

    print(json.dumps(report, ensure_ascii=False, indent=2))
    if args.strict and report["blockerCount"]:
        return 2
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
