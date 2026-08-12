#!/usr/bin/env bash
set -euo pipefail

cd "$(dirname "$0")/.."

if [ -z "${1:-}" ]; then
  echo "Usage: ./scripts/update-database.sh \"<connection-string-or-configuration-name>\""
  echo "Example: ./scripts/update-database.sh \"Server=(localdb)\\MSSQLLocalDB;Database=EdulyticsDev;Trusted_Connection=True;MultipleActiveResultSets=true\""
  exit 1
fi

CONNECTION_STRING_OR_NAME="$1"

dotnet ef database update \
  --project src/Edulytics.Data/Edulytics.Data.csproj \
  --startup-project src/Edulytics.Web/Edulytics.Web.csproj \
  --connection "$CONNECTION_STRING_OR_NAME"
