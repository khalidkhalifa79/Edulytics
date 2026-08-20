#!/usr/bin/env bash
set -euo pipefail

ROOT="$(
    cd "$(dirname "${BASH_SOURCE[0]}")/.."
    pwd
)"

RESULTS="${1:-$ROOT/artifacts/tenant}"

mkdir -p "$RESULTS"

run_gate() {
    local name="$1"
    local filter="$2"
    local trx="$RESULTS/${name}.trx"

    rm -f "$trx"

    dotnet test \
        "$ROOT/tests/Edulytics.Tests/Edulytics.Tests.csproj" \
        --no-build \
        --no-restore \
        --filter "$filter" \
        --results-directory "$RESULTS" \
        --logger "trx;LogFileName=${name}.trx"

    python3 \
        "$ROOT/scripts/ci-trx-gate.py" \
        "$trx"
}

run_gate \
    "school-authorization" \
    "FullyQualifiedName~Edulytics.Tests.Phase04.SchoolAuthorizationTests"

run_gate \
    "school-user-authorization" \
    "FullyQualifiedName~Edulytics.Tests.Phase05.SchoolUserAuthorizationTests"

run_gate \
    "school-user-tenant-acceptance" \
    "FullyQualifiedName~Edulytics.Tests.Phase05.Phase05AcceptanceCoverageTests"

run_gate \
    "subject-supervisor-tenant-authorization" \
    "FullyQualifiedName~Edulytics.Tests.Phase19"

run_gate \
    "report-export-tenant-authorization" \
    "FullyQualifiedName~Edulytics.Tests.Phase20"

run_gate \
    "notification-tenant-authorization" \
    "FullyQualifiedName~Edulytics.Tests.Phase21"


run_gate \
    "operational-admin-authorization" \
    "FullyQualifiedName~Edulytics.Tests.Phase22"


run_gate \
    "phase23-security-tenant-hardening" \
    "FullyQualifiedName~Edulytics.Tests.Phase23"

echo "TENANT / IDOR CI GATE: PASS"
