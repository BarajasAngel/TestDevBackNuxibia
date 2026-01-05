using CCenter.Services.Dtos;

namespace CCenter.Services.Contracts;

public interface ILoginService
{
    Task<IReadOnlyList<LoginResponse>> GetAllAsync(CancellationToken ct);
    Task<LoginResponse?> GetByIdAsync(long id, CancellationToken ct);

    Task<(bool ok, string? error, LoginResponse? created)> CreateAsync(LoginCreateRequest req, CancellationToken ct);
    Task<(bool ok, string? error)> UpdateAsync(long id, LoginUpdateRequest req, CancellationToken ct);
    Task<(bool ok, string? error)> DeleteAsync(long id, CancellationToken ct);
}
