namespace BOTC.Infrastructure.Persistence.Entities;

public sealed class RoomEntity
{
    public Guid Id { get; set; }

    public string Code { get; set; } = string.Empty;
    
    public string Name { get; set; } = string.Empty;

    public int Status { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public List<PlayerEntity> Players { get; set; } = [];
}
