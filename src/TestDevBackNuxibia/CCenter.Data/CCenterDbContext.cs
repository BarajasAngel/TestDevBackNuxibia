using CCenter.Data.Entities;
using CCenter.Data.QueryModels;
using Microsoft.EntityFrameworkCore;
using CCenter.Data.QueryModels;

namespace CCenter.Data;

public sealed class CCenterDbContext : DbContext
{
    public CCenterDbContext(DbContextOptions<CCenterDbContext> options) : base(options) { }

    public DbSet<LoginEvent> Logins => Set<LoginEvent>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Area> Areas => Set<Area>();
    public DbSet<WorkedHoursRow> WorkedHoursReport => Set<WorkedHoursRow>();


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<LoginEvent>(e =>
        {
            e.ToTable("ccloglogin", "dbo");

            
            e.HasKey(x => x.Id);

            e.Property(x => x.Id)
                .HasColumnName("Id")
                .ValueGeneratedOnAdd();

            e.Property(x => x.UserId)
                .HasColumnName("User_id")
                .HasColumnType("int");

            e.Property(x => x.Extension)
                .HasColumnName("Extension")
                .HasColumnType("int"); 

            e.Property(x => x.TipoMov)
                .HasColumnName("TipoMov")
                .HasColumnType("tinyint");

            e.Property(x => x.Fecha)
                .HasColumnName("fecha")
                .HasColumnType("datetime2"); 

            e.HasIndex(x => new { x.UserId, x.Fecha })
                .HasDatabaseName("IX_ccloglogin_User_Fecha");
        });

        modelBuilder.Entity<User>(e =>
        {
            e.ToTable("ccUsers", "dbo");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("User_id").HasColumnType("int");
        });

        modelBuilder.Entity<Area>(e =>
        {
            e.ToTable("ccRIACat_Areas", "dbo");
            e.HasKey(x => new { x.IDArea, x.AreaName });
            e.Property(x => x.IDArea).HasColumnName("IDArea").HasColumnType("int");
        });

        modelBuilder.Entity<WorkedHoursRow>(e =>
        {
            e.HasNoKey();
            e.ToView(null); 
        });
    }
}
