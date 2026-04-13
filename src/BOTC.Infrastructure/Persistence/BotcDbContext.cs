using BOTC.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace BOTC.Infrastructure.Persistence;

public sealed class BotcDbContext : DbContext
{
    public BotcDbContext(DbContextOptions<BotcDbContext> options) : base(options)
    {
    }

    public DbSet<UserEntity> Users => Set<UserEntity>();
    public DbSet<RoomEntity> Rooms => Set<RoomEntity>();
    public DbSet<PlayerEntity> Players => Set<PlayerEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(BotcDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
