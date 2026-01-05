namespace CCenter.Services.Contracts;

public interface IReportService
{
    Task<Stream> BuildWorkedHoursCsvAsync(CancellationToken ct);
}
