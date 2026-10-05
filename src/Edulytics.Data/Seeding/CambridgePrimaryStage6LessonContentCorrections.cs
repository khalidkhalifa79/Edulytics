using Edulytics.Core.Curriculum;

namespace Edulytics.Data.Seeding;

/// <summary>
/// Narrow, versioned corrections for four Cambridge Primary Stage 6
/// lessons whose original Phase 29 bodies were too generic for the exact
/// reviewed lesson skill. Outcome mappings are owned by the curriculum
/// blueprint; this class only upgrades learner-facing canonical content.
/// </summary>
public static class CambridgePrimaryStage6LessonContentCorrections
{
    public const string PackCode =
        "CAMBRIDGE-INTL-MATH";

    public const string BaseContentVersion =
        "phase29-cambridge-primary-stage6-dfe-ogl-v1";

    public const string PriorCorrectionContentVersion =
        "phase29-cambridge-primary-stage6-alignment-v2";

    public const string CorrectionContentVersion =
        "phase29-cambridge-primary-stage6-official-alignment-v3";

    public const string TwoUnknownsLessonCode =
        "PED:CAMBRIDGE-INTL-MATH:S6:6AS-MD-4:APPLY";

    public const string ScaleReadingBuildLessonCode =
        "PED:CAMBRIDGE-INTL-MATH:S6:6NPV-4:BUILD";

    public const string ScaleReadingLessonCode =
        "PED:CAMBRIDGE-INTL-MATH:S6:6NPV-4:APPLY";

    public const string FractionComparisonLessonCode =
        "PED:CAMBRIDGE-INTL-MATH:S6:6F-3:BUILD";

    private static readonly HashSet<string> TargetLessonCodes =
        new(StringComparer.Ordinal)
        {
            TwoUnknownsLessonCode,
            ScaleReadingBuildLessonCode,
            ScaleReadingLessonCode,
            FractionComparisonLessonCode
        };

    public static bool IsTarget(
        CanonicalLessonContentPackDocument document,
        CanonicalLessonContentPackLesson lesson) =>
        string.Equals(
            document.PackCode,
            PackCode,
            StringComparison.Ordinal) &&
        string.Equals(
            document.ContentVersion,
            BaseContentVersion,
            StringComparison.Ordinal) &&
        TargetLessonCodes.Contains(lesson.LessonCode);

    public static string GetExpectedContentVersion(
        CanonicalLessonContentPackDocument document,
        CanonicalLessonContentPackLesson lesson) =>
        IsTarget(document, lesson)
            ? CorrectionContentVersion
            : document.ContentVersion;

    public static bool CanUpgradeExisting(
        CanonicalLessonContentPackDocument document,
        CanonicalLessonContentPackLesson lesson,
        string existingContentVersion) =>
        IsTarget(document, lesson) &&
        (
            string.Equals(
                existingContentVersion,
                BaseContentVersion,
                StringComparison.Ordinal) ||
            string.Equals(
                existingContentVersion,
                PriorCorrectionContentVersion,
                StringComparison.Ordinal)
        );

    public static void ApplyApprovedCorrections(
        CanonicalLessonContentPackDocument document)
    {
        if (!string.Equals(
                document.PackCode,
                PackCode,
                StringComparison.Ordinal) ||
            !string.Equals(
                document.ContentVersion,
                BaseContentVersion,
                StringComparison.Ordinal))
        {
            return;
        }

        foreach (var lesson in document.Lessons)
        {
            if (!TargetLessonCodes.Contains(lesson.LessonCode))
                continue;

            var translation =
                lesson.Translations.SingleOrDefault(
                    x => string.Equals(
                        x.CultureCode,
                        "en",
                        StringComparison.Ordinal))
                ?? throw new InvalidOperationException(
                    $"Stage 6 alignment correction requires English canonical content for {lesson.LessonCode}.");

            ApplyLessonCorrection(
                lesson.LessonCode,
                translation);
        }
    }

    private static void ApplyLessonCorrection(
        string lessonCode,
        CanonicalLessonContentPackTranslation translation)
    {
        switch (lessonCode)
        {
            case TwoUnknownsLessonCode:
                ApplyTwoUnknownsCorrection(translation);
                break;

            case ScaleReadingBuildLessonCode:
                ApplyScaleReadingBuildCorrection(translation);
                break;

            case ScaleReadingLessonCode:
                ApplyScaleReadingCorrection(translation);
                break;

            case FractionComparisonLessonCode:
                ApplyFractionComparisonCorrection(translation);
                break;

            default:
                throw new InvalidOperationException(
                    $"Unsupported Stage 6 correction target: {lessonCode}.");
        }
    }

    private static void ApplyTwoUnknownsCorrection(
        CanonicalLessonContentPackTranslation translation)
    {
        translation.Explanation =
            "This Cambridge Primary Stage 6 lesson develops the reviewed focus “Solve problems with 2 unknowns” and is formally linked to Cambridge reference objective 6Nc.02. A two-unknown problem gives two quantities whose values are not known and two independent facts that link them. Represent both facts before calculating. For example, if x + y = 46 and y − x = 8, then the total and difference must be true at the same time: x = 19 and y = 27. A pair is a solution only when it satisfies both relationships. The lesson is Edulytics-authored from OGL material; Cambridge remains the academic reference authority and no Cambridge objective wording is reproduced here.";

        translation.KeyConceptsAndRules =
            "Source focus: Solve problems with 2 unknowns. Define what each unknown represents. Write two independent relationships from the two facts in the problem. Preserve equality while eliminating, substituting or reasoning from the total and difference. Solve for both unknowns, not just one. Check the final pair in both original relationships. Appropriate representations include two equations, a bar model, a table or another diagram that keeps both linked quantities visible.";

        translation.WorkedExamples =
            "Example A: Two boxes contain 46 counters altogether. Box B has 8 more counters than Box A. Let x be Box A and y be Box B. Then x + y = 46 and y − x = 8. Subtract the difference from the total: 46 − 8 = 38. Split 38 equally: x = 19. Then y = 27. Check: 19 + 27 = 46 and 27 − 19 = 8. Example B: Two numbers total 54 and one is twice the other. Let x be the smaller and y be the larger. Then x + y = 54 and y = 2x. Substitute: x + 2x = 54, so 3x = 54, x = 18 and y = 36. Check both relationships.";

        translation.StepByStepSolutions =
            "Step 1: Name the two unknown quantities, for example x and y. Step 2: Translate the first fact into a relationship. Step 3: Translate the second independent fact into another relationship. Step 4: Use both relationships together to find one unknown by elimination, substitution or structured reasoning. Step 5: Find the second unknown. Step 6: Substitute the pair back into both original relationships. If either fact fails, the pair is not a solution.";

        translation.CommonMistakes =
            "Do not solve only one relationship: many pairs can satisfy x + y = a fixed total. Do not assume the unknowns are equal unless the problem says so. Do not accept a pair just because it gives the correct total; it must also satisfy the second relationship. Keep the meaning of x and y consistent throughout the solution, and verify both equations independently.";

        translation.QuickSummary =
            "Two unknowns need two independent relationships. Represent both, solve them together, find both values, and verify the final pair in both original facts.";
    }

    private static void ApplyScaleReadingBuildCorrection(
        CanonicalLessonContentPackTranslation translation)
    {
        translation.Title =
            "Round decimals to the nearest tenth or whole number: Build the Idea";
        translation.Explanation =
            "Rounding a decimal means choosing the nearest target place value while preserving the number's approximate size. To round to the nearest tenth, locate the two neighbouring tenths and use the hundredths digit to decide which is closer. To round to the nearest whole number, locate the two neighbouring whole numbers and use the tenths digit. A number line provides an independent check.";
        translation.KeyConceptsAndRules =
            "Nearest tenth: inspect the hundredths digit; 0–4 rounds down and 5–9 rounds up. Nearest whole number: inspect the tenths digit with the same rule. Keep all digits to the left of the rounding place unchanged unless carrying is required. Boundary values ending in exactly 5 at the deciding digit round to the higher target value.";
        translation.WorkedExamples =
            "Example A: 4.36 lies between 4.3 and 4.4 and is closer to 4.4, so 4.36 rounds to 4.4 to the nearest tenth. Example B: 4.36 lies between 4 and 5 and is closer to 4, so it rounds to 4 to the nearest whole number. Example C: 7.95 rounds to 8.0 to the nearest tenth because the hundredths digit is 5 and carrying changes the whole-number digit.";
        translation.StepByStepSolutions =
            "Step 1: Identify whether the target is a tenth or a whole number. Step 2: Mark the rounding digit. Step 3: Inspect the digit immediately to its right. Step 4: Keep the rounding digit for 0–4 or increase it by one for 5–9, carrying if needed. Step 5: Remove digits to the right of the rounding place. Step 6: Check on a number line that the rounded value is one of the two nearest target values and is the closer one.";
        translation.CommonMistakes =
            "Do not inspect the wrong digit: hundredths decide rounding to tenths, while tenths decide rounding to whole numbers. Do not truncate instead of round. When the rounding digit is 9 and must increase, carry to the next place value correctly.";
        translation.QuickSummary =
            "Choose the target place value, inspect the next digit, round down for 0–4 or up for 5–9, then verify the result against neighbouring target values.";
    }

    private static void ApplyScaleReadingCorrection(
        CanonicalLessonContentPackTranslation translation)
    {
        translation.Title =
            "Round decimals to the nearest tenth or whole number: Reason and Apply";
        translation.Explanation =
            "Apply decimal rounding by identifying the required place value, comparing the number with its two neighbouring target values, and selecting the nearer one. The digit immediately to the right of the target place encodes that comparison: 0–4 keeps the rounding digit and 5–9 increases it by one. Use place value and a number line to justify the decision rather than relying on a memorised phrase alone.";
        translation.KeyConceptsAndRules =
            "For nearest tenth, the hundredths digit controls the decision. For nearest whole number, the tenths digit controls it. Values exactly halfway between two targets round to the higher target under the curriculum convention used here. Carry across place values when increasing a 9. A valid answer should be plausible in size and lie on the required tenth or whole-number grid.";
        translation.WorkedExamples =
            "Example A: 12.64 rounds to 12.6 to the nearest tenth because the hundredths digit is 4; it rounds to 13 to the nearest whole number because the tenths digit is 6. Example B: 9.96 rounds to 10.0 to the nearest tenth and 10 to the nearest whole number. Example C: A measurement of 3.05 rounds to 3.1 to the nearest tenth.";
        translation.StepByStepSolutions =
            "Step 1: State the requested accuracy. Step 2: Identify the rounding digit and the deciding digit immediately to its right. Step 3: Apply the 0–4 / 5–9 decision. Step 4: Carry if increasing the rounding digit crosses a place-value boundary. Step 5: Write the result at the requested accuracy. Step 6: Check it against the lower and upper neighbouring target values and explain why it is the nearer one.";
        translation.CommonMistakes =
            "Do not round twice in stages, because double rounding can change the result. Do not use the hundredths digit when rounding directly to a whole number. Do not forget carrying when the rounding digit is 9. Keep trailing zero notation when it communicates the requested tenth precision, for example 10.0.";
        translation.QuickSummary =
            "Round directly to the requested place using the next digit, handle carrying correctly, and verify the result against the two neighbouring target values.";
    }

    private static void ApplyFractionComparisonCorrection(
        CanonicalLessonContentPackTranslation translation)
    {
        translation.Explanation =
            "This Cambridge Primary Stage 6 lesson develops the reviewed focus “Compare fractions with different denominators” and is formally linked to Cambridge reference objective 6Nf.08. Fractions can be compared only by their values relative to the same whole. When denominators differ, rewrite the fractions as equivalent fractions with a common denominator, or use another valid representation such as equal-whole fraction bars or a number line. For example, 2/3 = 8/12 and 3/4 = 9/12, so 3/4 is larger. Equivalent forms change the numerator and denominator together without changing the fraction’s value. The lesson is Edulytics-authored from OGL material; Cambridge remains the academic reference authority and no Cambridge objective wording is reproduced here.";

        translation.KeyConceptsAndRules =
            "Source focus: Compare fractions with different denominators. The fractions must refer to the same-sized whole. Use a common denominator by making equivalent fractions, then compare the numerators. As a numerical check, cross-products can compare a/b and c/d by comparing a × d with c × b. Equal-whole fraction bars or a number line should preserve the same values shown by the calculation. Never decide which fraction is larger by looking at denominator digits alone.";

        translation.WorkedExamples =
            "Example A: Compare 2/3 and 3/4. A common denominator is 12: 2/3 = 8/12 and 3/4 = 9/12. Since 9/12 > 8/12, 3/4 is larger. Example B: Compare 3/5 and 5/8. A common denominator is 40: 3/5 = 24/40 and 5/8 = 25/40, so 3/5 < 5/8. Example C: A student says 3/8 > 1/2 because 8 > 2. This is false: 1/2 = 4/8, and 4/8 > 3/8. Equal-whole fraction bars or a number line show the same comparison.";

        translation.StepByStepSolutions =
            "Step 1: Check that the fractions describe the same whole. Step 2: Choose a useful common denominator, usually the least common multiple of the denominators, or use an equivalent visual representation. Step 3: Rewrite both fractions without changing their values. Step 4: Compare the new numerators because the denominators are now the same. Step 5: Write <, > or = and explain the comparison. Step 6: Check with fraction bars, a number line or cross-products when useful.";

        translation.CommonMistakes =
            "Do not assume the fraction with the larger denominator is larger; more equal parts make each part smaller. Do not compare only numerators when denominators differ. Do not change only the denominator when creating an equivalent fraction: multiply or divide numerator and denominator by the same non-zero factor. Make sure visual models use equal-sized wholes before comparing shaded parts.";

        translation.QuickSummary =
            "To compare fractions with different denominators, preserve each fraction’s value, create equivalent forms with a common denominator or use an equal-whole visual model, then compare the values and justify <, > or =.";
    }
}
