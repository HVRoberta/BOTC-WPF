using Microsoft.AspNetCore.Mvc;

namespace BOTC.Presentation.Api.Users.Common;

public static class UserProblemResults
{
    public static IResult BadRequest(string title, string detail)
    {
        return Results.BadRequest(new ProblemDetails
        {
            Title = title,
            Detail = detail,
            Status = StatusCodes.Status400BadRequest
        });
    }

    public static IResult Conflict(string title, string detail)
    {
        return Results.Conflict(new ProblemDetails
        {
            Title = title,
            Detail = detail,
            Status = StatusCodes.Status409Conflict
        });
    }

    public static IResult NotFound(string title, string detail)
    {
        return Results.NotFound(new ProblemDetails
        {
            Title = title,
            Detail = detail,
            Status = StatusCodes.Status404NotFound
        });
    }
}