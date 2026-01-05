using System.Globalization;
using System.Text;
using CCenter.Data;
using CCenter.Data.QueryModels;
using CCenter.Services.Contracts;
using Microsoft.EntityFrameworkCore;

namespace CCenter.Services;

public sealed class ReportService : IReportService
{
    private readonly CCenterDbContext _db;

    public ReportService(CCenterDbContext db) => _db = db;

    public async Task<Stream> BuildWorkedHoursCsvAsync(CancellationToken ct)
    {
        var rows = await _db.WorkedHoursReport
            .FromSqlRaw("EXEC dbo.usp_Report_WorkedHours")
            .AsNoTracking()
            .ToListAsync(ct);

        var ms = new MemoryStream();
        await using var writer = new StreamWriter(ms, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false), leaveOpen: true);

        await writer.WriteLineAsync("Login,NombreCompleto,Area,TotalHoras");

        foreach (var r in rows)
        {            
            await writer.WriteLineAsync(string.Join(",",
                Csv(r.Login),
                Csv(r.FullName),
                Csv(r.Area),
                r.TotalHours.ToString(CultureInfo.InvariantCulture)
            ));
        }

        await writer.FlushAsync();
        ms.Position = 0;
        return ms;
    }

    private static string Csv(string? value)
    {
        if (string.IsNullOrEmpty(value)) return "\"\"";

        var needsQuotes = value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r');
        var escaped = value.Replace("\"", "\"\"");

        return needsQuotes ? $"\"{escaped}\"" : escaped;
    }
}

