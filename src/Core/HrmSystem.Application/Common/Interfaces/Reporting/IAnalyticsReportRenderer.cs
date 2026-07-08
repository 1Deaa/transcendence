using HrmSystem.Application.Features.Analytics.Shared;

namespace HrmSystem.Application.Common.Interfaces.Reporting;

//? Everything the dashboard export contains — gathered once, rendered by format-specific renderers.
public sealed record AnalyticsReportModel(
    string CompanyName,
    DateOnly From,
    DateOnly To,
    KpiSummary Summary,
    IReadOnlyList<CategoryCount> DepartmentDistribution,
    LeaveStats LeaveStats
);

/*
    //?     One renderer per export format (csv / pdf), resolved by Format key.
    //!     PDF uses QuestPDF — Community license only below $1M USD annual revenue
    //!     (docs/architecture.md); the renderer isolation makes the library swappable.
*/
public interface IAnalyticsReportRenderer
{
    string Format { get; }

    ExportedReport Render(AnalyticsReportModel model);
}
