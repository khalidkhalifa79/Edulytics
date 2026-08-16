# Phase 12 - Production Hardening

## Scope

Phase 12 hardens the accepted Phase 11 application without changing
academic, assessment, analytics, import, or tenant-domain semantics.

The phase implements:

- structured JSON production logging;
- request correlation IDs;
- production response security headers;
- liveness health endpoint;
- readiness health endpoint;
- SQL Server connectivity readiness;
- pending EF migration readiness;
- Outbox background-worker readiness;
- startup configuration validation;
- production environment settings;
- friendly localized EN/PL error pages;
- production-safe exception/status handling;
- deployment documentation;
- monitoring and incident runbook;
- SQL Server backup/restore runbook;
- executable production verification;
- real backup/restore rehearsal.

## Health endpoints

### /health/live

Confirms that the ASP.NET Core process is alive.

It does not depend on SQL Server.

### /health/ready

Confirms:

1. SQL Server can be reached.
2. EF Core has no pending migrations.
3. The Outbox processor has started.
4. The Outbox processor heartbeat is current.

Health responses must not expose credentials, connection strings,
stack traces, SQL commands, or tenant data.

## Correlation IDs

Every HTTP request receives a correlation ID.

Header:

X-Correlation-ID

A safe caller-supplied identifier may be preserved. Unsafe or oversized
values are replaced with a generated random identifier.

The correlation ID is:

- returned in the HTTP response;
- assigned to HttpContext.TraceIdentifier;
- included in a structured logging scope;
- displayed on friendly error pages.

## Logging

Production console output uses the built-in ASP.NET Core JSON console
logger.

Request logging records structured fields for:

- HTTP method;
- path;
- response status;
- elapsed duration;
- correlation ID.

Query strings and request bodies are not included by the request
correlation middleware.

## Error handling

Outside Development:

- exceptions use the central production error path;
- status-code pages use a localized friendly error page;
- stack traces are never rendered;
- Development Mode instructions are never rendered;
- the user receives a correlation ID for support.

## Configuration

Production secrets are not committed.

The SQL Server connection string is supplied externally using:

ConnectionStrings__DefaultConnection

Production operational settings are validated during startup.

## Database

Phase 12 does not change the EF Core data model.

No Phase 12 migration is expected.

## Acceptance

Phase 12 cannot close until:

- build succeeds;
- Phase 12 tests pass;
- full regression passes;
- liveness returns Healthy;
- readiness returns Healthy using real SQL Server;
- migration readiness is current;
- Outbox worker readiness is healthy;
- correlation IDs propagate;
- production logs are valid JSON;
- production security headers are present;
- EN/PL error pages pass browser verification;
- target responsive widths have no horizontal overflow;
- SQL Server backup succeeds;
- RESTORE VERIFYONLY succeeds;
- a temporary restored database is validated;
- Release publish succeeds;
- vulnerability audit is clean;
- secret safety scan is clean;
- manual visual acceptance is approved;
- commit and push succeed;
- local HEAD equals origin/main;
- working tree is clean.
