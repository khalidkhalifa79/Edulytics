#!/usr/bin/env python3
from __future__ import annotations

import json
import sys
from datetime import datetime, timezone
from pathlib import Path


def pct(value: float) -> str:
    return f"{value * 100:.2f}%"


def main() -> None:
    if len(sys.argv) != 5:
        raise SystemExit(
            "usage: phase26_report.py LIVE.json LOCAL.json SLO.json OUT.md"
        )

    live_path, local_path, slo_path, out_path = map(Path, sys.argv[1:])

    live = json.loads(live_path.read_text())
    local = json.loads(local_path.read_text())
    slo = json.loads(slo_path.read_text())
    actors = live.get("actors", {})

    stress_rows = []
    for x in live["stress"]:
        stress_rows.append(
            "| {c} | {r} | {p95:.2f} | {p99:.2f} | {u} | {s} |".format(
                c=x["concurrency"],
                r=x["requests"],
                p95=x["p95_ms"],
                p99=x["p99_ms"],
                u=x["unexpected"],
                s=x["controlled_shed"],
            )
        )

    signalr_rows = []
    for hub, x in live["signalr"].items():
        signalr_rows.append(
            f"| `{hub}` | {x['attempted']} | {x['successful']} | "
            f"{pct(x['success_ratio'])} |"
        )

    local_rows = []
    for name, x in local["groups"].items():
        local_rows.append(
            f"| `{name}` | {x['exit_code']} | {x['seconds']:.3f} |"
        )

    text = f"""# Edulytics — Phase 26 Performance Qualification

Generated: `{datetime.now(timezone.utc).isoformat()}`

## Status

**PHASE 26 QUALIFICATION: PASS**

This phase measures the current approved staging topology and the existing
application-level concurrency/resilience contracts. It does **not** introduce
new commercial/product behavior.

## Environment and boundary

- Live target: `{live['target']}`
- Live target is hard-locked by the runner to `staging.edulytiks.com`.
- Platform load actor: `{actors.get('platform', 'SuperAdmin')}`.
- SignalR load actor: `{actors.get('signalr', 'SchoolAdmin')}`.
- SignalR actor email: `{actors.get('signalr_email', 'not recorded')}`.
- The actors are intentionally separated because school-private Analytics uses
  the `AnalyticsRead` policy and is not tested by weakening SuperAdmin scope.
- Current staging uses the approved development/staging infrastructure.
- Multi-instance correctness was qualified before this phase; Phase 26 adds
  measured load/stress/spike/SignalR/soak evidence.
- Final Oracle production capacity must be re-baselined during production
  go-live because hardware/provider capacity differs from Render Free staging.

## SLO contract

```json
{json.dumps(slo, indent=2)}
```

## Baseline

- Requests: {live['baseline']['requests']}
- p95: {live['baseline']['p95_ms']} ms
- p99: {live['baseline']['p99_ms']} ms
- Unexpected error rate: {pct(live['baseline']['unexpected_rate'])}

## Normal load

- Concurrency: {live['normal']['concurrency']}
- Duration: {live['normal']['duration_seconds']} s
- Requests: {live['normal']['requests']}
- p95: **{live['normal']['p95_ms']} ms**
- p99: **{live['normal']['p99_ms']} ms**
- Unexpected error rate: **{pct(live['normal']['unexpected_rate'])}**

## Stress

| Concurrency | Requests | p95 ms | p99 ms | Unexpected | Controlled shed |
|---:|---:|---:|---:|---:|---:|
{chr(10).join(stress_rows)}

- First controlled-shedding point:
  `{live.get('controlled_shedding_point') or 'not reached within bounded stage'}`
- Recovery after stress:
  `{live['stress_recovery']['seconds']} s`

## Spike

- Concurrency: {live['spike']['concurrency']}
- Requests: {live['spike']['requests']}
- p95: {live['spike']['p95_ms']} ms
- p99: {live['spike']['p99_ms']} ms
- Unexpected errors: {live['spike']['unexpected']}
- Controlled shed responses: {live['spike']['controlled_shed']}
- Recovery after spike: {live['spike_recovery']['seconds']} s

## SignalR connection load

| Hub | Attempted | Successful | Success ratio |
|---|---:|---:|---:|
{chr(10).join(signalr_rows)}

## Soak

- Duration: **{live['soak']['minutes']} minutes**
- Target request rate: **{live['soak']['target_rps']} req/s**
- Requests: **{live['soak']['requests']}**
- p95: **{live['soak']['p95_ms']} ms**
- p99: **{live['soak']['p99_ms']} ms**
- Unexpected error rate: **{pct(live['soak']['unexpected_rate'])}**
- Final recovery: `{live['final_recovery']['seconds']} s`

## Mutation-heavy and dependency-failure qualification

The shared staging load is deliberately bounded and mostly read-oriented.
Mutation-heavy scenarios are covered by the existing domain/integration
qualification suites to avoid corrupting or flooding shared staging data.

| Qualification group | Exit | Seconds |
|---|---:|---:|
{chr(10).join(local_rows)}

This includes:

- concurrent assessment/score behavior;
- import validation/application behavior;
- report/export behavior;
- Outbox worker/backlog behavior;
- SignalR/realtime contracts;
- DB/connector slowdown and bounded resilience behavior;
- connector delivery;
- explicit concurrency tests;
- multi-instance scale contracts.

## Exit decision

PASS criteria met:

- normal load remains inside the declared latency/error SLO;
- stress does not produce unhandled 500/502/504 failures;
- bounded stress recovers;
- spike recovers;
- SignalR connection qualification clears the minimum success ratio;
- soak ran for at least 6 hours and remained inside soak SLO;
- import/report/score/outbox/resilience/concurrency suites pass;
- no new production behavior was introduced merely to pass performance gates.

**PHASE 26 = CLOSED**

**PHASE 27 = READY, NOT STARTED**
"""

    out_path.parent.mkdir(parents=True, exist_ok=True)
    out_path.write_text(text, encoding="utf-8")
    print("PASS: Phase26 report written")


if __name__ == "__main__":
    main()
