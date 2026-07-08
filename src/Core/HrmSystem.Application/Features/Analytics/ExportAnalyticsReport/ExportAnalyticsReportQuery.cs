using HrmSystem.Application.Common.Interfaces.Messaging;
using HrmSystem.Application.Features.Analytics.Shared;

namespace HrmSystem.Application.Features.Analytics.ExportAnalyticsReport;

//! Not cached — exports are rare, big, and must reflect the exact current data.
public sealed record ExportAnalyticsReportQuery(
    string TenantId,
    DateOnly From,
    DateOnly To,
    string Format
) : IQuery<ExportedReport>;
