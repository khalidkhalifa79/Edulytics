#!/usr/bin/env bash
set -Eeuo pipefail

cd "$(git rev-parse --show-toplevel)"

DOMAIN="${1:-}"

case "$DOMAIN" in
    architecture)
        FILTER='FullyQualifiedName~Architecture'
        ;;
    authorization)
        FILTER='FullyQualifiedName~Authorization'
        ;;
    tenancy)
        FILTER='FullyQualifiedName~Tenant|FullyQualifiedName~SchoolUser'
        ;;
    schools)
        FILTER='FullyQualifiedName~Phase04'
        ;;
    users)
        FILTER='FullyQualifiedName~Phase05'
        ;;
    academics)
        FILTER='FullyQualifiedName~Phase06'
        ;;
    curriculum)
        FILTER='FullyQualifiedName~Phase07'
        ;;
    assessments)
        FILTER='FullyQualifiedName~Phase08'
        ;;
    analytics)
        FILTER='FullyQualifiedName~Phase09'
        ;;
    realtime)
        FILTER='FullyQualifiedName~Phase10|FullyQualifiedName~Realtime'
        ;;
    imports)
        FILTER='FullyQualifiedName~Phase11|FullyQualifiedName~Import'
        ;;
    audit)
        FILTER='FullyQualifiedName~Phase18|FullyQualifiedName~Audit'
        ;;
    supervisors)
        FILTER='FullyQualifiedName~Phase19|FullyQualifiedName~SubjectSupervisor'
        ;;
    reports)
        FILTER='FullyQualifiedName~Phase20|FullyQualifiedName~Report'
        ;;
    notifications)
        FILTER='FullyQualifiedName~Phase21|FullyQualifiedName~Notification'
        ;;
    operations)
        FILTER='FullyQualifiedName~Phase22|FullyQualifiedName~Operations'
        ;;
    security)
        FILTER='FullyQualifiedName~Phase23|FullyQualifiedName~Security'
        ;;
    production)
        FILTER='FullyQualifiedName~Phase12|FullyQualifiedName~Phase13|FullyQualifiedName~Phase14|FullyQualifiedName~Phase15|FullyQualifiedName~Phase16|FullyQualifiedName~Phase17'
        ;;
    *)
        echo "Usage: $0 <domain>"
        echo
        echo "Domains:"
        echo "  architecture authorization tenancy schools users academics"
        echo "  curriculum assessments analytics realtime imports audit"
        echo "  supervisors reports notifications operations security production"
        exit 2
        ;;
esac

echo "DOMAIN=$DOMAIN"
echo "FILTER=$FILTER"

dotnet test \
    tests/Edulytics.Tests/Edulytics.Tests.csproj \
    --no-restore \
    --filter "$FILTER" \
    --logger "console;verbosity=minimal"
