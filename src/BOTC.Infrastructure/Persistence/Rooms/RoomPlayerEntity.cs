namespace BOTC.Infrastructure.Persistence.Rooms;

public sealed class RoomPlayerEntity
{
    public Guid Id { get; set; }

    public Guid RoomId { get; set; }

    public string DisplayName { get; set; } = string.Empty;

    public string NormalizedDisplayName { get; set; } = string.Empty;

    public int Role { get; set; }

    public DateTime JoinedAtUtc { get; set; }

    public bool IsReady { get; set; }

    public RoomEntity Room { get; set; } = null!;
}

