# Edulytics — Phase 10 Real-Time Dashboards

Baseline:

`0a49564 feat: add analytics engine and dashboards`

## Scope

Phase 10 adds reliable real-time analytics invalidation after assessment-result
changes.

Implemented:

- authenticated SignalR analytics hub;
- server-controlled SignalR groups;
- own-school SchoolAdmin group;
- exact Teacher ClassGroup + Subject groups;
- AssessmentResultEntered / AssessmentResultUpdated events;
- transactional OutboxMessage persistence with result changes;
- RowVersion optimistic outbox claiming;
- retry lease for failed/crashed processing;
- background analytics projection refresh;
- SignalR AnalyticsUpdated notification;
- automatic dashboard reload;
- unassigned-teacher isolation;
- cross-school isolation.

Not implemented:

- Phase 11 imports;
- Phase 12 production hardening;
- SubjectSupervisor live groups because the current model has no safe
  SubjectSupervisor-to-Subject assignment relation.

## Flow

Assessment result save
→ raw result and StudentAnswer changes
→ OutboxMessage in the same EF SaveChanges transaction
→ background worker claims event
→ analytics projections rebuild
→ SignalR sends AnalyticsUpdated
→ connected authorized dashboards reload.

## Groups

School administrator:

`school:{schoolId}:admins`

Teacher:

`school:{schoolId}:class:{classId}:subject:{subjectId}:teachers`

The browser never supplies a group, SchoolId, ClassGroupId, or SubjectId.
Membership is resolved from authenticated identity and server-side assignments.

## Acceptance

Phase 10 is accepted only after:

- build;
- Phase 10 tests;
- full regression;
- additive migration audit;
- real SQL Server migration;
- real assessment-result save;
- real outbox processing;
- real analytics recalculation;
- real SignalR delivery;
- assigned Teacher delivery;
- unassigned Teacher isolation;
- cross-school isolation;
- browser evidence;
- manual gate;
- final build/test/security checks;
- commit/push;
- LOCAL == origin/main;
- clean working tree.
