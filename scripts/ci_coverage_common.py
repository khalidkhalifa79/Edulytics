from __future__ import annotations

import re
import xml.etree.ElementTree as ET
from pathlib import Path


def fail(message: str) -> None:
    raise SystemExit(f"FAIL: {message}")


def find_coverage_files(root: Path) -> list[Path]:
    files = sorted(
        root.rglob(
            "coverage.cobertura.xml")
    )

    if not files:
        fail(
            f"no coverage.cobertura.xml found under {root}"
        )

    return files


def summarize_coverage(root: Path) -> dict[str, object]:
    files = find_coverage_files(root)

    # Coverlet can emit more than one Cobertura shard for a single test
    # invocation. Merge by stable source-line identity instead of assuming
    # exactly one attachment or arbitrarily choosing one file.
    lines: dict[
        tuple[str, str, int],
        int
    ] = {}

    branches: dict[
        tuple[str, str, int],
        tuple[int, int]
    ] = {}

    condition_pattern = re.compile(
        r"\((\d+)\s*/\s*(\d+)\)"
    )

    for path in files:
        document = ET.parse(path).getroot()

        classes = list(
            document.findall(
                ".//class")
        )

        if not classes:
            fail(
                f"Cobertura report contains no classes: {path}"
            )

        for klass in classes:
            filename = (
                klass.attrib.get(
                    "filename")
                or ""
            ).replace("\\", "/")

            class_name = (
                klass.attrib.get(
                    "name")
                or ""
            )

            for line in klass.findall(
                "./lines/line"
            ):
                number_text = (
                    line.attrib.get(
                        "number")
                    or ""
                )

                if not number_text.isdigit():
                    fail(
                        "invalid Cobertura line number "
                        f"{number_text!r} in {path}"
                    )

                number = int(
                    number_text
                )

                key = (
                    filename,
                    class_name,
                    number
                )

                try:
                    hits = int(
                        line.attrib.get(
                            "hits",
                            "0")
                    )
                except ValueError as exc:
                    fail(
                        f"invalid hit count in {path}: {exc}"
                    )

                lines[key] = max(
                    lines.get(
                        key,
                        0),
                    hits
                )

                if (
                    line.attrib.get(
                        "branch",
                        "false")
                    .lower()
                    == "true"
                ):
                    coverage_text = (
                        line.attrib.get(
                            "condition-coverage")
                        or ""
                    )

                    match = (
                        condition_pattern
                        .search(
                            coverage_text)
                    )

                    if match is None:
                        fail(
                            "branch line is missing "
                            "condition-coverage counts: "
                            f"{path}:{filename}:{number}"
                        )

                    covered = int(
                        match.group(1)
                    )

                    total = int(
                        match.group(2)
                    )

                    previous = branches.get(
                        key,
                        (0, 0)
                    )

                    # Duplicate shards can contain the same sequence point.
                    # The maximum observed covered/total counts preserve one
                    # logical branch site instead of double counting it.
                    branches[key] = (
                        max(
                            previous[0],
                            covered),
                        max(
                            previous[1],
                            total)
                    )

    line_total = len(lines)
    line_covered = sum(
        1
        for hits in lines.values()
        if hits > 0
    )

    branch_covered = sum(
        covered
        for covered, _ in branches.values()
    )

    branch_total = sum(
        total
        for _, total in branches.values()
    )

    if line_total <= 0:
        fail(
            "merged coverage contains zero executable lines"
        )

    line_percent = (
        line_covered
        / line_total
        * 100.0
    )

    branch_percent = (
        (
            branch_covered
            / branch_total
            * 100.0
        )
        if branch_total > 0
        else 100.0
    )

    return {
        "files": files,
        "file_count": len(files),
        "line_covered": line_covered,
        "line_total": line_total,
        "line_percent": line_percent,
        "branch_covered": branch_covered,
        "branch_total": branch_total,
        "branch_percent": branch_percent,
    }


def read_trx_counts(
    trx_path: Path
) -> dict[str, int]:
    if not trx_path.is_file():
        fail(
            f"TRX not found: {trx_path}"
        )

    root = ET.parse(
        trx_path
    ).getroot()

    counters = None

    for element in root.iter():
        if element.tag.endswith(
            "Counters"
        ):
            counters = element
            break

    if counters is None:
        fail(
            f"TRX Counters element missing: {trx_path}"
        )

    names = (
        "total",
        "executed",
        "passed",
        "failed",
        "error",
        "timeout",
        "aborted",
        "inconclusive",
        "notExecuted",
    )

    result: dict[str, int] = {}

    for name in names:
        try:
            result[name] = int(
                counters.attrib.get(
                    name,
                    "0")
            )
        except ValueError as exc:
            fail(
                f"invalid TRX counter {name}: {exc}"
            )

    return result
