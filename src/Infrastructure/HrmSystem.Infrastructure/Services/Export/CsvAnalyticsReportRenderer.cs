using System.Globalization;
using System.Text;
using HrmSystem.Application.Common.Interfaces.Reporting;
using HrmSystem.Application.Features.Analytics.Shared;

namespace HrmSystem.Infrastructure.Services.Export;

/*
    //?     CSV rendering of the dashboard export — sectioned key/value + category tables.
    //!     Values are written through CsvEscape — labels can contain commas/quotes.
*/
internal sealed class CsvAnalyticsReportRenderer : IAnalyticsReportRenderer
{
    public string Format => "csv";

    public ExportedReport Render(AnalyticsReportModel model)
    {
        var csv = new StringBuilder();
        CultureInfo invariant = CultureInfo.InvariantCulture;

        csv.AppendLine("Section,Label,Value");
        csv.AppendLine(
            invariant,
            $"Report,Company,{CsvEscape(model.CompanyName)}"
        );
        csv.AppendLine(invariant, $"Report,From,{model.From:yyyy-MM-dd}");
        csv.AppendLine(invariant, $"Report,To,{model.To:yyyy-MM-dd}");

        csv.AppendLine(invariant, $"Summary,Total Employees,{model.Summary.TotalEmployees}");
        csv.AppendLine(invariant, $"Summary,Present Today,{model.Summary.PresentToday}");
        csv.AppendLine(invariant, $"Summary,Late Today,{model.Summary.LateToday}");
        csv.AppendLine(invariant, $"Summary,Absent Today,{model.Summary.AbsentToday}");
        csv.AppendLine(invariant, $"Summary,Pending Leaves,{model.Summary.PendingLeaves}");
        csv.AppendLine(invariant, $"Summary,Departments,{model.Summary.Departments}");

        foreach (CategoryCount item in model.DepartmentDistribution)
        {
            csv.AppendLine(invariant, $"Departments,{CsvEscape(item.Label)},{item.Value}");
        }

        foreach (CategoryCount item in model.LeaveStats.ByType)
        {
            csv.AppendLine(invariant, $"Leaves By Type,{CsvEscape(item.Label)},{item.Value}");
        }

        foreach (CategoryCount item in model.LeaveStats.ByStatus)
        {
            csv.AppendLine(invariant, $"Leaves By Status,{CsvEscape(item.Label)},{item.Value}");
        }

        return new ExportedReport(
            Encoding.UTF8.GetBytes(csv.ToString()),
            "text/csv",
            $"analytics-report_{model.From:yyyyMMdd}-{model.To:yyyyMMdd}.csv"
        );
    }

    private static string CsvEscape(string value) =>
        value.Contains(',') || value.Contains('"')
            ? $"\"{value.Replace("\"", "\"\"", StringComparison.Ordinal)}\""
            : value;
}
