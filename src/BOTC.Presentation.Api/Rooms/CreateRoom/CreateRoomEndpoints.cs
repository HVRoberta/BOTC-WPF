using BOTC.Application.Features.Rooms.CreateRoom;
using BOTC.Contracts.Rooms;
using BOTC.Presentation.Api.Rooms.Realtime;

namespace BOTC.Presentation.Api.Rooms;

public static class CreateRoomEndpoints
{
    public static IEndpointRouteBuilder MapCreateRoomEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/api/rooms", CreateRoomAsync)
            .WithName("CreateRoom")
            .WithTags("Rooms")
            .Produces<CreateRoomResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        return endpoints;
    }

    private static async Task<IResult> CreateRoomAsync(
        CreateRoomRequest request,
        CreateRoomHandler handler,
        IRoomLobbyNotifier roomLobbyNotifier,
        CancellationToken cancellationToken)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.HostDisplayName))
        {
            return RoomProblemResults.BadRequest("Invalid create room request.", "HostDisplayName is required.");
        }

        try
        {
            var command = CreateRoomMappings.ToCommand(request);
            var result = await handler.HandleAsync(command, cancellationToken);
            var response = CreateRoomMappings.ToResponse(result);

            await roomLobbyNotifier.NotifyLobbyUpdatedAsync(response.RoomCode, CancellationToken.None);

            return Results.Created($"/api/rooms/{response.RoomId}", response);
        }
        catch (RoomCodeGenerationExhaustedException exception)
        {
            return RoomProblemResults.ServiceUnavailable(
                "Room code generation temporarily unavailable.",
                exception.Message);
        }
        catch (ArgumentException exception)
        {
            return RoomProblemResults.BadRequest("Invalid create room request.", exception.Message);
        }
    }
}
