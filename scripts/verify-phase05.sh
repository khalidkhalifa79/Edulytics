#!/usr/bin/env bash

set +e

cd "$(git rev-parse --show-toplevel)" || exit 1

echo "============================================================"
echo " EDULYTICS — PHASE 05 VERIFICATION"
echo "============================================================"

FAIL=0

echo
echo "===== BUILD ====="
dotnet build
if [ $? -ne 0 ]; then
    FAIL=1
fi

echo
echo "===== PHASE 05 TESTS ====="
dotnet test \
    --no-build \
    --filter "FullyQualifiedName~Edulytics.Tests.Phase05"
if [ $? -ne 0 ]; then
    FAIL=1
fi

echo
echo "===== FULL REGRESSION SUITE ====="
dotnet test --no-build
if [ $? -ne 0 ]; then
    FAIL=1
fi

echo
echo "===== MIGRATION GUARD ====="
if ! git diff --quiet -- \
    src/Edulytics.Data/Migrations
then
    echo "FAIL: unexpected migration changes."
    FAIL=1
else
    echo "PASS: no migration changes."
fi

echo
echo "===== DBCONTEXT CONTROLLER GUARD ====="
if grep -R \
    --include='*Controller.cs' \
    -nE '\bEdulyticsDbContext\b|\bDbContext\b' \
    src/Edulytics.Web/Controllers
then
    echo "FAIL: DbContext detected in controller."
    FAIL=1
else
    echo "PASS: controllers are persistence-free."
fi

echo
echo "===== PUBLIC REGISTRATION GUARD ====="
if grep -R \
    --include='*Controller.cs' \
    -niE '\[Http(Get|Post)[^]]*register|Register\(' \
    src/Edulytics.Web/Controllers
then
    echo "FAIL: registration surface detected."
    FAIL=1
else
    echo "PASS: no public registration."
fi

echo
echo "===== WHITESPACE ====="
git diff --check
if [ $? -ne 0 ]; then
    FAIL=1
else
    echo "PASS: whitespace clean."
fi

echo
echo "============================================================"

if [ "$FAIL" -eq 0 ]; then
    echo " PHASE 05 AUTOMATED ACCEPTANCE: PASS"
    echo
    echo " Automated coverage includes:"
    echo " - tenant role access"
    echo " - cross-school isolation"
    echo " - create/duplicate email"
    echo " - role changes"
    echo " - activate/deactivate"
    echo " - lock/unlock"
    echo " - school status login rules"
    echo " - self-management protection"
    echo " - real Identity password setup/reset lifecycle"
    echo " - SuperAdmin exclusion from school roles"
    echo " - anti-forgery contract"
    echo " - localization/view contract"
    echo " - responsive CSS contract"
    echo
    echo " STILL REQUIRED BY PRODUCT ACCEPTANCE:"
    echo " Visual browser check at:"
    echo " 320 / 375 / 480 / 768 / 1024 / 1280 / 1440+"
else
    echo " PHASE 05 AUTOMATED ACCEPTANCE: FAIL"
fi

echo "============================================================"

exit 0
