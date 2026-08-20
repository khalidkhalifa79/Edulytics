# Edulytics Dependency Governance

- NuGet versions must not use floating wildcards.
- HIGH/CRITICAL known vulnerabilities block protected CI.
- GitHub SAST and container vulnerability scanning remain required checks.
- Dependencies are upgraded deliberately, with build/full-regression evidence.
- Do not suppress an advisory merely to turn CI green.
- Security-sensitive dependency changes require normal protected-PR review.
- Frontend vendor inventory/version documentation is Phase24 scope.
