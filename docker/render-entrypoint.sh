#!/bin/sh
set -eu

if [ "$#" -gt 0 ]; then
    exec "$@"
fi

exec dotnet Edulytics.Web.dll \
    --urls "http://0.0.0.0:${PORT:-10000}"
