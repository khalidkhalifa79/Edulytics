using Edulytics.Core.Analytics;
using Edulytics.Core.Entities;
using Edulytics.Core.Enums;

namespace Edulytics.Services.Analytics;

public sealed class AnalyticsProjectionBuilder
{
    public AnalyticsProjectionSet Build(
        AnalyticsSourceSnapshot source,
        DateTime calculatedAtUtc)
    {
        var assessments = source.Assessments
            .Where(x => x.Status != AssessmentStatus.Draft)
            .ToDictionary(x => x.Id);

        var classes = source.ClassGroups.ToDictionary(x => x.Id);
        var students = source.StudentProfiles.ToDictionary(x => x.Id);
        var outcomes = source.LearningOutcomes.ToDictionary(x => x.Id);
        var topics = source.CurriculumTopics.ToDictionary(x => x.Id);

        var questions = source.AssessmentQuestions
            .Where(x => assessments.ContainsKey(x.AssessmentId))
            .ToDictionary(x => x.Id);

        var results = source.AssessmentResults
            .Where(x => assessments.ContainsKey(x.AssessmentId))
            .ToDictionary(x => x.Id);

        var mappings = source.OutcomeMappings
            .GroupBy(x => x.AssessmentQuestionId)
            .ToDictionary(
                x => x.Key,
                x => x.ToArray());

        var accumulator =
            new Dictionary<StudentOutcomeKey, ScoreAccumulator>();

        foreach (var answer in source.StudentAnswers)
        {
            if (!results.TryGetValue(answer.AssessmentResultId, out var result))
                continue;

            if (!assessments.TryGetValue(result.AssessmentId, out var assessment))
                continue;

            if (!questions.TryGetValue(answer.AssessmentQuestionId, out var question) ||
                question.AssessmentId != assessment.Id)
            {
                throw new InvalidOperationException(
                    "StudentAnswer references a question outside its assessment.");
            }

            if (!classes.TryGetValue(assessment.ClassGroupId, out var classGroup))
            {
                throw new InvalidOperationException(
                    "Assessment ClassGroup is missing.");
            }

            if (!students.ContainsKey(result.StudentProfileId))
            {
                throw new InvalidOperationException(
                    "AssessmentResult StudentProfile is missing.");
            }

            if (question.MaxScore <= 0m ||
                answer.Score < 0m ||
                answer.Score > question.MaxScore)
            {
                throw new InvalidOperationException(
                    "Analytics source contains an invalid question score.");
            }

            if (!mappings.TryGetValue(question.Id, out var questionMappings) ||
                questionMappings.Length == 0)
            {
                throw new InvalidOperationException(
                    "Analytics source contains an unmapped assessment question.");
            }

            var mappedOutcomes = new List<LearningOutcome>();

            foreach (var mapping in questionMappings)
            {
                if (!outcomes.TryGetValue(mapping.LearningOutcomeId, out var outcome))
                {
                    throw new InvalidOperationException(
                        "Question mapping references a missing LearningOutcome.");
                }

                if (outcome.SchoolId != assessment.SchoolId ||
                    outcome.SubjectId != assessment.SubjectId ||
                    outcome.GradeLevelId != classGroup.GradeLevelId)
                {
                    throw new InvalidOperationException(
                        "Question mapping violates assessment curriculum scope.");
                }

                mappedOutcomes.Add(outcome);
            }

            var divisor = (decimal)mappedOutcomes.Count;
            var allocatedEarned = answer.Score / divisor;
            var allocatedPossible = question.MaxScore / divisor;

            foreach (var outcome in mappedOutcomes)
            {
                var key = new StudentOutcomeKey(
                    assessment.SchoolId,
                    assessment.AcademicYearId,
                    assessment.ClassGroupId,
                    assessment.SubjectId,
                    result.StudentProfileId,
                    outcome.Id);

                if (!accumulator.TryGetValue(key, out var value))
                {
                    value = new ScoreAccumulator();
                    accumulator[key] = value;
                }

                value.Earned += allocatedEarned;
                value.Possible += allocatedPossible;
                value.EvidenceCount++;
            }
        }

        var studentMasteries = accumulator
            .OrderBy(x => x.Key.AcademicYearId)
            .ThenBy(x => x.Key.ClassGroupId)
            .ThenBy(x => x.Key.SubjectId)
            .ThenBy(x => x.Key.StudentProfileId)
            .ThenBy(x => x.Key.LearningOutcomeId)
            .Select(x =>
            {
                var percentage = Percentage(
                    x.Value.Earned,
                    x.Value.Possible);

                return new StudentOutcomeMastery
                {
                    Id = Guid.NewGuid(),
                    SchoolId = x.Key.SchoolId,
                    AcademicYearId = x.Key.AcademicYearId,
                    ClassGroupId = x.Key.ClassGroupId,
                    SubjectId = x.Key.SubjectId,
                    StudentProfileId = x.Key.StudentProfileId,
                    LearningOutcomeId = x.Key.LearningOutcomeId,
                    EarnedScore = Round4(x.Value.Earned),
                    PossibleScore = Round4(x.Value.Possible),
                    MasteryPercentage = percentage,
                    EvidenceCount = x.Value.EvidenceCount,
                    Band = BandFor(percentage),
                    CalculatedAtUtc = calculatedAtUtc
                };
            })
            .ToArray();

        var classOutcomes = studentMasteries
            .GroupBy(x => new
            {
                x.SchoolId,
                x.AcademicYearId,
                x.ClassGroupId,
                x.SubjectId,
                x.LearningOutcomeId
            })
            .Select(group =>
            {
                var earned = group.Sum(x => x.EarnedScore);
                var possible = group.Sum(x => x.PossibleScore);
                var percentage = Percentage(earned, possible);

                return new ClassOutcomeSummary
                {
                    Id = Guid.NewGuid(),
                    SchoolId = group.Key.SchoolId,
                    AcademicYearId = group.Key.AcademicYearId,
                    ClassGroupId = group.Key.ClassGroupId,
                    SubjectId = group.Key.SubjectId,
                    LearningOutcomeId = group.Key.LearningOutcomeId,
                    EarnedScore = Round4(earned),
                    PossibleScore = Round4(possible),
                    AverageMasteryPercentage = percentage,
                    StudentCount = group
                        .Select(x => x.StudentProfileId)
                        .Distinct()
                        .Count(),
                    AtRiskStudentCount = group
                        .Count(x => x.MasteryPercentage < 60m),
                    EvidenceCount = group.Sum(x => x.EvidenceCount),
                    CalculatedAtUtc = calculatedAtUtc
                };
            })
            .OrderBy(x => x.AcademicYearId)
            .ThenBy(x => x.ClassGroupId)
            .ThenBy(x => x.SubjectId)
            .ThenBy(x => x.LearningOutcomeId)
            .ToArray();

        var topicInputs = classOutcomes
            .Select(x =>
            {
                if (!outcomes.TryGetValue(x.LearningOutcomeId, out var outcome))
                {
                    throw new InvalidOperationException(
                        "Class outcome references a missing LearningOutcome.");
                }

                if (!topics.ContainsKey(outcome.TopicId))
                {
                    throw new InvalidOperationException(
                        "LearningOutcome references a missing CurriculumTopic.");
                }

                return new
                {
                    Summary = x,
                    Outcome = outcome
                };
            })
            .ToArray();

        var classTopics = topicInputs
            .GroupBy(x => new
            {
                x.Summary.SchoolId,
                x.Summary.AcademicYearId,
                x.Summary.ClassGroupId,
                x.Summary.SubjectId,
                TopicId = x.Outcome.TopicId
            })
            .Select(group =>
            {
                var weighted = group
                    .Select(x => new
                    {
                        x.Summary.AverageMasteryPercentage,
                        Weight = x.Outcome.Weight > 0m
                            ? x.Outcome.Weight
                            : 1m
                    })
                    .ToArray();

                var denominator = weighted.Sum(x => x.Weight);
                var mastery = denominator <= 0m
                    ? 0m
                    : Round2(
                        weighted.Sum(
                            x =>
                                x.AverageMasteryPercentage *
                                x.Weight) /
                        denominator);

                var topicOutcomeIds = group
                    .Select(x => x.Outcome.Id)
                    .ToHashSet();

                var studentCount = studentMasteries
                    .Where(x =>
                        x.AcademicYearId == group.Key.AcademicYearId &&
                        x.ClassGroupId == group.Key.ClassGroupId &&
                        x.SubjectId == group.Key.SubjectId &&
                        topicOutcomeIds.Contains(x.LearningOutcomeId))
                    .Select(x => x.StudentProfileId)
                    .Distinct()
                    .Count();

                return new ClassTopicSummary
                {
                    Id = Guid.NewGuid(),
                    SchoolId = group.Key.SchoolId,
                    AcademicYearId = group.Key.AcademicYearId,
                    ClassGroupId = group.Key.ClassGroupId,
                    SubjectId = group.Key.SubjectId,
                    CurriculumTopicId = group.Key.TopicId,
                    MasteryPercentage = mastery,
                    OutcomeCount = group
                        .Select(x => x.Outcome.Id)
                        .Distinct()
                        .Count(),
                    WeakOutcomeCount = group
                        .Count(
                            x =>
                                x.Summary.AverageMasteryPercentage <
                                60m),
                    StudentCount = studentCount,
                    CalculatedAtUtc = calculatedAtUtc
                };
            })
            .OrderBy(x => x.AcademicYearId)
            .ThenBy(x => x.ClassGroupId)
            .ThenBy(x => x.SubjectId)
            .ThenBy(x => x.CurriculumTopicId)
            .ToArray();

        foreach (var result in results.Values)
        {
            if (result.Percentage < 0m || result.Percentage > 100m)
            {
                throw new InvalidOperationException(
                    "Analytics source contains an invalid result percentage.");
            }
        }

        var trends = results.Values
            .GroupBy(x => x.AssessmentId)
            .Select(group =>
            {
                var assessment = assessments[group.Key];

                return new ClassAssessmentTrend
                {
                    Id = Guid.NewGuid(),
                    SchoolId = assessment.SchoolId,
                    AcademicYearId = assessment.AcademicYearId,
                    ClassGroupId = assessment.ClassGroupId,
                    SubjectId = assessment.SubjectId,
                    AssessmentId = assessment.Id,
                    AssessmentTitle = assessment.Title,
                    AssessmentDate = assessment.AssessmentDate,
                    AveragePercentage = Round2(
                        group.Average(x => x.Percentage)),
                    StudentCount = group
                        .Select(x => x.StudentProfileId)
                        .Distinct()
                        .Count(),
                    AtRiskStudentCount = group
                        .Count(x => x.Percentage < 60m),
                    CalculatedAtUtc = calculatedAtUtc
                };
            })
            .OrderBy(x => x.AssessmentDate)
            .ThenBy(x => x.AssessmentTitle)
            .ToArray();

        var schoolSnapshots = studentMasteries
            .GroupBy(x => new
            {
                x.SchoolId,
                x.AcademicYearId
            })
            .Select(group =>
            {
                var earned = group.Sum(x => x.EarnedScore);
                var possible = group.Sum(x => x.PossibleScore);

                var riskStudents = group
                    .GroupBy(x => x.StudentProfileId)
                    .Count(student =>
                        Percentage(
                            student.Sum(x => x.EarnedScore),
                            student.Sum(x => x.PossibleScore)) < 60m);

                var criticalOutcomes = classOutcomes
                    .Where(
                        x =>
                            x.SchoolId == group.Key.SchoolId &&
                            x.AcademicYearId ==
                            group.Key.AcademicYearId)
                    .GroupBy(x => x.LearningOutcomeId)
                    .Count(outcome =>
                        Percentage(
                            outcome.Sum(x => x.EarnedScore),
                            outcome.Sum(x => x.PossibleScore)) < 40m);

                var weakTopics = classTopics
                    .Where(
                        x =>
                            x.SchoolId == group.Key.SchoolId &&
                            x.AcademicYearId ==
                            group.Key.AcademicYearId)
                    .GroupBy(x => x.CurriculumTopicId)
                    .Count(topic =>
                    {
                        var rows = topic.ToArray();

                        var denominator =
                            rows.Sum(
                                x =>
                                    Math.Max(
                                        x.StudentCount,
                                        1));

                        if (denominator <= 0)
                            return false;

                        var average = Round2(
                            rows.Sum(
                                x =>
                                    x.MasteryPercentage *
                                    Math.Max(
                                        x.StudentCount,
                                        1)) /
                            denominator);

                        return average < 60m;
                    });

                return new SchoolAnalyticsSnapshot
                {
                    Id = Guid.NewGuid(),
                    SchoolId = group.Key.SchoolId,
                    AcademicYearId = group.Key.AcademicYearId,
                    OverallMasteryPercentage =
                        Percentage(earned, possible),
                    StudentsWithEvidence = group
                        .Select(x => x.StudentProfileId)
                        .Distinct()
                        .Count(),
                    AtRiskStudents = riskStudents,
                    CriticalOutcomeCount = criticalOutcomes,
                    WeakTopicCount = weakTopics,
                    LatestSourceUpdatedAtUtc =
                        LatestSourceForYear(
                            source,
                            assessments,
                            group.Key.AcademicYearId),
                    CalculatedAtUtc = calculatedAtUtc
                };
            })
            .OrderBy(x => x.AcademicYearId)
            .ToArray();

        return new AnalyticsProjectionSet(
            studentMasteries,
            classOutcomes,
            classTopics,
            trends,
            schoolSnapshots);
    }

    public static MasteryBand BandFor(decimal percentage) =>
        percentage switch
        {
            < 40m => MasteryBand.CriticalGap,
            < 60m => MasteryBand.Weak,
            < 75m => MasteryBand.Developing,
            < 90m => MasteryBand.Secure,
            _ => MasteryBand.Strong
        };

    private static DateTime? LatestSourceForYear(
        AnalyticsSourceSnapshot source,
        IReadOnlyDictionary<Guid, Assessment> assessments,
        Guid academicYearId)
    {
        var assessmentIds = assessments.Values
            .Where(x => x.AcademicYearId == academicYearId)
            .Select(x => x.Id)
            .ToHashSet();

        var results = source.AssessmentResults
            .Where(x => assessmentIds.Contains(x.AssessmentId))
            .ToArray();

        var resultIds = results
            .Select(x => x.Id)
            .ToHashSet();

        DateTime? latest = null;

        foreach (var result in results)
        {
            if (!latest.HasValue ||
                result.UpdatedAtUtc > latest.Value)
            {
                latest = result.UpdatedAtUtc;
            }
        }

        foreach (var answer in source.StudentAnswers
                     .Where(
                         x =>
                             resultIds.Contains(
                                 x.AssessmentResultId)))
        {
            if (!latest.HasValue ||
                answer.UpdatedAtUtc > latest.Value)
            {
                latest = answer.UpdatedAtUtc;
            }
        }

        return latest;
    }

    private static decimal Percentage(
        decimal earned,
        decimal possible)
    {
        if (possible <= 0m)
            return 0m;

        return Round2(
            earned / possible * 100m);
    }

    private static decimal Round2(decimal value) =>
        decimal.Round(
            value,
            2,
            MidpointRounding.AwayFromZero);

    private static decimal Round4(decimal value) =>
        decimal.Round(
            value,
            4,
            MidpointRounding.AwayFromZero);

    private readonly record struct StudentOutcomeKey(
        Guid SchoolId,
        Guid AcademicYearId,
        Guid ClassGroupId,
        Guid SubjectId,
        Guid StudentProfileId,
        Guid LearningOutcomeId);

    private sealed class ScoreAccumulator
    {
        public decimal Earned { get; set; }
        public decimal Possible { get; set; }
        public int EvidenceCount { get; set; }
    }
}
