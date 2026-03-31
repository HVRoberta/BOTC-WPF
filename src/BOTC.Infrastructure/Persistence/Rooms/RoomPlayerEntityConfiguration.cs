using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BOTC.Infrastructure.Persistence.Rooms;

internal sealed class RoomPlayerEntityConfiguration : IEntityTypeConfiguration<RoomPlayerEntity>
{
    public void Configure(EntityTypeBuilder<RoomPlayerEntity> builder)
    {
        builder.ToTable("RoomPlayers");

        builder.HasKey(player => player.Id);

        builder.Property(player => player.DisplayName)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(player => player.NormalizedDisplayName)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(player => player.Role)
            .IsRequired();

        builder.Property(player => player.JoinedAtUtc)
            .IsRequired();

        builder.Property(player => player.IsReady)
            .IsRequired();

        builder.HasIndex(player => new { player.RoomId, player.NormalizedDisplayName })
            .IsUnique();

        builder.HasOne(player => player.Room)
            .WithMany(room => room.Players)
            .HasForeignKey(player => player.RoomId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

