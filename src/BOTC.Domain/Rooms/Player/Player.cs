using BOTC.Domain.Users;

namespace BOTC.Domain.Rooms.Players;

public sealed class Player
{
    private Player(
        PlayerId id,
        UserId userId,
        PlayerRole role,
        DateTime joinedAtUtc,
        bool isReady)
    {
        Id = id;
        UserId = userId;
        Role = role;
        JoinedAtUtc = EnsureUtc(joinedAtUtc);
        IsReady = isReady;
    }

    public PlayerId Id { get; }
    public UserId UserId { get; }
    public PlayerRole Role { get; }
    public DateTime JoinedAtUtc { get; }
    public bool IsReady { get; }

    public static Player Create(
        PlayerId id,
        UserId userId,
        PlayerRole role,
        DateTime joinedAtUtc,
        bool isReady = false)
    {
        ValidateRole(role);

        return new Player(
            id,
            userId,
            role,
            joinedAtUtc,
            isReady);
    }

    public static Player Rehydrate(
        PlayerId id,
        UserId userId,
        PlayerRole role,
        DateTime joinedAtUtc,
        bool isReady)
    {
        ValidateRole(role);

        return new Player(
            id,
            userId,
            role,
            joinedAtUtc,
            isReady);
    }

    public Player ChangeRole(PlayerRole role)
    {
        ValidateRole(role);

        return new Player(
            Id,
            UserId,
            role,
            JoinedAtUtc,
            IsReady);
    }

    public Player SetReady(bool isReady) =>
        new Player(
            Id,
            UserId,
            Role,
            JoinedAtUtc,
            isReady);

    private static void ValidateRole(PlayerRole role)
    {
        if (!Enum.IsDefined(role))
        {
            throw new ArgumentOutOfRangeException(nameof(role), role, "Invalid role.");
        }
    }

    private static DateTime EnsureUtc(DateTime value)
    {
        return value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => throw new ArgumentException("Date must be UTC or Local.", nameof(value))
        };
    }
}