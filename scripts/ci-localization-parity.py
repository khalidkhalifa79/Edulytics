#!/usr/bin/env python3

from __future__ import annotations

import sys
import xml.etree.ElementTree as ET
from pathlib import Path


ROOT = Path(
    "src/Edulytics.Web/Resources"
)


def fail(message: str) -> None:
    raise SystemExit(f"FAIL: {message}")


def keys(path: Path) -> set[str]:
    root = ET.parse(path).getroot()
    result: set[str] = set()

    for data in root.findall("data"):
        name = data.attrib.get("name")

        if not name:
            fail(
                f"resource entry without name: {path}"
            )

        if name in result:
            fail(
                f"duplicate resource key {name}: {path}"
            )

        result.add(name)

        value = data.find("value")

        if (
            value is None
            or value.text is None
            or not value.text.strip()
        ):
            fail(
                f"empty localized value "
                f"{name}: {path}"
            )

    return result


if not ROOT.is_dir():
    fail(f"resource directory missing: {ROOT}")

defaults = sorted(
    path
    for path in ROOT.rglob("*.resx")
    if not path.name.endswith(".pl.resx")
)

if not defaults:
    fail("no default resource files found")

pair_count = 0
key_count = 0

for default in defaults:
    polish = default.with_name(
        default.stem + ".pl.resx"
    )

    if not polish.is_file():
        fail(
            f"Polish resource counterpart missing: "
            f"{polish}"
        )

    default_keys = keys(default)
    polish_keys = keys(polish)

    missing_pl = sorted(
        default_keys - polish_keys
    )

    orphan_pl = sorted(
        polish_keys - default_keys
    )

    if missing_pl or orphan_pl:
        fail(
            f"resource parity mismatch: {default}; "
            f"missing_pl={missing_pl}; "
            f"orphan_pl={orphan_pl}"
        )

    pair_count += 1
    key_count += len(default_keys)

for polish in ROOT.rglob("*.pl.resx"):
    base = polish.with_name(
        polish.name.replace(
            ".pl.resx",
            ".resx"
        )
    )

    if not base.is_file():
        fail(
            f"orphan Polish resource: {polish}"
        )

print(
    "PASS: EN/default ↔ PL resource parity "
    f"pairs={pair_count}, keys={key_count}"
)
