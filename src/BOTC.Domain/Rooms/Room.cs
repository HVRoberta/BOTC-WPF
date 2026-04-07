using System.Collections.ObjectModel;
using BOTC.Domain.Rooms.Events;

namespace BOTC.Domain.Rooms;

public sealed class Room : AggregateRoot
{
    public const int MinPlayersToStartGame = 2;
    private const int MaxPlayers = 20;

    private readonly List<RoomPlayer> players;
    private readonly ReadOnlyCollection<RoomPlayer> readOnlyPlayers;

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
        readOnlyPlayers = this.players.AsReadOnly();
        Status = ValidateStatus(status);
        CreatedAtUtc = EnsureUtc(createdAtUtc, nameof(createdAtUtc));

        EnsurePlayerInvariants(this.players);
    }

    public RoomId Id { get; }

    public RoomCode Code { get; }

    public IReadOnlyCollection<RoomPlayer> Players => readOnlyPlayers;

    public RoomPlayerId HostPlayerId => GetHost().Id;

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

        var room = new Room(
            id,
            code,
            [hostPlayer],
            RoomStatus.WaitingForPlayers,
            createdAtUtc);

        room.RaiseDomainEvent(new RoomCreatedDomainEvent(
            id,
            code,
            hostPlayer.Id,
            hostPlayer.DisplayName,
            room.CreatedAtUtc)); // use the UTC-normalized value stored on the aggregate

        return room;
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
            throw new RoomJoinDisplayNameAlreadyInUseException();
        }

        if (players.Count >= MaxPlayers)
        {
            throw new RoomJoinCapacityReachedException(MaxPlayers);
        }

        var player = RoomPlayer.Create(RoomPlayerId.New(), displayName, RoomPlayerRole.Player, joinedAtUtc);
        players.Add(player);

        RaiseDomainEvent(new PlayerJoinedRoomDomainEvent(
            Id,
            Code,
            player.Id,
            player.DisplayName,
            joinedAtUtc));

        return player;
    }

    public RoomLeaveOutcome LeavePlayer(RoomPlayerId playerId)
    {
        var leavingPlayer = players.SingleOrDefault(player => player.Id == playerId);
        if (leavingPlayer is null)
        {
            throw new RoomLeavePlayerNotFoundException(playerId);
        }

        if (players.Count == 1)
        {
            players.RemoveAt(0);
            
            RaiseDomainEvent(new PlayerLeftRoomDomainEvent(
                Id,
                Code,
                playerId,
                null,
                IsRoomDeleted: true,
                DateTime.UtcNow));

            return new RoomLeaveOutcome(true, null);
        }

        players.RemoveAll(player => player.Id == playerId);

        if (leavingPlayer.Role != RoomPlayerRole.Host)
        {
            EnsurePlayerInvariants(players);
            
            RaiseDomainEvent(new PlayerLeftRoomDomainEvent(
                Id,
                Code,
                playerId,
                null,
                IsRoomDeleted: false,
                DateTime.UtcNow));

            return new RoomLeaveOutcome(false, null);
        }

        var successor = players
            .OrderBy(player => player.JoinedAtUtc)
            .ThenBy(player => player.Id.Value)
            .First();

        var successorIndex = players.FindIndex(player => player.Id == successor.Id);
        players[successorIndex] = successor.WithRole(RoomPlayerRole.Host);

        EnsurePlayerInvariants(players);

        RaiseDomainEvent(new PlayerLeftRoomDomainEvent(
            Id,
            Code,
            playerId,
            successor.Id,
            IsRoomDeleted: false,
            DateTime.UtcNow));

        return new RoomLeaveOutcome(false, successor.Id);
    }

    public void RenameHost(string hostDisplayName)
    {
        var host = GetHost();
        var candidateNormalizedName = RoomPlayer.NormalizeDisplayName(hostDisplayName);

        if (players.Any(p => p.Id != host.Id && string.Equals(p.NormalizedDisplayName, candidateNormalizedName, StringComparison.Ordinal)))
        {
            throw new InvalidOperationException("Display name is already in use for this room.");
        }

        var renamedHost = host.WithDisplayName(hostDisplayName);
        var hostIndex = players.FindIndex(p => p.Id == host.Id);
        players[hostIndex] = renamedHost;
    }

    public void SetPlayerReady(RoomPlayerId playerId, bool isReady)
    {
        EnsureReadinessChangeAllowed();

        var playerIndex = players.FindIndex(player => player.Id == playerId);
        if (playerIndex < 0)
        {
            throw new RoomSetPlayerReadyPlayerNotFoundException(playerId);
        }

        players[playerIndex] = players[playerIndex].WithReadyState(isReady);

        RaiseDomainEvent(new PlayerReadyStateChangedDomainEvent(
            Id,
            Code,
            playerId,
            isReady,
            DateTime.UtcNow));
    }

    public RoomStartGameOutcome StartGame(RoomPlayerId starterPlayerId)
    {
        var starter = players.SingleOrDefault(player => player.Id == starterPlayerId);
        if (starter is null)
        {
            return RoomStartGameOutcome.Blocked(RoomStartGameBlockedReason.StarterPlayerNotFound);
        }

        if (starter.Role != RoomPlayerRole.Host)
        {
            return RoomStartGameOutcome.Blocked(RoomStartGameBlockedReason.StartedByNonHost);
        }

        if (Status != RoomStatus.WaitingForPlayers)
        {
            return RoomStartGameOutcome.Blocked(RoomStartGameBlockedReason.RoomIsNotWaitingForPlayers);
        }

        if (players.Count < MinPlayersToStartGame)
        {
            return RoomStartGameOutcome.Blocked(RoomStartGameBlockedReason.NotEnoughPlayers);
        }

        var hasUnreadyNonHostPlayer = players.Any(player =>
            player.Role == RoomPlayerRole.Player
            && !player.IsReady);

        if (hasUnreadyNonHostPlayer)
        {
            return RoomStartGameOutcome.Blocked(RoomStartGameBlockedReason.NonHostPlayersNotReady);
        }

        Status = RoomStatus.InProgress;

        RaiseDomainEvent(new GameStartedDomainEvent(
            Id,
            Code,
            DateTime.UtcNow));

        return RoomStartGameOutcome.Started();
    }

    private void EnsureJoinAllowed()
    {
        if (Status != RoomStatus.WaitingForPlayers)
        {
            throw new RoomJoinNotAllowedException();
        }
    }

    private void EnsureReadinessChangeAllowed()
    {
        if (Status != RoomStatus.WaitingForPlayers)
        {
            throw new RoomSetPlayerReadyNotAllowedException();
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
