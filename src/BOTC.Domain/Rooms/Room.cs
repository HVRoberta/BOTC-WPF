namespace BOTC.Domain.Rooms;

public sealed class Room
{
    private const int MaxPlayers = 20;

    private readonly List<RoomPlayer> players;

    private Room(
        RoomId id,
        RoomCode code,
        IEnumerable<RoomPlayer> players,
        RoomStatus status,
        DateTime createdAtUtc)
    {
        Id = id;
        Code = code;
        this.players = players.ToList();
        Status = ValidateStatus(status);
        CreatedAtUtc = EnsureUtc(createdAtUtc, nameof(createdAtUtc));

        EnsurePlayerInvariants(this.players);
    }

    public RoomId Id { get; }

    public RoomCode Code { get; }

    public IReadOnlyCollection<RoomPlayer> Players => players;

    public string HostDisplayName => GetHost().DisplayName;

    public RoomStatus Status { get; private set; }

    public DateTime CreatedAtUtc { get; }

    public static Room Create(RoomId id, RoomCode code, string hostDisplayName, DateTime createdAtUtc)
    {
        var hostPlayer = RoomPlayer.Create(
            RoomPlayerId.New(),
            hostDisplayName,
            RoomPlayerRole.Host,
            createdAtUtc);

        return new Room(
            id,
            code,
            [hostPlayer],
            RoomStatus.WaitingForPlayers,
            createdAtUtc);
    }

    public static Room Rehydrate(
        RoomId id,
        RoomCode code,
        IEnumerable<RoomPlayer> players,
        RoomStatus status,
        DateTime createdAtUtc)
    {
        ArgumentNullException.ThrowIfNull(players);

        return new Room(id, code, players, status, createdAtUtc);
    }

    public RoomPlayer JoinPlayer(string displayName, DateTime joinedAtUtc)
    {
        EnsureJoinAllowed();

        var candidateNormalizedName = RoomPlayer.NormalizeDisplayName(displayName);
        if (players.Any(p => string.Equals(p.NormalizedDisplayName, candidateNormalizedName, StringComparison.Ordinal)))
        {
            throw new InvalidOperationException("Display name is already in use for this room.");
        }

        if (players.Count >= MaxPlayers)
        {
            throw new InvalidOperationException($"Room cannot exceed {MaxPlayers} participants.");
        }

        var player = RoomPlayer.Create(RoomPlayerId.New(), displayName, RoomPlayerRole.Player, joinedAtUtc);
        players.Add(player);

        return player;
    }

    public void RenameHost(string hostDisplayName)
    {
        var host = GetHost();
        var candidateNormalizedName = RoomPlayer.NormalizeDisplayName(hostDisplayName);

        if (players.Any(p => p.Id != host.Id && string.Equals(p.NormalizedDisplayName, candidateNormalizedName, StringComparison.Ordinal)))
        {
            throw new InvalidOperationException("Display name is already in use for this room.");
        }

        var renamedHost = RoomPlayer.Create(host.Id, hostDisplayName, RoomPlayerRole.Host, host.JoinedAtUtc);
        var hostIndex = players.FindIndex(p => p.Id == host.Id);
        players[hostIndex] = renamedHost;
    }

    private void EnsureJoinAllowed()
    {
        if (Status != RoomStatus.WaitingForPlayers)
        {
            throw new InvalidOperationException("Room does not accept new players in its current state.");
        }
    }

    private RoomPlayer GetHost()
    {
        var host = players.SingleOrDefault(player => player.Role == RoomPlayerRole.Host);
        if (host is null)
        {
            throw new InvalidOperationException("Room must have exactly one host player.");
        }

        return host;
    }

    private static void EnsurePlayerInvariants(IReadOnlyCollection<RoomPlayer> players)
    {
        if (players.Count == 0)
        {
            throw new ArgumentException("Room must contain at least one player.", nameof(players));
        }

        var hostCount = players.Count(player => player.Role == RoomPlayerRole.Host);
        if (hostCount != 1)
        {
            throw new ArgumentException("Room must have exactly one host player.", nameof(players));
        }

        var duplicatedDisplayName = players
            .GroupBy(player => player.NormalizedDisplayName, StringComparer.Ordinal)
            .Any(group => group.Count() > 1);

        if (duplicatedDisplayName)
        {
            throw new ArgumentException("Player display names must be unique within a room.", nameof(players));
        }

        if (players.Count > MaxPlayers)
        {
            throw new ArgumentException($"Room cannot exceed {MaxPlayers} participants.", nameof(players));
        }
    }

    private static RoomStatus ValidateStatus(RoomStatus status)
    {
        if (!Enum.IsDefined(status))
        {
            throw new ArgumentOutOfRangeException(nameof(status), status, "Invalid room status.");
        }

        return status;
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

        throw new ArgumentException("Created date must specify UTC or Local kind.", paramName);
    }
}
