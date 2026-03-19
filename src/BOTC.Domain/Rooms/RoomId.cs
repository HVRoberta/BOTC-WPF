namespace BOTC.Domain.Rooms;

public readonly record struct RoomId
{
    public RoomId(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException("Room id cannot be empty.", nameof(value));
        }

        Value = value;
    }

    public Guid Value { get; }

    public static RoomId New() => new(Guid.NewGuid());

    public override string ToString() => Value.ToString();
}

