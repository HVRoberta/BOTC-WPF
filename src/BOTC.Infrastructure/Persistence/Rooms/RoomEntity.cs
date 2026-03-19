namespace BOTC.Infrastructure.Persistence.Rooms;

public sealed class RoomEntity
{
    public Guid Id { get; set; }

    public string Code { get; set; } = string.Empty;

    public string HostDisplayName { get; set; } = string.Empty;

    public int Status { get; set; }

    public DateTime CreatedAtUtc { get; set; }
}

