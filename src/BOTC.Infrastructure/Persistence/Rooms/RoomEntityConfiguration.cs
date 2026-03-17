using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BOTC.Infrastructure.Persistence.Rooms;

internal sealed class RoomEntityConfiguration : IEntityTypeConfiguration<RoomEntity>
{
    public void Configure(EntityTypeBuilder<RoomEntity> builder)
    {
        builder.ToTable("Rooms");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Code)
            .IsRequired()
            .HasMaxLength(6)
            .IsFixedLength();

        builder.Property(r => r.HostDisplayName)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(r => r.Status)
            .IsRequired();

        builder.Property(r => r.CreatedAtUtc)
            .IsRequired();

        builder.HasIndex(r => r.Code)
            .IsUnique();
    }
}

