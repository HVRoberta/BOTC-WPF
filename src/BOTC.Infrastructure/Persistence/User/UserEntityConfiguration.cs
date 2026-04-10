using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BOTC.Infrastructure.Persistence.User;

internal sealed class UserEntityConfiguration : IEntityTypeConfiguration<UserEntity> 
{
    public void Configure(EntityTypeBuilder<UserEntity> builder)
    {
        builder.ToTable("Users");

        builder.HasIndex(user => user.Username)
            .IsUnique();
        
        builder.HasKey(user => user.Id);

        builder.Property(user => user.Id)
            .ValueGeneratedNever();

        builder.Property(user => user.Username)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(user => user.NickName)
            .IsRequired()
            .HasMaxLength(50);
        
        builder.Property(room => room.CreatedAtUtc)
            .IsRequired()
            .HasColumnType("timestamp with time zone");
        
        builder.Property(room => room.UpdatedAtUtc)
            .IsRequired()
            .HasColumnType("timestamp with time zone");
    }
}