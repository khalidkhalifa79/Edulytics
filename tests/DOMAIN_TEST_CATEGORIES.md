# Edulytics Test Categories

Historical `PhaseXX` test folders remain acceptance evidence and are not moved
merely for cosmetic repository reorganization.

For daily maintenance, tests can be selected through domain-oriented categories
using `scripts/test-domain.sh`.

Supported categories:

```text
architecture
authorization
tenancy
schools
users
academics
curriculum
assessments
analytics
realtime
imports
audit
supervisors
reports
notifications
operations
security
production
```

Examples:

```bash
scripts/test-domain.sh schools
scripts/test-domain.sh reports
scripts/test-domain.sh security
```

New tests may live directly in domain folders when that is clearer, but
historical phase acceptance tests remain intact.
