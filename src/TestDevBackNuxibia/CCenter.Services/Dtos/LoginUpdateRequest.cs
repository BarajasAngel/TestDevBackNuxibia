namespace CCenter.Services.Dtos;

public sealed record LoginUpdateRequest(
    short Extension,
    byte TipoMov,
    DateTime Fecha
);
