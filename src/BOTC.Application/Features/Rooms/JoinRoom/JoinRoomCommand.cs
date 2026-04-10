using BOTC.Domain.Users;

namespace BOTC.Application.Features.Rooms.JoinRoom;

public sealed record JoinRoomCommand(string RoomCode, UserId UserId, string Name);

