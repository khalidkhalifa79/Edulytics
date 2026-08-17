#!/usr/bin/env python3

from __future__ import annotations

import json
import sys
from pathlib import Path

from ci_coverage_common import (
    fail,
    read_trx_counts,
    summarize_coverage,
)


if len(sys.argv) != 4:
    fail(
        "usage: ci-coverage-gate.py "
        "<baseline.json> <coverage-root> <trx>"
    )

baseline_path = Path(
    sys.argv[1]
)

coverage_root = Path(
    sys.argv[2]
)

trx_path = Path(
    sys.argv[3]
)

if not baseline_path.is_file():
    fail(
        f"baseline not found: {baseline_path}"
    )

baseline = json.loads(
    baseline_path.read_text(
        encoding="utf-8")
)

summary = summarize_coverage(
    coverage_root
)

line_percent = float(
    summary[
        "line_percent"
    ]
)

branch_percent = float(
    summary[
        "branch_percent"
    ]
)

required_line = float(
    baseline[
        "line_percent"
    ]
)

required_branch = float(
    baseline[
        "branch_percent"
    ]
)

epsilon = 0.051

if (
    line_percent
    + epsilon
    < required_line
):
    fail(
        "line coverage regression: "
        f"{line_percent:.2f}% "
        f"< {required_line:.1f}%"
    )

if (
    branch_percent
    + epsilon
    < required_branch
):
    fail(
        "branch coverage regression: "
        f"{branch_percent:.2f}% "
        f"< {required_branch:.1f}%"
    )

counts = read_trx_counts(
    trx_path
)

failed = counts["failed"]
error = counts["error"]
timeout = counts["timeout"]
aborted = counts["aborted"]
total = counts["total"]

if any(
    (
        failed,
        error,
        timeout,
        aborted,
    )
):
    fail(
        "coverage run is not green: "
        f"failed={failed} "
        f"error={error} "
        f"timeout={timeout} "
        f"aborted={aborted}"
    )

minimum_tests = int(
    baseline[
        "minimum_test_count"
    ]
)

if total < minimum_tests:
    fail(
        "test-count regression: "
        f"{total} < {minimum_tests}"
    )

print(
    "PASS: coverage gate "
    f"files={summary['file_count']} "
    f"line={line_percent:.2f}% "
    f"(min {required_line:.1f}%), "
    f"branch={branch_percent:.2f}% "
    f"(min {required_branch:.1f}%), "
    f"tests={total} "
    f"(min {minimum_tests})"
)
