#!/usr/bin/env bash
set -euo pipefail

cd "$(git rev-parse --show-toplevel)"

echo "============================================================"
echo " EDULYTICS — PHASE 08 VERIFICATION"
echo "============================================================"

dotnet build Edulytics.sln --no-restore

dotnet test Edulytics.sln \
  --no-build \
  --no-restore \
  --filter "FullyQualifiedName~Edulytics.Tests.Phase08"

dotnet test Edulytics.sln \
  --no-build \
  --no-restore

if grep -R --include='*Controller.cs' \
    -nE '\bEdulyticsDbContext\b|\bDbContext\b' \
    src/Edulytics.Web/Controllers
then
    echo "FAIL: DbContext detected in controller."
    exit 1
fi

if grep -R --include='*Controller.cs' \
    -niE '\[Http(Get|Post)[^]]*register|Register\(' \
    src/Edulytics.Web/Controllers
then
    echo "FAIL: public registration surface detected."
    exit 1
fi

python3 <<'PY'
import xml.etree.ElementTree as ET

def keys(path):
    root = ET.parse(path).getroot()
    return sorted(x.attrib["name"] for x in root.findall("data"))

en = keys("src/Edulytics.Web/Resources/AssessmentResource.resx")
pl = keys("src/Edulytics.Web/Resources/AssessmentResource.pl.resx")

if en != pl:
    raise SystemExit("Assessment EN/PL resource parity failed.")

print("PASS: assessment EN/PL parity.")
PY

grep -q 'Property(x => x.RowVersion).IsRequired()' \
  src/Edulytics.Data/Configurations/AssessmentConfiguration.cs

grep -q 'IsConcurrencyToken()' \
  src/Edulytics.Data/Configurations/AssessmentConfiguration.cs

grep -q 'ValueGeneratedNever()' \
  src/Edulytics.Data/Configurations/AssessmentConfiguration.cs

grep -q 'Property(x => x.RowVersion).IsRequired()' \
  src/Edulytics.Data/Configurations/AssessmentResultConfiguration.cs

grep -q 'IsConcurrencyToken()' \
  src/Edulytics.Data/Configurations/AssessmentResultConfiguration.cs

grep -q 'ValueGeneratedNever()' \
  src/Edulytics.Data/Configurations/AssessmentResultConfiguration.cs

grep -q 'ResolveEligibleFrameworkVersionIds' \
  src/Edulytics.Services/Assessments/AssessmentService.cs

grep -q 'SchoolCurriculumAdoptions' \
  src/Edulytics.Data/Repositories/AssessmentRepository.cs

grep -q 'AcademicYearId is null' \
  src/Edulytics.Services/Assessments/AssessmentService.cs

grep -q 'YearSpecificAdoptionOverridesDefaultAdoption' \
  tests/Edulytics.Tests/Phase08/AssessmentServiceTests.cs

git diff --check

echo "PHASE 08 VERIFICATION: PASS"
