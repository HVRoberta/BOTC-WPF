namespace BOTC.Domain.Rooms;

public readonly record struct RoomPlayerId
{
    public RoomPlayerId(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException("Room player id cannot be empty.", nameof(value));
        }

        Value = value;
    }

    public Guid Value { get; }

    public static RoomPlayerId New() => new(Guid.NewGuid());

    public override string ToString() => Value.ToString();
}

