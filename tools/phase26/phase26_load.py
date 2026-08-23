#!/usr/bin/env python3
from __future__ import annotations

import argparse
import base64
import concurrent.futures
import getpass
import hashlib
import html as html_lib
import http.client
import json
import math
import os
import random
import re
import socket
import ssl
import statistics
import struct
import threading
import time
from dataclasses import dataclass
from datetime import datetime, timezone
from html.parser import HTMLParser
from http.cookiejar import CookieJar
from pathlib import Path
from urllib.error import HTTPError, URLError
from urllib.parse import urlencode, urljoin, urlparse, quote
from urllib.request import (
    HTTPCookieProcessor,
    HTTPSHandler,
    Request,
    build_opener,
)

LOCKED_HOST = "staging.edulytiks.com"


def die(message: str) -> None:
    raise SystemExit("FAIL: " + message)


class AuthRejected(Exception):
    pass


def percentile(values: list[float], p: float) -> float:
    if not values:
        return 0.0
    ordered = sorted(values)
    if len(ordered) == 1:
        return ordered[0]
    rank = (len(ordered) - 1) * p
    lo = math.floor(rank)
    hi = math.ceil(rank)
    if lo == hi:
        return ordered[lo]
    return ordered[lo] + (ordered[hi] - ordered[lo]) * (rank - lo)


@dataclass
class Form:
    action: str
    fields: dict[str, str]


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
        if self.current is not None and tag in {"input", "button", "textarea"}:
            name = a.get("name")
            if name:
                self.current.fields.setdefault(name, a.get("value", ""))

    def handle_endtag(self, tag: str) -> None:
        if tag == "form" and self.current is not None:
            self.forms.append(self.current)
            self.current = None


def forms(markup: str) -> list[Form]:
    p = FormParser()
    p.feed(markup)
    return p.forms


class Browser:
    def __init__(self, base: str) -> None:
        self.base = base.rstrip("/") + "/"
        self.jar = CookieJar()
        self.opener = build_opener(
            HTTPCookieProcessor(self.jar),
            HTTPSHandler(context=ssl.create_default_context()),
        )

    def request(self, path: str, data: dict[str, str] | None = None):
        url = urljoin(self.base, path)
        headers = {
            "User-Agent": "Edulytics-Phase26/1.0",
            "Accept": "text/html,application/xhtml+xml",
        }
        body = None
        if data is not None:
            body = urlencode(data).encode()
            headers["Content-Type"] = "application/x-www-form-urlencoded"

        req = Request(
            url,
            data=body,
            headers=headers,
            method="POST" if data is not None else "GET",
        )
        try:
            with self.opener.open(req, timeout=120) as response:
                raw = response.read()
                charset = response.headers.get_content_charset() or "utf-8"
                return (
                    response.geturl(),
                    response.status,
                    raw.decode(charset, errors="replace"),
                )
        except HTTPError as exc:
            body_text = exc.read().decode("utf-8", errors="replace")
            return exc.geturl(), exc.code, body_text

    def cookie_header(self) -> str:
        return "; ".join(f"{c.name}={c.value}" for c in self.jar)


def exact_form(markup: str, ending: str) -> Form:
    wanted = ending.rstrip("/")
    matches = [
        f
        for f in forms(markup)
        if f.action.split("?", 1)[0].rstrip("/").endswith(wanted)
    ]
    if len(matches) != 1:
        die(f"expected one form ending {ending}; found {len(matches)}")
    if "__RequestVerificationToken" not in matches[0].fields:
        die(f"anti-forgery token missing for {ending}")
    return matches[0]


def login(
    browser: Browser,
    email: str,
    password: str,
    verify_path: str,
    marker: str | None,
    actor_label: str,
) -> None:
    _, _, home = browser.request("/")
    culture = [
        f
        for f in forms(home)
        if f.action.split("?", 1)[0].rstrip("/").endswith("/set-culture")
        and f.fields.get("culture") == "en"
    ]
    if len(culture) == 1:
        data = dict(culture[0].fields)
        data["culture"] = "en"
        data["returnUrl"] = "/account/login"
        browser.request(culture[0].action, data)

    _, status, page = browser.request("/account/login")
    if status != 200:
        die(
            f"{actor_label} login page did not return HTTP 200 after culture setup"
        )

    form = exact_form(page, "/account/login")
    data = dict(form.fields)
    data["Email"] = email
    data["Password"] = password
    browser.request(form.action, data)

    url, status, body = browser.request(verify_path)
    lowered = url.lower()
    if (
        status != 200
        or "/account/login" in lowered
        or "/access-denied" in lowered
    ):
        raise AuthRejected(
            f"{actor_label} rejected: status={status} final_url={url}"
        )

    if marker is not None:
        visible = re.sub(r"<[^>]+>", " ", body)
        if marker not in visible:
            raise AuthRejected(
                f"{actor_label} verification marker {marker!r} missing"
            )


def interactive_login(
    base: str,
    email: str,
    verify_path: str,
    marker: str | None,
    actor_label: str,
) -> Browser:
    for attempt in range(1, 4):
        password = getpass.getpass(
            f"{actor_label} password (hidden; not saved): "
        )

        if not password:
            print(
                f"FAIL: empty {actor_label} password attempt "
                f"{attempt}/3; no load started."
            )
            continue

        browser = Browser(base)
        try:
            login(
                browser,
                email,
                password,
                verify_path,
                marker,
                actor_label,
            )
        except UnicodeEncodeError:
            print(
                f"FAIL: {actor_label} password input encoding invalid on "
                f"attempt {attempt}/3; no load started."
            )
            continue
        except AuthRejected as exc:
            print(
                f"FAIL: {exc} (attempt {attempt}/3); no load started."
            )
            continue
        finally:
            password = ""

        if not browser.cookie_header():
            print(
                f"FAIL: {actor_label} authenticated cookie missing on "
                f"attempt {attempt}/3; no load started."
            )
            continue

        print(f"PASS: {actor_label} authenticated")
        return browser

    die(
        f"{actor_label} authentication did not pass after three attempts; "
        "no live load was started"
    )


def request_once(
    host: str,
    cookie: str,
    path: str,
    timeout: float = 20.0,
) -> tuple[int, float, str | None]:
    started = time.perf_counter()
    conn = http.client.HTTPSConnection(
        host,
        timeout=timeout,
        context=ssl.create_default_context(),
    )
    try:
        conn.request(
            "GET",
            path,
            headers={
                "User-Agent": "Edulytics-Phase26-Load/1.0",
                "Accept": "text/html,application/xhtml+xml",
                "Cookie": cookie,
                "Connection": "keep-alive",
            },
        )
        response = conn.getresponse()
        response.read(128 * 1024)
        elapsed = (time.perf_counter() - started) * 1000.0
        return response.status, elapsed, None
    except Exception as exc:
        elapsed = (time.perf_counter() - started) * 1000.0
        return 0, elapsed, f"{type(exc).__name__}: {exc}"
    finally:
        try:
            conn.close()
        except Exception:
            pass


def classify(results: list[tuple[int, float, str | None]]) -> dict:
    latencies = [lat for _, lat, _ in results]
    counts: dict[str, int] = {}
    errors: list[str] = []
    unexpected = 0
    controlled = 0

    for status, _, error in results:
        counts[str(status)] = counts.get(str(status), 0) + 1
        if error:
            unexpected += 1
            if len(errors) < 20:
                errors.append(error)
        elif status in {429, 503}:
            controlled += 1
        elif status != 200:
            unexpected += 1

    total = len(results)
    return {
        "requests": total,
        "status_counts": counts,
        "p50_ms": round(percentile(latencies, 0.50), 2),
        "p95_ms": round(percentile(latencies, 0.95), 2),
        "p99_ms": round(percentile(latencies, 0.99), 2),
        "max_ms": round(max(latencies) if latencies else 0.0, 2),
        "unexpected": unexpected,
        "unexpected_rate": (unexpected / total) if total else 1.0,
        "controlled_shed": controlled,
        "errors": errors,
    }


def run_stage(
    host: str,
    cookie: str,
    routes: list[str],
    concurrency: int,
    duration: int,
    worker_delay: float = 1.0,
    progress_label: str | None = None,
    progress_interval: int = 300,
) -> dict:
    started_at = time.monotonic()
    stop = started_at + duration
    all_results: list[tuple[int, float, str | None]] = []
    lock = threading.Lock()
    progress_stop = threading.Event()

    def progress_worker() -> None:
        if not progress_label:
            return
        while not progress_stop.wait(progress_interval):
            elapsed = int(time.monotonic() - started_at)
            remaining = max(0, duration - elapsed)
            print(
                f"INFO: {progress_label} still running; "
                f"elapsed={elapsed}s remaining~={remaining}s",
                flush=True,
            )

    def worker(worker_id: int) -> None:
        local: list[tuple[int, float, str | None]] = []
        i = worker_id
        while time.monotonic() < stop:
            route = routes[i % len(routes)]
            local.append(request_once(host, cookie, route))
            i += 1
            remaining = stop - time.monotonic()
            if remaining > 0 and worker_delay > 0:
                time.sleep(min(worker_delay, remaining))
        with lock:
            all_results.extend(local)

    progress_thread = None
    if progress_label:
        progress_thread = threading.Thread(
            target=progress_worker,
            name="phase26-progress",
            daemon=True,
        )
        progress_thread.start()

    try:
        with concurrent.futures.ThreadPoolExecutor(max_workers=concurrency) as pool:
            futures = [pool.submit(worker, i) for i in range(concurrency)]
            for f in futures:
                f.result()
    finally:
        progress_stop.set()
        if progress_thread is not None:
            progress_thread.join(timeout=2)

    data = classify(all_results)
    data.update(
        {
            "concurrency": concurrency,
            "duration_seconds": duration,
            "worker_delay_seconds": worker_delay,
        }
    )
    return data


def run_spike(
    host: str,
    cookie: str,
    routes: list[str],
    concurrency: int,
    waves: int,
    pause: int,
) -> dict:
    results: list[tuple[int, float, str | None]] = []

    def one(i: int):
        return request_once(host, cookie, routes[i % len(routes)])

    for wave in range(waves):
        with concurrent.futures.ThreadPoolExecutor(max_workers=concurrency) as pool:
            results.extend(pool.map(one, range(concurrency)))
        if wave + 1 < waves:
            time.sleep(pause)

    data = classify(results)
    data.update(
        {
            "concurrency": concurrency,
            "waves": waves,
            "wave_pause_seconds": pause,
        }
    )
    return data


def recover(
    host: str,
    cookie: str,
    max_seconds: int,
) -> dict:
    start = time.monotonic()
    attempts = []
    while time.monotonic() - start <= max_seconds:
        health = request_once(host, "", "/health/ready")
        dashboard = request_once(host, cookie, "/platform/dashboard")
        attempts.append(
            {
                "elapsed_seconds": round(time.monotonic() - start, 2),
                "health": health[0],
                "dashboard": dashboard[0],
            }
        )
        if health[0] == 200 and dashboard[0] == 200:
            return {
                "recovered": True,
                "seconds": round(time.monotonic() - start, 2),
                "attempts": attempts,
            }
        time.sleep(2)
    return {
        "recovered": False,
        "seconds": round(time.monotonic() - start, 2),
        "attempts": attempts,
    }


def masked_frame(payload: bytes, opcode: int = 1) -> bytes:
    first = 0x80 | opcode
    mask = os.urandom(4)
    n = len(payload)
    if n < 126:
        header = bytes([first, 0x80 | n])
    elif n < 65536:
        header = bytes([first, 0x80 | 126]) + struct.pack("!H", n)
    else:
        header = bytes([first, 0x80 | 127]) + struct.pack("!Q", n)
    masked = bytes(payload[i] ^ mask[i % 4] for i in range(n))
    return header + mask + masked


def recv_headers(sock: ssl.SSLSocket) -> bytes:
    data = b""
    while b"\r\n\r\n" not in data and len(data) < 65536:
        chunk = sock.recv(4096)
        if not chunk:
            break
        data += chunk
    return data


def negotiate(browser: Browser, hub: str) -> str:
    path = hub.rstrip("/") + "/negotiate?negotiateVersion=1"
    url = urljoin(browser.base, path)
    req = Request(
        url,
        data=b"",
        headers={
            "User-Agent": "Edulytics-Phase26-SignalR/1.0",
            "Cookie": browser.cookie_header(),
            "Content-Type": "text/plain;charset=UTF-8",
        },
        method="POST",
    )
    try:
        with browser.opener.open(req, timeout=30) as response:
            data = json.loads(response.read().decode())
    except Exception as exc:
        die(f"SignalR negotiate failed for {hub}: {exc}")

    token = data.get("connectionToken") or data.get("connectionId")
    if not token:
        die(f"SignalR negotiate returned no token for {hub}")
    return str(token)


def open_signalr_socket(
    host: str,
    cookie: str,
    hub: str,
    token: str,
    hold_seconds: int,
) -> tuple[bool, str]:
    raw = socket.create_connection((host, 443), timeout=20)
    sock = ssl.create_default_context().wrap_socket(raw, server_hostname=host)
    sock.settimeout(20)

    key = base64.b64encode(os.urandom(16)).decode()
    path = hub.rstrip("/") + "?id=" + quote(token, safe="")
    request = (
        f"GET {path} HTTP/1.1\r\n"
        f"Host: {host}\r\n"
        "Upgrade: websocket\r\n"
        "Connection: Upgrade\r\n"
        f"Sec-WebSocket-Key: {key}\r\n"
        "Sec-WebSocket-Version: 13\r\n"
        f"Origin: https://{host}\r\n"
        f"Cookie: {cookie}\r\n"
        "User-Agent: Edulytics-Phase26-SignalR/1.0\r\n"
        "\r\n"
    ).encode()

    try:
        sock.sendall(request)
        response = recv_headers(sock)
        first = response.split(b"\r\n", 1)[0]
        if b" 101 " not in first:
            return False, first.decode(errors="replace")

        signalr_handshake = b'{"protocol":"json","version":1}\x1e'
        sock.sendall(masked_frame(signalr_handshake, 1))
        sock.settimeout(1.0)

        deadline = time.monotonic() + hold_seconds
        while time.monotonic() < deadline:
            try:
                data = sock.recv(4096)
                if not data:
                    return False, "server closed connection"
            except socket.timeout:
                pass

        try:
            sock.sendall(masked_frame(b"", 8))
        except Exception:
            pass
        return True, "held"
    except Exception as exc:
        return False, f"{type(exc).__name__}: {exc}"
    finally:
        try:
            sock.close()
        except Exception:
            pass


def signalr_load(
    browser: Browser,
    hubs: list[str],
    per_hub: int,
    hold_seconds: int,
) -> dict:
    results = {}
    cookie = browser.cookie_header()

    for hub in hubs:
        tokens = [negotiate(browser, hub) for _ in range(per_hub)]
        with concurrent.futures.ThreadPoolExecutor(max_workers=per_hub) as pool:
            outcomes = list(
                pool.map(
                    lambda token: open_signalr_socket(
                        LOCKED_HOST,
                        cookie,
                        hub,
                        token,
                        hold_seconds,
                    ),
                    tokens,
                )
            )
        ok = sum(1 for success, _ in outcomes if success)
        results[hub] = {
            "attempted": per_hub,
            "successful": ok,
            "success_ratio": ok / per_hub,
            "details": [detail for success, detail in outcomes if not success][:10],
        }
    return results


def load_existing_result(out: Path, target: str, soak_minutes: int) -> dict | None:
    if not out.exists():
        return None

    try:
        data = json.loads(out.read_text(encoding="utf-8"))
    except Exception:
        return None

    if data.get("target") != target:
        return None

    status = data.get("phase26_live_status")
    if status == "pass":
        recorded = int(data.get("soak", {}).get("minutes", 0))
        if recorded >= soak_minutes:
            return data
        return None

    if status == "pre_soak_pass":
        required = {
            "route_status",
            "routes_under_load",
            "baseline",
            "normal",
            "stress",
            "stress_recovery",
            "spike",
            "spike_recovery",
            "signalr",
        }
        if required.issubset(data):
            return data

    return None


def write_result(out: Path, report: dict) -> None:
    out.parent.mkdir(parents=True, exist_ok=True)
    temp = out.with_suffix(out.suffix + ".tmp")
    temp.write_text(json.dumps(report, indent=2), encoding="utf-8")
    temp.replace(out)


def main() -> None:
    ap = argparse.ArgumentParser()
    ap.add_argument("--base", required=True)
    ap.add_argument("--email", required=True)
    ap.add_argument("--signalr-email", required=True)
    ap.add_argument("--slo", required=True)
    ap.add_argument("--out", required=True)
    ap.add_argument("--soak-minutes", type=int, required=True)
    ap.add_argument("--hubs", default="")
    args = ap.parse_args()

    parsed = urlparse(args.base)
    if parsed.scheme != "https" or parsed.hostname != LOCKED_HOST:
        die(
            "load target is hard-locked to "
            f"https://{LOCKED_HOST}; got {args.base}"
        )

    if args.soak_minutes < 360:
        die("final Phase26 soak cannot be shorter than 360 minutes")

    out = Path(args.out)
    existing = load_existing_result(out, args.base, args.soak_minutes)

    if existing is not None and existing.get("phase26_live_status") == "pass":
        print(
            "PASS: existing Phase26 live qualification already contains a "
            f"completed {existing['soak']['minutes']}-minute soak; skipping "
            "all live work"
        )
        return

    slo = json.loads(Path(args.slo).read_text())
    hubs = [x.strip() for x in args.hubs.split(",") if x.strip()]
    if not hubs:
        die("no SignalR MapHub route discovered in source")

    print("INFO: authenticating Platform load actor before any live load")
    browser = interactive_login(
        args.base,
        args.email,
        "/platform/dashboard",
        "Platform",
        "SuperAdmin",
    )
    cookie = browser.cookie_header()

    resume_pre_soak = (
        existing is not None
        and existing.get("phase26_live_status") == "pre_soak_pass"
    )

    if resume_pre_soak:
        print(
            "PASS: preserved pre-soak live evidence found; "
            "normal/stress/spike/SignalR will not be repeated"
        )
        route_status = existing["route_status"]
        routes = existing["routes_under_load"]
        baseline = existing["baseline"]
        normal = existing["normal"]
        stress = existing["stress"]
        controlled_point = existing.get("controlled_shedding_point")
        stress_recovery = existing["stress_recovery"]
        spike = existing["spike"]
        spike_recovery = existing["spike_recovery"]
        signalr = existing["signalr"]
    else:
        print("INFO: authenticating school-scoped SignalR actor")
        signalr_browser = interactive_login(
            args.base,
            args.signalr_email,
            "/school/dashboard",
            "Your school",
            "SignalR SchoolAdmin",
        )

        signalr_cookie = signalr_browser.cookie_header()
        print("INFO: running SignalR negotiate/WebSocket preflight before load")
        for hub in hubs:
            token = negotiate(signalr_browser, hub)
            ok, detail = open_signalr_socket(
                LOCKED_HOST,
                signalr_cookie,
                hub,
                token,
                2,
            )
            if not ok:
                die(f"SignalR preflight failed for {hub}: {detail}")
        print("PASS: SignalR SchoolAdmin negotiate/WebSocket preflight")

        core_routes = [
            "/platform/dashboard",
            "/Platform/Billing",
            "/Platform/Operations",
        ]
        candidate_routes = core_routes + [
            "/Platform/Subscriptions",
            "/Platform/Schools",
            "/Audit?page=1&pageSize=20",
        ]

        print("INFO: probing authenticated Platform routes")
        route_status = {}
        routes = []
        for route in candidate_routes:
            status, latency, error = request_once(LOCKED_HOST, cookie, route)
            route_status[route] = {
                "status": status,
                "latency_ms": round(latency, 2),
                "error": error,
            }
            if route in core_routes and status != 200:
                die(
                    f"core authenticated route failed before load: "
                    f"{route} -> {status}"
                )
            if status == 200:
                routes.append(route)

        if len(routes) < 3:
            die("fewer than three authenticated routes are usable")

        print("INFO: measuring baseline")
        baseline_results = []
        for _ in range(5):
            for route in routes:
                baseline_results.append(
                    request_once(LOCKED_HOST, cookie, route)
                )
        baseline = classify(baseline_results)
        print(
            f"PASS: baseline p95={baseline['p95_ms']}ms "
            f"p99={baseline['p99_ms']}ms"
        )

        normal_cfg = slo["normal"]
        print(
            "INFO: starting normal load "
            f"concurrency={normal_cfg['concurrency']} "
            f"duration={normal_cfg['duration_seconds']}s"
        )
        normal = run_stage(
            LOCKED_HOST,
            cookie,
            routes,
            int(normal_cfg["concurrency"]),
            int(normal_cfg["duration_seconds"]),
            1.0,
            progress_label="normal load",
            progress_interval=30,
        )

        if normal["p95_ms"] > normal_cfg["p95_ms_max"]:
            die(
                f"normal-load p95 {normal['p95_ms']}ms exceeds "
                f"{normal_cfg['p95_ms_max']}ms"
            )
        if normal["p99_ms"] > normal_cfg["p99_ms_max"]:
            die(
                f"normal-load p99 {normal['p99_ms']}ms exceeds "
                f"{normal_cfg['p99_ms_max']}ms"
            )
        if normal["unexpected_rate"] > normal_cfg["unexpected_error_rate_max"]:
            die(
                "normal-load unexpected error rate "
                f"{normal['unexpected_rate']:.4f} exceeds "
                f"{normal_cfg['unexpected_error_rate_max']:.4f}"
            )
        print(
            f"PASS: normal load p95={normal['p95_ms']}ms "
            f"p99={normal['p99_ms']}ms"
        )

        stress_cfg = slo["stress"]
        stress = []
        controlled_point = None
        for concurrency in stress_cfg["concurrency_stages"]:
            print(
                f"INFO: starting stress stage concurrency={concurrency} "
                f"duration={stress_cfg['stage_seconds']}s"
            )
            stage = run_stage(
                LOCKED_HOST,
                cookie,
                routes,
                int(concurrency),
                int(stress_cfg["stage_seconds"]),
                1.0,
                progress_label=f"stress concurrency {concurrency}",
                progress_interval=20,
            )
            stress.append(stage)

            status_500 = int(stage["status_counts"].get("500", 0))
            status_502 = int(stage["status_counts"].get("502", 0))
            status_504 = int(stage["status_counts"].get("504", 0))
            if (
                status_500 + status_502 + status_504
                > stress_cfg["unexpected_5xx_allowed"]
            ):
                die(
                    "stress produced unhandled 500/502/504 at concurrency "
                    f"{concurrency}: {stage['status_counts']}"
                )

            print(
                f"PASS: stress stage concurrency={concurrency} "
                f"p95={stage['p95_ms']}ms controlled_shed="
                f"{stage['controlled_shed']}"
            )

            if stage["controlled_shed"] > 0 and controlled_point is None:
                controlled_point = concurrency
                break

        print("INFO: checking recovery after stress")
        stress_recovery = recover(
            LOCKED_HOST,
            cookie,
            int(stress_cfg["recovery_seconds_max"]),
        )
        if not stress_recovery["recovered"]:
            die("staging failed to recover after stress")
        print(
            f"PASS: stress recovery in {stress_recovery['seconds']}s"
        )

        spike_cfg = slo["spike"]
        print(
            f"INFO: starting spike concurrency={spike_cfg['concurrency']} "
            f"waves={spike_cfg['waves']}"
        )
        spike = run_spike(
            LOCKED_HOST,
            cookie,
            routes,
            int(spike_cfg["concurrency"]),
            int(spike_cfg["waves"]),
            int(spike_cfg["wave_pause_seconds"]),
        )

        if int(spike["status_counts"].get("500", 0)) > 0:
            die("traffic spike produced HTTP 500")

        spike_recovery = recover(
            LOCKED_HOST,
            cookie,
            int(spike_cfg["recovery_seconds_max"]),
        )
        if not spike_recovery["recovered"]:
            die("staging failed to recover after spike")
        print(
            f"PASS: spike/recovery p95={spike['p95_ms']}ms "
            f"recovery={spike_recovery['seconds']}s"
        )

        sig_cfg = slo["signalr"]
        print(
            f"INFO: starting SignalR load with SchoolAdmin actor; "
            f"connections_per_hub={sig_cfg['connections_per_hub']}"
        )
        signalr = signalr_load(
            signalr_browser,
            hubs,
            int(sig_cfg["connections_per_hub"]),
            int(sig_cfg["hold_seconds"]),
        )
        for hub, value in signalr.items():
            if value["success_ratio"] < sig_cfg["success_ratio_min"]:
                die(
                    f"SignalR connection success ratio for {hub} "
                    f"{value['success_ratio']:.2%} below "
                    f"{sig_cfg['success_ratio_min']:.2%}"
                )
            print(
                f"PASS: SignalR {hub} successful={value['successful']}/"
                f"{value['attempted']}"
            )

        pre_soak = {
            "phase26_live_status": "pre_soak_pass",
            "generated_at_utc": datetime.now(timezone.utc).isoformat(),
            "target": args.base,
            "actors": {
                "platform": "SuperAdmin",
                "signalr": "SchoolAdmin",
                "signalr_email": args.signalr_email,
            },
            "route_status": route_status,
            "routes_under_load": routes,
            "baseline": baseline,
            "normal": normal,
            "stress": stress,
            "controlled_shedding_point": controlled_point,
            "stress_recovery": stress_recovery,
            "spike": spike,
            "spike_recovery": spike_recovery,
            "signalr": signalr,
        }
        write_result(out, pre_soak)
        print(
            "PASS: pre-soak evidence checkpoint saved; if the process stops "
            "later, pre-soak stages will not be repeated"
        )

    soak_cfg = slo["soak"]
    soak_seconds = args.soak_minutes * 60
    soak_concurrency = 4
    worker_delay = soak_concurrency / float(soak_cfg["target_rps"])

    print(
        f"INFO: starting {args.soak_minutes}-minute soak "
        f"at ~{soak_cfg['target_rps']} req/s"
    )
    print(
        "INFO: soak progress will be printed every 5 minutes; "
        "the terminal is not frozen between messages"
    )

    soak_started = datetime.now(timezone.utc).isoformat()
    soak = run_stage(
        LOCKED_HOST,
        cookie,
        routes,
        soak_concurrency,
        soak_seconds,
        worker_delay,
        progress_label=f"Phase26 soak ({args.soak_minutes} minutes)",
        progress_interval=300,
    )
    soak_ended = datetime.now(timezone.utc).isoformat()
    soak["started_at_utc"] = soak_started
    soak["ended_at_utc"] = soak_ended
    soak["minutes"] = args.soak_minutes
    soak["target_rps"] = soak_cfg["target_rps"]

    if soak["p95_ms"] > soak_cfg["p95_ms_max"]:
        die(
            f"soak p95 {soak['p95_ms']}ms exceeds "
            f"{soak_cfg['p95_ms_max']}ms"
        )
    if soak["p99_ms"] > soak_cfg["p99_ms_max"]:
        die(
            f"soak p99 {soak['p99_ms']}ms exceeds "
            f"{soak_cfg['p99_ms_max']}ms"
        )
    if soak["unexpected_rate"] > soak_cfg["unexpected_error_rate_max"]:
        die(
            "soak unexpected error rate "
            f"{soak['unexpected_rate']:.4f} exceeds "
            f"{soak_cfg['unexpected_error_rate_max']:.4f}"
        )

    print("INFO: checking final recovery after soak")
    final_recovery = recover(LOCKED_HOST, cookie, 60)
    if not final_recovery["recovered"]:
        die("staging not healthy after soak")

    report = {
        "phase26_live_status": "pass",
        "generated_at_utc": datetime.now(timezone.utc).isoformat(),
        "target": args.base,
        "actors": {
            "platform": "SuperAdmin",
            "signalr": "SchoolAdmin",
            "signalr_email": args.signalr_email,
        },
        "route_status": route_status,
        "routes_under_load": routes,
        "baseline": baseline,
        "normal": normal,
        "stress": stress,
        "controlled_shedding_point": controlled_point,
        "stress_recovery": stress_recovery,
        "spike": spike,
        "spike_recovery": spike_recovery,
        "signalr": signalr,
        "soak": soak,
        "final_recovery": final_recovery,
    }

    write_result(out, report)

    print(
        "PASS: Phase26 live load/stress/spike/SignalR/soak qualification"
    )


if __name__ == "__main__":
    main()
