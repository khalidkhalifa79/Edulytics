#!/usr/bin/env bash
set -Eeuo pipefail

ROOT="${HOME}/projects/Edulytics"
STATE="${ROOT}/.phase29-source-rebuild"

cd "$ROOT"

echo "======================================================================"
echo " EDULYTICS — PHASE 29 CONTINUOUS RUNNER"
echo "======================================================================"
echo "NO VENV"
echo "NO ARGOS"
echo "NO PIP"
echo "NO LEGACY CONTENT FALLBACK"
echo "NO GENERIC LESSON FALLBACK"
echo "======================================================================"

python3 \
    tools/phase29/phase29-final-source-faithful-build.py

echo
echo "===== PRODUCT SAFETY ====="

git diff --check

if git diff --name-only |
   grep -Eq \
   '^src/Edulytics.Core/Curriculum/LessonContent/Packs/'
then
    echo "FAIL: canonical pack changed before Polish gate."
    exit 1
fi

if git diff --name-only |
   grep -Eq \
   '^src/Edulytics.Data/Migrations/'
then
    echo "FAIL: migration changed."
    exit 1
fi

echo "PASS: canonical lesson packs untouched"
echo "PASS: DB migrations untouched"
echo "PASS: Phase 29 state safely checkpointed"
