namespace BOTC.Contracts.Users;

public sealed record CreateUserResponse(
    string UserId,
    string Username,
    string NickName);