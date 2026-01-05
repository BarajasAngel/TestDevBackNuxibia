using System.ComponentModel.DataAnnotations;

namespace CCenter.Services.Dtos;

public sealed class LoginUpdateDto
{
    [Required]
    public int Extension { get; set; }

    [Range(0, 1)]
    public byte TipoMov { get; set; }

    [Required]
    public DateTime Fecha { get; set; }
}
