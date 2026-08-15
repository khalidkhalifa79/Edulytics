#!/usr/bin/env bash
set -euo pipefail

cd "$(git rev-parse --show-toplevel)"

echo "============================================================"
echo " EDULYTICS — PHASE 10 VERIFICATION"
echo "============================================================"

dotnet build Edulytics.sln --no-restore

dotnet test \
    Edulytics.sln \
    --no-build \
    --no-restore \
    --filter \
    "FullyQualifiedName~Edulytics.Tests.Phase10"

dotnet test \
    Edulytics.sln \
    --no-build \
    --no-restore

grep -q \
    'AddSignalR' \
    src/Edulytics.Web/Extensions/RealtimeRegistrationExtensions.cs

grep -q \
    'MapHub<AnalyticsHub>' \
    src/Edulytics.Web/Program.cs

grep -q \
    'AddOutboxAsync' \
    src/Edulytics.Services/Assessments/AssessmentService.cs

grep -q \
    'IAnalyticsProjectionRefreshService' \
    src/Edulytics.Web/Background/OutboxProcessorBackgroundService.cs

grep -q \
    'IDashboardRealtimeNotifier' \
    src/Edulytics.Web/Background/OutboxProcessorBackgroundService.cs

if grep -nEi \
    '\.invoke\(|school:' \
    src/Edulytics.Web/wwwroot/js/analytics-live.js
then
    echo "FAIL: client-controlled SignalR group behavior detected."
    exit 1
fi

if grep -R \
    --include='*.cs' \
    -nE '\bEdulyticsDbContext\b|\bDbContext\b' \
    src/Edulytics.Web/Controllers \
    src/Edulytics.Web/Hubs
then
    echo "FAIL: DbContext detected in controller/hub."
    exit 1
fi

if grep -R \
    --include='*Controller.cs' \
    -niE '\[Http(Get|Post)[^]]*register|Register\(' \
    src/Edulytics.Web/Controllers
then
    echo "FAIL: public registration detected."
    exit 1
fi

test -s \
    src/Edulytics.Web/wwwroot/lib/signalr/signalr.min.js

test -s \
    src/Edulytics.Web/wwwroot/lib/signalr/VERSION.txt

git diff --check

echo "============================================================"
echo " PHASE 10 AUTOMATED VERIFICATION: PASS"
echo "============================================================"
