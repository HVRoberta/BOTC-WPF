using BOTC.Application.Features.Rooms.CreateRoom;
using BOTC.Contracts.Rooms;
using BOTC.Presentation.Api.Rooms.Common;

namespace BOTC.Presentation.Api.Rooms.CreateRoom;

public static class CreateRoomEndpoint
{
    public static RouteGroupBuilder MapCreateRoomEndpoint(this RouteGroupBuilder group)
    {
        ArgumentNullException.ThrowIfNull(group);

        group.MapPost("/", HandleAsync)
            .WithName("CreateRoom")
            .Produces<CreateRoomResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        return group;
    }

    private static async Task<IResult> HandleAsync(
        CreateRoomRequest request,
        CreateRoomHandler handler,
        CancellationToken cancellationToken)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.HostName))
        {
            return RoomProblemResults.BadRequest(
                "Invalid create room request.",
                "HostName is required.");
        }

        try
        {
            var command = CreateRoomMappings.ToCommand(request);
            var result = await handler.HandleAsync(command, cancellationToken);
            var response = CreateRoomMappings.ToResponse(result);

            return Results.Created($"/api/rooms/{response.RoomCode}", response);
        }
        catch (RoomCodeGenerationExhaustedException exception)
        {
            return RoomProblemResults.ServiceUnavailable(
                "Room code generation temporarily unavailable.",
                exception.Message);
        }
        catch (ArgumentException exception)
        {
            return RoomProblemResults.BadRequest(
                "Invalid create room request.",
                exception.Message);
        }
    }
}