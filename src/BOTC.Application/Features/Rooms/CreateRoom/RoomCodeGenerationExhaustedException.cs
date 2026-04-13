using System.Globalization;

namespace BOTC.Application.Features.Rooms.CreateRoom;

public sealed class RoomCodeGenerationExhaustedException : InvalidOperationException
{
    public RoomCodeGenerationExhaustedException(int maxAttempts)
        : base(string.Format(
            CultureInfo.InvariantCulture,
            "Unable to generate a unique room code after {0} attempts.",
            maxAttempts))
    {
    }
}
