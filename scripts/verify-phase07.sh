#!/usr/bin/env bash
set -euo pipefail

cd "$(git rev-parse --show-toplevel)"

echo "============================================================"
echo " EDULYTICS — PHASE 07 VERIFICATION"
echo "============================================================"

dotnet build Edulytics.sln --no-restore

dotnet test Edulytics.sln \
  --no-build \
  --no-restore \
  --filter "FullyQualifiedName~Edulytics.Tests.Phase07"

dotnet test Edulytics.sln \
  --no-build \
  --no-restore

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
    echo "FAIL: public registration surface detected."
    exit 1
fi

python3 <<'PY'
from pathlib import Path
import xml.etree.ElementTree as ET

root = Path("src/Edulytics.Web/Resources")
en = root / "CurriculumResource.resx"
pl = root / "CurriculumResource.pl.resx"

def keys(path):
    tree = ET.parse(path)
    return sorted(
        item.attrib["name"]
        for item in tree.getroot().findall("data")
    )

if keys(en) != keys(pl):
    raise SystemExit("Curriculum resource parity failed.")

print("PASS: curriculum EN/PL parity.")
PY

git diff --check

echo "PHASE 07 VERIFICATION: PASS"
