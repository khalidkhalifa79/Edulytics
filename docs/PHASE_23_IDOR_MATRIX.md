# Phase 23 IDOR / Tenant Fail-Closed Matrix

| Surface | SuperAdmin | SchoolAdmin | SubjectSupervisor | Teacher |
| --- | --- | --- | --- | --- |
| Platform operations | Allow | Deny | Deny | Deny |
| School user management | Platform scope only | Own school | Deny | Deny |
| Academic structure writes | Deny school UI | Own school | Deny | Deny |
| Curriculum administration | Deny school UI | Own school | Assigned scope only where designed | Deny writes |
| Assessment access | No implicit tenant bypass | Own school | Assigned subject scope | Assigned teaching scope |
| Analytics | No implicit school context | Own school | Assigned subject scope | Assigned teaching scope |
| Reports | No implicit school report scope | Own school | Assigned subject scope | Assigned teaching scope |
| Notifications | No tenant impersonation | Own inbox | Own inbox | Own inbox |
| Realtime groups | No tenant inference | Own school | Assigned subject scope | Assigned teaching scope |
| Audit | Platform policy / explicit scope | Own-school scope where authorized | Deny unless explicitly authorized | Deny unless explicitly authorized |

## Universal deny rules

For all tenant roles:

- another SchoolId is denied/not visible;
- missing SchoolId fails closed;
- inactive/deleted school fails closed;
- unsupported/multiple role state is denied;
- unassigned subject/class/resource is denied;
- changing only a route/query/body identifier cannot widen authorization;
- realtime membership and report scope use the same durable assignments as
  normal reads.

Historical Phase04/05/19/20/21/22 authorization tests remain regression
evidence and Phase23 is added to the tenant/IDOR CI gate.
