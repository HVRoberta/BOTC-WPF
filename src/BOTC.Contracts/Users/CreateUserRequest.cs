namespace BOTC.Contracts.Users;

public sealed record CreateUserRequest(
    string Username,
    string NickName);