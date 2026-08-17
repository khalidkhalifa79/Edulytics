#!/usr/bin/env bash
set -euo pipefail

OUTPUT="${1:-artifacts/security/nuget-vulnerabilities.txt}"

mkdir -p "$(dirname "$OUTPUT")"

dotnet list Edulytics.sln \
    package \
    --vulnerable \
    --include-transitive \
    > "$OUTPUT"

cat "$OUTPUT"

if grep -qi \
    'has the following vulnerable packages' \
    "$OUTPUT"
then
    echo "FAIL: NuGet vulnerability gate detected a vulnerable package."
    exit 1
fi

echo "DEPENDENCY VULNERABILITY GATE: PASS"
