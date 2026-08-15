#!/usr/bin/env bash
set -euo pipefail

cd "$(git rev-parse --show-toplevel)"

echo "============================================================"
echo " EDULYTICS — PHASE 09 VERIFICATION"
echo "============================================================"

dotnet build Edulytics.sln --no-restore

dotnet test \
    Edulytics.sln \
    --no-build \
    --no-restore \
    --filter \
    "FullyQualifiedName~Edulytics.Tests.Phase09"

dotnet test \
    Edulytics.sln \
    --no-build \
    --no-restore

echo
echo "===== CONTROLLER PERSISTENCE GUARD ====="

if grep -R \
    --include='*Controller.cs' \
    -nE '\bEdulyticsDbContext\b|\bDbContext\b' \
    src/Edulytics.Web/Controllers
then
    echo "FAIL: DbContext detected in controller."
    exit 1
fi

echo "PASS: controllers remain persistence-free."

echo
echo "===== PUBLIC REGISTRATION GUARD ====="

if grep -R \
    --include='*Controller.cs' \
    -niE '\[Http(Get|Post)[^]]*register|Register\(' \
    src/Edulytics.Web/Controllers
then
    echo "FAIL: public registration surface detected."
    exit 1
fi

echo "PASS: no public registration."

echo
echo "===== ANALYTICS RESOURCE PARITY ====="

python3 <<'PY'
import xml.etree.ElementTree as ET

def keys(path):
    root = ET.parse(path).getroot()
    return sorted(
        x.attrib["name"]
        for x in root.findall("data")
    )

en = keys(
    "src/Edulytics.Web/Resources/"
    "AnalyticsResource.resx"
)

pl = keys(
    "src/Edulytics.Web/Resources/"
    "AnalyticsResource.pl.resx"
)

if en != pl:
    raise SystemExit(
        "FAIL: Analytics EN/PL resource keys differ."
    )

if not en:
    raise SystemExit(
        "FAIL: Analytics resources are empty."
    )

print(
    f"PASS: Analytics EN/PL parity = {len(en)} keys."
)
PY

echo
echo "===== ARCHITECTURE GUARDS ====="

grep -q \
    'DbSet<StudentOutcomeMastery>' \
    src/Edulytics.Data/Contexts/EdulyticsDbContext.cs

grep -q \
    'IAnalyticsRepository' \
    src/Edulytics.Data/Repositories/AnalyticsRepository.cs

grep -q \
    'IAnalyticsService' \
    src/Edulytics.Web/Controllers/AnalyticsController.cs

grep -q \
    'RoleNames.Teacher' \
    src/Edulytics.Web/Extensions/AnalyticsRegistrationExtensions.cs

grep -q \
    'RoleNames.SchoolAdmin' \
    src/Edulytics.Web/Extensions/AnalyticsRegistrationExtensions.cs

grep -q \
    'RecalculationRequiresSchoolAdmin' \
    src/Edulytics.Services/Analytics/AnalyticsService.cs

echo "PASS: analytics layer boundaries detected."

echo
echo "===== WHITESPACE ====="

git diff --check

echo "PASS: whitespace clean."

echo
echo "============================================================"
echo " PHASE 09 AUTOMATED VERIFICATION: PASS"
echo "============================================================"
