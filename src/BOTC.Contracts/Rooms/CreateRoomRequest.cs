using BOTC.Domain.Users;

namespace BOTC.Contracts.Rooms;

public sealed record CreateRoomRequest(UserId HostUserId, string HostName);

