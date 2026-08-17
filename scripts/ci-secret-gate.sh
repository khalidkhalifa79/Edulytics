#!/usr/bin/env bash
set -euo pipefail

OUTPUT="${1:-artifacts/security/gitleaks.log}"

mkdir -p "$(dirname "$OUTPUT")"

set -o pipefail

docker run \
    --rm \
    -v "$PWD:/repo" \
    -w /repo \
    ghcr.io/gitleaks/gitleaks:v8.30.1 \
    detect \
    --source=/repo \
    --redact \
    --no-banner \
    2>&1 |
    tee "$OUTPUT"

echo "SECRET HISTORY GATE: PASS"
