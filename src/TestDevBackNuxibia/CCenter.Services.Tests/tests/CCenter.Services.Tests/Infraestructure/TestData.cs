using CCenter.Data.Entities;

namespace CCenter.Services.Tests.Infrastructure;

public static class TestData
{
    public static User CreateUser(int id, int idArea = 1, string login = "user.test")
        => new()
        {
            Id = id,
            Login = login,
            Nombres = "Nombre",
            ApellidoPaterno = "ApellidoP",
            ApellidoMaterno = "ApellidoM",
            IDArea = idArea
        };

    public static Area CreateArea(int idArea = 1, string areaName = "Default")
        => new()
        {
            IDArea = idArea,
            AreaName = areaName
        };

    public static LoginEvent CreateLoginEvent(
        long id,
        int userId,
        int extension,
        byte tipoMov,
        DateTime fechaUtc)
        => new()
        {            
            Id = id,

            UserId = userId,
            Extension = extension,
            TipoMov = tipoMov,
            Fecha = DateTime.SpecifyKind(fechaUtc, DateTimeKind.Utc)
        };
}
