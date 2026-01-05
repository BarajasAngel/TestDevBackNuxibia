namespace CCenter.Services.Dtos;

public sealed record LoginCreateRequest(
    int UserId,
    short Extension,
    byte TipoMov,     
    DateTime Fecha
);
