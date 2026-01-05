using CCenter.Data;
using CCenter.Data.Entities;
using CCenter.Services.Contracts;
using CCenter.Services.Dtos;
using Microsoft.EntityFrameworkCore;

namespace CCenter.Services;

public sealed class LoginService : ILoginService
{
    private static readonly DateTime MinDate = new(2000, 1, 1);

    private readonly CCenterDbContext _db;

    public LoginService(CCenterDbContext db) => _db = db;

    public async Task<IReadOnlyList<LoginResponse>> GetAllAsync(CancellationToken ct)
        => await _db.Logins.AsNoTracking()
            .OrderByDescending(x => x.Fecha)
            .Select(x => new LoginResponse(x.Id, x.UserId, x.Extension, x.TipoMov, x.Fecha))
            .ToListAsync(ct);

    public async Task<LoginResponse?> GetByIdAsync(long id, CancellationToken ct)
        => await _db.Logins.AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new LoginResponse(x.Id, x.UserId, x.Extension, x.TipoMov, x.Fecha))
            .FirstOrDefaultAsync(ct);

    public async Task<(bool ok, string? error, LoginResponse? created)> CreateAsync(LoginCreateRequest req, CancellationToken ct)
    {
        var err = await ValidateCreateAsync(req, ct);
        if (err is not null) return (false, err, null);

        var entity = new LoginEvent
        {
            UserId = req.UserId,
            Extension = req.Extension,
            TipoMov = req.TipoMov,
            Fecha = req.Fecha
        };

        _db.Logins.Add(entity);
        await _db.SaveChangesAsync(ct);

        return (true, null, new LoginResponse(entity.Id, entity.UserId, entity.Extension, entity.TipoMov, entity.Fecha));
    }

    public async Task<(bool ok, string? error)> UpdateAsync(long id, LoginUpdateRequest req, CancellationToken ct)
    {
        var entity = await _db.Logins.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (entity is null) return (false, "Registro no encontrado.");

        var err = await ValidateUpdateAsync(entity, req, ct);
        if (err is not null) return (false, err);

        entity.Extension = req.Extension;
        entity.TipoMov = req.TipoMov;
        entity.Fecha = req.Fecha;

        await _db.SaveChangesAsync(ct);
        return (true, null);
    }

    public async Task<(bool ok, string? error)> DeleteAsync(long id, CancellationToken ct)
    {
        var entity = await _db.Logins.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (entity is null) return (false, "Registro no encontrado.");

        _db.Logins.Remove(entity);
        await _db.SaveChangesAsync(ct);
        return (true, null);
    }

    private async Task<string?> ValidateCreateAsync(LoginCreateRequest req, CancellationToken ct)
    {
        if (req.Fecha < MinDate) return "La fecha no es válida.";
        if (req.Fecha > DateTime.UtcNow.AddMinutes(10)) return "La fecha no puede estar en el futuro.";
        if (req.TipoMov is not (0 or 1)) return "TipoMov inválido. Usa 1=login o 0=logout.";

        var userExists = await _db.Users.AsNoTracking().AnyAsync(u => u.Id == req.UserId, ct);
        if (!userExists) return "User_id no existe en ccUsers.";

        // Último evento del usuario (O(1) con índice UserId+Fecha)
        var last = await _db.Logins.AsNoTracking()
            .Where(x => x.UserId == req.UserId)
            .OrderByDescending(x => x.Fecha)
            .ThenByDescending(x => x.Id)
            .Select(x => new { x.TipoMov, x.Fecha })
            .FirstOrDefaultAsync(ct);

        if (req.TipoMov == 1)
        {
            // login sin logout anterior
            if (last is not null && last.TipoMov == 1)
                return "No puedes registrar login: falta logout previo.";
            if (last is not null && req.Fecha <= last.Fecha)
                return "La fecha debe ser mayor al último evento del usuario.";
        }
        else
        {
            // logout sin login anterior
            if (last is null || last.TipoMov == 0)
                return "No puedes registrar logout: no hay login previo pendiente.";
            if (req.Fecha <= last.Fecha)
                return "La fecha de logout debe ser mayor al login.";
        }

        return null;
    }

    private async Task<string?> ValidateUpdateAsync(LoginEvent entity, LoginUpdateRequest req, CancellationToken ct)
    {
        // Validación base
        if (req.Fecha < MinDate) return "La fecha no es válida.";
        if (req.Fecha > DateTime.UtcNow.AddMinutes(10)) return "La fecha no puede estar en el futuro.";
        if (req.TipoMov is not (0 or 1)) return "TipoMov inválido. Usa 1=login o 0=logout.";

        // Validación timeline mínima (evitar “romper” alternancia inmediata)
        var prev = await _db.Logins.AsNoTracking()
            .Where(x => x.UserId == entity.UserId && x.Id != entity.Id && x.Fecha <= req.Fecha)
            .OrderByDescending(x => x.Fecha).ThenByDescending(x => x.Id)
            .Select(x => x.TipoMov)
            .FirstOrDefaultAsync(ct);

        // Si existe previo, no permitir dos seguidos iguales
        if (prev != default && prev == req.TipoMov) return "Actualización inválida: rompe secuencia login/logout.";

        return null;
    }
}
