using Edulytics.Core.Enums;
using Edulytics.Services.Analytics;

namespace Edulytics.Web.ViewModels.Analytics;

public sealed record AnalyticsIndexViewModel(
    AnalyticsDashboard Dashboard)
{
    public static string BandKey(
        MasteryBand band) =>
        band switch
        {
            MasteryBand.CriticalGap =>
                "MasteryCritical",
            MasteryBand.Weak =>
                "MasteryWeak",
            MasteryBand.Developing =>
                "MasteryDeveloping",
            MasteryBand.Secure =>
                "MasterySecure",
            _ =>
                "MasteryStrong"
        };

    public static string BandClass(
        MasteryBand band) =>
        band switch
        {
            MasteryBand.CriticalGap =>
                "analytics-band-critical",
            MasteryBand.Weak =>
                "analytics-band-weak",
            MasteryBand.Developing =>
                "analytics-band-developing",
            MasteryBand.Secure =>
                "analytics-band-secure",
            _ =>
                "analytics-band-strong"
        };
}
