#!/usr/bin/env python3
"""Install the accepted Phase 29 English objects into canonical CCSS packs."""

import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
MANIFEST = ROOT / ".phase29-source-rebuild/translation-manifest/en-to-pl.json"
SOURCE_MAP = ROOT / ".phase29-source-rebuild/exact-lesson-source-map.json"


def read(path):
    return json.loads(path.read_text(encoding="utf-8-sig"))


def write(path, value):
    path.write_text(json.dumps(value, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")


manifest = read(MANIFEST)
rows = {row["lessonCode"]: row for row in manifest["rows"]}
source_map = read(SOURCE_MAP)
mapped = {row["lessonCode"]: row for row in source_map["lessons"]}
pack_by_blueprint = {}
for source in source_map["lessons"]:
    if source.get("canonicalContentPackPath"):
        previous = pack_by_blueprint.setdefault(source["blueprintPath"], source["canonicalContentPackPath"])
        if previous != source["canonicalContentPackPath"]:
            raise RuntimeError(f'Multiple canonical packs for {source["blueprintPath"]}')

paths = sorted((ROOT / "src/Edulytics.Core/Curriculum/LessonContent/Packs").glob("us-ccss-*.lesson-content-pack.json"))
documents = {str(path.relative_to(ROOT)): (path, read(path)) for path in paths}

for relative, (_, document) in documents.items():
    document["academicLanguage"] = "en"
    document["curriculumTranslationRequired"] = False
    document["contentVersion"] = "phase29-source-faithful-en-final-v1"
    document["lessons"] = []

for lesson_code, row in rows.items():
    source = mapped[lesson_code]
    relative = source.get("canonicalContentPackPath") or pack_by_blueprint.get(source["blueprintPath"])
    if relative not in documents:
        raise RuntimeError(f"Unknown canonical pack for {lesson_code}: {relative}")
    document = documents[relative][1]
    locator = source.get("sourceLocator") or {}
    lesson = {
        "lessonCode": lesson_code,
        "titleProvenance": "PedagogicalSource",
        "titleSourceReference": f'{source.get("sourceLessonCode", "")} — {source["sourceUrl"]}',
        "outcomeCodes": source.get("outcomeCodes", []),
        "isSupporting": row["lessonType"] == "SUPPORTING",
        "sourceUrl": source["sourceUrl"],
        "sourceLocator": json.dumps(locator, ensure_ascii=False, sort_keys=True) if locator else source.get("sourceLessonCode", ""),
        "sourceTitle": source.get("sourceTitle") or document["pedagogicalSourceTitle"],
        "sourcePublisher": source.get("sourcePublisher") or document["pedagogicalSourcePublisher"],
        "sourceEdition": source.get("sourceEdition") or document["pedagogicalSourceEdition"],
        "sourceRights": source.get("declaredSourceLicense") or document["pedagogicalSourceRightsNote"],
        "sourceSha256": source["artifactSha256"],
        "canonicalBodySha256": row["sourceSha256"],
        "sourceVerifiedAtUtc": source.get("retrievalTimestamp") or document["pedagogicalSourceCheckedAtUtc"],
        "retrievalUrl": source.get("retrievalUrl", source["sourceUrl"]),
        "retrievalChannel": source.get("retrievalChannel", ""),
        "retrievalTimestamp": source.get("retrievalTimestamp", ""),
        "adaptationStatus": "SOURCE_FAITHFUL_ADAPTED" if row["lessonType"] == "STANDALONE" else "SOURCE_FAITHFUL_SUPPORTING",
        "translations": [{"cultureCode": "en", **row["en"]}],
    }
    if lesson["isSupporting"] and lesson["outcomeCodes"]:
        raise RuntimeError(f"Supporting lesson has outcomes: {lesson_code}")
    documents[relative][1]["lessons"].append(lesson)

for path, document in documents.values():
    document["lessons"].sort(key=lambda lesson: lesson["lessonCode"])
    write(path, document)

if len(rows) != 1560 or sum(r["lessonType"] == "STANDALONE" for r in rows.values()) != 1466 or sum(r["lessonType"] == "SUPPORTING" for r in rows.values()) != 94:
    raise RuntimeError("Accepted manifest coverage changed")

print(f"Updated {len(paths)} Common Core packs with {len(rows)} English lessons")
