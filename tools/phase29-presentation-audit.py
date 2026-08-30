#!/usr/bin/env python3

import json
import pathlib
import re

ROOT = pathlib.Path(__file__).resolve().parents[1]
PACKS = (
    ROOT /
    "src/Edulytics.Core/Curriculum/LessonContent/Packs"
)

DESCRIPTION = re.compile(
    r"Description\s*:\s*(?:<p>)?(.*?)(?:</p>|\n\n|$)",
    re.I | re.S,
)

def classify(text: str):
    value = text.lower()

    if (
        "line with three markings" in value
        and "first mark" in value
        and "second mark" in value
        and "third mark" in value
    ):
        return "measured-path"

    if "double number line" in value:
        top = (
            "top line" in value
            or "top number line" in value
        )
        bottom = (
            "bottom line" in value
            or "bottom number line" in value
        )
        values = (
            "labels:" in value
            or "numbers " in value
        )

        if top and bottom and values:
            return "double-number-line"

        return None

    if (
        "four drawings" in value
        and "shape a" in value
        and "shape b" in value
        and "shape c" in value
        and "shape d" in value
        and "squares" in value
    ):
        return "area-four-panels"

    if "tangram" in value:
        return "area-tangram"

    if (
        ("decompos" in value or "rearrang" in value)
        and "area" in value
    ):
        return "area-rearrangement"

    if (
        "coordinate plane" in value
        and (
            "horizontal axis" in value
            or "vertical axis" in value
            or "x-axis" in value
            or "y-axis" in value
        )
    ):
        return "coordinate-plane"

    if (
        "number line" in value
        and "double number line" not in value
        and len(re.findall(r"-?\d+(?:\.\d+)?", value)) >= 2
    ):
        return "number-line"

    if (
        "array" in value
        or "grid of squares" in value
    ):
        return "array-grid"

    if (
        "fraction bar" in value
        or "ratio bar" in value
        or "tape diagram" in value
    ):
        return "segmented-bar"

    if any(
        x in value
        for x in (
            "triangle",
            "rectangle",
            "quadrilateral",
            "polygon",
        )
    ):
        return "geometry"

    return None

lessons = 0
raw_markup_fields = 0
description_passages = 0
mapped = 0
affected = set()
unsupported = []

for path in sorted(
    PACKS.glob(
        "us-ccss-*.lesson-content-pack.json")
):
    data = json.loads(
        path.read_text(
            encoding="utf-8")
    )

    for lesson in data["lessons"]:
        lessons += 1

        code = lesson["lessonCode"]

        for translation in lesson["translations"]:
            for field in (
                "explanation",
                "keyConceptsAndRules",
                "workedExamples",
                "stepByStepSolutions",
                "commonMistakes",
                "quickSummary",
            ):
                value = translation.get(field, "")

                if re.search(
                    r"</?(?:p|br)\b",
                    value,
                    re.I,
                ):
                    raw_markup_fields += 1

                for match in DESCRIPTION.finditer(value):
                    description_passages += 1
                    affected.add(code)

                    text = re.sub(
                        r"<[^>]*>",
                        "",
                        match.group(1),
                    ).strip()

                    kind = classify(text)

                    if kind:
                        mapped += 1
                    else:
                        unsupported.append(
                            {
                                "lessonCode": code,
                                "field": field,
                                "description": text,
                            }
                        )

result = {
    "lessonsSeen": lessons,
    "rawMarkupFieldsHandledAtPresentation": raw_markup_fields,
    "explicitDescriptionPassages": description_passages,
    "safelyMappedVisualDescriptions": mapped,
    "unsupportedVisualDescriptionCount": len(unsupported),
    "affectedLessonCodeCount": len(affected),
    "fakeGenericVisualFallback": False,
    "runtimeAiRequired": False,
    "paidApiRequired": False,
    "unsupportedVisualDescriptions": unsupported,
}

print(
    json.dumps(
        result,
        indent=2,
        ensure_ascii=False,
    )
)
