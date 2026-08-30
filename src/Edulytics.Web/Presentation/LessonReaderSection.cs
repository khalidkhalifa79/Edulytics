namespace Edulytics.Web.Presentation;

public sealed record LessonReaderSection(
    string Number,
    string Title,
    string Kind,
    IReadOnlyList<LessonPresentationItem> Items);
