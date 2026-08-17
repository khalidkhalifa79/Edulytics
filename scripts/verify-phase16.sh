#!/usr/bin/env bash
set -euo pipefail

ROOT="$(
    cd "$(dirname "${BASH_SOURCE[0]}")/.."
    pwd
)"

cd "$ROOT"

dotnet tool restore
dotnet restore Edulytics.sln
dotnet build Edulytics.sln --no-restore

rm -rf artifacts/verify-phase16
mkdir -p artifacts/verify-phase16/coverage

set -o pipefail

dotnet test tests/Edulytics.Tests/Edulytics.Tests.csproj \
    --no-build \
    --no-restore \
    --collect:"XPlat Code Coverage" \
    --results-directory \
    artifacts/verify-phase16/coverage \
    --logger "trx;LogFileName=full.trx" \
    2>&1 |
    tee artifacts/verify-phase16/full-test.log

bash scripts/ci-test-log-gate.sh \
    artifacts/verify-phase16/full-test.log

python3 scripts/ci-coverage-gate.py \
    .ci/quality-baseline.json \
    artifacts/verify-phase16/coverage \
    artifacts/verify-phase16/coverage/full.trx

python3 scripts/ci-localization-parity.py
python3 scripts/ci-architecture-gate.py

bash scripts/ci-tenant-idor-gate.sh \
    artifacts/verify-phase16/tenant

bash scripts/ci-dependency-gate.sh \
    artifacts/verify-phase16/nuget-vulnerabilities.txt

git diff --check

echo "PHASE 16 LOCAL QUALITY VERIFICATION: PASS"
