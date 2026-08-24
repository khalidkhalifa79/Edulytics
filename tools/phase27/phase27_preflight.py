#!/usr/bin/env python3
from __future__ import annotations

import argparse
import os
import re
import sys
from urllib.parse import urlparse


REQUIRED_EVIDENCE = (
    "PHASE27_BACKUP_EVIDENCE",
    "PHASE27_ALERT_EVIDENCE",
    "PHASE27_ONCALL_CONTACT",
    "PHASE27_DNS_TLS_EVIDENCE",
    "PHASE27_PRODUCTION_SECRETS_EVIDENCE",
    "PHASE27_ACCEPTED_P1",
)


def fail(message: str) -> None:
    raise SystemExit("FAIL: " + message)


def truthy_evidence(name: str) -> str:
    value = os.environ.get(name, "").strip()
    if not value:
        fail(f"missing required production evidence: {name}")
    if value.lower() in {"yes", "true", "1", "ok", "pass"}:
        fail(
            f"{name} must contain concrete evidence/reference, not a bare boolean"
        )
    return value


def parse_connection(value: str) -> dict[str, str]:
    value = value.strip()
    if "://" in value:
        parsed = urlparse(value)
        result = {
            "host": parsed.hostname or "",
            "database": parsed.path.lstrip("/"),
        }
        query = parsed.query.lower()
        result["ssl"] = "sslmode=require" if "sslmode=require" in query else ""
        return result

    result: dict[str, str] = {}
    for item in value.split(";"):
        if "=" not in item:
            continue
        key, val = item.split("=", 1)
        result[key.strip().lower()] = val.strip()

    return {
        "host": result.get("host", ""),
        "database": result.get("database", ""),
        "ssl": result.get("ssl mode", result.get("sslmode", "")),
    }


def validate(
    *,
    base_url: str,
    runtime_connection: str,
    migration_connection: str,
    release_sha: str,
) -> None:
    parsed = urlparse(base_url)
    host = (parsed.hostname or "").lower()

    if parsed.scheme != "https":
        fail("production URL must use https")
    if not host or host in {"localhost", "127.0.0.1"}:
        fail("production URL host is invalid")
    if host == "staging.edulytiks.com" or "staging" in host:
        fail("production URL must not target staging")

    if not re.fullmatch(r"[0-9a-f]{40}", release_sha):
        fail("PHASE27_RELEASE_SHA must be an exact 40-character Git SHA")

    runtime = parse_connection(runtime_connection)
    migration = parse_connection(migration_connection)

    if not runtime["host"] or not migration["host"]:
        fail("database host could not be parsed")

    if "pooler" not in runtime["host"].lower():
        fail("runtime connection must use the approved pooled Neon endpoint")

    if "pooler" in migration["host"].lower():
        fail("migration connection must use a direct/non-pooler Neon endpoint")

    if runtime["database"] and migration["database"]:
        if runtime["database"] != migration["database"]:
            fail("runtime and migration connections target different databases")

    for name, connection in (
        ("runtime", runtime_connection),
        ("migration", migration_connection),
    ):
        lowered = connection.lower()
        if "ssl mode=disable" in lowered or "sslmode=disable" in lowered:
            fail(f"{name} production connection disables TLS")

    for name in REQUIRED_EVIDENCE:
        truthy_evidence(name)


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--self-test", action="store_true")
    args = parser.parse_args()

    if args.self_test:
        os.environ.update(
            {
                "PHASE27_BACKUP_EVIDENCE": "neon-recovery-window-recorded",
                "PHASE27_ALERT_EVIDENCE": "render-alert-test-recorded",
                "PHASE27_ONCALL_CONTACT": "operations-owner-recorded",
                "PHASE27_DNS_TLS_EVIDENCE": "production-tls-certificate-recorded",
                "PHASE27_PRODUCTION_SECRETS_EVIDENCE": "secret-inventory-recorded",
                "PHASE27_ACCEPTED_P1": "accepted-p1-list-recorded",
            }
        )
        validate(
            base_url="https://app.example.invalid",
            runtime_connection=(
                "Host=ep-example-pooler.eu-central-1.aws.neon.tech;"
                "Database=edulytics;Username=app;Password=x;SSL Mode=Require"
            ),
            migration_connection=(
                "Host=ep-example.eu-central-1.aws.neon.tech;"
                "Database=edulytics;Username=migrator;Password=x;SSL Mode=Require"
            ),
            release_sha="a" * 40,
        )
        print("PHASE27_PREFLIGHT_SELF_TEST_PASS")
        return

    base_url = os.environ.get("PHASE27_PRODUCTION_URL", "").strip()
    runtime_connection = os.environ.get(
        "ConnectionStrings__DefaultConnection", ""
    ).strip()
    migration_connection = os.environ.get(
        "ConnectionStrings__MigrationConnection", ""
    ).strip()
    release_sha = os.environ.get("PHASE27_RELEASE_SHA", "").strip().lower()

    if not base_url:
        fail("PHASE27_PRODUCTION_URL is missing")
    if not runtime_connection:
        fail("ConnectionStrings__DefaultConnection is missing")
    if not migration_connection:
        fail("ConnectionStrings__MigrationConnection is missing")
    if not release_sha:
        fail("PHASE27_RELEASE_SHA is missing")

    validate(
        base_url=base_url,
        runtime_connection=runtime_connection,
        migration_connection=migration_connection,
        release_sha=release_sha,
    )

    print("PHASE27_PRODUCTION_PREFLIGHT_PASS")
    print("Production URL:", base_url)
    print("Release SHA:", release_sha)
    print("PASS: evidence references are present")
    print("PASS: runtime connection is pooled")
    print("PASS: migration connection is direct/non-pooler")
    print("PASS: no credential value was printed")


if __name__ == "__main__":
    main()
