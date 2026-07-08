namespace HrmSystem.Application.Features.Analytics.Shared;

/*
    //?     Read models for the analytics dashboard — shaped to feed ApexCharts directly:
    //?     ChartSeries → series[{name, data}], CategoryCount → labels[] + series[].
*/
public sealed record KpiSummary(
    int TotalEmployees,
    int PresentToday,
    int LateToday,
    int AbsentToday,
    int PendingLeaves,
    int Departments
);

public sealed record TimeSeriesPoint(DateOnly Date, decimal Value);

public sealed record ChartSeries(string Name, IReadOnlyList<TimeSeriesPoint> Points);

public sealed record CategoryCount(string Label, int Value);

public sealed record LeaveStats(
    IReadOnlyList<CategoryCount> ByType,
    IReadOnlyList<CategoryCount> ByStatus
);

//? Binary export produced by the report renderers (CSV / PDF).
public sealed record ExportedReport(byte[] Content, string ContentType, string FileName);
