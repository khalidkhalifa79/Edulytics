#!/usr/bin/env python3
from __future__ import annotations

import json
import subprocess
import sys
import time
from datetime import datetime, timezone
from pathlib import Path


GROUPS = [
    ("assessment_score_edits", "FullyQualifiedName~Edulytics.Tests.Phase08"),
    ("signalr_realtime_contracts", "FullyQualifiedName~Edulytics.Tests.Phase10"),
    ("imports", "FullyQualifiedName~Edulytics.Tests.Phase11"),
    ("db_connector_slowdown_resilience", "FullyQualifiedName~Edulytics.Tests.Phase14"),
    ("outbox_backlog_and_worker", "FullyQualifiedName~Edulytics.Tests.Phase15"),
    ("reports_exports", "FullyQualifiedName~Edulytics.Tests.Phase20"),
    ("connector_delivery", "FullyQualifiedName~Edulytics.Tests.Phase21"),
    ("multi_instance_scale_contracts", "FullyQualifiedName~Edulytics.Tests.Phase25"),
    (
        "explicit_concurrency_tests",
        "FullyQualifiedName~Concurrency|FullyQualifiedName~Concurrent",
    ),
]


def run(cmd: list[str]) -> dict:
    started = time.perf_counter()
    cp = subprocess.run(
        cmd,
        text=True,
        stdout=subprocess.PIPE,
        stderr=subprocess.STDOUT,
        check=False,
    )
    elapsed = time.perf_counter() - started
    return {
        "command": cmd,
        "exit_code": cp.returncode,
        "seconds": round(elapsed, 3),
        "output_tail": cp.stdout[-6000:],
    }


def main() -> None:
    if len(sys.argv) != 2:
        raise SystemExit("usage: phase26_local_qualification.py OUT.json")

    out = Path(sys.argv[1])
    out.parent.mkdir(parents=True, exist_ok=True)

    results = {
        "generated_at_utc": datetime.now(timezone.utc).isoformat(),
        "groups": {},
    }

    for name, filter_expr in GROUPS:
        print(f"INFO: local qualification group={name}")
        item = run(
            [
                "dotnet",
                "test",
                "tests/Edulytics.Tests/Edulytics.Tests.csproj",
                "--no-build",
                "--no-restore",
                "--filter",
                filter_expr,
            ]
        )
        results["groups"][name] = item
        print(
            f"INFO: group={name} exit={item['exit_code']} "
            f"seconds={item['seconds']}"
        )
        if item["exit_code"] != 0:
            out.write_text(json.dumps(results, indent=2), encoding="utf-8")
            print(item["output_tail"])
            raise SystemExit(f"FAIL: local qualification group failed: {name}")

    out.write_text(json.dumps(results, indent=2), encoding="utf-8")
    print("PASS: local import/report/score/outbox/resilience/concurrency qualification")


if __name__ == "__main__":
    main()
