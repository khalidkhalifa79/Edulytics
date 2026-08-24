#!/bin/sh
set -eu

MIGRATION_CONNECTION="${ConnectionStrings__MigrationConnection:-}"

if [ -z "$MIGRATION_CONNECTION" ]; then
    echo "Phase 27 pre-deploy migration connection is not configured."
    exit 1
fi

# Neon pooled hosts conventionally contain "-pooler".
# Production schema changes must use the direct/non-pooler endpoint.
case "$MIGRATION_CONNECTION" in
    *-pooler.*|*pooler*)
        echo "Phase 27 pre-deploy rejected a pooled migration endpoint."
        exit 1
        ;;
esac

echo "Phase 27 pre-deploy: applying EF Core migrations through direct connection."

ConnectionStrings__DefaultConnection="$MIGRATION_CONNECTION" \
EDULYTICS_CONNECTION_STRING="$MIGRATION_CONNECTION" \
/app/efbundle \
    --connection "$MIGRATION_CONNECTION"

echo "Phase 27 pre-deploy: migration completed."
