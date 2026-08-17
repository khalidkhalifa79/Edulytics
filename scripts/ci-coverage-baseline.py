#!/usr/bin/env python3

from __future__ import annotations

import json
import math
import sys
from pathlib import Path

from ci_coverage_common import (
    fail,
    read_trx_counts,
    summarize_coverage,
)


if len(sys.argv) != 4:
    fail(
        "usage: ci-coverage-baseline.py "
        "<coverage-root> <trx> <baseline.json>"
    )

coverage_root = Path(
    sys.argv[1]
)

trx_path = Path(
    sys.argv[2]
)

output_path = Path(
    sys.argv[3]
)

summary = summarize_coverage(
    coverage_root
)

counts = read_trx_counts(
    trx_path
)

total = counts["total"]
failed = counts["failed"]
error = counts["error"]
timeout = counts["timeout"]
aborted = counts["aborted"]

if total <= 0:
    fail(
        "baseline TRX contains zero tests"
    )

if any(
    (
        failed,
        error,
        timeout,
        aborted,
    )
):
    fail(
        "baseline tests are not green: "
        f"total={total} "
        f"failed={failed} "
        f"error={error} "
        f"timeout={timeout} "
        f"aborted={aborted}"
    )


def floor_one(
    value: float
) -> float:
    return (
        math.floor(
            value * 10.0
        )
        / 10.0
    )


baseline = {
    "line_percent":
        floor_one(
            float(
                summary[
                    "line_percent"
                ]
            )
        ),
    "branch_percent":
        floor_one(
            float(
                summary[
                    "branch_percent"
                ]
            )
        ),
    "minimum_test_count":
        total,
    "measurement": {
        "line_percent_exact":
            round(
                float(
                    summary[
                        "line_percent"
                    ]
                ),
                4
            ),
        "branch_percent_exact":
            round(
                float(
                    summary[
                        "branch_percent"
                    ]
                ),
                4
            ),
        "coverage_file_count":
            int(
                summary[
                    "file_count"
                ]
            ),
        "line_covered":
            int(
                summary[
                    "line_covered"
                ]
            ),
        "line_total":
            int(
                summary[
                    "line_total"
                ]
            ),
        "branch_covered":
            int(
                summary[
                    "branch_covered"
                ]
            ),
        "branch_total":
            int(
                summary[
                    "branch_total"
                ]
            ),
    },
}

output_path.parent.mkdir(
    parents=True,
    exist_ok=True
)

output_path.write_text(
    json.dumps(
        baseline,
        indent=2
    )
    + "\n",
    encoding="utf-8",
    newline="\n"
)

print(
    "PASS: merged coverage baseline "
    f"files={summary['file_count']} "
    f"line={summary['line_percent']:.2f}% "
    f"branch={summary['branch_percent']:.2f}% "
    f"tests={total}"
)
