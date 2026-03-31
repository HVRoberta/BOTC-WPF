using BOTC.Domain.Rooms;

namespace BOTC.Application.Features.Rooms.StartGame;

public sealed record StartGameResult(
    RoomCode RoomCode,
    RoomPlayerId StarterPlayerId,
    bool IsStarted,
    RoomStartGameBlockedReason? BlockedReason,
    RoomStatus RoomStatus);

