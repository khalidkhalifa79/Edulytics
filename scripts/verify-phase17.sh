#!/usr/bin/env bash
set -Eeuo pipefail

ROOT="$(
    cd "$(dirname "${BASH_SOURCE[0]}")/.."
    pwd
)"

cd "$ROOT"

command -v openssl >/dev/null 2>&1 || {
    echo "ERROR: openssl is required for Phase 17 production smoke."
    exit 1
}

IMAGE="${PHASE17_IMAGE:-edulytics:phase17-local}"
NETWORK="edulytics-phase17-net"
PG="edulytics-phase17-postgres"
APP="edulytics-phase17-app"
PORT="${PHASE17_LOCAL_PORT:-18017}"

CERT_DIR=""
CERT_PASSWORD="phase17-local-certificate"

cleanup() {
    docker rm -f "$APP" >/dev/null 2>&1 || true
    docker rm -f "$PG" >/dev/null 2>&1 || true
    docker network rm "$NETWORK" >/dev/null 2>&1 || true
    if [ -n "${CERT_DIR:-}" ]; then
        rm -rf "$CERT_DIR" >/dev/null 2>&1 || true
    fi
}

trap cleanup EXIT

echo "===== PHASE 17 STATIC CONTRACT ====="

test -f Dockerfile
test -f render.yaml
test -x docker/render-entrypoint.sh

grep -q 'runtime: docker' render.yaml
grep -q 'healthCheckPath: /health/ready' render.yaml
grep -q 'ConnectionStrings__DefaultConnection' render.yaml
grep -q 'ConnectionStrings__MigrationConnection' render.yaml
grep -q 'preDeployCommand' render.yaml

grep -q 'PersistKeysToDbContext' \
    src/Edulytics.Web/Extensions/ServiceCollectionExtensions.cs

grep -q 'IDataProtectionKeyContext' \
    src/Edulytics.Data/Contexts/EdulyticsDbContext.cs

grep -q 'DataProtectionKeys' \
    src/Edulytics.Data/Contexts/EdulyticsDbContext.cs

grep -q 'TrustForwardedHeaders' \
    src/Edulytics.Web/Program.cs

echo "PASS: Phase 17 static deployment contract."


echo
echo "===== PHASE 16 QUALITY CONTRACT ====="

bash scripts/verify-phase16.sh

echo "PASS: inherited Phase 16 quality gates."


echo
echo "===== PRODUCTION IMAGE BUILD ====="

docker build \
    --pull \
    -f Dockerfile \
    -t "$IMAGE" \
    .

echo "PASS: production Docker image built."


echo
echo "===== NON-ROOT IMAGE CHECK ====="

IMAGE_USER="$(docker image inspect "$IMAGE" --format '{{.Config.User}}')"

if [ "$IMAGE_USER" != "app" ]; then
    echo "ERROR: Docker image user is '$IMAGE_USER', expected 'app'."
    exit 1
fi

echo "PASS: image runtime user=app."


echo
echo "===== LOCAL POSTGRESQL 17 ====="

cleanup

docker network create "$NETWORK" >/dev/null

docker run -d \
    --name "$PG" \
    --network "$NETWORK" \
    -e POSTGRES_USER=postgres \
    -e POSTGRES_PASSWORD=phase17-local-only \
    -e POSTGRES_DB=edulytics \
    postgres:17-alpine >/dev/null

for i in $(seq 1 60); do
    if docker exec "$PG" \
        pg_isready \
        -U postgres \
        -d edulytics \
        >/dev/null 2>&1; then
        break
    fi

    if [ "$i" -eq 60 ]; then
        docker logs "$PG" || true
        echo "ERROR: PostgreSQL did not become ready."
        exit 1
    fi

    sleep 1
done

echo "PASS: PostgreSQL ready."


echo
echo "===== MIGRATION BUNDLE ====="

DB_CONNECTION="Host=${PG};Port=5432;Database=edulytics;Username=postgres;Password=phase17-local-only;SSL Mode=Disable"

docker run --rm \
    --network "$NETWORK" \
    -e ConnectionStrings__DefaultConnection="$DB_CONNECTION" \
    -e ConnectionStrings__MigrationConnection="$DB_CONNECTION" \
    -e EDULYTICS_CONNECTION_STRING="$DB_CONNECTION" \
    "$IMAGE" \
    /app/efbundle \
    --connection "$DB_CONNECTION"

DP_TABLE="$(
    docker exec "$PG" \
        psql \
        -U postgres \
        -d edulytics \
        -tAc \
        "SELECT to_regclass('public.\"DataProtectionKeys\"') IS NOT NULL;" \
        | tr -d '[:space:]'
)"

if [ "$DP_TABLE" != "t" ]; then
    echo "ERROR: DataProtectionKeys table is missing."
    exit 1
fi

echo "PASS: EF bundle applied."
echo "PASS: persistent DataProtectionKeys schema exists."


echo
echo "===== PRODUCTION-MODE CONTAINER ====="

CERT_DIR="$(mktemp -d)"

openssl req \
    -x509 \
    -newkey rsa:2048 \
    -sha256 \
    -nodes \
    -days 1 \
    -subj "/CN=Edulytics Phase17 Local" \
    -keyout "$CERT_DIR/key.pem" \
    -out "$CERT_DIR/cert.pem" \
    >/dev/null 2>&1

openssl pkcs12 \
    -export \
    -inkey "$CERT_DIR/key.pem" \
    -in "$CERT_DIR/cert.pem" \
    -out "$CERT_DIR/cert.pfx" \
    -password "pass:${CERT_PASSWORD}" \
    >/dev/null 2>&1

CERTIFICATE_BASE64="$(
    base64 < "$CERT_DIR/cert.pfx" |
        tr -d '\n\r'
)"

docker run -d \
    --name "$APP" \
    --network "$NETWORK" \
    -p "127.0.0.1:${PORT}:10000" \
    -e PORT=10000 \
    -e ASPNETCORE_ENVIRONMENT=Production \
    -e ConnectionStrings__DefaultConnection="$DB_CONNECTION" \
    -e Edulytics__Hosting__TrustForwardedHeaders=true \
    -e Edulytics__Hosting__DataProtectionApplicationName=Edulytics-Phase17-Smoke \
    -e Edulytics__Hosting__RequireDataProtectionCertificate=true \
    -e Edulytics__Hosting__DataProtectionCertificateBase64="$CERTIFICATE_BASE64" \
    -e Edulytics__Hosting__DataProtectionCertificatePassword="$CERT_PASSWORD" \
    "$IMAGE" >/dev/null

health() {
    local path="$1"

    curl \
        --fail \
        --silent \
        --show-error \
        -H "X-Forwarded-Proto: https" \
        "http://127.0.0.1:${PORT}${path}"
}

for i in $(seq 1 120); do
    if health "/health/live" >/dev/null 2>&1; then
        break
    fi

    if [ "$i" -eq 120 ]; then
        docker logs "$APP" || true
        echo "ERROR: /health/live did not become healthy."
        exit 1
    fi

    sleep 1
done

echo "PASS: /health/live healthy."

for i in $(seq 1 120); do
    if health "/health/ready" >/dev/null 2>&1; then
        break
    fi

    if [ "$i" -eq 120 ]; then
        docker logs "$APP" || true
        echo "ERROR: /health/ready did not become healthy."
        exit 1
    fi

    sleep 1
done

echo "PASS: /health/ready healthy."


echo
echo "===== HTTP ENTRY SMOKE ====="

STATUS="$(
    curl \
        --silent \
        --output /tmp/phase17-home.html \
        --write-out '%{http_code}' \
        -H "X-Forwarded-Proto: https" \
        "http://127.0.0.1:${PORT}/"
)"

case "$STATUS" in
    200|302|303)
        ;;
    *)
        docker logs "$APP" || true
        echo "ERROR: unexpected home HTTP status: $STATUS"
        exit 1
        ;;
esac

echo "PASS: application entry HTTP status=$STATUS."


echo
echo "===== RESTART ACCEPTANCE ====="

docker restart "$APP" >/dev/null

for i in $(seq 1 120); do
    if health "/health/ready" >/dev/null 2>&1; then
        break
    fi

    if [ "$i" -eq 120 ]; then
        docker logs "$APP" || true
        echo "ERROR: app failed readiness after restart."
        exit 1
    fi

    sleep 1
done

echo "PASS: application returned to ready after restart."


echo
echo "===== CONTAINER LOG DEFECT SCAN ====="

LOGS="$(docker logs "$APP" 2>&1 || true)"

if printf '%s\n' "$LOGS" |
    grep -Eiq \
        'Unhandled exception|Stack overflow|OutOfMemoryException|NpgsqlException.*password authentication failed|No XML encryptor configured'; then

    printf '%s\n' "$LOGS"
    echo "ERROR: fatal defect found in container logs."
    exit 1
fi

echo "PASS: no fatal startup/runtime defect detected."


echo
echo "===== WHITESPACE ====="

git diff --check

echo
echo "============================================================"
echo " PHASE 17 LOCAL REPOSITORY ACCEPTANCE: PASS"
echo " Live Render/Neon staging is still an external acceptance gate."
echo "============================================================"
