#!/usr/bin/env python3

from __future__ import annotations

import xml.etree.ElementTree as ET
from pathlib import Path


ROOT = Path("src")


def fail(message: str) -> None:
    raise SystemExit(f"FAIL: {message}")


projects = {
    "Core": ROOT / "Edulytics.Core" / "Edulytics.Core.csproj",
    "Services": ROOT / "Edulytics.Services" / "Edulytics.Services.csproj",
    "Data": ROOT / "Edulytics.Data" / "Edulytics.Data.csproj",
    "Web": ROOT / "Edulytics.Web" / "Edulytics.Web.csproj",
}

allowed = {
    "Core": set(),
    "Services": {"Edulytics.Core"},
    "Data": {"Edulytics.Core"},
    "Web": {"Edulytics.Services", "Edulytics.Data"},
}


def project_refs(path: Path) -> set[str]:
    root = ET.parse(path).getroot()
    result: set[str] = set()

    for item in root.iter():
        if not item.tag.endswith("ProjectReference"):
            continue

        include = item.attrib.get("Include")

        if not include:
            continue

        result.add(
            Path(include).stem
        )

    return result


for name, path in projects.items():
    if not path.is_file():
        fail(f"project missing: {path}")

    actual = project_refs(path)

    if actual != allowed[name]:
        fail(
            f"{name} project reference boundary changed: "
            f"actual={sorted(actual)} "
            f"allowed={sorted(allowed[name])}"
        )

core_text = "\n".join(
    path.read_text(
        encoding="utf-8-sig",
        errors="replace"
    )
    for path in (
        ROOT / "Edulytics.Core"
    ).rglob("*.cs")
)

for forbidden in (
    "Microsoft.EntityFrameworkCore",
    "Edulytics.Data.",
    "Edulytics.Services.",
    "Edulytics.Web.",
):
    if forbidden in core_text:
        fail(
            f"Core architecture violation: {forbidden}"
        )

services_text = "\n".join(
    path.read_text(
        encoding="utf-8-sig",
        errors="replace"
    )
    for path in (
        ROOT / "Edulytics.Services"
    ).rglob("*.cs")
)

for forbidden in (
    "Edulytics.Data.",
    "Edulytics.Web.",
):
    if forbidden in services_text:
        fail(
            f"Services architecture violation: {forbidden}"
        )

data_text = "\n".join(
    path.read_text(
        encoding="utf-8-sig",
        errors="replace"
    )
    for path in (
        ROOT / "Edulytics.Data"
    ).rglob("*.cs")
)

for forbidden in (
    "Edulytics.Services.",
    "Edulytics.Web.",
):
    if forbidden in data_text:
        fail(
            f"Data architecture violation: {forbidden}"
        )

print(
    "PASS: architecture reference/source boundaries"
)
