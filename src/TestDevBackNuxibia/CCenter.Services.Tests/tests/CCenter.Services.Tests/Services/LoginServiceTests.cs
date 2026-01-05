using CCenter.Data;
using CCenter.Services;
using CCenter.Services.Dtos;
using CCenter.Services.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace CCenter.Services.Tests.Services;

public sealed class LoginServiceTests
{
    private static LoginService CreateSut(CCenterDbContext db) => new(db);

    [Fact]
    public async Task GetAllAsync_ReturnsOrderedByLogId()
    {
        await using var db = TestDbContextFactory.Create();

        db.Users.Add(TestData.CreateUser(id: 70));

        // Inserto Id=2 y luego Id=1 para validar el OrderBy
        db.Logins.Add(TestData.CreateLoginEvent(
            id: 2, userId: 70, extension: 101, tipoMov: 1,
            fechaUtc: new DateTime(2026, 01, 05, 10, 00, 00, DateTimeKind.Utc)));

        db.Logins.Add(TestData.CreateLoginEvent(
            id: 1, userId: 70, extension: 100, tipoMov: 0,
            fechaUtc: new DateTime(2026, 01, 05, 09, 00, 00, DateTimeKind.Utc)));

        await db.SaveChangesAsync();

        var sut = CreateSut(db);

        var result = await sut.GetAllAsync(CancellationToken.None);

        result.Should().HaveCount(2);

        // Ajusta esto si tu DTO expone "Id" en vez de "LogId"
        result.Select(x => x.LogId).Should().ContainInOrder(1, 2);
    }

    [Fact]
    public async Task CreateAsync_ReturnsError_WhenUserDoesNotExist()
    {
        await using var db = TestDbContextFactory.Create();
        var sut = CreateSut(db);

        var dto = new LoginCreateDto
        {
            UserId = 999,
            Extension = 100,
            TipoMov = 1,
            Fecha = new DateTime(2026, 01, 05, 12, 00, 00, DateTimeKind.Utc)
        };

        var (ok, error, created) = await sut.CreateAsync(dto, CancellationToken.None);

        ok.Should().BeFalse();
        error.Should().NotBeNullOrWhiteSpace();
        created.Should().BeNull();
    }

    [Fact]
    public async Task CreateAsync_ReturnsError_WhenFechaIsLessThanLastEvent()
    {
        await using var db = TestDbContextFactory.Create();

        db.Users.Add(TestData.CreateUser(id: 70));
        db.Logins.Add(TestData.CreateLoginEvent(
            id: 10, userId: 70, extension: 100, tipoMov: 1,
            fechaUtc: new DateTime(2026, 01, 05, 12, 00, 00, DateTimeKind.Utc)));

        await db.SaveChangesAsync();

        var sut = CreateSut(db);

        var dto = new LoginCreateDto
        {
            UserId = 70,
            Extension = 100,
            TipoMov = 0,
            Fecha = new DateTime(2026, 01, 05, 11, 59, 00, DateTimeKind.Utc)
        };

        var (ok, error, created) = await sut.CreateAsync(dto, CancellationToken.None);

        ok.Should().BeFalse();
        error.Should().NotBeNullOrWhiteSpace();
        created.Should().BeNull();
    }

    [Fact]
    public async Task CreateAsync_InsertsAndReturnsCreated()
    {
        await using var db = TestDbContextFactory.Create();

        db.Users.Add(TestData.CreateUser(id: 70));
        await db.SaveChangesAsync();

        var sut = CreateSut(db);

        var dto = new LoginCreateDto
        {
            UserId = 70,
            Extension = 100,
            TipoMov = 1,
            Fecha = new DateTime(2026, 01, 05, 12, 00, 00, DateTimeKind.Utc)
        };

        var (ok, error, created) = await sut.CreateAsync(dto, CancellationToken.None);

        ok.Should().BeTrue();
        error.Should().BeNullOrWhiteSpace();
        created.Should().NotBeNull();
        created!.LogId.Should().BeGreaterThan(0);

        (await db.Logins.AnyAsync(x =>
            x.UserId == 70 &&
            x.Extension == 100 &&
            x.TipoMov == 1))
        .Should().BeTrue();
    }

    [Fact]
    public async Task UpdateAsync_ReturnsNotFound_WhenLogDoesNotExist()
    {
        await using var db = TestDbContextFactory.Create();

        db.Users.Add(TestData.CreateUser(id: 70));
        await db.SaveChangesAsync();

        var sut = CreateSut(db);

        var dto = new LoginUpdateDto
        {
            Extension = 200,
            TipoMov = 0,
            Fecha = new DateTime(2026, 01, 05, 13, 00, 00, DateTimeKind.Utc)
        };

        var (ok, error, updated) = await sut.UpdateAsync(id: 9999, dto, CancellationToken.None);

        ok.Should().BeFalse();
        error.Should().NotBeNullOrWhiteSpace();
        updated.Should().BeNull();
    }
    [Fact]
    public async Task UpdateAsync_UpdatesAndReturnsUpdated()
    {
        await using var db = TestDbContextFactory.Create();

        db.Users.Add(TestData.CreateUser(id: 70));
        
        var login = TestData.CreateLoginEvent(
            id: 0,
            userId: 70,
            extension: 100,
            tipoMov: 1,
            fechaUtc: new DateTime(2026, 01, 05, 12, 00, 00, DateTimeKind.Utc));
        
        var logout = TestData.CreateLoginEvent(
            id: 0,
            userId: 70,
            extension: 100,
            tipoMov: 0,
            fechaUtc: login.Fecha.AddMinutes(10));

        db.Logins.AddRange(login, logout);
        await db.SaveChangesAsync();

        var idToUpdate = logout.Id; 

        var sut = CreateSut(db);

        var dto = new LoginUpdateDto
        {
            Extension = 101,
            TipoMov = 0, 
            Fecha = logout.Fecha.AddMinutes(5) 
        };

        var (ok, error, updated) = await sut.UpdateAsync(idToUpdate, dto, CancellationToken.None);

        ok.Should().BeTrue($"UpdateAsync falló: {error}");
        error.Should().BeNullOrWhiteSpace();
        updated.Should().NotBeNull();

        updated!.Extension.Should().Be(101);
        updated.TipoMov.Should().Be(0);

        var entity = await db.Logins.SingleAsync(x => x.Id == idToUpdate);
        entity.Extension.Should().Be(101);
        entity.TipoMov.Should().Be(0);
    }


}
