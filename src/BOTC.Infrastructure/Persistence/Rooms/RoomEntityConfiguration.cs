using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BOTC.Infrastructure.Persistence.Rooms;

internal sealed class RoomEntityConfiguration : IEntityTypeConfiguration<RoomEntity>
{
    public void Configure(EntityTypeBuilder<RoomEntity> builder)
    {
        builder.ToTable("Rooms");

        builder.HasKey(room => room.Id);

        builder.Property(room => room.Code)
            .IsRequired()
            .HasMaxLength(6)
            .IsFixedLength();

        builder.Property(room => room.Status)
            .IsRequired();

        builder.Property(room => room.CreatedAtUtc)
            .IsRequired();

        builder.HasIndex(room => room.Code)
            .IsUnique();

        builder.HasMany(room => room.Players)
            .WithOne(player => player.Room)
            .HasForeignKey(player => player.RoomId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
