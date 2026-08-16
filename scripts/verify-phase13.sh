#!/usr/bin/env bash
set -euo pipefail
cd "$(git rev-parse --show-toplevel)"
export DOTNET_CLI_TELEMETRY_OPTOUT=1

dotnet build Edulytics.sln --no-restore
dotnet test Edulytics.sln --no-build --no-restore --filter 'FullyQualifiedName~Edulytics.Tests.Phase13'
dotnet test Edulytics.sln --no-build --no-restore

if grep -RInE 'UseSqlServer|Microsoft\.EntityFrameworkCore\.SqlServer|Microsoft\.Data\.SqlClient|\bSqlConnection\b|\bSqlException\b' \
    src --include='*.cs' --include='*.csproj' --exclude-dir=bin --exclude-dir=obj; then
    echo 'FAIL: active SQL Server runtime dependency found.'
    exit 1
fi

grep -q 'Npgsql.EntityFrameworkCore.PostgreSQL' src/Edulytics.Data/Edulytics.Data.csproj

if grep -RInE 'SqlServer:|UseIdentityColumns|HasColumnType\("rowversion"\)|type: "rowversion"|uniqueidentifier|datetime2|nvarchar\(' \
    src/Edulytics.Data/Migrations --include='*.cs'; then
    echo 'FAIL: active SQL Server migration metadata found.'
    exit 1
fi

if [[ -n "${EDULYTICS_PHASE13_VERIFY_CONNECTION:-}" ]]; then
    env \
        "ConnectionStrings__DefaultConnection=$EDULYTICS_PHASE13_VERIFY_CONNECTION" \
        "ConnectionStrings__MigrationConnection=$EDULYTICS_PHASE13_VERIFY_CONNECTION" \
        dotnet ef migrations has-pending-model-changes \
            --project src/Edulytics.Data/Edulytics.Data.csproj \
            --startup-project src/Edulytics.Web/Edulytics.Web.csproj \
            --context EdulyticsDbContext \
            --no-build
fi

git diff --check
echo 'PHASE 13 VERIFICATION: PASS'
