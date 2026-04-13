namespace BOTC.Application.Features.Users.CreateUser;

public sealed class UserAlreadyExistsException : Exception
{
    public string Username { get; }

    public UserAlreadyExistsException(string username)
        : base($"A user with username '{username}' already exists.")
    {
        Username = username ?? throw new ArgumentNullException(nameof(username));
    }
}