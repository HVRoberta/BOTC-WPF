using BOTC.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BOTC.Infrastructure.Persistence.Configurations;

internal sealed class RoomEntityConfiguration : IEntityTypeConfiguration<RoomEntity>
{
    public void Configure(EntityTypeBuilder<RoomEntity> builder)
    {
        builder.ToTable("Rooms");

        builder.HasKey(room => room.Id);

        builder.Property(room => room.Id)
            .ValueGeneratedNever();

        builder.Property(room => room.Code)
            .IsRequired()
            .HasMaxLength(6);

        builder.Property(room => room.Name)
            .IsRequired()
            .HasMaxLength(30);

        builder.Property(room => room.Status)
            .IsRequired();

        builder.Property(room => room.CreatedAtUtc)
            .IsRequired()
            .HasColumnType("timestamp with time zone");

        builder.HasIndex(room => room.Code)
            .IsUnique();

        builder.HasMany(room => room.Players)
            .WithOne(player => player.Room)
            .HasForeignKey(player => player.RoomId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);
    }
}