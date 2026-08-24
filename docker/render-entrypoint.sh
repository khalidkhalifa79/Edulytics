#!/bin/sh
set -eu

if [ "$#" -gt 0 ]; then
    exec "$@"
fi

# Startup migrations are an explicit staging-only compatibility path.
# Production must run migrations in the host's pre-deploy phase using
# /app/phase27-predeploy.sh, then start the web process with this flag false.
RUN_STARTUP_MIGRATIONS="$(
    printf '%s' \
        "${Edulytics__Deployment__RunStartupMigrations:-false}" |
        tr '[:upper:]' '[:lower:]'
)"

case "$RUN_STARTUP_MIGRATIONS" in
    true)
        if [ -z "${ConnectionStrings__MigrationConnection:-}" ]; then
            echo "Startup migrations requested but migration connection is missing."
            exit 1
        fi

        echo "Applying pending database migrations before web startup..."

        ConnectionStrings__DefaultConnection="$ConnectionStrings__MigrationConnection" \
        EDULYTICS_CONNECTION_STRING="$ConnectionStrings__MigrationConnection" \
        /app/efbundle \
            --connection "$ConnectionStrings__MigrationConnection"

        echo "Database migration check completed."
        ;;
    false)
        echo "Startup migrations disabled; expecting controlled pre-deploy migration."
        ;;
    *)
        echo "Invalid Edulytics__Deployment__RunStartupMigrations value."
        exit 1
        ;;
esac

exec dotnet Edulytics.Web.dll \
    --urls "http://0.0.0.0:${PORT:-10000}"
