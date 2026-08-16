#!/usr/bin/env bash
set -euo pipefail

ROOT="$(
    cd "$(dirname "${BASH_SOURCE[0]}")/.." &&
    pwd
)"

cd "$ROOT"

echo "============================================================"
echo " EDULYTICS - PHASE 12 VERIFICATION"
echo "============================================================"

dotnet build \
    Edulytics.sln \
    --no-restore

dotnet test \
    Edulytics.sln \
    --no-build \
    --no-restore \
    --filter \
    "FullyQualifiedName~Edulytics.Tests.Phase12"

dotnet test \
    Edulytics.sln \
    --no-build \
    --no-restore

grep -q \
    'AddProductionHardeningPhase12' \
    src/Edulytics.Web/Program.cs

grep -q \
    'AddJsonConsole' \
    src/Edulytics.Web/Program.cs

grep -q \
    '"/health/live"' \
    src/Edulytics.Web/Program.cs

grep -q \
    '"/health/ready"' \
    src/Edulytics.Web/Program.cs

grep -q \
    'ValidateOnStart' \
    src/Edulytics.Web/Extensions/ProductionHardeningRegistrationExtensions.cs

grep -q \
    'GetPendingMigrationsAsync' \
    src/Edulytics.Web/Health/DatabaseReadinessHealthCheck.cs

grep -q \
    'OutboxWorkerHealthState' \
    src/Edulytics.Web/Background/OutboxProcessorBackgroundService.cs

grep -q \
    'X-Correlation-ID' \
    src/Edulytics.Web/Middleware/CorrelationIdMiddleware.cs

grep -q \
    'X-Content-Type-Options' \
    src/Edulytics.Web/Middleware/SecurityHeadersMiddleware.cs

for file in \
    docs/PHASE_12_IMPLEMENTATION_PLAN.md \
    docs/PRODUCTION_DEPLOYMENT.md \
    docs/MONITORING_RUNBOOK.md \
    docs/BACKUP_RESTORE_RUNBOOK.md
do
    test -s "$file"
done

if find \
    src/Edulytics.Data/Migrations \
    -maxdepth 1 \
    -type f \
    -iname '*Phase12*' \
    | grep -q .
then
    echo "FAIL: unexpected Phase 12 migration."
    exit 1
fi


grep -q \
    'app.MapFallback(' \
    src/Edulytics.Web/Program.cs

grep -q \
    'StatusCodes.Status404NotFound' \
    src/Edulytics.Web/Program.cs

git diff --check

echo "============================================================"
echo " PHASE 12 AUTOMATED VERIFICATION: PASS"
echo "============================================================"
