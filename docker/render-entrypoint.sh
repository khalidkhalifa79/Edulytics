#!/bin/sh
set -eu

if [ "$#" -gt 0 ]; then
    exec "$@"
fi

# Render Free web services do not support pre-deploy commands.
# For the temporary staging environment, apply any pending EF Core
# migrations before ASP.NET Core starts. The direct/non-pooler Neon
# connection is used only for the migration bundle. The connection
# string itself is never written to logs.
if [ -n "${ConnectionStrings__MigrationConnection:-}" ]; then
    echo "Applying pending database migrations before web startup..."

    ConnectionStrings__DefaultConnection="$ConnectionStrings__MigrationConnection" \
    EDULYTICS_CONNECTION_STRING="$ConnectionStrings__MigrationConnection" \
    /app/efbundle \
        --connection "$ConnectionStrings__MigrationConnection"

    echo "Database migration check completed."
else
    echo "Migration connection is not configured; skipping startup migrations."
fi

exec dotnet Edulytics.Web.dll \
    --urls "http://0.0.0.0:${PORT:-10000}"
