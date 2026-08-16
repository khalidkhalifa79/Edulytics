#!/usr/bin/env bash
set -euo pipefail

ROOT="$(
    cd "$(dirname "${BASH_SOURCE[0]}")/.."
    pwd
)"

cd "$ROOT"

dotnet build Edulytics.sln --no-restore

dotnet test Edulytics.sln \
    --no-build \
    --no-restore \
    --filter \
    'FullyQualifiedName~Edulytics.Tests.Phase15'

dotnet test Edulytics.sln \
    --no-build \
    --no-restore

grep -q 'SKIP LOCKED' \
    src/Edulytics.Data/Repositories/OutboxRepository.cs

grep -q 'LeaseToken' \
    src/Edulytics.Data/Repositories/OutboxRepository.cs

grep -q 'DeadLetter' \
    src/Edulytics.Data/Repositories/OutboxRepository.cs

grep -q 'OutboxRequeueAudits' \
    src/Edulytics.Data/Configurations/OutboxRequeueAuditConfiguration.cs

grep -q 'AnalyticsRefreshStates' \
    src/Edulytics.Data/Configurations/AnalyticsRefreshStateConfiguration.cs

grep -q 'RefreshSchoolAsync' \
    src/Edulytics.Web/Background/AnalyticsRefreshBackgroundService.cs

if grep -q 'RefreshSchoolAsync' \
    src/Edulytics.Web/Background/OutboxProcessorBackgroundService.cs
then
    echo 'FAIL: Outbox worker still performs inline analytics refresh.'
    exit 1
fi

grep -q 'onreconnected' \
    src/Edulytics.Web/wwwroot/js/analytics-live.js

grep -q 'window.clearTimeout' \
    src/Edulytics.Web/wwwroot/js/analytics-live.js

grep -q 'window.location.reload' \
    src/Edulytics.Web/wwwroot/js/analytics-live.js

git diff --check

echo 'PHASE 15 VERIFICATION: PASS'
