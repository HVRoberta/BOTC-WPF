namespace BOTC.Application.Features.Rooms.CreateRoom;

public sealed class RoomCodeGenerationExhaustedException : InvalidOperationException
{
    public RoomCodeGenerationExhaustedException(int maxAttempts)
        : base($"Unable to generate a unique room code after {maxAttempts} attempts.")
    {
    }
}

