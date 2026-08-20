# Phase 23 Privacy and Retention Decision

## Data minimization

### Import payloads

`RowsJson` and `OriginalFileName` are transient sensitive processing data.

They are scrubbed 24 hours after a batch reaches `Completed` or
`ValidationFailed`.

A `Validated` batch is never scrubbed because it still requires confirmation.

`FileHash` is retained as the non-content durable idempotency key.

### Report exports

The Phase20 24-hour `ExpiresAtUtc` contract remains.

Phase23 adds physical destruction of:

- FileContent;
- FileName;
- ContentType;

when expiry is reached, and marks the export `Expired`.

### Notifications

Read notifications are retained for 180 days.

Terminal Sent/Failed delivery metadata is deleted first, then the read
notification.

A pending delivery prevents notification deletion.

### Audit

AuditLog is durable append-only compliance evidence.

Phase23 does not invent a legal deletion period. Automated audit destruction
requires an approved legal/contractual retention schedule before go-live.

## Logging

Retention jobs log aggregate counts only.

Do not log:

- email addresses;
- file names;
- import rows;
- password/setup tokens;
- report binary content;
- notification recipient identifiers.
