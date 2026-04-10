namespace BOTC.Domain.Users;

public sealed class UserId : IEquatable<UserId>
{
    public UserId(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException("User id cannot be empty.", nameof(value));
        }

        Value = value;
    }

    public Guid Value { get; }

    public static UserId New() => new(Guid.NewGuid());

    public bool Equals(UserId? other) => other is not null && Value == other.Value;

    public override bool Equals(object? obj) => obj is UserId other && Equals(other);

    public override int GetHashCode() => Value.GetHashCode();

    public static bool operator ==(UserId? left, UserId? right) =>
        left is null ? right is null : left.Equals(right);

    public static bool operator !=(UserId? left, UserId? right) => !(left == right);

    public override string ToString() => Value.ToString();
}