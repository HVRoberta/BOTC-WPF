namespace BOTC.Application.Features.Users.CreateUser;

public sealed record CreateUserCommand(
    string Username,
    string NickName);