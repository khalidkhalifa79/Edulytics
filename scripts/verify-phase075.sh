#!/usr/bin/env bash
set -euo pipefail
cd "$(git rev-parse --show-toplevel)"

echo "============================================================"
echo " EDULYTICS — PHASE 07.5 VERIFICATION"
echo "============================================================"

dotnet build Edulytics.sln --no-restore

dotnet test Edulytics.sln \
  --no-build \
  --no-restore \
  --filter "FullyQualifiedName~Edulytics.Tests.Phase07|FullyQualifiedName~Edulytics.Tests.Phase075"

dotnet test Edulytics.sln --no-build --no-restore

if grep -R --include='*Controller.cs' -nE '\bEdulyticsDbContext\b|\bDbContext\b' src/Edulytics.Web/Controllers; then
  echo "FAIL: DbContext detected in controller."
  exit 1
fi

python3 <<'PY'
from pathlib import Path
import xml.etree.ElementTree as ET

root = Path("src/Edulytics.Web/Resources")

def keys(path):
    tree = ET.parse(path)
    return sorted(
        node.attrib["name"]
        for node in tree.getroot().findall("data")
    )

if keys(root / "CurriculumResource.resx") != keys(root / "CurriculumResource.pl.resx"):
    raise SystemExit("Curriculum EN/PL resource parity failed.")

print("PASS: curriculum EN/PL parity.")
PY

grep -q 'FrameworkVersionId' src/Edulytics.Core/Entities/CurriculumTopic.cs
grep -q 'FrameworkVersionId' src/Edulytics.Core/Entities/LearningOutcome.cs
grep -q 'SubjectId' src/Edulytics.Core/Entities/LearningOutcome.cs
grep -q 'GradeLevelId' src/Edulytics.Core/Entities/LearningOutcome.cs

git diff --check

echo "PHASE 07.5 VERIFICATION: PASS"
