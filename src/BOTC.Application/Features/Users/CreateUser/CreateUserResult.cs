using BOTC.Domain.Users;

namespace BOTC.Application.Features.Users.CreateUser;

public sealed record CreateUserResult(
    UserId UserId,
    string Username,
    string NickName); 