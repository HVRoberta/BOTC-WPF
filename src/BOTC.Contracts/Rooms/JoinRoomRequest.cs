using BOTC.Domain.Users;

namespace BOTC.Contracts.Rooms;

public sealed record JoinRoomRequest(UserId UserId, string Name);

