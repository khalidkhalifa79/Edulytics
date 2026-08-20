# CSS Organization

`src/Edulytics.Web/wwwroot/css/site.css` currently carries public and
authenticated application styles in one cascade.

Phase 24 does not split the file mechanically because changing stylesheet
boundaries and source order can change rendered behavior.

## Maintained conceptual order

1. design tokens and global defaults;
2. language selector and public surfaces;
3. authentication;
4. authenticated application shell;
5. responsive shell rules;
6. schools and school users;
7. academic structure;
8. curriculum;
9. assessments;
10. analytics;
11. imports;
12. audit;
13. reports;
14. notifications;
15. operations;
16. feature-specific responsive rules.

Shared variables and tokens stay centralized.

## Extraction rule

CSS may be extracted only when:

- selectors map to an unambiguous feature;
- stylesheet load order is explicit;
- EN/PL behavior remains equivalent;
- 320px and 375px required actions remain reachable;
- no horizontal overflow or cascade regression is introduced.

Phase 24 therefore inventories the current file first and performs only
evidence-backed cleanup.
