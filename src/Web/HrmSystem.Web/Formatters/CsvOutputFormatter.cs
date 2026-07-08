using System.Collections;
using System.Reflection;
using System.Text;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Net.Http.Headers;

namespace HrmSystem.Web.Formatters;

/*
    //?     text/csv output formatter for export endpoints — makes CSV a first-class citizen
    //?     of content negotiation next to JSON and XML:
    //>       Accept: text/csv                      → this formatter
    //>       GET /api/employees/export?format=csv  → via FormatterMappings + [FormatFilter]
    //
    //?     Reflection-based header/row writer over any IEnumerable of flat DTOs; the
    //?     PropertyInfo[] is resolved once per response, not per row.
    //!     Values ride through CsvEscape — user data can contain commas, quotes, newlines.
*/
public sealed class CsvOutputFormatter : TextOutputFormatter
{
    public CsvOutputFormatter()
    {
        SupportedMediaTypes.Add(MediaTypeHeaderValue.Parse("text/csv"));
        SupportedEncodings.Add(Encoding.UTF8);
    }

    protected override bool CanWriteType(Type? type) =>
        type is not null && typeof(IEnumerable).IsAssignableFrom(type) && type != typeof(string);

    public override async Task WriteResponseBodyAsync(
        OutputFormatterWriteContext context,
        Encoding selectedEncoding
    )
    {
        var buffer = new StringBuilder();

        if (context.Object is IEnumerable rows)
        {
            PropertyInfo[]? properties = null;

            foreach (object? row in rows)
            {
                if (row is null)
                {
                    continue;
                }

                if (properties is null)
                {
                    properties = row.GetType().GetProperties(
                        BindingFlags.Public | BindingFlags.Instance
                    );
                    buffer.AppendLine(string.Join(',', properties.Select(p => p.Name)));
                }

                buffer.AppendLine(
                    string.Join(
                        ',',
                        properties.Select(p => CsvEscape(p.GetValue(row)?.ToString() ?? string.Empty))
                    )
                );
            }
        }

        await context.HttpContext.Response.WriteAsync(buffer.ToString(), selectedEncoding);
    }

    private static string CsvEscape(string value) =>
        value.Contains(',') || value.Contains('"') || value.Contains('\n')
            ? $"\"{value.Replace("\"", "\"\"", StringComparison.Ordinal)}\""
            : value;
}
