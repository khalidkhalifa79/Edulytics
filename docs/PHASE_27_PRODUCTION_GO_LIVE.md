# Edulytics — Phase 27 Production Readiness / Deferred Oracle Go-Live

## Status and scope

Phase 27 validates that the Edulytics application is **production-ready at the
application level** using the approved no-cost validation environment.

The actual paid production infrastructure cutover is intentionally deferred.

This is an explicit project decision that supersedes the earlier Phase 27
assumption of creating a paid Render production service during application
development.

## Approved current environment

Until the program and all acceptance phases are complete:

```text
GitHub protected main
  -> immutable GHCR image
  -> Render Free staging
  -> Neon current project / staging validation branch
```

Rules:

- keep the existing Render Free staging service;
- do not create a paid Render production service;
- do not upgrade Render merely to close Phase 27;
- do not subscribe to or provision Oracle production resources yet;
- reuse accepted Phase 26 performance/soak evidence unless a material runtime
  change invalidates it;
- continue using the current free environment for remaining application
  acceptance work.

## Final production destination

The final production host/domain is:

```text
https://edulytiks.com
```

`app.edulytiks.com` is not the approved final application domain.

No DNS cutover to the final application host is performed during this Phase 27
readiness closure.

After the entire program and all required tests are accepted, begin a separate
Oracle production migration/go-live plan. That plan must determine the actual
Oracle topology, cost, capacity, network, database connectivity, backup,
monitoring and rollback design before any subscription/provisioning action.

## Current Neon state

The existing Edulytics Neon project already contains both staging and production
branches. Phase 27 does not create or mutate a paid production host merely
because the production database branch exists.

Runtime and migration credential separation remains the required application
contract:

```text
ConnectionStrings__DefaultConnection
ConnectionStrings__MigrationConnection
```

The actual Oracle-era production database placement/connection plan must be
revalidated during the Oracle handoff rather than guessed now.

## Application-level readiness gates

Phase 27 readiness requires:

- accepted Phase 26 load/stress/spike/SignalR/360-minute soak evidence;
- Release build green;
- full regression green;
- protected GitHub CI green;
- immutable SHA-tagged container image;
- staging `/health/live` green;
- staging `/health/ready` green;
- public security headers;
- EN/PL public localization entry flow;
- migration bundle/pre-deploy contract retained;
- startup migration remains explicit opt-in for temporary free staging only;
- production startup migration default remains disabled;
- tenant/concurrency/security contracts remain covered by the accepted suites;
- repository secret-history gate green.

## What Phase 27 does NOT claim

Closing Phase 27 under this approved plan does **not** mean:

- Oracle is provisioned;
- `edulytiks.com` has been cut over to the final application host;
- production customer traffic is live;
- final Oracle sizing has been measured;
- final production RPO/RTO has been measured;
- Phase 28 post-launch review has started.

Those claims are prohibited until the later Oracle go-live.

## Oracle handoff

The future Oracle production work is specified in:

`docs/ORACLE_PRODUCTION_HANDOFF.md`

## Closure

The approved free-environment application-readiness gates passed through
protected `main`.

Durable acceptance evidence:

`docs/PHASE_27_READINESS_ACCEPTANCE.md`

**PHASE 27 = CLOSED — APPLICATION PRODUCTION READINESS**

**ORACLE GO-LIVE = DEFERRED UNTIL PROGRAM COMPLETION**

**FINAL PRODUCTION DOMAIN = edulytiks.com**

**PHASE 28 = NOT STARTED**
