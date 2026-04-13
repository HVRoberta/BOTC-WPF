namespace BOTC.Infrastructure.Persistence.Entities;

public sealed class PlayerEntity
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }
    public UserEntity? User { get; set; }
    
    public Guid RoomId { get; set; }
    public RoomEntity Room { get; set; } = null!;

    public int Role { get; set; }

    public DateTime JoinedAtUtc { get; set; }

    public bool IsReady { get; set; }
}

