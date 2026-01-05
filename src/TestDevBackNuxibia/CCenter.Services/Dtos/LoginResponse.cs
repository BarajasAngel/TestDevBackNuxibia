namespace CCenter.Services.Dtos;

public sealed record LoginResponse(
    long LogId,
    int UserId,
    int Extension,
    byte TipoMov,
    DateTime Fecha
);
