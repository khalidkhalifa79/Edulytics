#!/usr/bin/env bash
set -Eeuo pipefail

ROOT="$(git rev-parse --show-toplevel)"
cd "$ROOT"

export DOTNET_CLI_TELEMETRY_OPTOUT=1
export DOTNET_NOLOGO=1

test -x docker/render-entrypoint.sh
test -x docker/phase27-predeploy.sh
test -x tools/phase27/phase27_preflight.py
test -x tools/phase27/phase27_public_smoke.py

grep -Fq \
    'Edulytics__Deployment__RunStartupMigrations:-false' \
    docker/render-entrypoint.sh

grep -Fq \
    'Edulytics__Deployment__RunStartupMigrations' \
    render.yaml

grep -Fq \
    'COPY docker/phase27-predeploy.sh /app/phase27-predeploy.sh' \
    Dockerfile

grep -Fq \
    'PostgreSQL / Neon Backup and Restore Runbook' \
    docs/BACKUP_RESTORE_RUNBOOK.md

grep -Fq \
    'PostgreSQL/Neon' \
    docs/MONITORING_RUNBOOK.md

python3 -m py_compile \
    tools/phase27/phase27_preflight.py \
    tools/phase27/phase27_public_smoke.py

python3 tools/phase27/phase27_preflight.py --self-test
python3 tools/phase27/phase27_public_smoke.py --self-test

dotnet test \
    tests/Edulytics.Tests/Edulytics.Tests.csproj \
    -c Release \
    --no-restore \
    --filter 'FullyQualifiedName~Edulytics.Tests.Phase27'

git diff --check

echo "PHASE27_LOCAL_CONTRACT_PASS"
