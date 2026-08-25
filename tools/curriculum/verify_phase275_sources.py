#!/usr/bin/env python3
from __future__ import annotations

import hashlib
import json
import os
import pathlib
import ssl
import sys
import time
import urllib.error
import urllib.request

OUT = pathlib.Path("/tmp/edulytics-phase275-official-sources")
OUT.mkdir(parents=True, exist_ok=True)

SOURCES = [
    {
        "source_id": "uk-national-curriculum-math",
        "url": "https://www.gov.uk/government/publications/national-curriculum-in-england-mathematics-programmes-of-study/national-curriculum-in-england-mathematics-programmes-of-study",
        "required": True,
        "markers": ["National curriculum in England", "mathematics", "Year 1"],
    },
    {
        "source_id": "uk-as-a-level-math",
        "url": "https://www.gov.uk/government/publications/gce-as-and-a-level-mathematics",
        "required": True,
        "markers": ["AS and A level", "math"],
    },
    {
        "source_id": "common-core-math-html",
        "url": "https://corestandards.org/mathematics-standards/",
        "required": True,
        "markers": ["Mathematics Standards", "Common Core"],
    },
    {
        "source_id": "common-core-math-pdf",
        "url": "https://corestandards.org/wp-content/uploads/2023/09/Math_Standards1.pdf",
        "required": True,
        "binary": True,
        "min_bytes": 100000,
    },
    {
        "source_id": "uae-moe-portal",
        "url": "https://www.moe.gov.ae/",
        "required": True,
        "markers": ["Ministry", "Education"],
    },
    {
        "source_id": "uae-assessment-policy-2025-2026",
        "url": "https://www.moe.gov.ae/en/guides/Pages/Student-Assessment-Policy-Guide-2025-2026.aspx",
        "required": False,
        "markers": ["Assessment", "2025"],
    },
    {
        "source_id": "poland-early",
        "url": "https://zpe.gov.pl/podstawa-programowa/edukacja-wczesnoszkolna",
        "required": True,
        "markers": ["Podstawa", "programowa"],
    },
    {
        "source_id": "poland-primary-math",
        "url": "https://zpe.gov.pl/podstawa-programowa/szkola-podstawowa/matematyka",
        "required": True,
        "markers": ["Matematyka", "2025/2026"],
    },
    {
        "source_id": "poland-upper-math",
        "url": "https://zpe.gov.pl/podstawa-programowa/szkola-ponadpodstawowa/matematyka",
        "required": True,
        "markers": ["Matematyka", "2025/2026"],
    },
]

ctx = ssl.create_default_context()
manifest = []

def fetch(item):
    req = urllib.request.Request(
        item["url"],
        headers={
            "User-Agent": "Edulytics-Phase27.5-SourceVerifier/1.0 (+https://edulytiks.com)"
        },
    )
    last = None
    for attempt in range(1, 4):
        try:
            with urllib.request.urlopen(req, timeout=30, context=ctx) as r:
                body = r.read()
                status = getattr(r, "status", 200)
                content_type = r.headers.get("Content-Type", "")
                return status, content_type, body
        except Exception as exc:
            last = exc
            if attempt < 3:
                time.sleep(attempt * 2)
    raise last

failures = []
for item in SOURCES:
    record = {
        "source_id": item["source_id"],
        "url": item["url"],
        "required": item["required"],
    }
    try:
        status, ctype, body = fetch(item)
        if status < 200 or status >= 400:
            raise RuntimeError(f"HTTP {status}")
        if item.get("binary"):
            if len(body) < item.get("min_bytes", 1):
                raise RuntimeError(f"binary source too small: {len(body)} bytes")
        else:
            text = body.decode("utf-8", errors="ignore")
            missing = [m for m in item.get("markers", []) if m.lower() not in text.lower()]
            if missing:
                raise RuntimeError(f"missing markers: {missing}")
        sha = hashlib.sha256(body).hexdigest()
        suffix = ".pdf" if "pdf" in ctype.lower() or item.get("binary") else ".html"
        (OUT / f"{item['source_id']}{suffix}").write_bytes(body)
        record.update(
            {
                "ok": True,
                "http_status": status,
                "content_type": ctype,
                "bytes": len(body),
                "sha256": sha,
            }
        )
        print(f"PASS {item['source_id']} status={status} bytes={len(body)} sha256={sha[:16]}...")
    except Exception as exc:
        record.update({"ok": False, "error": str(exc)})
        prefix = "FAIL" if item["required"] else "WARN"
        print(f"{prefix} {item['source_id']}: {exc}")
        if item["required"]:
            failures.append(item["source_id"])
    manifest.append(record)

manifest_path = OUT / "manifest.json"
manifest_path.write_text(
    json.dumps(manifest, ensure_ascii=False, indent=2) + "\n",
    encoding="utf-8",
)

if failures:
    print("Required official-source verification failed:", ", ".join(failures), file=sys.stderr)
    sys.exit(1)

print(f"PHASE275_OFFICIAL_SOURCE_VERIFICATION_PASS manifest={manifest_path}")
