using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BOTC.Infrastructure.Persistence.Rooms;

internal sealed class PlayerEntityConfiguration : IEntityTypeConfiguration<PlayerEntity>
{
    public void Configure(EntityTypeBuilder<PlayerEntity> builder)
    {
        builder.ToTable("RoomPlayers");

        builder.HasKey(player => player.Id);

        builder.Property(player => player.Id)
            .ValueGeneratedNever();

        builder.Property(player => player.RoomId)
            .IsRequired();

        builder.Property(player => player.UserId)
            .IsRequired();

        builder.Property(player => player.Role)
            .IsRequired();

        builder.Property(player => player.JoinedAtUtc)
            .IsRequired()
            .HasColumnType("timestamp with time zone");

        builder.Property(player => player.IsReady)
            .IsRequired();

        builder.HasIndex(player => new { player.RoomId, player.UserId })
            .IsUnique();
        
        builder.HasOne(player => player.User)
            .WithMany()
            .HasForeignKey(player => player.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}