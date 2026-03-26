using BOTC.Infrastructure.Persistence.Rooms;
using Microsoft.EntityFrameworkCore;

namespace BOTC.Infrastructure.Persistence;

public sealed class BotcDbContext : DbContext
{
    public BotcDbContext(DbContextOptions<BotcDbContext> options) : base(options)
    {
    }

    public DbSet<RoomEntity> Rooms => Set<RoomEntity>();

    public DbSet<RoomPlayerEntity> RoomPlayers => Set<RoomPlayerEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new RoomEntityConfiguration());
        modelBuilder.ApplyConfiguration(new RoomPlayerEntityConfiguration());
    }
}
