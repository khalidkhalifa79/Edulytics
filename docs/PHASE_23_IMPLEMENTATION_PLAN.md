# Edulytics — Phase 23 Security, Privacy, Tenant and Accessibility Hardening

## Baseline

`207bc6ad6b11eef93aa3389a1e1e571f8657b747`

## Security invariants

- tenant users fail closed unless they have one supported tenant role and an
  active SchoolId;
- only SuperAdmin may have no SchoolId and still authenticate;
- cross-school and out-of-scope identifiers remain NotFound/AccessDenied;
- anonymous health endpoints reveal only aggregate status and correlation ID;
- browser JavaScript is governed by a nonce-based CSP;
- inline JavaScript event attributes are forbidden;
- Production AllowedHosts is not wildcard;
- login and operational recovery mutations are rate limited;
- application-user mutation concurrency is controlled by ASP.NET Identity
  ConcurrencyStamp through UserManager;
- expired sensitive report artifacts are physically removed;
- terminal raw import payloads are physically scrubbed;
- read notifications have bounded retention;
- audit records remain append-only and are not automatically destroyed without
  an approved legal/compliance retention schedule.

## CSP

The policy denies objects, framing and inline event handlers.

All Razor script elements receive a request-specific nonce via
`CspNonceTagHelper`.

`style-src 'unsafe-inline'` remains intentionally allowed because the existing
Razor/Bootstrap UI may use style attributes. Script execution does not use
`'unsafe-inline'`.

## AllowedHosts

Production configuration accepts current staging hostnames plus localhost for
container/platform health access.

A future production hostname must be explicitly supplied before go-live; the
wildcard is not the production default.

## ApplicationUser concurrency decision

Do not add a second RowVersion concurrency system to ASP.NET Identity users.

`ConcurrencyStamp` is explicitly retained as the ApplicationUser concurrency
token and mutations continue through `UserManager.UpdateAsync` and related
Identity APIs.

## Schema impact

No migration is required.

All retention behavior uses existing timestamps/status columns.

## Phase boundary

Phase 24 owns repository maintainability/hygiene.

Phase 25 owns distributed multi-instance behavior.

Phase 23 does not start either phase.
