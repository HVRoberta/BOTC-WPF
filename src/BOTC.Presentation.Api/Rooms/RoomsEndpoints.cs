using BOTC.Application.Features.Rooms.CreateRoom;
using BOTC.Contracts.Rooms;
using Microsoft.AspNetCore.Mvc;

namespace BOTC.Presentation.Api.Rooms;

public static class RoomsEndpoints
{
    public static IEndpointRouteBuilder MapRoomsEndpoints(this IEndpointRouteBuilder endpoints)
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
        CancellationToken cancellationToken)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.HostDisplayName))
        {
            return Results.BadRequest(new ProblemDetails
            {
                Title = "Invalid create room request.",
                Detail = "HostDisplayName is required.",
                Status = StatusCodes.Status400BadRequest
            });
        }

        try
        {
            var command = CreateRoomMappings.ToCommand(request);
            var result = await handler.HandleAsync(command, cancellationToken);
            var response = CreateRoomMappings.ToResponse(result);

            return Results.Created($"/api/rooms/{response.RoomId}", response);
        }
        catch (RoomCodeGenerationExhaustedException exception)
        {
            return Results.Problem(
                title: "Room code generation temporarily unavailable.",
                detail: exception.Message,
                statusCode: StatusCodes.Status503ServiceUnavailable);
        }
        catch (ArgumentException exception)
        {
            return Results.BadRequest(new ProblemDetails
            {
                Title = "Invalid create room request.",
                Detail = exception.Message,
                Status = StatusCodes.Status400BadRequest
            });
        }
    }
}

