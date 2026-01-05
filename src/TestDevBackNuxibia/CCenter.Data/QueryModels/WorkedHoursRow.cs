namespace CCenter.Data.QueryModels;

public sealed class WorkedHoursRow
{
    public string Login { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Area { get; set; } = string.Empty;
    public decimal TotalHours { get; set; }
}
