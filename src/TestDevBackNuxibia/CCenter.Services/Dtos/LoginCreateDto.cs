using System.ComponentModel.DataAnnotations;

namespace CCenter.Services.Dtos;

public sealed class LoginCreateDto
{
    [Required]
    public int UserId { get; set; }

    [Required]
    public int Extension { get; set; }

    /// <summary>1 = login, 0 = logout</summary>
    [Range(0, 1)]
    public byte TipoMov { get; set; }

    [Required]
    public DateTime Fecha { get; set; }
}
