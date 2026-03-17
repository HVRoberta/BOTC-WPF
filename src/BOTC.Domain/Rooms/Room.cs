namespace BOTC.Domain.Rooms;

public sealed class Room
{
    private const int MaxHostDisplayNameLength = 50;
    
    private Room(RoomId id, RoomCode code, string hostDisplayName, DateTime createdAtUtc)
    {
        Id = id;
        Code = code;
        HostDisplayName = ValidateHostDisplayName(hostDisplayName);
        CreatedAtUtc = EnsureUtc(createdAtUtc, nameof(createdAtUtc));
        Status = RoomStatus.WaitingForPlayers;
    }

    public RoomId Id { get; }

    public RoomCode Code { get; }

    public string HostDisplayName { get; private set; }

    public RoomStatus Status { get; private set; }

    public DateTime CreatedAtUtc { get; }

    public static Room Create(RoomId id, RoomCode code, string hostDisplayName, DateTime createdAtUtc)
    {
        return new Room(id, code, hostDisplayName, createdAtUtc);
    }

    public void RenameHost(string hostDisplayName)
    {
        HostDisplayName = ValidateHostDisplayName(hostDisplayName);
    }

    private static string ValidateHostDisplayName(string hostDisplayName)
    {
        if (string.IsNullOrWhiteSpace(hostDisplayName))
        {
            throw new ArgumentException("Host display name is required.", nameof(hostDisplayName));
        }

        var normalized = hostDisplayName.Trim();

        if (normalized.Length > MaxHostDisplayNameLength)
        {
            throw new ArgumentException(
                $"Host display name must not exceed {MaxHostDisplayNameLength} characters.",
                nameof(hostDisplayName));
        }

        return normalized;
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

