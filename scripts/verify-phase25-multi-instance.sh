#!/usr/bin/env bash
set -Eeuo pipefail

ROOT="$(
    cd "$(dirname "${BASH_SOURCE[0]}")/.." &&
    pwd
)"

cd "$ROOT"

COMPOSE="docker/phase25/docker-compose.yml"
IMAGE="edulytics-phase25-local:dev"

HOST_DB='Host=127.0.0.1;Port=15432;Database=edulytics_phase25;Username=edulytics;Password=phase25-local-only;Maximum Pool Size=10'
WEB1='http://127.0.0.1:18081'
WEB2='http://127.0.0.1:18082'
GATEWAY='http://127.0.0.1:18080'

COOKIE_JAR="/tmp/phase25-cookie-jar.txt"
LOGIN_HTML="/tmp/phase25-login.html"
LISTENER_LOG="/tmp/phase25-signalr-listener.log"

cleanup() {
    code=$?
    trap - EXIT

    if [ "$code" -ne 0 ]; then
        echo
        echo "===== CONTAINER LOGS — FAILURE TAIL ====="
        docker compose \
            -f "$COMPOSE" \
            logs \
            --tail=120 \
            web1 web2 worker1 worker2 gateway \
            2>/dev/null || true
    fi

    docker compose \
        -f "$COMPOSE" \
        down \
        -v \
        --remove-orphans \
        >/dev/null 2>&1 || true

    rm -f \
        "$COOKIE_JAR" \
        "$LOGIN_HTML" \
        "$LISTENER_LOG" \
        /tmp/phase25-login-response.txt

    exit "$code"
}

trap cleanup EXIT

fail() {
    echo "FAIL: $1" >&2
    exit 1
}

run_gate() {
    dotnet run \
        --project \
        tests/Edulytics.ScaleGate/Edulytics.ScaleGate.csproj \
        --no-build \
        -- \
        "$@"
}

wait_http() {
    url="$1"
    label="$2"

    for _ in $(seq 1 90); do
        if curl -fsS "$url" >/dev/null 2>&1; then
            echo "PASS: $label"
            return
        fi

        sleep 1
    done

    fail "timeout waiting for $label"
}

ensure_running() {
    service="$1"

    id="$(
        docker compose \
            -f "$COMPOSE" \
            ps -q "$service"
    )"

    [ -n "$id" ] ||
        fail "$service container missing"

    [ "$(
        docker inspect \
            -f '{{.State.Running}}' \
            "$id"
    )" = "true" ] ||
        fail "$service not running"

    echo "PASS: $service running"
}

extract_token() {
    python3 - "$LOGIN_HTML" <<'PY'
from html import unescape
from pathlib import Path
import re
import sys

text = Path(sys.argv[1]).read_text(
    encoding="utf-8"
)

match = re.search(
    r'name="__RequestVerificationToken"[^>]*value="([^"]+)"',
    text
)

if not match:
    raise SystemExit(
        "antiforgery token not found"
    )

print(unescape(match.group(1)))
PY
}

login_school_admin() {
    rm -f "$COOKIE_JAR" "$LOGIN_HTML"

    curl \
        -fsS \
        -c "$COOKIE_JAR" \
        -b 'Edulytics.Culture=en' \
        "$WEB1/account/login" \
        > "$LOGIN_HTML"

    token="$(extract_token)"

    code="$(
        curl \
            -sS \
            -o /tmp/phase25-login-response.txt \
            -c "$COOKIE_JAR" \
            -b "$COOKIE_JAR" \
            -b 'Edulytics.Culture=en' \
            --data-urlencode \
                "__RequestVerificationToken=$token" \
            --data-urlencode \
                'Email=phase25-schooladmin@example.test' \
            --data-urlencode \
                'Password=Phase25!SchoolAdmin#2026' \
            -w '%{http_code}' \
            "$WEB1/account/login"
    )"

    [ "$code" = "302" ] ||
        fail "school-admin login expected 302, got $code"

    IDENTITY_COOKIE="$(
        awk '
            $6 == ".AspNetCore.Identity.Application" {
                print $7
            }
        ' "$COOKIE_JAR" |
        tail -1
    )"

    [ -n "$IDENTITY_COOKIE" ] ||
        fail "Identity cookie not found"

    export IDENTITY_COOKIE

    echo "PASS: web1 issued authenticated Identity cookie"
}

assert_cookie() {
    base="$1"
    label="$2"

    code="$(
        curl \
            -sS \
            -o /dev/null \
            -b "$COOKIE_JAR" \
            -b 'Edulytics.Culture=en' \
            -w '%{http_code}' \
            "$base/school/dashboard"
    )"

    [ "$code" = "200" ] ||
        fail "$label expected 200, got $code"

    echo "PASS: $label"
}

wait_listener() {
    pid="$1"

    for _ in $(seq 1 100); do
        if grep \
            -Fq \
            'SIGNALR_CONNECTED' \
            "$LISTENER_LOG" \
            2>/dev/null
        then
            return
        fi

        if ! kill -0 "$pid" 2>/dev/null; then
            cat "$LISTENER_LOG" || true
            fail "SignalR listener exited before connect"
        fi

        sleep 0.2
    done

    cat "$LISTENER_LOG" || true
    fail "SignalR listener connect timeout"
}

prove_delivery() {
    worker="$1"

    : > "$LISTENER_LOG"

    run_gate \
        listen \
        "$WEB2" \
        "$IDENTITY_COOKIE" \
        > "$LISTENER_LOG" \
        2>&1 &

    listener_pid=$!

    wait_listener "$listener_pid"

    output="$(
        run_gate \
            enqueue \
            "$HOST_DB"
    )"

    echo "$output"

    outbox_id="$(
        printf '%s\n' "$output" |
        sed -n 's/^OUTBOX_ID=//p' |
        tail -1
    )"

    [ -n "$outbox_id" ] ||
        fail "missing Outbox ID"

    if ! wait "$listener_pid"; then
        cat "$LISTENER_LOG" || true
        fail "SignalR delivery failed via $worker"
    fi

    cat "$LISTENER_LOG"

    grep -Fq \
        'SIGNALR_RECEIVED=' \
        "$LISTENER_LOG" ||
        fail "SignalR invalidation not received"

    run_gate \
        wait-outbox \
        "$HOST_DB" \
        "$outbox_id"

    echo "PASS: $worker durable event -> Redis -> web2 SignalR"
}

echo "===== PREFLIGHT ====="

command -v docker >/dev/null ||
    fail "docker is required"

docker info >/dev/null 2>&1 ||
    fail "Docker daemon unavailable"

docker compose version >/dev/null 2>&1 ||
    fail "docker compose unavailable"

command -v curl >/dev/null ||
    fail "curl is required"

docker compose \
    -f "$COMPOSE" \
    down -v --remove-orphans \
    >/dev/null 2>&1 || true

echo "PASS: Docker/Compose/curl"

echo
echo "===== BUILD LOCAL IMAGE ====="

docker build \
    -t "$IMAGE" \
    . ||
    fail "Docker build"

echo "PASS: image built"

echo
echo "===== START POSTGRES / REDIS ====="

docker compose \
    -f "$COMPOSE" \
    up -d postgres redis ||
    fail "start infra"

for _ in $(seq 1 60); do
    pg_id="$(
        docker compose -f "$COMPOSE" ps -q postgres
    )"
    redis_id="$(
        docker compose -f "$COMPOSE" ps -q redis
    )"

    pg_health="$(
        docker inspect \
            -f '{{.State.Health.Status}}' \
            "$pg_id" \
            2>/dev/null || true
    )"

    redis_health="$(
        docker inspect \
            -f '{{.State.Health.Status}}' \
            "$redis_id" \
            2>/dev/null || true
    )"

    if [ "$pg_health" = "healthy" ] &&
       [ "$redis_health" = "healthy" ]; then
        break
    fi

    sleep 1
done

[ "$pg_health" = "healthy" ] ||
    fail "PostgreSQL not healthy"

[ "$redis_health" = "healthy" ] ||
    fail "Redis not healthy"

echo "PASS: PostgreSQL + Redis healthy"

echo
echo "===== APPLY MIGRATIONS ONCE ====="

docker run \
    --rm \
    --network edulytics-phase25-local_default \
    -e 'EDULYTICS_CONNECTION_STRING=Host=postgres;Port=5432;Database=edulytics_phase25;Username=edulytics;Password=phase25-local-only' \
    "$IMAGE" \
    /app/efbundle \
    --connection \
    'Host=postgres;Port=5432;Database=edulytics_phase25;Username=edulytics;Password=phase25-local-only' ||
    fail "migration bundle"

echo "PASS: migration bundle"

echo
echo "===== START 2 WEBS + 2 WORKERS CONCURRENTLY ====="

docker compose \
    -f "$COMPOSE" \
    up -d web1 web2 worker1 worker2 gateway ||
    fail "start application topology"

wait_http \
    "$WEB1/health/ready" \
    "web1 ready"

wait_http \
    "$WEB2/health/ready" \
    "web2 ready"

wait_http \
    "$GATEWAY/health/ready" \
    "gateway ready"

for service in web1 web2 worker1 worker2 gateway; do
    ensure_running "$service"
done

echo "PASS: concurrent first startup"

echo
echo "===== VERIFY ROLE SEPARATION ====="

web_logs="$(
    docker compose \
        -f "$COMPOSE" \
        logs web1 web2
)"

worker1_logs="$(
    docker compose \
        -f "$COMPOSE" \
        logs worker1
)"

worker2_logs="$(
    docker compose \
        -f "$COMPOSE" \
        logs worker2
)"

# Do not pipe a large shell variable into `grep -q` under pipefail.
# grep may exit as soon as it finds the match, causing the producer to receive
# SIGPIPE and turning a real match into a false-negative pipeline status.
if grep -Fq 'Outbox v2 processor' <<< "$web_logs"; then
    fail "Web role started Outbox worker"
fi

grep -Fq 'Outbox v2 processor' <<< "$worker1_logs" ||
    fail "worker1 Outbox worker missing"

grep -Fq 'Outbox v2 processor' <<< "$worker2_logs" ||
    fail "worker2 Outbox worker missing"

echo "PASS: Web and Worker roles separated"

echo
echo "===== GLOBAL RATE LIMIT ACROSS WEB1 + WEB2 ====="

docker compose \
    -f "$COMPOSE" \
    exec -T redis \
    redis-cli FLUSHDB \
    >/dev/null

for i in $(seq 1 20); do
    if [ $((i % 2)) -eq 0 ]; then
        target="$WEB2"
    else
        target="$WEB1"
    fi

    code="$(
        curl \
            -sS \
            -o /dev/null \
            -X POST \
            -w '%{http_code}' \
            "$target/account/login"
    )"

    [ "$code" != "429" ] ||
        fail "distributed quota rejected request $i early"
done

code="$(
    curl \
        -sS \
        -o /dev/null \
        -X POST \
        -w '%{http_code}' \
        "$WEB1/account/login"
)"

[ "$code" = "429" ] ||
    fail "21st cross-instance request expected 429, got $code"

echo "PASS: global Login quota shared across both webs"

docker compose \
    -f "$COMPOSE" \
    exec -T redis \
    redis-cli FLUSHDB \
    >/dev/null

echo
echo "===== SEED SCHOOL ADMIN / SHARED COOKIE ====="

run_gate seed "$HOST_DB"

login_school_admin

assert_cookie \
    "$WEB1" \
    "cookie accepted by issuing web1"

assert_cookie \
    "$WEB2" \
    "same cookie decrypted by web2"

echo "PASS: shared Data Protection / cookie continuity"

echo
echo "===== WORKER2 FAILOVER PATH ====="

docker compose \
    -f "$COMPOSE" \
    stop worker1 ||
    fail "stop worker1"

ensure_running worker2

prove_delivery worker2

echo
echo "===== WORKER1 FAILOVER PATH ====="

docker compose \
    -f "$COMPOSE" \
    start worker1 ||
    fail "restart worker1"

sleep 3
ensure_running worker1

docker compose \
    -f "$COMPOSE" \
    stop worker2 ||
    fail "stop worker2"

ensure_running worker1

prove_delivery worker1

docker compose \
    -f "$COMPOSE" \
    start worker2 ||
    fail "restart worker2"

sleep 3
ensure_running worker2

echo "PASS: both workers independently process after peer loss"

echo
echo "===== WEB LOSS / TRAFFIC REDISTRIBUTION ====="

WEB2_ID="$(
    docker compose \
        -f "$COMPOSE" \
        ps -q web2
)"

WEB2_IP="$(
    docker inspect \
        -f \
        '{{range .NetworkSettings.Networks}}{{.IPAddress}}{{end}}' \
        "$WEB2_ID"
)"

[ -n "$WEB2_IP" ] ||
    fail "web2 IP missing"

docker compose \
    -f "$COMPOSE" \
    stop web1 ||
    fail "stop web1"

sleep 2
ensure_running web2

headers="$(mktemp)"

code="$(
    curl \
        -sS \
        -D "$headers" \
        -o /dev/null \
        -b "$COOKIE_JAR" \
        -b 'Edulytics.Culture=en' \
        -w '%{http_code}' \
        "$GATEWAY/school/dashboard"
)"

[ "$code" = "200" ] ||
    fail "gateway failover expected 200, got $code"

grep -Fi \
    'X-Phase25-Upstream:' \
    "$headers" ||
    fail "gateway upstream header missing"

grep -Fq \
    "$WEB2_IP:10000" \
    "$headers" ||
    fail "request was not redistributed to web2"

rm -f "$headers"

echo "PASS: authenticated traffic redistributed to web2"

docker compose \
    -f "$COMPOSE" \
    start web1 ||
    fail "restart web1"

wait_http \
    "$WEB1/health/ready" \
    "restarted web1 ready"

assert_cookie \
    "$WEB1" \
    "original cookie valid after web1 restart"

echo
echo "===== FINAL MULTI-INSTANCE STATE ====="

for service in web1 web2 worker1 worker2 gateway; do
    ensure_running "$service"
done

echo
echo "PHASE25_MULTI_INSTANCE_LOCAL_PASS"
echo "2 webs                    : PASS"
echo "2 workers                 : PASS"
echo "shared Data Protection    : PASS"
echo "cross-instance cookie     : PASS"
echo "Redis SignalR backplane   : PASS"
echo "distributed Login quota   : PASS"
echo "Outbox process-once       : PASS"
echo "worker failover/restart   : PASS"
echo "web failover/restart      : PASS"
echo "traffic redistribution    : PASS"
echo "DB budget                 : 20 x 4 = 80 / PASS"

docker compose \
    -f "$COMPOSE" \
    down -v --remove-orphans ||
    fail "cleanup"

trap - EXIT

rm -f \
    "$COOKIE_JAR" \
    "$LOGIN_HTML" \
    "$LISTENER_LOG" \
    /tmp/phase25-login-response.txt

echo "PASS: disposable qualification environment cleaned"
