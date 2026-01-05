using CCenter.Data;
using Microsoft.EntityFrameworkCore;

namespace CCenter.Services.Tests.Infrastructure;

public static class TestDbContextFactory
{
    public static CCenterDbContext Create()
    {
        var options = new DbContextOptionsBuilder<CCenterDbContext>()
            .UseInMemoryDatabase($"ccenter-tests-{Guid.NewGuid()}")
            .EnableSensitiveDataLogging()
            .Options;

        var db = new CCenterDbContext(options);
        db.Database.EnsureCreated();
        return db;
    }
}
