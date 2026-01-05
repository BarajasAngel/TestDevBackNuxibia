using CCenter.Data;
using CCenter.Data.Entities;
using CCenter.Services.Contracts;
using CCenter.Services.Dtos;
using Microsoft.EntityFrameworkCore;

namespace CCenter.Services;

public sealed class LoginService : ILoginService
{
    private readonly CCenterDbContext _db;

    public LoginService(CCenterDbContext db) => _db = db;

    public async Task<IReadOnlyList<LoginResponse>> GetAllAsync(CancellationToken ct)
        => await _db.Logins.AsNoTracking()
            .OrderBy(x => x.Id)
            .Select(x => new LoginResponse(x.Id, x.UserId, x.Extension, x.TipoMov, x.Fecha))
            .ToListAsync(ct);

    public async Task<LoginResponse?> GetByIdAsync(long id, CancellationToken ct)
        => await _db.Logins.AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new LoginResponse(x.Id, x.UserId, x.Extension, x.TipoMov, x.Fecha))
            .FirstOrDefaultAsync(ct);

    public async Task<(bool ok, string? error, LoginResponse? created)> CreateAsync(LoginCreateDto dto, CancellationToken ct)
    {
        if (dto.TipoMov is not (0 or 1))
            return (false, "TipoMov debe ser 0 (logout) o 1 (login).", null);

        if (!IsFechaValida(dto.Fecha))
            return (false, "Fecha inválida.", null);

        var userExists = await _db.Users.AsNoTracking().AnyAsync(u => u.Id == dto.UserId, ct);
        if (!userExists)
            return (false, $"User_id {dto.UserId} no existe en ccUsers.", null);

        var last = await _db.Logins.AsNoTracking()
            .Where(x => x.UserId == dto.UserId)
            .OrderByDescending(x => x.Fecha).ThenByDescending(x => x.Id)
            .Select(x => new { x.TipoMov, x.Fecha })
            .FirstOrDefaultAsync(ct);

        // Regla secuencia (simple y efectiva)
        if (last is not null)
        {
            if (dto.Fecha < last.Fecha)
                return (false, "No se permite insertar un evento con fecha menor al último evento del usuario.", null);

            if (dto.TipoMov == 1 && last.TipoMov == 1)
                return (false, "No puedes registrar login sin un logout anterior.", null);

            if (dto.TipoMov == 0 && last.TipoMov == 0)
                return (false, "No puedes registrar logout sin un login anterior.", null);
        }
        else
        {
            if (dto.TipoMov == 0)
                return (false, "No puedes registrar logout si el usuario no tiene un login previo.", null);
        }

        var entity = new LoginEvent
        {
            UserId = dto.UserId,
            Extension = dto.Extension,
            TipoMov = dto.TipoMov,
            Fecha = dto.Fecha
        };

        _db.Logins.Add(entity);
        await _db.SaveChangesAsync(ct);

        return (true, null, new LoginResponse(entity.Id, entity.UserId, entity.Extension, entity.TipoMov, entity.Fecha));
    }

    public async Task<(bool ok, string? error, LoginResponse? updated)> UpdateAsync(long id, LoginUpdateDto dto, CancellationToken ct)
    {
        if (dto.TipoMov is not (0 or 1))
            return (false, "TipoMov debe ser 0 (logout) o 1 (login).", null);

        if (!IsFechaValida(dto.Fecha))
            return (false, "Fecha inválida.", null);

        var entity = await _db.Logins.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (entity is null)
            return (false, "Registro no encontrado.", null);

        // Validación de alternancia local (prev/next)
        var userId = entity.UserId;
        var newTipo = dto.TipoMov;
        var newFecha = dto.Fecha;

        var prev = await _db.Logins.AsNoTracking()
            .Where(x => x.UserId == userId && x.Id != id)
            .Where(x => x.Fecha < newFecha || (x.Fecha == newFecha && x.Id < id))
            .OrderByDescending(x => x.Fecha).ThenByDescending(x => x.Id)
            .Select(x => new { x.TipoMov })
            .FirstOrDefaultAsync(ct);

        var next = await _db.Logins.AsNoTracking()
            .Where(x => x.UserId == userId && x.Id != id)
            .Where(x => x.Fecha > newFecha || (x.Fecha == newFecha && x.Id > id))
            .OrderBy(x => x.Fecha).ThenBy(x => x.Id)
            .Select(x => new { x.TipoMov })
            .FirstOrDefaultAsync(ct);

        if (newTipo == 0 && prev is null)
            return (false, "No puedes dejar un logout como primer evento del usuario.", null);

        if (prev is not null && prev.TipoMov == newTipo)
            return (false, "Secuencia inválida: evento previo tiene el mismo TipoMov.", null);

        if (next is not null && next.TipoMov == newTipo)
            return (false, "Secuencia inválida: evento siguiente tiene el mismo TipoMov.", null);

        entity.Extension = dto.Extension;
        entity.TipoMov = dto.TipoMov;
        entity.Fecha = dto.Fecha;

        await _db.SaveChangesAsync(ct);

        return (true, null, new LoginResponse(entity.Id, entity.UserId, entity.Extension, entity.TipoMov, entity.Fecha));
    }

    public async Task<(bool ok, string? error)> DeleteAsync(long id, CancellationToken ct)
    {
        var entity = await _db.Logins.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (entity is null)
            return (false, "Registro no encontrado.");

        _db.Logins.Remove(entity);
        await _db.SaveChangesAsync(ct);

        return (true, null);
    }

    private static bool IsFechaValida(DateTime fecha)
    {
        // regla simple: no default / no fechas absurdas / no futuro lejano
        if (fecha == default) return false;
        if (fecha < new DateTime(2000, 1, 1)) return false;

        var now = DateTime.UtcNow;
        return fecha <= now.AddMinutes(10);
    }
}
