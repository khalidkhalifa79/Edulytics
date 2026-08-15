using Edulytics.Core.Analytics;
using Edulytics.Core.Entities;
using Edulytics.Core.Enums;
using Edulytics.Services.Analytics;

namespace Edulytics.Tests.Phase09;

public sealed class AnalyticsProjectionBuilderTests
{
    [Fact]
    public void MasteryAndTopicWeighting_AreCalculatedFromAnswers()
    {
        var source = BuildTwoQuestionSource(
            AssessmentStatus.Open,
            firstScore: 2m,
            secondScore: 5m);

        var result =
            new AnalyticsProjectionBuilder()
                .Build(
                    source,
                    new DateTime(
                        2026,
                        8,
                        15,
                        12,
                        0,
                        0,
                        DateTimeKind.Utc));

        Assert.Equal(
            2,
            result.StudentOutcomeMasteries.Count);

        var values =
            result.StudentOutcomeMasteries
                .OrderBy(x => x.LearningOutcomeId)
                .Select(x => x.MasteryPercentage)
                .OrderBy(x => x)
                .ToArray();

        Assert.Equal(
            [40m, 100m],
            values);

        var topic =
            Assert.Single(
                result.ClassTopicSummaries);

        // weights are 1 and 3:
        // (40*1 + 100*3) / 4 = 85.
        Assert.Equal(
            85m,
            topic.MasteryPercentage);

        var trend =
            Assert.Single(
                result.ClassAssessmentTrends);

        Assert.Equal(
            70m,
            trend.AveragePercentage);
    }

    [Fact]
    public void DraftAssessment_IsExcluded()
    {
        var source = BuildTwoQuestionSource(
            AssessmentStatus.Draft,
            5m,
            5m);

        var result =
            new AnalyticsProjectionBuilder()
                .Build(
                    source,
                    DateTime.UtcNow);

        Assert.Empty(
            result.StudentOutcomeMasteries);

        Assert.Empty(
            result.ClassOutcomeSummaries);

        Assert.Empty(
            result.ClassTopicSummaries);

        Assert.Empty(
            result.ClassAssessmentTrends);
    }

    [Fact]
    public void MultiOutcomeQuestion_DoesNotDoubleCountRawScore()
    {
        var schoolId = Guid.NewGuid();
        var yearId = Guid.NewGuid();
        var gradeId = Guid.NewGuid();
        var classId = Guid.NewGuid();
        var subjectId = Guid.NewGuid();
        var studentId = Guid.NewGuid();
        var topicId = Guid.NewGuid();
        var outcome1 = Guid.NewGuid();
        var outcome2 = Guid.NewGuid();
        var assessmentId = Guid.NewGuid();
        var questionId = Guid.NewGuid();
        var resultId = Guid.NewGuid();
        var framework = Guid.NewGuid();

        var source = new AnalyticsSourceSnapshot(
            [
                new AcademicYear
                {
                    Id = yearId,
                    SchoolId = schoolId,
                    Name = "2026/2027",
                    StartsOn =
                        new DateOnly(2026, 9, 1),
                    EndsOn =
                        new DateOnly(2027, 6, 30),
                    Status =
                        AcademicStructureStatus.Active
                }
            ],
            [
                new ClassGroup
                {
                    Id = classId,
                    SchoolId = schoolId,
                    AcademicYearId = yearId,
                    GradeLevelId = gradeId,
                    Name = "6A",
                    Code = "6A",
                    NormalizedCode = "6A",
                    Status =
                        AcademicStructureStatus.Active
                }
            ],
            [
                new Subject
                {
                    Id = subjectId,
                    SchoolId = schoolId,
                    Name = "Mathematics",
                    Code = "MATH",
                    NormalizedCode = "MATH",
                    Status =
                        AcademicStructureStatus.Active
                }
            ],
            [
                new StudentProfile
                {
                    Id = studentId,
                    SchoolId = schoolId,
                    StudentNumber = "S1",
                    NormalizedStudentNumber = "S1",
                    FirstName = "A",
                    LastName = "Student",
                    DisplayName = "A Student",
                    Status =
                        AcademicStructureStatus.Active
                }
            ],
            [],
            [],
            [
                new CurriculumTopic
                {
                    Id = topicId,
                    SchoolId = schoolId,
                    FrameworkVersionId = framework,
                    SubjectId = subjectId,
                    GradeLevelId = gradeId,
                    Name = "Numbers",
                    Order = 1
                }
            ],
            [
                new LearningOutcome
                {
                    Id = outcome1,
                    SchoolId = schoolId,
                    FrameworkVersionId = framework,
                    SubjectId = subjectId,
                    GradeLevelId = gradeId,
                    TopicId = topicId,
                    Code = "N1",
                    Description = "One",
                    Weight = 1m,
                    Order = 1
                },
                new LearningOutcome
                {
                    Id = outcome2,
                    SchoolId = schoolId,
                    FrameworkVersionId = framework,
                    SubjectId = subjectId,
                    GradeLevelId = gradeId,
                    TopicId = topicId,
                    Code = "N2",
                    Description = "Two",
                    Weight = 1m,
                    Order = 2
                }
            ],
            [
                new Assessment
                {
                    Id = assessmentId,
                    SchoolId = schoolId,
                    SubjectId = subjectId,
                    ClassGroupId = classId,
                    AcademicYearId = yearId,
                    TermId = Guid.NewGuid(),
                    Title = "Assessment",
                    AssessmentDate =
                        new DateOnly(2026, 9, 20),
                    MaxScore = 10m,
                    Status = AssessmentStatus.Open,
                    CreatedByUserId = Guid.NewGuid()
                }
            ],
            [
                new AssessmentQuestion
                {
                    Id = questionId,
                    SchoolId = schoolId,
                    AssessmentId = assessmentId,
                    Prompt = "Question",
                    MaxScore = 10m,
                    Order = 1
                }
            ],
            [
                new QuestionLearningOutcome
                {
                    Id = Guid.NewGuid(),
                    SchoolId = schoolId,
                    AssessmentQuestionId = questionId,
                    LearningOutcomeId = outcome1
                },
                new QuestionLearningOutcome
                {
                    Id = Guid.NewGuid(),
                    SchoolId = schoolId,
                    AssessmentQuestionId = questionId,
                    LearningOutcomeId = outcome2
                }
            ],
            [
                new AssessmentResult
                {
                    Id = resultId,
                    SchoolId = schoolId,
                    AssessmentId = assessmentId,
                    StudentProfileId = studentId,
                    Score = 8m,
                    Percentage = 80m,
                    EnteredByUserId = Guid.NewGuid(),
                    UpdatedAtUtc = DateTime.UtcNow
                }
            ],
            [
                new StudentAnswer
                {
                    Id = Guid.NewGuid(),
                    SchoolId = schoolId,
                    AssessmentResultId = resultId,
                    AssessmentQuestionId = questionId,
                    Score = 8m,
                    UpdatedAtUtc = DateTime.UtcNow
                }
            ]);

        var built =
            new AnalyticsProjectionBuilder()
                .Build(
                    source,
                    DateTime.UtcNow);

        Assert.Equal(
            2,
            built.StudentOutcomeMasteries.Count);

        Assert.Equal(
            8m,
            built.StudentOutcomeMasteries
                .Sum(x => x.EarnedScore));

        Assert.Equal(
            10m,
            built.StudentOutcomeMasteries
                .Sum(x => x.PossibleScore));

        Assert.All(
            built.StudentOutcomeMasteries,
            x => Assert.Equal(
                80m,
                x.MasteryPercentage));
    }

    [Theory]
    [InlineData(0, MasteryBand.CriticalGap)]
    [InlineData(39.99, MasteryBand.CriticalGap)]
    [InlineData(40, MasteryBand.Weak)]
    [InlineData(59.99, MasteryBand.Weak)]
    [InlineData(60, MasteryBand.Developing)]
    [InlineData(74.99, MasteryBand.Developing)]
    [InlineData(75, MasteryBand.Secure)]
    [InlineData(89.99, MasteryBand.Secure)]
    [InlineData(90, MasteryBand.Strong)]
    [InlineData(100, MasteryBand.Strong)]
    public void MasteryBandBoundaries_AreDeterministic(
        double value,
        MasteryBand expected)
    {
        Assert.Equal(
            expected,
            AnalyticsProjectionBuilder.BandFor(
                (decimal)value));
    }

    private static AnalyticsSourceSnapshot
        BuildTwoQuestionSource(
            AssessmentStatus status,
            decimal firstScore,
            decimal secondScore)
    {
        var school = Guid.NewGuid();
        var year = Guid.NewGuid();
        var grade = Guid.NewGuid();
        var cls = Guid.NewGuid();
        var subject = Guid.NewGuid();
        var student = Guid.NewGuid();
        var framework = Guid.NewGuid();
        var topic = Guid.NewGuid();
        var outcome1 = Guid.NewGuid();
        var outcome2 = Guid.NewGuid();
        var assessment = Guid.NewGuid();
        var q1 = Guid.NewGuid();
        var q2 = Guid.NewGuid();
        var result = Guid.NewGuid();

        return new AnalyticsSourceSnapshot(
            [
                new AcademicYear
                {
                    Id = year,
                    SchoolId = school,
                    Name = "2026/2027",
                    StartsOn =
                        new DateOnly(2026, 9, 1),
                    EndsOn =
                        new DateOnly(2027, 6, 30),
                    Status =
                        AcademicStructureStatus.Active
                }
            ],
            [
                new ClassGroup
                {
                    Id = cls,
                    SchoolId = school,
                    AcademicYearId = year,
                    GradeLevelId = grade,
                    Name = "6A",
                    Code = "6A",
                    NormalizedCode = "6A",
                    Status =
                        AcademicStructureStatus.Active
                }
            ],
            [
                new Subject
                {
                    Id = subject,
                    SchoolId = school,
                    Name = "Math",
                    Code = "MATH",
                    NormalizedCode = "MATH",
                    Status =
                        AcademicStructureStatus.Active
                }
            ],
            [
                new StudentProfile
                {
                    Id = student,
                    SchoolId = school,
                    StudentNumber = "S1",
                    NormalizedStudentNumber = "S1",
                    FirstName = "A",
                    LastName = "Student",
                    DisplayName = "A Student",
                    Status =
                        AcademicStructureStatus.Active
                }
            ],
            [],
            [],
            [
                new CurriculumTopic
                {
                    Id = topic,
                    SchoolId = school,
                    FrameworkVersionId = framework,
                    SubjectId = subject,
                    GradeLevelId = grade,
                    Name = "Numbers",
                    Order = 1
                }
            ],
            [
                new LearningOutcome
                {
                    Id = outcome1,
                    SchoolId = school,
                    FrameworkVersionId = framework,
                    SubjectId = subject,
                    GradeLevelId = grade,
                    TopicId = topic,
                    Code = "N1",
                    Description = "Outcome 1",
                    Weight = 1m,
                    Order = 1
                },
                new LearningOutcome
                {
                    Id = outcome2,
                    SchoolId = school,
                    FrameworkVersionId = framework,
                    SubjectId = subject,
                    GradeLevelId = grade,
                    TopicId = topic,
                    Code = "N2",
                    Description = "Outcome 2",
                    Weight = 3m,
                    Order = 2
                }
            ],
            [
                new Assessment
                {
                    Id = assessment,
                    SchoolId = school,
                    SubjectId = subject,
                    ClassGroupId = cls,
                    AcademicYearId = year,
                    TermId = Guid.NewGuid(),
                    Title = "Unit",
                    AssessmentDate =
                        new DateOnly(2026, 9, 15),
                    MaxScore = 10m,
                    Status = status,
                    CreatedByUserId = Guid.NewGuid()
                }
            ],
            [
                new AssessmentQuestion
                {
                    Id = q1,
                    SchoolId = school,
                    AssessmentId = assessment,
                    Prompt = "Q1",
                    MaxScore = 5m,
                    Order = 1
                },
                new AssessmentQuestion
                {
                    Id = q2,
                    SchoolId = school,
                    AssessmentId = assessment,
                    Prompt = "Q2",
                    MaxScore = 5m,
                    Order = 2
                }
            ],
            [
                new QuestionLearningOutcome
                {
                    Id = Guid.NewGuid(),
                    SchoolId = school,
                    AssessmentQuestionId = q1,
                    LearningOutcomeId = outcome1
                },
                new QuestionLearningOutcome
                {
                    Id = Guid.NewGuid(),
                    SchoolId = school,
                    AssessmentQuestionId = q2,
                    LearningOutcomeId = outcome2
                }
            ],
            [
                new AssessmentResult
                {
                    Id = result,
                    SchoolId = school,
                    AssessmentId = assessment,
                    StudentProfileId = student,
                    Score =
                        firstScore +
                        secondScore,
                    Percentage =
                        (firstScore +
                         secondScore) /
                        10m *
                        100m,
                    EnteredByUserId = Guid.NewGuid(),
                    UpdatedAtUtc = DateTime.UtcNow
                }
            ],
            [
                new StudentAnswer
                {
                    Id = Guid.NewGuid(),
                    SchoolId = school,
                    AssessmentResultId = result,
                    AssessmentQuestionId = q1,
                    Score = firstScore,
                    UpdatedAtUtc = DateTime.UtcNow
                },
                new StudentAnswer
                {
                    Id = Guid.NewGuid(),
                    SchoolId = school,
                    AssessmentResultId = result,
                    AssessmentQuestionId = q2,
                    Score = secondScore,
                    UpdatedAtUtc = DateTime.UtcNow
                }
            ]);
    }
}
