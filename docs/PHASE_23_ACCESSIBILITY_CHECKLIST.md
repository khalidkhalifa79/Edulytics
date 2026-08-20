# Phase 23 Accessibility Acceptance

## Automated

CI checks:

- dynamic document language (`en` / `pl`);
- no positive tabindex;
- no inline JavaScript event attributes;
- every image has alt text or is explicitly aria-hidden;
- CSP blocks script attributes;
- full regression remains green.

## Manual staging acceptance

Using keyboard only:

1. Login page is operable.
2. Tab focus is visible.
3. Dashboard navigation is reachable.
4. Operational console is reachable by SuperAdmin.
5. Forms have understandable labels.
6. Error / Access denied content is understandable.
7. EN and PL document language is correct.
8. 320/375 px width does not hide required actions.

Manual acceptance is evidence-based and occurs only after the Phase23 release
is on staging.
