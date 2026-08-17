#!/usr/bin/env python3

from __future__ import annotations

import sys
import xml.etree.ElementTree as ET
from pathlib import Path


def fail(message: str) -> None:
    raise SystemExit(f"FAIL: {message}")


if len(sys.argv) != 2:
    fail("usage: ci-trx-gate.py <trx>")

path = Path(sys.argv[1])

if not path.is_file():
    fail(f"TRX not found: {path}")

root = ET.parse(path).getroot()

counters = None

for element in root.iter():
    if element.tag.endswith("Counters"):
        counters = element
        break

if counters is None:
    fail(f"TRX Counters element missing: {path}")

total = int(counters.attrib.get("total", "0"))
failed = int(counters.attrib.get("failed", "0"))
error = int(counters.attrib.get("error", "0"))
timeout = int(counters.attrib.get("timeout", "0"))
aborted = int(counters.attrib.get("aborted", "0"))

if total <= 0:
    fail(f"TRX contains zero tests: {path}")

if any((failed, error, timeout, aborted)):
    fail(
        "TRX has failures: "
        f"total={total} failed={failed} error={error} "
        f"timeout={timeout} aborted={aborted}"
    )

print(
    "PASS: TRX "
    f"{path.name}: total={total}, failed=0"
)
