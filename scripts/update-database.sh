#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")/.."

MIGRATION_CONNECTION="${EDULYTICS_MIGRATION_CONNECTION:-}"
if [[ -z "$MIGRATION_CONNECTION" ]]; then
    MIGRATION_CONNECTION="${ConnectionStrings__MigrationConnection:-}"
fi

if [[ -z "$MIGRATION_CONNECTION" ]]; then
    echo "ERROR: migration connection is required."
    echo "Set EDULYTICS_MIGRATION_CONNECTION or ConnectionStrings__MigrationConnection."
    echo "For Neon use the DIRECT/non-pooler PostgreSQL endpoint."
    exit 1
fi

export ConnectionStrings__MigrationConnection="$MIGRATION_CONNECTION"
export ConnectionStrings__DefaultConnection="$MIGRATION_CONNECTION"

dotnet ef database update \
    --project src/Edulytics.Data/Edulytics.Data.csproj \
    --startup-project src/Edulytics.Web/Edulytics.Web.csproj \
    --context EdulyticsDbContext
