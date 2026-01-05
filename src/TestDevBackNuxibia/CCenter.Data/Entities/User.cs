namespace CCenter.Data.Entities;

public sealed class User
{
    public int Id { get; set; }
    public string Login { get; set; } = string.Empty;
    public string Nombres { get; set; } = string.Empty;
    public string ApellidoPaterno { get; set; } = string.Empty;
    public string? ApellidoMaterno { get; set; }
    public int IDArea { get; set; }
}
