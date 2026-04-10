using System.Collections.ObjectModel;
using BOTC.Domain.Rooms.Players;
using BOTC.Domain.Rooms.Events;
using BOTC.Domain.Rooms.Exceptions;
using BOTC.Domain.Rooms.Outcomes;
using BOTC.Domain.Users;

namespace BOTC.Domain.Rooms;

public sealed class Room : AggregateRoot
{
    public const int MinPlayersToStartGame = 2;
    private const int MaxPlayers = 20;
    private const int MaxNameLength = 30;

    private readonly List<Player> players;
    private readonly ReadOnlyCollection<Player> readOnlyPlayers;

    private Room(
        RoomId id,
        RoomCode code,
        string name,
        IEnumerable<Player> players,
        RoomStatus status,
        DateTime createdAtUtc)
    {
        ArgumentNullException.ThrowIfNull(players);

        Id = id;
        Code = code;
        Name = ValidateName(name);
        this.players = players.ToList();
        readOnlyPlayers = this.players.AsReadOnly();
        Status = ValidateStatus(status);
        CreatedAtUtc = EnsureUtc(createdAtUtc, nameof(createdAtUtc));

        EnsurePlayerInvariants(this.players);
    }

    public RoomId Id { get; }
    public RoomCode Code { get; }
    public string Name { get; private set; }
    public IReadOnlyCollection<Player> Players => readOnlyPlayers;
    public PlayerId HostPlayerId => GetHost().Id;
    public RoomStatus Status { get; private set; }
    public DateTime CreatedAtUtc { get; }

    public static Room Create(
        RoomId id,
        RoomCode code,
        string  name,
        UserId hostUserId,
        DateTime createdAtUtc)
    {
        var utcCreatedAt = EnsureUtc(createdAtUtc, nameof(createdAtUtc));

        var hostPlayer = Player.Create(
            PlayerId.New(),
            hostUserId,
            PlayerRole.Host,
            utcCreatedAt);

        var room = new Room(
            id,
            code,
            name,
            [hostPlayer],
            RoomStatus.WaitingForPlayers,
            utcCreatedAt);

        room.RaiseDomainEvent(new RoomCreatedDomainEvent(
            room.Id,
            room.Code,
            hostPlayer.Id,
            room.CreatedAtUtc));

        return room;
    }

    public static Room Rehydrate(
        RoomId id,
        RoomCode code,
        string name,
        IEnumerable<Player> players,
        RoomStatus status,
        DateTime createdAtUtc)
    {
        return new Room(id, code, name, players, status, createdAtUtc);
    }

    public Player JoinPlayer(
        UserId userId,
        DateTime joinedAtUtc)
    {
        EnsureJoinAllowed();
        EnsureCapacityAvailable();
        EnsureUserNotAlreadyInRoom(userId);

        var utcJoinedAt = EnsureUtc(joinedAtUtc, nameof(joinedAtUtc));

        var player = Player.Create(
            PlayerId.New(),
            userId,
            PlayerRole.Player,
            utcJoinedAt);

        players.Add(player);

        RaiseDomainEvent(new PlayerJoinedRoomDomainEvent(
            Id,
            Code,
            player.Id,
            utcJoinedAt));

        return player;
    }
    
    public void Rename(string name)
    {
        Name = ValidateName(name);
    }
    
    private static string ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Room name is required.", nameof(name));

        var trimmed = name.Trim();

        if (trimmed.Length > MaxNameLength)
            throw new ArgumentException($"Room name must not exceed {MaxNameLength} characters.", nameof(name));

        return trimmed;
    }

    public RoomLeaveOutcome LeavePlayer(PlayerId playerId)
    {
        var leavingPlayer = FindPlayer(playerId)
            ?? throw new RoomLeavePlayerNotFoundException(playerId);

        if (players.Count == 1)
        {
            players.Clear();

            RaiseDomainEvent(new PlayerLeftRoomDomainEvent(
                Id,
                Code,
                playerId,
                null, 
                true,
                DateTime.UtcNow));

            return new RoomLeaveOutcome(true, null);
        }

        players.RemoveAll(player => player.Id == playerId);

        if (leavingPlayer.Role != PlayerRole.Host)
        {
            EnsurePlayerInvariants(players);

            RaiseDomainEvent(new PlayerLeftRoomDomainEvent(
                Id,
                Code,
                playerId,
                null,
                false,
                DateTime.UtcNow));

            return new RoomLeaveOutcome(false, null);
        }

        var successor = SelectNextHost();
        ReplacePlayer(successor.ChangeRole(PlayerRole.Host));

        EnsurePlayerInvariants(players);

        RaiseDomainEvent(new PlayerLeftRoomDomainEvent(
            Id,
            Code,
            playerId,
            successor.Id,
            false,
            DateTime.UtcNow));

        return new RoomLeaveOutcome(false, successor.Id);
    }

    public void SetPlayerReady(PlayerId playerId, bool isReady)
    {
        EnsureReadinessChangeAllowed();

        var player = FindPlayer(playerId)
            ?? throw new RoomSetPlayerReadyPlayerNotFoundException(playerId);

        ReplacePlayer(player.SetReady(isReady));

        RaiseDomainEvent(new PlayerReadyStateChangedDomainEvent(
            Id,
            Code,
            playerId,
            isReady,
            DateTime.UtcNow));
    }

    public RoomStartGameOutcome StartGame(PlayerId starterPlayerId)
    {
        var starter = FindPlayer(starterPlayerId);
        if (starter is null)
        {
            return RoomStartGameOutcome.Blocked(RoomStartGameBlockedReason.StarterPlayerNotFound);
        }

        if (starter.Role != PlayerRole.Host)
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
            player.Role == PlayerRole.Player &&
            !player.IsReady);

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

    private void EnsureCapacityAvailable()
    {
        if (players.Count >= MaxPlayers)
        {
            throw new RoomJoinCapacityReachedException(MaxPlayers);
        }
    }

    private void EnsureUserNotAlreadyInRoom(UserId userId)
    {
        var alreadyJoined = players.Any(player => player.UserId == userId);

        if (alreadyJoined)
        {
            throw new RoomUserAlreadyJoinedException(userId);
        }
    }

    private Player GetHost()
    {
        return players.Single(player => player.Role == PlayerRole.Host);
    }

    private Player? FindPlayer(PlayerId playerId)
    {
        return players.SingleOrDefault(player => player.Id == playerId);
    }

    private Player SelectNextHost()
    {
        return players
            .OrderBy(player => player.JoinedAtUtc)
            .ThenBy(player => player.Id.Value)
            .First();
    }

    private void ReplacePlayer(Player updatedPlayer)
    {
        var index = players.FindIndex(player => player.Id == updatedPlayer.Id);
        if (index < 0)
        {
            throw new InvalidOperationException("Player to replace was not found in the room.");
        }

        players[index] = updatedPlayer;
    }

    private static void EnsurePlayerInvariants(IReadOnlyCollection<Player> players)
    {
        if (players.Count == 0)
        {
            throw new ArgumentException("Room must contain at least one player.", nameof(players));
        }

        if (players.Count > MaxPlayers)
        {
            throw new ArgumentException($"Room cannot exceed {MaxPlayers} players.", nameof(players));
        }

        var hostCount = players.Count(player => player.Role == PlayerRole.Host);
        if (hostCount != 1)
        {
            throw new ArgumentException("Room must have exactly one host player.", nameof(players));
        }

        var duplicateUsers = players
            .GroupBy(player => player.UserId)
            .Any(group => group.Count() > 1);

        if (duplicateUsers)
        {
            throw new ArgumentException("A user cannot join the same room more than once.", nameof(players));
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
        return value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => throw new ArgumentException("Date must specify UTC or Local kind.", paramName)
        };
    }
}