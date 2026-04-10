namespace BOTC.Contracts.Rooms;

public sealed record JoinRoomResponse(
    string RoomCode,
    string PlayerId,
    string Name);

