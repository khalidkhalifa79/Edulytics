#!/usr/bin/env python3
from __future__ import annotations

import json
import re
from dataclasses import dataclass
from pathlib import Path
from typing import Any

ROOT = Path(__file__).resolve().parents[2]
RULES_FILE = ROOT / "src/Edulytics.Core/Mathematics/Curriculum/supporting-practice-target-rules.v1.json"
SKILL_REGISTRY = ROOT / "src/Edulytics.Core/Mathematics/Skills/skill-registry.v1.json"
FAMILY_REGISTRY = ROOT / "src/Edulytics.Core/Mathematics/Generation/question-family-registry.v1.json"


def read_json(path: Path) -> Any:
    return json.loads(path.read_text(encoding="utf-8-sig"))


def get_case(row: dict[str, Any], *names: str, default: Any = None) -> Any:
    for name in names:
        if name in row:
            return row[name]
    return default


def clean_list(value: Any) -> list[str]:
    if not isinstance(value, list):
        return []
    return [str(item).strip() for item in value if str(item).strip()]


def normalize_space(value: Any) -> str:
    return re.sub(r"\s+", " ", str(value or "")).strip()


def normalize_title(title: str) -> str:
    value = re.sub(r"\s*[—-]\s*advanced reasoning\s*$", "", title or "", flags=re.I)
    value = re.sub(r":\s*(?:build the idea|reason and apply)\s*$", "", value, flags=re.I)
    value = re.sub(r"^\s*consolidating\s+", "", value, flags=re.I)
    return normalize_space(value)


def choose_translation(lesson: dict[str, Any], academic_language: str = "") -> dict[str, Any]:
    rows = [
        row for row in get_case(lesson, "Translations", "translations", default=[])
        if isinstance(row, dict)
    ]
    if not rows:
        return {}
    if academic_language:
        for row in rows:
            culture = str(get_case(row, "CultureCode", "cultureCode", default="") or "")
            if culture.lower() == academic_language.lower():
                return row
    for row in rows:
        culture = str(get_case(row, "CultureCode", "cultureCode", default="") or "").lower()
        if culture.startswith("en"):
            return row
    return rows[0]


@dataclass(frozen=True)
class SupportingRule:
    rule_id: str
    title_patterns: tuple[re.Pattern[str], ...]
    code_patterns: tuple[re.Pattern[str], ...]
    skill_id: str
    mechanic: str
    families: tuple[str, ...]
    content: dict[str, str]


def load_rules() -> tuple[list[SupportingRule], list[str]]:
    doc = read_json(RULES_FILE)
    errors: list[str] = []
    result: list[SupportingRule] = []
    seen: set[str] = set()
    for row in doc.get("rules") or []:
        if not isinstance(row, dict):
            continue
        rule_id = str(row.get("id") or "").strip()
        if not rule_id:
            errors.append("Supporting Practice rule has no id.")
            continue
        if rule_id in seen:
            errors.append(f"Duplicate Supporting Practice rule id: {rule_id}")
            continue
        seen.add(rule_id)

        def compile_all(values: Any, field: str) -> tuple[re.Pattern[str], ...]:
            compiled: list[re.Pattern[str]] = []
            for raw in clean_list(values):
                try:
                    compiled.append(re.compile(raw, re.I | re.S))
                except re.error as ex:
                    errors.append(f"Invalid Supporting Practice regex {rule_id}.{field}: {raw!r}: {ex}")
            return tuple(compiled)

        title_patterns = compile_all(row.get("titlePatterns"), "titlePatterns")
        code_patterns = compile_all(row.get("codePatterns"), "codePatterns")
        if not title_patterns and not code_patterns:
            errors.append(f"Supporting Practice rule {rule_id} has no target pattern.")

        content = row.get("content") if isinstance(row.get("content"), dict) else {}
        required_content = {}
        for name in ("concept", "workedExample", "solution", "commonMistake", "summary"):
            value = str(content.get(name) or "").strip()
            if not value:
                errors.append(f"Supporting Practice rule {rule_id} has blank content.{name}.")
            required_content[name] = value

        skill_id = str(row.get("skillId") or "").strip()
        families = tuple(clean_list(row.get("families")))
        if not skill_id:
            errors.append(f"Supporting Practice rule {rule_id} has no SkillId.")
        if not families:
            errors.append(f"Supporting Practice rule {rule_id} has no families.")

        result.append(SupportingRule(
            rule_id,
            title_patterns,
            code_patterns,
            skill_id,
            str(row.get("mechanic") or "SUPPORTING_EXACT").strip(),
            families,
            required_content,
        ))
    return result, errors


def match_rule(
    lesson_code: str,
    title: str,
    rules: list[SupportingRule],
) -> SupportingRule | None:
    normalized = normalize_title(title)
    for rule in rules:
        if rule.title_patterns and not any(p.search(normalized) for p in rule.title_patterns):
            continue
        if rule.code_patterns and not any(p.search(lesson_code) for p in rule.code_patterns):
            continue
        return rule
    return None


def validate_rules(rules: list[SupportingRule]) -> list[str]:
    skills_doc = read_json(SKILL_REGISTRY)
    families_doc = read_json(FAMILY_REGISTRY)
    skills = {
        str(row.get("id") or ""): row
        for row in skills_doc.get("skills") or []
        if isinstance(row, dict) and str(row.get("id") or "")
    }
    families = {
        str(row.get("id") or ""): row
        for row in families_doc.get("families") or []
        if isinstance(row, dict) and str(row.get("id") or "")
    }

    errors: list[str] = []
    for rule in rules:
        if rule.skill_id not in skills:
            errors.append(f"Supporting Practice rule {rule.rule_id} references unknown SkillId {rule.skill_id}.")
        for family_id in rule.families:
            family = families.get(family_id)
            if family is None:
                errors.append(f"Supporting Practice rule {rule.rule_id} references missing family {family_id}.")
                continue
            if str(family.get("skillId") or "") != rule.skill_id:
                errors.append(
                    f"Supporting Practice rule {rule.rule_id} family {family_id} belongs to "
                    f"{family.get('skillId')!r}, not {rule.skill_id!r}."
                )
            if family.get("lessonPracticeRouting") is not True:
                errors.append(
                    f"Supporting Practice rule {rule.rule_id} family {family_id} is not lessonPracticeRouting."
                )
            if not str(family.get("verificationPolicy") or "").strip():
                errors.append(
                    f"Supporting Practice rule {rule.rule_id} family {family_id} has no verification policy."
                )
    return errors


def mapping_from_rule(lesson_code: str, rule: SupportingRule) -> dict[str, Any]:
    return {
        "lessonCode": lesson_code,
        "primarySkills": [rule.skill_id],
        "sourceType": "SupportingRule",
        "officialOutcomeMapped": False,
        "mappingConfidence": "ReviewedRule",
        "practiceReadiness": "READY_VERIFIED",
        "practiceMechanic": rule.mechanic,
        "allowedQuestionFamilies": list(rule.families),
        "supportingPracticeRuleId": rule.rule_id,
    }


def load_rule_mappings(content_dir: Path) -> tuple[dict[str, dict[str, Any]], list[dict[str, str]], list[str]]:
    rules, errors = load_rules()
    errors.extend(validate_rules(rules))
    mappings: dict[str, dict[str, Any]] = {}
    unmatched: list[dict[str, str]] = []
    seen: set[str] = set()

    for path in sorted(content_dir.glob("*.lesson-content-pack.json")):
        try:
            doc = read_json(path)
        except (json.JSONDecodeError, OSError) as ex:
            errors.append(f"Unable to read Supporting Practice content pack {path.name}: {ex}")
            continue

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
            if outcomes:
                continue

            translation = choose_translation(lesson, academic_language)
            title = normalize_space(
                get_case(translation, "Title", "title", default="")
                or get_case(lesson, "Title", "title", default="")
            )
            rule = match_rule(lesson_code, title, rules)
            if rule is None:
                unmatched.append({
                    "lessonCode": lesson_code,
                    "title": title,
                    "pack": path.name,
                })
                continue

            mappings[lesson_code] = mapping_from_rule(lesson_code, rule)

    return mappings, unmatched, errors
