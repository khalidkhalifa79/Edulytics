# Phase 01 — Empty Solution Foundation

This phase creates the clean foundation for Edulytics without introducing database work, migrations, Entity Framework, Identity, localization implementation, or tenant logic.

## Scope

- solution scaffold
- SDK pinning
- project structure
- reference architecture
- basic MVC app shell
- tests project
- scripts for build/test/restore/clean
- git hygiene

## Explicitly excluded in Phase 01

- DbContext
- EF Core packages
- SQL Server packages
- migrations
- Identity
- authentication
- login flows
- language selector implementation
- school entity
- tenant model implementation
- database creation

## Required project names

- src/Edulytics.Core
- src/Edulytics.Services
- src/Edulytics.Data
- src/Edulytics.Web
- tests/Edulytics.Tests

## Architecture rules kept in Phase 01

- Web references Services and Data only for the allowed future layering.
- No DbContext in MVC controllers.
- No SQL in controllers or Razor views.
- No business logic in Razor views.
- Controllers call services.

## Verification

- dotnet build
- dotnet test
- git diff --check
- git status --short
