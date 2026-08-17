#!/usr/bin/env bash
set -euo pipefail

LOG="${1:?usage: ci-test-log-gate.sh <test-log>}"

[[ -f "$LOG" ]] || {
    echo "FAIL: test log missing: $LOG"
    exit 1
}

if grep -En \
    'TransactionIgnoredWarning|Outbox v2 polling failed|Analytics coalescing loop failed' \
    "$LOG"
then
    echo "FAIL: Testing host emitted a forbidden Phase15 background failure."
    exit 1
fi

echo "PASS: test log contains no forbidden Phase15 background-worker errors"
