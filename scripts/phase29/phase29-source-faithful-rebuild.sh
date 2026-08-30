#!/usr/bin/env bash

set -Eeuo pipefail

cd "$HOME/projects/Edulytics"

STATE_ROOT=".phase29-source-rebuild"
RUN_STATE="$STATE_ROOT/run.json"
AUDITOR="tools/phase29/source_faithful_audit.py"

fail() {
    echo
    echo "=============================================================="
    echo " PHASE 29 SOURCE-FAITHFUL REBUILD STOPPED"
    echo "=============================================================="
    echo "$1"
    echo
    echo "Completed valid checkpoints remain preserved."
    echo
    echo "Resume with the SAME command:"
    echo "  ./scripts/phase29/phase29-source-faithful-rebuild.sh"
    echo "=============================================================="
    exit 1
}

trap 'fail "Unexpected failure at line $LINENO"' ERR

[ -f "$RUN_STATE" ] ||
    fail "Run state missing."

checkpoint_status() {
    python3 - "$1" <<'PY'
import json
import sys
from pathlib import Path

name = sys.argv[1]

state = json.loads(
    Path(
        ".phase29-source-rebuild/run.json"
    ).read_text(
        encoding="utf-8"
    )
)

checkpoint = (
    state
    .get("checkpoints", {})
    .get(name)
)

if checkpoint is None:
    print("PENDING")
else:
    print(
        checkpoint.get(
            "status",
            "UNKNOWN"
        )
    )
PY
}

echo "=============================================================="
echo " EDULYTICS PHASE 29 — RESUMABLE SOURCE-FAITHFUL REBUILD"
echo "=============================================================="

ARCH_STATUS="$(
    checkpoint_status architecture-audit
)"

if [ "$ARCH_STATUS" = "PASS" ]; then
    echo "01 Architecture audit          : PASS → SKIP"
else
    echo "01 Architecture audit          : $ARCH_STATUS → RUN"

    python3 \
        "$AUDITOR" \
        --audit
fi

echo
echo "02 Source/license acquisition  : NEXT"
echo "03 Source importer             : PENDING"
echo "04 Replace 1466 bodies         : PENDING"
echo "05 Add 94 supporting bodies    : PENDING"
echo "06 Polish translations         : PENDING"
echo "07 Provenance/attribution      : PENDING"
echo "08 Production Ready rules      : PENDING"
echo "09 Source-fidelity validation  : PENDING"
echo "10 Coverage UI                 : PENDING"
echo "11 Regression / PR / CI        : PENDING"
echo "12 Staging acceptance          : PENDING"

echo
echo "--------------------------------------------------------------"
echo "This runner is resumable."
echo "Completed checkpoints are skipped."
echo "A checkpoint is rerun only when we later invalidate it because"
echo "its inputs or source hashes changed."
echo "--------------------------------------------------------------"
