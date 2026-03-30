using BOTC.Domain.Rooms;
namespace BOTC.Application.Features.Rooms.LeaveRoom;
public sealed class RoomLeaveSaveRoomMissingException : Exception
{
    public RoomLeaveSaveRoomMissingException(RoomId roomId)
        : base($"Room with id '{roomId.Value}' was not found while saving leave changes.")
    {
        RoomId = roomId;
    }
    public RoomId RoomId { get; }
}