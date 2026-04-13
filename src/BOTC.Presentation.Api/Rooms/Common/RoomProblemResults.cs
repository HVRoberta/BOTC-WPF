using Microsoft.AspNetCore.Mvc;

namespace BOTC.Presentation.Api.Rooms.Common;

internal static class RoomProblemResults
{
    public static IResult BadRequest(string title, string detail)
    {
        return Results.BadRequest(Create(title, detail, StatusCodes.Status400BadRequest));
    }

    public static IResult NotFound(string title, string detail)
    {
        return Results.NotFound(Create(title, detail, StatusCodes.Status404NotFound));
    }

    public static IResult Conflict(string title, string detail)
    {
        return Results.Conflict(Create(title, detail, StatusCodes.Status409Conflict));
    }

    public static IResult ServiceUnavailable(string title, string detail)
    {
        return Results.Problem(title: title, detail: detail, statusCode: StatusCodes.Status503ServiceUnavailable);
    }

    private static ProblemDetails Create(string title, string detail, int status)
    {
        return new ProblemDetails
        {
            Title = title,
            Detail = detail,
            Status = status
        };
    }
}

