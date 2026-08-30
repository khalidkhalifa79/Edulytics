# Curriculum language policy

Status: authoritative, effective for Phase 29 and later.

Curriculum academic content remains in the official/source academic language of its curriculum. Application localization is a separate concern: changing Edulytics UI culture changes navigation, chrome, system messages, and section labels, but never selects or generates a translated curriculum body.

- US Common Core academic language: English (`en`).
- England curriculum academic language: English (`en`).
- Polish national curriculum academic language: Polish (`pl`).
- Future packs must declare their source academic language.

Canonical readers select a body using `MathematicsCurriculumPackDefinition.AcademicLanguage`, not `CurrentUICulture`. There is no runtime AI translation, automatic curriculum translation, or school/user API-key dependency.

For Common Core, Polish translation is cancelled and not applicable. Only each row's `en` object in `.phase29-source-rebuild/translation-manifest/en-to-pl.json` is authoritative. Its `pl` null is historical workflow shape, not missing required content. `.phase29-source-rebuild/polish-authoring/` is experimental, non-canonical, and must never be imported.

Official standards, outcomes, wording, and codes retain their source identity. Supporting lessons are source-derived canonical lessons with zero independent OutcomeCodes; that absence must never be filled with a fabricated official mapping.
