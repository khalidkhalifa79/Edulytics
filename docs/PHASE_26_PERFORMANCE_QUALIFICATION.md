# Edulytics — Phase 26 Performance Qualification

Generated: `2026-08-23T21:14:03.184817+00:00`

## Status

**PHASE 26 QUALIFICATION: PASS**

This phase measures the current approved staging topology and the existing
application-level concurrency/resilience contracts. It does **not** introduce
new commercial/product behavior.

## Environment and boundary

- Live target: `https://staging.edulytiks.com`
- Live target is hard-locked by the runner to `staging.edulytiks.com`.
- Platform load actor: `SuperAdmin`.
- SignalR load actor: `SchoolAdmin`.
- SignalR actor email: `info.ourcs+phase26.20260823141350.e3e6e0@gmail.com`.
- The actors are intentionally separated because school-private Analytics uses
  the `AnalyticsRead` policy and is not tested by weakening SuperAdmin scope.
- Current staging uses the approved development/staging infrastructure.
- Multi-instance correctness was qualified before this phase; Phase 26 adds
  measured load/stress/spike/SignalR/soak evidence.
- Final Oracle production capacity must be re-baselined during production
  go-live because hardware/provider capacity differs from Render Free staging.

## SLO contract

```json
{
  "version": 1,
  "environment": "current-approved-staging",
  "normal": {
    "concurrency": 8,
    "duration_seconds": 120,
    "p95_ms_max": 2000,
    "p99_ms_max": 4000,
    "unexpected_error_rate_max": 0.01
  },
  "stress": {
    "concurrency_stages": [
      8,
      16,
      24,
      32,
      48,
      64
    ],
    "stage_seconds": 20,
    "allowed_controlled_shed_statuses": [
      429,
      503
    ],
    "unexpected_5xx_allowed": 0,
    "recovery_seconds_max": 60
  },
  "spike": {
    "concurrency": 64,
    "waves": 5,
    "wave_pause_seconds": 2,
    "recovery_seconds_max": 60
  },
  "signalr": {
    "connections_per_hub": 10,
    "hold_seconds": 30,
    "success_ratio_min": 0.9
  },
  "soak": {
    "minimum_minutes": 360,
    "target_rps": 2.0,
    "p95_ms_max": 2500,
    "p99_ms_max": 5000,
    "unexpected_error_rate_max": 0.01
  }
}
```

## Baseline

- Requests: 30
- p95: 1727.96 ms
- p99: 1745.68 ms
- Unexpected error rate: 0.00%

## Normal load

- Concurrency: 8
- Duration: 120 s
- Requests: 492
- p95: **1794.47 ms**
- p99: **2063.7 ms**
- Unexpected error rate: **0.00%**

## Stress

| Concurrency | Requests | p95 ms | p99 ms | Unexpected | Controlled shed |
|---:|---:|---:|---:|---:|---:|
| 8 | 86 | 1738.62 | 1782.69 | 0 | 0 |
| 16 | 166 | 1832.30 | 2016.01 | 0 | 0 |
| 24 | 251 | 2095.66 | 2488.90 | 0 | 0 |
| 32 | 330 | 1962.08 | 2140.60 | 0 | 0 |
| 48 | 472 | 2114.93 | 2399.50 | 0 | 0 |
| 64 | 459 | 4217.67 | 5469.11 | 0 | 0 |

- First controlled-shedding point:
  `not reached within bounded stage`
- Recovery after stress:
  `0.74 s`

## Spike

- Concurrency: 64
- Requests: 320
- p95: 2654.57 ms
- p99: 2911.24 ms
- Unexpected errors: 0
- Controlled shed responses: 0
- Recovery after spike: 0.79 s

## SignalR connection load

| Hub | Attempted | Successful | Success ratio |
|---|---:|---:|---:|
| `/hubs/analytics` | 10 | 10 | 100.00% |

## Soak

- Duration: **360 minutes**
- Target request rate: **2.0 req/s**
- Requests: **25837**
- p95: **2170.32 ms**
- p99: **2446.77 ms**
- Unexpected error rate: **0.00%**
- Final recovery: `1.12 s`

## Mutation-heavy and dependency-failure qualification

The shared staging load is deliberately bounded and mostly read-oriented.
Mutation-heavy scenarios are covered by the existing domain/integration
qualification suites to avoid corrupting or flooding shared staging data.

| Qualification group | Exit | Seconds |
|---|---:|---:|
| `assessment_score_edits` | 0 | 3.276 |
| `signalr_realtime_contracts` | 0 | 2.417 |
| `imports` | 0 | 2.288 |
| `db_connector_slowdown_resilience` | 0 | 2.207 |
| `outbox_backlog_and_worker` | 0 | 2.136 |
| `reports_exports` | 0 | 2.223 |
| `connector_delivery` | 0 | 2.221 |
| `multi_instance_scale_contracts` | 0 | 2.851 |
| `explicit_concurrency_tests` | 0 | 2.237 |

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
