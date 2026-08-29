# Phase 29 — Pedagogical Source License Policy

## Purpose

Edulytics uses external pedagogical sources only when their reuse rights are
compatible with both personal and commercial operation of the product.

A source being free to read on the internet is not sufficient.

The source must have an explicitly accepted reuse license before it can become
a resolved source-driven pedagogical blueprint.

## Mandatory requirements

A pedagogical source used by Edulytics must permit:

1. personal use;
2. commercial use;
3. copying and redistribution;
4. adaptation / modification;
5. use without royalties or recurring source-license fees.

The source license must be verified before the source scope can become
`ResolvedExact`.

Unknown, unclear or restrictive licenses fail closed.

## Automatically approved license identifiers

The runtime allowlist is intentionally narrow:

- `Public Domain`
- `CC0 1.0`
- `CC BY 4.0`

An additional license may be added only after explicit review confirms that it
meets Edulytics commercial-reuse and adaptation requirements.

Adding a license to the allowlist is a deliberate code change with tests and
review; source ingestion may not silently bypass the gate.

## Blocked by default

The following are not automatically acceptable:

- `CC BY-NC`
- `CC BY-NC-SA`
- `CC BY-ND`
- `CC BY-NC-ND`
- `CC BY-SA`
- "free for educational use"
- "free for schools"
- "free to view"
- "all rights reserved"
- an unknown, undocumented or ambiguous license
- a source requiring royalties, paid permission or a commercial-use upgrade

Some licenses in the blocked-by-default list may technically permit some forms
of reuse. They remain blocked because the Edulytics product policy is narrower
than the maximum set of licenses that might be legally usable.

## Attribution

Where the accepted license requires attribution, Edulytics must retain the
required attribution and source provenance.

Attribution does not convert third-party material into Edulytics-owned
copyright.

## Excluded third-party assets

An open license for curriculum text or sequence must not be assumed to cover
separately excluded assets such as:

- trademarks;
- logos;
- book covers;
- proprietary graphic design;
- separately licensed images;
- separately licensed assessment banks;
- any asset expressly excluded by the source's license terms.

Those assets must be excluded unless their own reuse rights independently pass
the same policy.

## Edulytics-authored layer

Edulytics should independently author its own:

- explanations;
- key concepts;
- worked examples;
- step-by-step solutions;
- common mistakes;
- summaries;
- practice items;
- assessment items;
- diagnostics;
- mastery and learning-intelligence content.

External open sources provide traceable curriculum structure and alignment
where appropriate; they do not replace the Edulytics-authored instructional
and intelligence layer.

## Current Common Core middle-school state

The current Common Core Grades 6–8 source-driven blueprints identify their
pedagogical source license as `CC BY 4.0`.

They therefore pass the initial automatic allowlist.

## Fail-closed rule

If a future blueprint declares any SourceLicense not present in
`PedagogicalSourceLicensePolicy.ApprovedCommercialReuseAndAdaptationLicenses`,
the blueprint contract must reject it.

No developer, importer or seeder may infer commercial permission from the fact
that content is publicly accessible.

## Scope note

This is an Edulytics product-engineering control. It records and enforces the
product's accepted source-license policy; it is not a guarantee that no third
party can ever make a legal claim.

Official curriculum authority/source rights and third-party trademarks remain
subject to their own provenance and rights verification.
