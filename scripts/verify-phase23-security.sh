#!/usr/bin/env bash
set -euo pipefail

ROOT="$(
    cd "$(dirname "${BASH_SOURCE[0]}")/.."
    pwd
)"

cd "$ROOT"

grep -q \
    'Content-Security-Policy' \
    src/Edulytics.Web/Middleware/SecurityHeadersMiddleware.cs

grep -q \
    "script-src 'self' 'nonce-" \
    src/Edulytics.Web/Middleware/SecurityHeadersMiddleware.cs

grep -q \
    "script-src-attr 'none'" \
    src/Edulytics.Web/Middleware/SecurityHeadersMiddleware.cs

if grep -E \
    'script-src[^;]*unsafe-inline' \
    src/Edulytics.Web/Middleware/SecurityHeadersMiddleware.cs
then
    echo "FAIL: script-src allows unsafe-inline"
    exit 1
fi

python3 <<'PY2'
from pathlib import Path
import json
import re
import xml.etree.ElementTree as ET

prod = json.loads(
    Path(
        "src/Edulytics.Web/"
        "appsettings.Production.json"
    ).read_text(encoding="utf-8")
)

hosts = prod.get("AllowedHosts", "")

if not hosts or hosts.strip() == "*":
    raise SystemExit(
        "FAIL: Production AllowedHosts is wildcard."
    )

views = Path(
    "src/Edulytics.Web/Views"
)

positive_tabindex = re.compile(
    r'\btabindex\s*=\s*["\']?\s*[1-9]',
    re.I
)

event_handler = re.compile(
    r'\son[a-z]+\s*=',
    re.I
)

image_tag = re.compile(
    r'<img\b[^>]*>',
    re.I | re.S
)

alt_attr = re.compile(
    r'\balt\s*=',
    re.I
)

aria_hidden = re.compile(
    r'\baria-hidden\s*=\s*["\']true["\']',
    re.I
)

for path in views.rglob("*.cshtml"):
    text = path.read_text(
        encoding="utf-8",
        errors="replace"
    )

    if positive_tabindex.search(text):
        raise SystemExit(
            f"FAIL: positive tabindex: {path}"
        )

    if event_handler.search(text):
        raise SystemExit(
            f"FAIL: inline JS event attribute: {path}"
        )

    for match in image_tag.finditer(text):
        tag = match.group(0)

        if not (
            alt_attr.search(tag) or
            aria_hidden.search(tag)
        ):
            raise SystemExit(
                f"FAIL: image without alt/aria-hidden: "
                f"{path}: {tag[:120]}"
            )

layout = (
    views /
    "Shared/_Layout.cshtml"
).read_text(encoding="utf-8")

if 'lang="@pageLanguage"' not in layout:
    raise SystemExit(
        "FAIL: document language is not dynamic."
    )

# Dependency governance: reject floating versions.
for csproj in Path(".").rglob("*.csproj"):
    tree = ET.parse(csproj)

    for node in tree.findall(".//PackageReference"):
        version = (
            node.attrib.get("Version")
            or node.findtext("Version")
            or ""
        ).strip()

        if "*" in version:
            raise SystemExit(
                f"FAIL: floating NuGet version "
                f"{csproj}: "
                f"{node.attrib.get('Include')}"
            )

print("PASS: AllowedHosts fail-closed")
print("PASS: accessibility static checks")
print("PASS: dependency version governance")
PY2

grep -q \
    'EnableRateLimiting("Login")' \
    src/Edulytics.Web/Controllers/AccountController.cs

grep -q \
    'EnableRateLimiting("OperationalMutation")' \
    src/Edulytics.Web/Controllers/OperationsController.cs

grep -q \
    'x => x.ConcurrencyStamp' \
    src/Edulytics.Data/Configurations/ApplicationUserConfiguration.cs

grep -q \
    'ISensitiveDataRetentionRepository' \
    src/Edulytics.Data/Repositories/SensitiveDataRetentionRepository.cs

if grep -E \
    '\.Description|\.Data|totalDurationMs|checks[[:space:]]*=' \
    src/Edulytics.Web/Health/HealthResponseWriter.cs
then
    echo "FAIL: detailed anonymous health information remains"
    exit 1
fi

git diff --check

echo "PHASE23_SECURITY_STATIC_GATE_PASS"
