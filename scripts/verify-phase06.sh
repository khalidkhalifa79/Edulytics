#!/usr/bin/env bash
set +e

cd "$(git rev-parse --show-toplevel)" || exit 1

FAIL=0

echo "============================================================"
echo " EDULYTICS — PHASE 06 AUTOMATED VERIFICATION"
echo "============================================================"

dotnet build Edulytics.sln --no-restore
[ $? -eq 0 ] || FAIL=1

dotnet test Edulytics.sln \
  --no-build --no-restore \
  --filter "FullyQualifiedName~Edulytics.Tests.Phase06"
[ $? -eq 0 ] || FAIL=1

dotnet test Edulytics.sln --no-build --no-restore
[ $? -eq 0 ] || FAIL=1

if grep -R --include='*Controller.cs' \
    -nE '\bEdulyticsDbContext\b|\bDbContext\b' \
    src/Edulytics.Web/Controllers
then
    echo "FAIL: DbContext detected in controller."
    FAIL=1
else
    echo "PASS: controllers are persistence-free."
fi

if grep -R --include='*Controller.cs' \
    -niE '\[Http(Get|Post)[^]]*register|Register\(' \
    src/Edulytics.Web/Controllers
then
    echo "FAIL: public registration surface detected."
    FAIL=1
else
    echo "PASS: no public registration."
fi

git diff --check
[ $? -eq 0 ] || FAIL=1

if [ "$FAIL" -eq 0 ]; then
    echo "PHASE 06 AUTOMATED VERIFICATION: PASS"
else
    echo "PHASE 06 AUTOMATED VERIFICATION: FAIL"
fi

exit "$FAIL"
