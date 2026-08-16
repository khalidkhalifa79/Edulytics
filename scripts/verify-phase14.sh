#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"

dotnet build Edulytics.sln --no-restore
dotnet test Edulytics.sln --no-build --no-restore --filter 'FullyQualifiedName~Edulytics.Tests.Phase14'
dotnet test Edulytics.sln --no-build --no-restore

grep -q 'UseRequestTimeouts' src/Edulytics.Web/Program.cs
grep -q 'IdempotencyMiddleware' src/Edulytics.Web/Program.cs
grep -q 'AddConcurrencyLimiter' src/Edulytics.Web/Extensions/BackendResilienceRegistrationExtensions.cs
grep -q 'RetryAfter' src/Edulytics.Web/Extensions/BackendResilienceRegistrationExtensions.cs
grep -q 'CommandTimeout' src/Edulytics.Web/Extensions/ServiceCollectionExtensions.cs
grep -q 'NpgsqlMaxPoolSize' src/Edulytics.Web/Resilience/BackendResilienceOptions.cs
grep -q 'UX_IdempotencyRecords_Actor_Operation_Key' src/Edulytics.Data/Configurations/IdempotencyRecordConfiguration.cs

git diff --check

echo 'PHASE 14 VERIFICATION: PASS'
