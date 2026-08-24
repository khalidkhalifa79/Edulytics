#!/usr/bin/env python3
from __future__ import annotations

import argparse
import re
import ssl
import time
from http.cookiejar import CookieJar
from html.parser import HTMLParser
from urllib.error import HTTPError
from urllib.parse import urlencode, urljoin, urlparse
from urllib.request import (
    HTTPCookieProcessor,
    HTTPSHandler,
    Request,
    build_opener,
)

LOCKED_HOST = "staging.edulytiks.com"


def fail(message: str) -> None:
    raise SystemExit("FAIL: " + message)


class Form:
    def __init__(self, action: str, fields: dict[str, str]) -> None:
        self.action = action
        self.fields = fields


class FormParser(HTMLParser):
    def __init__(self) -> None:
        super().__init__(convert_charrefs=True)
        self.forms: list[Form] = []
        self.current: Form | None = None

    def handle_starttag(self, tag: str, attrs) -> None:
        a = dict(attrs)
        if tag == "form":
            self.current = Form(a.get("action", ""), {})
            return

        if self.current is not None and tag in {
            "input",
            "button",
            "textarea",
        }:
            name = a.get("name")
            if name:
                self.current.fields.setdefault(
                    name,
                    a.get("value", ""),
                )

    def handle_endtag(self, tag: str) -> None:
        if tag == "form" and self.current is not None:
            self.forms.append(self.current)
            self.current = None


def forms(markup: str) -> list[Form]:
    parser = FormParser()
    parser.feed(markup)
    return parser.forms


def validate_base(base: str) -> None:
    parsed = urlparse(base)
    if parsed.scheme != "https":
        fail("Phase 27 free readiness requires HTTPS")
    if (parsed.hostname or "").lower() != LOCKED_HOST:
        fail(
            "Phase 27 free readiness is locked to "
            f"https://{LOCKED_HOST}"
        )


class Browser:
    def __init__(self, base: str) -> None:
        self.base = base.rstrip("/") + "/"
        self.jar = CookieJar()
        self.opener = build_opener(
            HTTPCookieProcessor(self.jar),
            HTTPSHandler(context=ssl.create_default_context()),
        )

    def request(
        self,
        path: str,
        data: dict[str, str] | None = None,
    ):
        headers = {
            "User-Agent": "Edulytics-Phase27-FreeReadiness/1.0",
            "Accept": "text/html,application/xhtml+xml",
        }
        body = None

        if data is not None:
            body = urlencode(data).encode()
            headers["Content-Type"] = (
                "application/x-www-form-urlencoded"
            )

        req = Request(
            urljoin(self.base, path),
            data=body,
            headers=headers,
            method="POST" if data is not None else "GET",
        )

        try:
            with self.opener.open(req, timeout=30) as response:
                raw = response.read(256 * 1024)
                charset = (
                    response.headers.get_content_charset()
                    or "utf-8"
                )
                return (
                    response.geturl(),
                    response.status,
                    {
                        k.lower(): v
                        for k, v in response.headers.items()
                    },
                    raw.decode(
                        charset,
                        errors="replace",
                    ),
                )
        except HTTPError as exc:
            return (
                exc.geturl(),
                exc.code,
                {
                    k.lower(): v
                    for k, v in exc.headers.items()
                },
                exc.read().decode(
                    "utf-8",
                    errors="replace",
                ),
            )


def set_culture(
    browser: Browser,
    culture: str,
) -> str:
    _, status, _, home = browser.request("/")
    if status != 200:
        fail(f"home returned HTTP {status}")

    matches = [
        f
        for f in forms(home)
        if (
            f.action.split("?", 1)[0]
            .rstrip("/")
            .endswith("/set-culture")
        )
        and f.fields.get("culture") == culture
    ]

    if len(matches) != 1:
        fail(
            f"expected one {culture} culture form; "
            f"found {len(matches)}"
        )

    data = dict(matches[0].fields)
    data["culture"] = culture
    data["returnUrl"] = "/account/login"

    browser.request(matches[0].action, data)

    _, status, _, page = browser.request(
        "/account/login"
    )

    if status != 200:
        fail(
            f"{culture} login page returned HTTP {status}"
        )

    if not re.search(
        rf"<html[^>]*\blang=[\"']"
        rf"{re.escape(culture)}[\"']",
        page,
        re.IGNORECASE,
    ):
        fail(
            f"{culture} login page html lang mismatch"
        )

    return page


def wait_health(
    browser: Browser,
    path: str,
    *,
    timeout_seconds: int = 600,
) -> None:
    deadline = time.monotonic() + timeout_seconds
    attempt = 0
    last_status = 0

    while True:
        attempt += 1
        _, status, _, _ = browser.request(path)
        last_status = status

        print(
            f"HEALTH_WAIT: path={path} "
            f"attempt={attempt} status={status}"
        )

        if status == 200:
            print(f"PASS: {path} = 200")
            return

        if time.monotonic() >= deadline:
            fail(
                f"{path} did not become healthy "
                f"within {timeout_seconds}s; "
                f"last HTTP {last_status}"
            )

        time.sleep(10)


def run(base: str) -> None:
    validate_base(base)
    browser = Browser(base)

    # Render Free can spin down after inactivity. The first request
    # may wake the service instead of immediately reaching the app.
    wait_health(browser, "/health/live")
    wait_health(browser, "/health/ready")

    _, status, headers, _ = browser.request("/")
    if status != 200:
        fail(f"/ returned HTTP {status}")

    for header in (
        "content-security-policy",
        "x-content-type-options",
        "referrer-policy",
    ):
        if header not in headers:
            fail(
                f"security header missing: {header}"
            )

    print("PASS: public security headers")

    en = set_culture(browser, "en")
    pl = set_culture(browser, "pl")

    if en == pl:
        fail(
            "EN and PL login pages are "
            "unexpectedly byte-identical"
        )

    print("PASS: EN public entry")
    print("PASS: PL public entry")
    print(
        "PHASE27_FREE_ENVIRONMENT_READINESS_PASS"
    )


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--base-url")
    parser.add_argument(
        "--self-test",
        action="store_true",
    )
    args = parser.parse_args()

    if args.self_test:
        validate_base(
            "https://staging.edulytiks.com"
        )

        rejected = 0
        for bad in (
            "http://staging.edulytiks.com",
            "https://edulytiks.com",
            "https://app.edulytiks.com",
        ):
            try:
                validate_base(bad)
            except SystemExit:
                rejected += 1

        if rejected != 3:
            fail(
                "self-test did not reject all "
                "non-staging targets"
            )

        print(
            "PHASE27_FREE_READINESS_SELF_TEST_PASS"
        )
        return

    if not args.base_url:
        fail("--base-url is required")

    run(args.base_url)


if __name__ == "__main__":
    main()
