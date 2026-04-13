namespace BOTC.Domain.Rooms;

public sealed class PlayerId : IEquatable<PlayerId>
{
    public PlayerId(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException("Room player id cannot be empty.", nameof(value));
        }

        Value = value;
    }

    public Guid Value { get; }

    public static PlayerId New() => new(Guid.NewGuid());

    public bool Equals(PlayerId? other) => other is not null && Value == other.Value;

    public override bool Equals(object? obj) => obj is PlayerId other && Equals(other);

    public override int GetHashCode() => Value.GetHashCode();

    public static bool operator ==(PlayerId? left, PlayerId? right) =>
        left is null ? right is null : left.Equals(right);

    public static bool operator !=(PlayerId? left, PlayerId? right) => !(left == right);

    public override string ToString() => Value.ToString();
}
