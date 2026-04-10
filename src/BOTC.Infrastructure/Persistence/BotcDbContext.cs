using BOTC.Infrastructure.Persistence.Rooms;
using BOTC.Infrastructure.Persistence.User;
using Microsoft.EntityFrameworkCore;

namespace BOTC.Infrastructure.Persistence;

public sealed class BotcDbContext : DbContext
{
    public BotcDbContext(DbContextOptions<BotcDbContext> options) : base(options)
    {
    }

    public DbSet<UserEntity> Users => Set<UserEntity>();
    public DbSet<RoomEntity> Rooms => Set<RoomEntity>();
    public DbSet<PlayerEntity> RoomPlayers => Set<PlayerEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new UserEntityConfiguration());
        modelBuilder.ApplyConfiguration(new RoomEntityConfiguration());
        modelBuilder.ApplyConfiguration(new PlayerEntityConfiguration());
    }
}
