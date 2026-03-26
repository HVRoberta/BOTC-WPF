namespace BOTC.Domain.Rooms;

public sealed class RoomPlayer
{
    internal const int MaxDisplayNameLength = 50;

    private RoomPlayer(
        RoomPlayerId id,
        string displayName,
        string normalizedDisplayName,
        RoomPlayerRole role,
        DateTime joinedAtUtc)
    {
        Id = id;
        DisplayName = displayName;
        NormalizedDisplayName = normalizedDisplayName;
        Role = role;
        JoinedAtUtc = joinedAtUtc;
    }

    public RoomPlayerId Id { get; }

    public string DisplayName { get; }

    public string NormalizedDisplayName { get; }

    public RoomPlayerRole Role { get; }

    public DateTime JoinedAtUtc { get; }

    public static RoomPlayer Create(
        RoomPlayerId id,
        string displayName,
        RoomPlayerRole role,
        DateTime joinedAtUtc)
    {
        ValidateRole(role);

        var normalizedDisplayName = NormalizeDisplayName(displayName);
        var trimmedDisplayName = displayName.Trim();
        var utcJoinedAt = EnsureUtc(joinedAtUtc, nameof(joinedAtUtc));

        return new RoomPlayer(id, trimmedDisplayName, normalizedDisplayName, role, utcJoinedAt);
    }

    public static RoomPlayer Rehydrate(
        RoomPlayerId id,
        string displayName,
        string normalizedDisplayName,
        RoomPlayerRole role,
        DateTime joinedAtUtc)
    {
        ValidateRole(role);

        var trimmedDisplayName = ValidateDisplayName(displayName, nameof(displayName));
        var expectedNormalizedDisplayName = NormalizeDisplayName(trimmedDisplayName);
        if (!string.Equals(expectedNormalizedDisplayName, normalizedDisplayName, StringComparison.Ordinal))
        {
            throw new ArgumentException("Persisted normalized display name does not match display name.", nameof(normalizedDisplayName));
        }

        var utcJoinedAt = EnsureUtc(joinedAtUtc, nameof(joinedAtUtc));

        return new RoomPlayer(id, trimmedDisplayName, normalizedDisplayName, role, utcJoinedAt);
    }

    public static string NormalizeDisplayName(string displayName)
    {
        var trimmedDisplayName = ValidateDisplayName(displayName, nameof(displayName));
        return trimmedDisplayName.ToUpperInvariant();
    }

    private static string ValidateDisplayName(string displayName, string paramName)
    {
        if (string.IsNullOrWhiteSpace(displayName))
        {
            throw new ArgumentException("Display name is required.", paramName);
        }

        var trimmedDisplayName = displayName.Trim();
        if (trimmedDisplayName.Length > MaxDisplayNameLength)
        {
            throw new ArgumentException(
                $"Display name must not exceed {MaxDisplayNameLength} characters.",
                paramName);
        }

        return trimmedDisplayName;
    }

    private static void ValidateRole(RoomPlayerRole role)
    {
        if (!Enum.IsDefined(role))
        {
            throw new ArgumentOutOfRangeException(nameof(role), role, "Invalid room player role.");
        }
    }

    private static DateTime EnsureUtc(DateTime value, string paramName)
    {
        if (value.Kind == DateTimeKind.Utc)
        {
            return value;
        }

        if (value.Kind == DateTimeKind.Local)
        {
            return value.ToUniversalTime();
        }

        throw new ArgumentException("Date must specify UTC or Local kind.", paramName);
    }
}

