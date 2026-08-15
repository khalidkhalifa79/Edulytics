#!/usr/bin/env bash
set -euo pipefail

cd "$(git rev-parse --show-toplevel)"

echo "============================================================"
echo " EDULYTICS — PHASE 11 VERIFICATION"
echo "============================================================"

dotnet build \
    Edulytics.sln \
    --no-restore

dotnet test \
    Edulytics.sln \
    --no-build \
    --no-restore \
    --filter \
    "FullyQualifiedName~Edulytics.Tests.Phase11"

dotnet test \
    Edulytics.sln \
    --no-build \
    --no-restore

grep -q \
    'AddDataImportPhase11' \
    src/Edulytics.Web/Program.cs

grep -q \
    'BeginTransactionAsync' \
    src/Edulytics.Data/Repositories/ImportRepository.cs

grep -q \
    'SHA256.HashData' \
    src/Edulytics.Services/Imports/DataImportService.cs

grep -q \
    'ImportBatchCompleted' \
    src/Edulytics.Services/Imports/ImportPlanBuilder.cs

grep -q \
    'ImportBatchCompleted' \
    src/Edulytics.Web/Background/OutboxProcessorBackgroundService.cs

if grep -R \
    --include='*Controller.cs' \
    -nE '\bEdulyticsDbContext\b|\bDbContext\b' \
    src/Edulytics.Web/Controllers
then
    echo "FAIL: DbContext detected in controller."
    exit 1
fi

if grep -R \
    --include='*Controller.cs' \
    -niE '\[Http(Get|Post)[^]]*register|Register\(' \
    src/Edulytics.Web/Controllers
then
    echo "FAIL: public registration detected."
    exit 1
fi

python3 <<'PY'
from pathlib import Path
import xml.etree.ElementTree as ET

root = Path(
    "src/Edulytics.Web/Resources"
)

def keys(path):
    return sorted(
        node.attrib["name"]
        for node in
        ET.parse(path)
            .getroot()
            .findall("data")
    )

en = keys(
    root / "ImportResource.resx"
)

pl = keys(
    root / "ImportResource.pl.resx"
)

if en != pl:
    raise SystemExit(
        "FAIL: import localization parity."
    )

print(
    f"PASS: EN/PL parity = {len(en)} keys."
)
PY

git diff --check

echo "============================================================"
echo " PHASE 11 AUTOMATED VERIFICATION: PASS"
echo "============================================================"
