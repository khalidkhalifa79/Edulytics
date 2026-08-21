# Phase 25 — Scale Discovery

## Accepted baseline

`710b3c4ff796d7e16000a09f11db620b1aee84fb`

## Existing safe foundations

- Data Protection keys are persisted through the shared PostgreSQL DbContext.
- Data Protection uses an explicit application name.
- staging requires a Data Protection certificate and supports encrypted key
  persistence.
- Outbox v2 already uses PostgreSQL ownership/lease semantics from Phase 15.
- Phase 14 already bounds per-process request concurrency and Npgsql pool size.

## Known Phase 25 boundaries at discovery

The existing runtime still needs explicit qualification for process-independent
behavior. The discovery runner records whether the following exist on the
accepted baseline:

- SignalR scale-out backplane;
- dedicated web/worker process roles;
- distributed sensitive quotas;
- total connection-budget validation;
- declared 2-web / 2-worker staging topology.

## Important distinction

ASP.NET Core in-memory fixed-window and concurrency limiters remain useful
per-process overload protection. They are not automatically global security
quotas across multiple web instances.

## Raw discovery evidence

```text
PHASE25 SCALE DISCOVERY
baseline=710b3c4ff796d7e16000a09f11db620b1aee84fb

===== SIGNALR REGISTRATION =====
src/Edulytics.Web/Extensions/RealtimeRegistrationExtensions.cs:23:        services.AddSignalR();

===== DATA PROTECTION =====
src/Edulytics.Web/Extensions/ServiceCollectionExtensions.cs:71:                .AddDataProtection()
src/Edulytics.Web/Extensions/ServiceCollectionExtensions.cs:72:                .SetApplicationName(
src/Edulytics.Web/Extensions/ServiceCollectionExtensions.cs:74:                .PersistKeysToDbContext<
src/Edulytics.Web/Extensions/ServiceCollectionExtensions.cs:77:        var requireDataProtectionCertificate =
src/Edulytics.Web/Extensions/ServiceCollectionExtensions.cs:79:                "Edulytics:Hosting:RequireDataProtectionCertificate");
src/Edulytics.Web/Extensions/ServiceCollectionExtensions.cs:83:                "Edulytics:Hosting:DataProtectionCertificateBase64"];
src/Edulytics.Web/Extensions/ServiceCollectionExtensions.cs:87:                "Edulytics:Hosting:DataProtectionCertificatePassword"];
src/Edulytics.Web/Extensions/ServiceCollectionExtensions.cs:92:            if (requireDataProtectionCertificate)
src/Edulytics.Web/Extensions/ServiceCollectionExtensions.cs:139:                .ProtectKeysWithCertificate(
src/Edulytics.Web/appsettings.Production.json:17:      "RequireDataProtectionCertificate": true
render.yaml:33:      - key: Edulytics__Hosting__RequireDataProtectionCertificate
render.yaml:36:      - key: Edulytics__Hosting__DataProtectionCertificateBase64
render.yaml:39:      - key: Edulytics__Hosting__DataProtectionCertificatePassword

===== HOSTED WORKERS =====
src/Edulytics.Web/Extensions/RealtimeRegistrationExtensions.cs:104:            services.AddHostedService<
src/Edulytics.Web/Extensions/RealtimeRegistrationExtensions.cs:105:                OutboxProcessorBackgroundService>();
src/Edulytics.Web/Extensions/RealtimeRegistrationExtensions.cs:107:            services.AddHostedService<
src/Edulytics.Web/Extensions/RealtimeRegistrationExtensions.cs:108:                AnalyticsRefreshBackgroundService>();

===== RATE LIMITERS =====
103:        builder.Services.AddRateLimiter(
144:                        return RateLimitPartition
145:                            .GetFixedWindowLimiter(
165:                        return RateLimitPartition
166:                            .GetFixedWindowLimiter(
185:                        return RateLimitPartition
186:                            .GetFixedWindowLimiter(
209:                        return RateLimitPartition
210:                            .GetFixedWindowLimiter(
232:                        return RateLimitPartition
233:                            .GetFixedWindowLimiter(
253:                        return RateLimitPartition
254:                            .GetFixedWindowLimiter(
268:                options.AddConcurrencyLimiter(
281:                options.AddConcurrencyLimiter(
294:                options.AddConcurrencyLimiter(
307:                options.AddConcurrencyLimiter(
320:                options.AddConcurrencyLimiter(

===== DATABASE POOL BUDGET =====
src/Edulytics.Web/Extensions/ServiceCollectionExtensions.cs:43:                        "Edulytics:Resilience:NpgsqlMaxPoolSize")
src/Edulytics.Web/Extensions/ServiceCollectionExtensions.cs:51:                        "Maximum Pool Size"))
src/Edulytics.Web/Extensions/ServiceCollectionExtensions.cs:53:                    connectionBuilder.MaxPoolSize =
src/Edulytics.Web/Extensions/BackendResilienceRegistrationExtensions.cs:32:                    x.NpgsqlMaxPoolSize > 0 &&
src/Edulytics.Web/appsettings.json:20:      "NpgsqlMaxPoolSize": 40,

===== RENDER TOPOLOGY =====
services:
  - type: web
    name: edulytics-staging
    runtime: docker
    plan: starter
    region: frankfurt
    branch: main

    autoDeployTrigger: checksPass

    dockerfilePath: ./Dockerfile
    dockerContext: .

    healthCheckPath: /health/ready
    maxShutdownDelaySeconds: 60

    preDeployCommand: >-
      ConnectionStrings__DefaultConnection="$ConnectionStrings__MigrationConnection"
      EDULYTICS_CONNECTION_STRING="$ConnectionStrings__MigrationConnection"
      /app/efbundle
      --connection "$ConnectionStrings__MigrationConnection"

    envVars:
      - key: ASPNETCORE_ENVIRONMENT
        value: Production

      - key: Edulytics__Hosting__TrustForwardedHeaders
        value: "true"

      - key: Edulytics__Hosting__DataProtectionApplicationName
        value: Edulytics-Staging

      - key: Edulytics__Hosting__RequireDataProtectionCertificate
        value: "true"

      - key: Edulytics__Hosting__DataProtectionCertificateBase64
        sync: false

      - key: Edulytics__Hosting__DataProtectionCertificatePassword
        sync: false

      # Neon pooled runtime connection.
      - key: ConnectionStrings__DefaultConnection
        sync: false

      # Neon DIRECT/non-pooler migration connection.
      - key: ConnectionStrings__MigrationConnection
        sync: false

      # Optional after the first secure bootstrap.
      - key: Edulytics__SuperAdmin__Email
        sync: false

      - key: Edulytics__SuperAdmin__Password
        sync: false

      # SMTP sandbox / staging provider.
      - key: Email__Smtp__Enabled
        sync: false

      - key: Email__Smtp__Host
        sync: false

      - key: Email__Smtp__Port
        sync: false

      - key: Email__Smtp__Security
        sync: false

      - key: Email__Smtp__Username
        sync: false

      - key: Email__Smtp__Password
        sync: false

      - key: Email__Smtp__FromAddress
        sync: false

      - key: Email__Smtp__FromName
        value: Edulytics

===== EXISTING SCALE / MULTI-WORKER TESTS =====
tests/Edulytics.PostgresGate/Program.cs
tests/Edulytics.PostgresGate/obj/Release/net10.0/Edulytics.PostgresGate.AssemblyInfo.cs
tests/Edulytics.Tests/Phase05/IdentitySchoolUserRepositoryTests.cs
tests/Edulytics.Tests/Phase05/Phase05IdentityLifecycleTests.cs
tests/Edulytics.Tests/Phase17/Phase17MigrationContractTests.cs
tests/Edulytics.Tests/Phase22/Phase22OperationalAdminTests.cs
tests/Edulytics.Tests/Phase24/Phase24MaintainabilityTests.cs

===== PHASE 15/17 VERIFIERS =====
scripts/verify-phase15.sh
scripts/verify-phase17.sh

===== RELEVANT PACKAGES =====
src/Edulytics.Data/Edulytics.Data.csproj:15:    <PackageReference Include="Microsoft.AspNetCore.DataProtection.EntityFrameworkCore" Version="10.0.11" />

```

## Core implementation decision

Phase 25 uses Redis-compatible shared state for two cross-instance concerns:

1. ASP.NET Core SignalR scale-out;
2. security-sensitive fixed-window quotas.

The existing ASP.NET Core in-process rate limiters remain in place as local
overload protection. The Redis quota middleware runs first when Phase 25 scale
mode is enabled, so the global security quota cannot multiply by web-instance
count.

Runtime roles are explicit:

- `Combined` — backwards-compatible single-process mode;
- `Web` — serves web traffic without background processors;
- `Worker` — runs background processors.

Outbox/analytics processors and sensitive-data retention are worker-role gated.
The local Outbox worker readiness check is also worker-role gated; a web-only
instance is not considered unready merely because it intentionally does not host
a worker.

Scale mode is disabled by default. Staging/production must explicitly enable
it and provide the shared Redis connection.

## Database connection budget

The Phase 25 application budget is validated against:

```text
NpgsqlMaxPoolSize × (ExpectedWebInstances + ExpectedWorkerInstances)
```

The configured application budget is an internal ceiling, not a claim about the
database provider plan. Staging acceptance must verify that the provider's
actual connection capacity is at least the selected application budget before
horizontal scaling is approved.

## Qualification hardening discovered during harness design

Every application process executes `EdulyticsDatabaseBootstrapper` before
serving. With four first-start processes, the previous check-then-create role
bootstrap could race. Phase 25 serializes PostgreSQL bootstrap with a
session-level advisory lock while leaving the EF InMemory Testing host
unchanged.

The local multi-instance gate also requires `UseRouting()` to precede the
distributed quota middleware so endpoint `EnableRateLimiting` metadata is
available.

## Local executable evidence

The disposable Phase 25 topology now proves:

- 2 Web-role processes;
- 2 Worker-role processes;
- shared PostgreSQL Data Protection;
- Redis SignalR scale-out;
- Redis distributed sensitive Login quota;
- Outbox exactly-once processing attempts;
- each worker operating after the peer is stopped;
- web process loss/restart;
- authenticated gateway redistribution;
- cross-instance authentication-cookie continuity.
